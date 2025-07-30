namespace HRChatBot.Models.Requests
{
    public class ApproveLeaveRequest
    {
        public int EmpId { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

    }
}
