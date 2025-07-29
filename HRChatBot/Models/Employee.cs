using System.ComponentModel.DataAnnotations;

namespace HRChatBot.Models
{
    public class Employee
    {
        [Key]
        public int EmpId { get; set; }
        public string EmpName { get; set; }
        public string Gender { get; set; }
        public string Department { get; set; }
        public DateTime JoinDate { get; set; }

        public List<Leave> Leaves { get; set; } = new();
    }
}
