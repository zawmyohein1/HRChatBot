using HRChatBot.Controllers;
using HRChatBot.Models.Responses;

namespace HRChatBot.Services
{
    public interface IActionRouter
    {
        ActionEndpoint GetEndpoint(string action);
    }
}