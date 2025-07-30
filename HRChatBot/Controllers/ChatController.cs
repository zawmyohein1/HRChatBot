using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HRChatBot.Services;
using HRChatBot.Models.Requests;
using HRChatBot.Utilities;

namespace HRChatBot.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatController> _logger;
        private readonly IPromptManager _promptManager;
        private readonly IActionRouter _actionRouter;

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<ChatController> logger, IPromptManager promptManager, IActionRouter actionRouter)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
            _promptManager = promptManager;
            _actionRouter = actionRouter;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest("Message is required.");

            var apiKey = _config["OpenAI:ApiKey"];
            var endpoint = _config["OpenAI:EndPoint"];
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            string actionType = null;

            try
            {
                var classifierPrompt = _promptManager.GetPrompt("CommandClassifier", request.Message);
                var classifyPayload = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[] { new { role = "user", content = classifierPrompt } },
                    temperature = 0.2
                };

                var classifyResponse = await http.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(classifyPayload), Encoding.UTF8, "application/json"));
                var classifyJson = await classifyResponse.Content.ReadAsStringAsync();
                var replyContent = JsonDocument.Parse(classifyJson).RootElement
                    .GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                var parsed = JsonSerializer.Deserialize<ChatCommand>(replyContent);
                actionType = parsed?.Action;

                LogUtil.WriteLog($"[Classifier] UserInput: {request.Message}, Parsed Action: {actionType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Intent classification failed.");
                LogUtil.WriteLog($"[Classifier Error] {ex.Message}");
                return BuildBotResponse("Sorry, I couldn't understand your request.");
            }

            if (string.IsNullOrWhiteSpace(actionType) || actionType == "Unknown")
                return BuildBotResponse("Sorry, I couldn't understand your request.");

            ChatCommand parsedCommand;

            try
            {
                var finalPrompt = _promptManager.GetPrompt(actionType, request.Message);
                var finalPayload = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[] { new { role = "user", content = finalPrompt } },
                    temperature = 0.2
                };

                var finalResponse = await http.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(finalPayload), Encoding.UTF8, "application/json"));
                var resultJson = await finalResponse.Content.ReadAsStringAsync();

                var reply = JsonDocument.Parse(resultJson).RootElement
                    .GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                parsedCommand = JsonSerializer.Deserialize<ChatCommand>(reply);
                LogUtil.WriteLog($"[Parsed Command] JSON: {reply}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract detailed parameters.");
                LogUtil.WriteLog($"[Command Parse Error] {ex.Message}");
                return BuildBotResponse("Sorry, I couldn’t complete your request.");
            }

            try
            {
                var routeInfo = _actionRouter.GetEndpoint(parsedCommand.Action);
                var resolvedUrl = routeInfo.Endpoint
                    .Replace("{empId}", parsedCommand.EmpId.ToString())
                    .Replace("{leaveId}", parsedCommand.LeaveId?.ToString() ?? "");

                HttpResponseMessage response;
                string payload = null;

                switch (routeInfo.Method.ToUpper())
                {
                    case "GET":
                        response = await http.GetAsync(resolvedUrl);
                        break;

                    case "POST":
                    case "PUT":
                        payload = JsonSerializer.Serialize(new
                        {
                            empId = parsedCommand.EmpId,
                            leaveType = parsedCommand.LeaveType,
                            startDate = parsedCommand.StartDate,
                            endDate = parsedCommand.EndDate
                        });

                        var content = new StringContent(payload, Encoding.UTF8, "application/json");

                        response = routeInfo.Method.ToUpper() == "POST"
                            ? await http.PostAsync(resolvedUrl, content)
                            : await http.PutAsync(resolvedUrl, content);
                        break;

                    default:
                        return BuildBotResponse("Unsupported method.");
                }

                var result = await response.Content.ReadAsStringAsync();
                LogUtil.WriteLog($"[API CALL] Method: {routeInfo.Method}, URL: {resolvedUrl}, Payload: {payload}, Status: {response.StatusCode}");
                return BuildBotResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call routed API.");
                LogUtil.WriteLog($"[API Call Error] {ex.Message}");
                return BuildBotResponse("Sorry, something went wrong while calling internal services.");
            }
        }      

        private IActionResult BuildBotResponse(string message)
        {
            string displayMessage = message;

            try
            {
                using var jsonDoc = JsonDocument.Parse(message);
                if (jsonDoc.RootElement.TryGetProperty("message", out JsonElement msgElement))
                {
                    displayMessage = msgElement.GetString();
                }
            }
            catch
            {
                // message is plain text, not JSON — leave as is
            }

            return Ok(new
            {
                choices = new[]
                {
            new { message = new { content = displayMessage } }
        }
            });
        }

    }
}
