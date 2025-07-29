using HRChatBot.Controllers;
using HRChatBot.Models.Responses;
using System.Text.Json;

namespace HRChatBot.Services
{
    public class ActionRouter : IActionRouter
    {
        private readonly Dictionary<string, ActionEndpoint> _routes;

        public ActionRouter(IWebHostEnvironment env)
        {
            var path = Path.Combine(env.ContentRootPath, "Config/actions-config.json");
            var json = File.ReadAllText(path);
            _routes = JsonSerializer.Deserialize<Dictionary<string, ActionEndpoint>>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

        }

        public ActionEndpoint GetEndpoint(string action)
        {
            if (_routes.TryGetValue(action, out var endpoint))
                return endpoint;

            throw new KeyNotFoundException($"Action '{action}' not found in config.");
        }
    }
}
