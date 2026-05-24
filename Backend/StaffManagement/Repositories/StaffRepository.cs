using Microsoft.EntityFrameworkCore;
using StaffManagement.Data;
using StaffManagement.Interface;
using StaffManagement.Model.Entities;

namespace StaffManagement.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly AppDbContext _context;

    public StaffRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Staff>> GetAllAsync()
    {
        return await _context.Staffs
            .OrderBy(staff => staff.StaffId)
            .ToListAsync();
    }

    public async Task<Staff?> GetByIdAsync(string staffId)
    {
        return await _context.Staffs.FindAsync(staffId);
    }

    public async Task<Staff?> GetLastStaffAsync()
    {
        return await _context.Staffs
            .OrderByDescending(staff => staff.StaffId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Staff>> SearchAsync(
        string? staffId,
        int? gender,
        DateOnly? birthdayFrom,
        DateOnly? birthdayTo)
    {
        var query = _context.Staffs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(staffId))
        {
            query = query.Where(staff => staff.StaffId.Contains(staffId));
        }

        if (gender.HasValue)
        {
            query = query.Where(staff => staff.Gender == gender.Value);
        }

        if (birthdayFrom.HasValue)
        {
            query = query.Where(staff => staff.Birthday >= birthdayFrom.Value);
        }

        if (birthdayTo.HasValue)
        {
            query = query.Where(staff => staff.Birthday <= birthdayTo.Value);
        }

        return await query
            .OrderBy(staff => staff.StaffId)
            .ToListAsync();
    }

    public async Task AddAsync(Staff staff)
    {
        _context.Staffs.Add(staff);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Staff staff)
    {
        _context.Staffs.Update(staff);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Staff staff)
    {
        _context.Staffs.Remove(staff);
        await _context.SaveChangesAsync();
    }
}
