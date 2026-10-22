using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruongThaiBao.ChamCong.API.Data;
using TruongThaiBao.ChamCong.API.Models;

namespace TruongThaiBao.ChamCong.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController(AppDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = db.Attendances.Include(a => a.User).AsQueryable();
        if (!IsAdmin) query = query.Where(a => a.UserId == CurrentUserId);
        var rows = await query.OrderByDescending(a => a.CheckInUtc)
            .Select(a => new { a.Id, a.UserId, Username = a.User!.Username, a.CheckInUtc, a.CheckOutUtc, a.Note })
            .ToListAsync();
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.Attendances.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
        if (item is null) return NotFound();
        if (!IsAdmin && item.UserId != CurrentUserId) return Forbid();
        return Ok(new { item.Id, item.UserId, Username = item.User!.Username, item.CheckInUtc, item.CheckOutUtc, item.Note });
    }

    [HttpPost("check-in")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> CheckIn()
    {
        var hasOpen = await db.Attendances.AnyAsync(a => a.UserId == CurrentUserId && a.CheckOutUtc == null);
        if (hasOpen) return Conflict("Bạn đã chấm công vào và chưa chấm công ra.");
        var item = new Attendance { UserId = CurrentUserId, CheckInUtc = DateTime.UtcNow };
        db.Attendances.Add(item);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPost("check-out/{id:int}")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> CheckOut(int id, CheckOutRequest request)
    {
        var item = await db.Attendances.FindAsync(id);
        if (item is null) return NotFound();
        if (!IsAdmin && item.UserId != CurrentUserId) return Forbid();
        if (item.CheckOutUtc is not null) return Conflict("Bản ghi đã chấm công ra.");
        item.CheckOutUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note)) item.Note = request.Note;
        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UpdateAttendanceRequest request)
    {
        var item = await db.Attendances.FindAsync(id);
        if (item is null) return NotFound();
        item.CheckInUtc = request.CheckInUtc;
        item.CheckOutUtc = request.CheckOutUtc;
        item.Note = request.Note;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Attendances.FindAsync(id);
        if (item is null) return NotFound();
        db.Attendances.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
