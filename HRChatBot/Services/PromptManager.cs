using System.Text.Json;

namespace HRChatBot.Services
{
    public class PromptManager : IPromptManager
    {
        private readonly Dictionary<string, string> _promptTemplates;

        public PromptManager(IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Prompts/hr-prompts.json");
            var json = File.ReadAllText(filePath);
            _promptTemplates = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }

        public string GetPrompt(string action, string userMessage)
        {
            if (!_promptTemplates.TryGetValue(action, out var template))
                throw new KeyNotFoundException($"Prompt template for action '{action}' not found.");

            var safeMessage = userMessage.Replace("\"", "\\\"");
            return template.Replace("{userMessage}", safeMessage);
        }
    }
}
