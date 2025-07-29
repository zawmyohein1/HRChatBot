using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRChatBot.Data;
using HRChatBot.Models;
using HRChatBot.Models.Requests;

namespace HRChatBot.Controllers.Api
{
    [Route("api/leave/employee")]
    [ApiController]
    public class LeaveController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeaveController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{empId}")]
        public async Task<IActionResult> GetLeaveByEmpId(int empId)
        {
            var leaves = await _context.Leaves
                .Where(l => l.EmpId == empId)
                .OrderByDescending(l => l.StartDate)
                .Select(l => new
                {
                    id = l.LeaveId,
                    type = l.LeaveType,
                    startDate = l.StartDate.ToString("MMM dd"),
                    endDate = l.EndDate.ToString("MMM dd"),
                    status = l.Status
                })
                .ToListAsync();

            if (!leaves.Any())
                return Ok(new { message = "No leave records found." });

            return Ok(leaves); // Return as JSON array for table rendering
        }


        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLeave([FromBody] LeaveRequest request)
        {
            if (request == null || request.EmpId <= 0 || string.IsNullOrWhiteSpace(request.LeaveType))
                return BadRequest("Invalid leave request.");

            var validLeaveTypes = new[] { "Annual", "Sick", "Unpaid", "Emergency", "Maternity", "Compassionate" };

            if (!validLeaveTypes.Contains(request.LeaveType, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Invalid leave type.");

            if (request.FromDate > request.ToDate)
                return BadRequest("Start date cannot be after end date.");

            // Check for overlapping leave
            bool hasOverlap = await _context.Leaves.AnyAsync(l =>
                l.EmpId == request.EmpId &&
                l.Status != "Cancelled" &&
                l.Status != "Withdrawn" &&
                (
                    (request.FromDate >= l.StartDate && request.FromDate <= l.EndDate) || // overlaps start
                    (request.ToDate >= l.StartDate && request.ToDate <= l.EndDate) ||     // overlaps end
                    (request.FromDate <= l.StartDate && request.ToDate >= l.EndDate)      // fully overlaps
                )
            );

            if (hasOverlap)
                return BadRequest("You already have an existing leave during this period.");

            var leave = new Leave
            {
                EmpId = request.EmpId,
                LeaveType = request.LeaveType,
                StartDate = request.FromDate,
                EndDate = request.ToDate,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();

            return Ok($"Leave applied for {request.LeaveType} from {request.FromDate:MMM dd} to {request.ToDate:MMM dd}.");
        }


        [HttpPut("approve/{leaveId}")]
        public async Task<IActionResult> ApproveLeave(int leaveId)
        {
            var leave = await _context.Leaves.FindAsync(leaveId);
            if (leave == null)
                return NotFound("Leave record not found.");

            leave.Status = "Approved";
            await _context.SaveChangesAsync();
            return Ok("Leave has been approved.");
        }

        [HttpPut("cancel/{leaveId}")]
        public async Task<IActionResult> CancelLeave(int leaveId)
        {
            var leave = await _context.Leaves.FindAsync(leaveId);
            if (leave == null)
                return NotFound("Leave record not found.");

            if (leave.Status != "Approved")
                return BadRequest("Only approved leaves can be canceled.");

            leave.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return Ok("Leave has been cancelled.");
        }

        [HttpPut("withdraw/{leaveId}")]
        public async Task<IActionResult> WithdrawLeave(int leaveId)
        {
            var leave = await _context.Leaves.FindAsync(leaveId);
            if (leave == null)
                return NotFound("Leave record not found.");

            if (leave.Status != "Pending")
                return BadRequest("Only pending leaves can be withdrawn.");

            leave.Status = "Withdrawn";
            await _context.SaveChangesAsync();
            return Ok("Leave has been withdrawn.");
        }
    }
}
