using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using HRChatBot.Services;
using HRChatBot.Models.Requests;

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

        public IActionResult Index()
        {
            return View(); // This will look for Views/Chat/Index.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest("Message is required.");

            var apiKey = _config["OpenAI:ApiKey"];
            var endpoint = _config["OpenAI:EndPoint"];
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Step 1: Classify intent
            var classifierPrompt = _promptManager.GetPrompt("CommandClassifier", request.Message);
            var classifyPayload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = classifierPrompt } },
                temperature = 0.2
            };

            string actionType = null;
            try
            {
                var classifyResponse = await http.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(classifyPayload), Encoding.UTF8, "application/json"));
                var classifyJson = await classifyResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(classifyJson);
                var replyContent = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                var parsed = JsonSerializer.Deserialize<ChatCommand>(replyContent);
                actionType = parsed?.Action;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to classify user message.");
                return BuildBotResponse("Sorry, I couldn't understand your request.");
            }

            if (string.IsNullOrWhiteSpace(actionType) || actionType == "Unknown")
                return BuildBotResponse("Sorry, I couldn't understand your request.");

            // Step 2: Get detailed prompt & extract full parameters
            var finalPrompt = _promptManager.GetPrompt(actionType, request.Message);
            var finalPayload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = finalPrompt } },
                temperature = 0.2
            };

            ChatCommand parsedCommand;
            try
            {
                var finalResponse = await http.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(finalPayload), Encoding.UTF8, "application/json"));
                var resultJson = await finalResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resultJson);
                var reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                parsedCommand = JsonSerializer.Deserialize<ChatCommand>(reply);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process final GPT request.");
                return BuildBotResponse("Sorry, I couldn’t complete your request.");
            }

            // Step 3: Dynamic API Routing
            try
            {
                var routeInfo = _actionRouter.GetEndpoint(parsedCommand.Action);
                var resolvedUrl = routeInfo.Endpoint
                    .Replace("{empId}", parsedCommand.EmpId.ToString())
                    .Replace("{leaveId}", parsedCommand.LeaveId?.ToString() ?? "");

                HttpResponseMessage response;
                switch (routeInfo.Method.ToUpper())
                {
                    case "GET":
                        response = await http.GetAsync(resolvedUrl);
                        break;
                    case "POST":
                        var postPayload = JsonSerializer.Serialize(new
                        {
                            empId = parsedCommand.EmpId,
                            leaveType = parsedCommand.LeaveType,
                            fromDate = parsedCommand.FromDate,
                            toDate = parsedCommand.ToDate
                        });
                        response = await http.PostAsync(resolvedUrl, new StringContent(postPayload, Encoding.UTF8, "application/json"));
                        break;
                    case "PUT":
                        response = await http.PutAsync(resolvedUrl, null);
                        break;
                    default:
                        return BuildBotResponse("Unsupported method.");
                }

                var result = await response.Content.ReadAsStringAsync();
                return BuildBotResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call routed API.");
                return BuildBotResponse("Sorry, something went wrong while calling internal services.");
            }
        }

        private IActionResult BuildBotResponse(string message)
        {
            return Ok(new
            {
                choices = new[]
                {
                    new { message = new { content = message } }
                }
            });
        }
    }  
}
