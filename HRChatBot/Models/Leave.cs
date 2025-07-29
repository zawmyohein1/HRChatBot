using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRChatBot.Models
{
    public class Leave
    {
        [Key]
        public int LeaveId { get; set; }

        public int EmpId { get; set; }

        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; } 

        [ForeignKey("EmpId")]
        public Employee Employee { get; set; }
    }
}