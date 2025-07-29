namespace HRChatBot.Models.Requests
{
    public class LeaveRequest
    {
        public int EmpId { get; set; }
        public string LeaveType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
