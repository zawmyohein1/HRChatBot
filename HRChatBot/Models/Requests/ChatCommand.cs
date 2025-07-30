using System.Text.Json.Serialization;

public class ChatCommand
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("empId")]
    public int EmpId { get; set; }

    [JsonPropertyName("leaveId")]
    public int? LeaveId { get; set; }

    [JsonPropertyName("leaveType")]
    public string LeaveType { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }
}
