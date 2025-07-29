using System.Text.Json.Serialization;

namespace HRChatBot.Models.Requests
{
    public class ChatCommand
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("empId")]
        public int EmpId { get; set; }

        [JsonPropertyName("leaveType")]
        public string LeaveType { get; set; }

        [JsonPropertyName("fromDate")]
        public DateTime? FromDate { get; set; }

        [JsonPropertyName("toDate")]
        public DateTime? ToDate { get; set; }

        [JsonPropertyName("leaveId")]
        public int? LeaveId { get; set; }
    }
}
