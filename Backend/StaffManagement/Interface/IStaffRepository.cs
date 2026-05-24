using StaffManagement.Model.Entities;

namespace StaffManagement.Interface;

public interface IStaffRepository
{
    Task<List<Staff>> GetAllAsync();
    Task<Staff?> GetByIdAsync(string staffId);
    Task<Staff?> GetLastStaffAsync();
    Task<List<Staff>> SearchAsync(string? staffId, int? gender, DateOnly? birthdayFrom, DateOnly? birthdayTo);
    Task AddAsync(Staff staff);
    Task UpdateAsync(Staff staff);
    Task DeleteAsync(Staff staff);
}
