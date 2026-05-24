using StaffManagement.Common;
using StaffManagement.Model.Entities;
using StaffManagement.Model.Request;

namespace StaffManagement.Interface;

public interface IStaffService
{
    Task<List<Staff>> GetAllAsync();
    Task<Staff?> GetByIdAsync(string staffId);
    Task<ServiceResult<List<Staff>>> SearchAsync(StaffSearchRequest request);
    Task<ServiceResult<byte[]>> ExportExcelAsync(StaffSearchRequest request);
    Task<ServiceResult<byte[]>> ExportPdfAsync(StaffSearchRequest request);
    Task<ServiceResult<Staff>> CreateAsync(CreateStaffRequest request);
    Task<ServiceResult> UpdateAsync(string staffId, UpdateStaffRequest staff);
    Task<ServiceResult> DeleteAsync(string staffId);
}
