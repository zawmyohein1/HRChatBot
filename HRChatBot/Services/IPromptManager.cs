namespace HRChatBot.Services
{
    public interface IPromptManager
    {
        string GetPrompt(string action, string userMessage);
    }
}
