using StaffManagement.Interface;
using StaffManagement.Model.Entities;
using StaffManagement.Model.Request;
using StaffManagement.Service;

namespace StaffManagement.Tests;

[TestFixture]
public class StaffServiceTests
{
    private FakeStaffRepository _repository = null!;
    private StaffService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new FakeStaffRepository();
        _service = new StaffService(_repository);
    }

    [Test]
    public async Task CreateAsync_WhenThereIsNoStaff_GeneratesFirstStaffId()
    {
        var request = new CreateStaffRequest
        {
            FullName = "Sok Dara",
            Birthday = new DateOnly(2000, 1, 15),
            Gender = 1
        };

        var result = await _service.CreateAsync(request);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.StaffId, Is.EqualTo("STF00001"));
        Assert.That(result.Data.FullName, Is.EqualTo("Sok Dara"));
        Assert.That(_repository.Staffs, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_WhenStaffAlreadyExists_GeneratesNextStaffId()
    {
        _repository.Staffs.Add(new Staff
        {
            StaffId = "STF00001",
            FullName = "First Staff",
            Birthday = new DateOnly(1999, 1, 1),
            Gender = 1
        });

        var request = new CreateStaffRequest
        {
            FullName = "Second Staff",
            Birthday = new DateOnly(2001, 5, 20),
            Gender = 2
        };

        var result = await _service.CreateAsync(request);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!.StaffId, Is.EqualTo("STF00002"));
        Assert.That(_repository.Staffs, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CreateAsync_WhenLegacyStaffIdExists_IgnoresInvalidStaffIdFormat()
    {
        _repository.Staffs.AddRange(
        [
            new Staff
            {
                StaffId = "STF00001",
                FullName = "Valid Staff",
                Birthday = new DateOnly(1999, 1, 1),
                Gender = 1
            },
            new Staff
            {
                StaffId = "stringst",
                FullName = "Legacy Staff",
                Birthday = new DateOnly(1999, 1, 1),
                Gender = 1
            }
        ]);

        var request = new CreateStaffRequest
        {
            FullName = "New Staff",
            Birthday = new DateOnly(2001, 5, 20),
            Gender = 2
        };

        var result = await _service.CreateAsync(request);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!.StaffId, Is.EqualTo("STF00002"));
    }

    [Test]
    public async Task UpdateAsync_WhenStaffDoesNotExist_ReturnsFailedResult()
    {
        var request = new UpdateStaffRequest
        {
            FullName = "Missing Staff",
            Birthday = new DateOnly(2000, 1, 1),
            Gender = 1
        };

        var result = await _service.UpdateAsync("STF99999", request);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("Staff not found."));
    }

    [Test]
    public async Task UpdateAsync_WhenStaffExists_UpdatesStaff()
    {
        _repository.Staffs.Add(new Staff
        {
            StaffId = "STF00001",
            FullName = "Old Name",
            Birthday = new DateOnly(1995, 3, 10),
            Gender = 1
        });

        var request = new UpdateStaffRequest
        {
            FullName = "New Name",
            Birthday = new DateOnly(1996, 4, 11),
            Gender = 2
        };

        var result = await _service.UpdateAsync("STF00001", request);
        var staff = await _repository.GetByIdAsync("STF00001");

        Assert.That(result.Success, Is.True);
        Assert.That(staff!.FullName, Is.EqualTo("New Name"));
        Assert.That(staff.Birthday, Is.EqualTo(new DateOnly(1996, 4, 11)));
        Assert.That(staff.Gender, Is.EqualTo(2));
    }

    [Test]
    public async Task DeleteAsync_WhenStaffExists_RemovesStaff()
    {
        _repository.Staffs.Add(new Staff
        {
            StaffId = "STF00001",
            FullName = "Delete Me",
            Birthday = new DateOnly(1998, 8, 8),
            Gender = 1
        });

        var result = await _service.DeleteAsync("STF00001");

        Assert.That(result.Success, Is.True);
        Assert.That(_repository.Staffs, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_WhenCriteriaMatch_ReturnsFilteredStaffs()
    {
        _repository.Staffs.AddRange(
        [
            new Staff
            {
                StaffId = "STF00001",
                FullName = "Male Staff",
                Birthday = new DateOnly(1995, 1, 1),
                Gender = 1
            },
            new Staff
            {
                StaffId = "STF00002",
                FullName = "Female Staff",
                Birthday = new DateOnly(2000, 1, 1),
                Gender = 2
            },
            new Staff
            {
                StaffId = "STF00003",
                FullName = "Another Female Staff",
                Birthday = new DateOnly(2005, 1, 1),
                Gender = 2
            }
        ]);

        var request = new StaffSearchRequest
        {
            Gender = 2,
            BirthdayFrom = new DateOnly(1999, 1, 1),
            BirthdayTo = new DateOnly(2001, 1, 1)
        };

        var result = await _service.SearchAsync(request);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data![0].StaffId, Is.EqualTo("STF00002"));
    }

    [Test]
    public async Task SearchAsync_WhenBirthdayRangeIsInvalid_ReturnsFailedResult()
    {
        var request = new StaffSearchRequest
        {
            BirthdayFrom = new DateOnly(2001, 1, 1),
            BirthdayTo = new DateOnly(2000, 1, 1)
        };

        var result = await _service.SearchAsync(request);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("Birthday from date cannot be after birthday to date."));
    }

    [Test]
    public async Task ExportExcelAsync_ReturnsExcelReadableBytes()
    {
        _repository.Staffs.Add(new Staff
        {
            StaffId = "STF00001",
            FullName = "Sok Dara",
            Birthday = new DateOnly(2000, 1, 15),
            Gender = 1
        });

        var result = await _service.ExportExcelAsync(new StaffSearchRequest());
        var fileText = System.Text.Encoding.UTF8.GetString(result.Data!);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Empty);
        Assert.That(fileText, Does.Contain("<table"));
        Assert.That(fileText, Does.Contain("STF00001"));
        Assert.That(fileText, Does.Contain("Sok Dara"));
    }

    [Test]
    public async Task ExportPdfAsync_ReturnsPdfBytes()
    {
        _repository.Staffs.Add(new Staff
        {
            StaffId = "STF00001",
            FullName = "Sok Dara",
            Birthday = new DateOnly(2000, 1, 15),
            Gender = 1
        });

        var result = await _service.ExportPdfAsync(new StaffSearchRequest());
        var fileHeader = System.Text.Encoding.ASCII.GetString(result.Data!.Take(8).ToArray());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Empty);
        Assert.That(fileHeader, Is.EqualTo("%PDF-1.4"));
    }

    private class FakeStaffRepository : IStaffRepository
    {
        public List<Staff> Staffs { get; } = [];

        public Task<List<Staff>> GetAllAsync()
        {
            return Task.FromResult(Staffs.OrderBy(staff => staff.StaffId).ToList());
        }

        public Task<Staff?> GetByIdAsync(string staffId)
        {
            var staff = Staffs.FirstOrDefault(staff => staff.StaffId == staffId);

            return Task.FromResult(staff);
        }

        public Task<Staff?> GetLastStaffAsync()
        {
            var staff = Staffs.OrderByDescending(staff => staff.StaffId).FirstOrDefault();

            return Task.FromResult(staff);
        }

        public Task<List<Staff>> SearchAsync(
            string? staffId,
            int? gender,
            DateOnly? birthdayFrom,
            DateOnly? birthdayTo)
        {
            var query = Staffs.AsEnumerable();

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

            return Task.FromResult(query.OrderBy(staff => staff.StaffId).ToList());
        }

        public Task AddAsync(Staff staff)
        {
            Staffs.Add(staff);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Staff staff)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Staff staff)
        {
            Staffs.Remove(staff);

            return Task.CompletedTask;
        }
    }
}
