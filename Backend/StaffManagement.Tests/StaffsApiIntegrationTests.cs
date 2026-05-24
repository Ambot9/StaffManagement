using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StaffManagement.Data;
using StaffManagement.Model.Entities;
using StaffManagement.Model.Request;

namespace StaffManagement.Tests;

[TestFixture]
public class StaffsApiIntegrationTests
{
    private StaffManagementApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new StaffManagementApiFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task CreateAndGetAll_WhenRequestIsValid_ReturnsCreatedStaff()
    {
        var createRequest = new CreateStaffRequest
        {
            FullName = "Sok Dara",
            Birthday = new DateOnly(2000, 1, 15),
            Gender = 1
        };

        var createResponse = await _client.PostAsJsonAsync("/api/Staffs", createRequest);
        var staffs = await _client.GetFromJsonAsync<List<Staff>>("/api/Staffs");

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(staffs, Is.Not.Null);
        Assert.That(staffs, Has.Count.EqualTo(1));
        Assert.That(staffs![0].StaffId, Is.EqualTo("STF00001"));
    }

    [Test]
    public async Task Search_WhenCriteriaMatch_ReturnsFilteredStaffs()
    {
        await CreateStaffAsync("Male Staff", new DateOnly(1995, 1, 1), 1);
        await CreateStaffAsync("Female Staff", new DateOnly(2000, 1, 1), 2);
        await CreateStaffAsync("Another Female Staff", new DateOnly(2005, 1, 1), 2);

        var staffs = await _client.GetFromJsonAsync<List<Staff>>(
            "/api/Staffs/search?gender=2&birthdayFrom=1999-01-01&birthdayTo=2001-01-01");

        Assert.That(staffs, Is.Not.Null);
        Assert.That(staffs, Has.Count.EqualTo(1));
        Assert.That(staffs![0].StaffId, Is.EqualTo("STF00002"));
    }

    [Test]
    public async Task ExportExcel_WhenCalled_ReturnsExcelFile()
    {
        await CreateStaffAsync("Sok Dara", new DateOnly(2000, 1, 15), 1);

        var response = await _client.GetAsync("/api/Staffs/export/excel");
        var content = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/vnd.ms-excel"));
        Assert.That(content, Does.Contain("STF00001"));
        Assert.That(content, Does.Contain("Sok Dara"));
    }

    [Test]
    public async Task ExportPdf_WhenCalled_ReturnsPdfFile()
    {
        await CreateStaffAsync("Sok Dara", new DateOnly(2000, 1, 15), 1);

        var response = await _client.GetAsync("/api/Staffs/export/pdf");
        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        var header = System.Text.Encoding.ASCII.GetString(fileBytes.Take(8).ToArray());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/pdf"));
        Assert.That(header, Is.EqualTo("%PDF-1.4"));
    }

    [Test]
    public async Task UpdateAndDelete_WhenStaffExists_ChangesAndRemovesStaff()
    {
        await CreateStaffAsync("Old Name", new DateOnly(1995, 3, 10), 1);

        var updateRequest = new UpdateStaffRequest
        {
            FullName = "New Name",
            Birthday = new DateOnly(1996, 4, 11),
            Gender = 2
        };

        var updateResponse = await _client.PutAsJsonAsync("/api/Staffs/STF00001", updateRequest);
        var updatedStaff = await _client.GetFromJsonAsync<Staff>("/api/Staffs/STF00001");
        var deleteResponse = await _client.DeleteAsync("/api/Staffs/STF00001");
        var getDeletedResponse = await _client.GetAsync("/api/Staffs/STF00001");

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(updatedStaff!.FullName, Is.EqualTo("New Name"));
        Assert.That(updatedStaff.Gender, Is.EqualTo(2));
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(getDeletedResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private async Task CreateStaffAsync(string fullName, DateOnly birthday, int gender)
    {
        var request = new CreateStaffRequest
        {
            FullName = fullName,
            Birthday = birthday,
            Gender = gender
        };

        var response = await _client.PostAsJsonAsync("/api/Staffs", request);

        response.EnsureSuccessStatusCode();
    }
}

public class StaffManagementApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            var dbContextOptions = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextOptions != null)
            {
                services.Remove(dbContextOptions);
            }

            var dbConnection = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbConnection));

            if (dbConnection != null)
            {
                services.Remove(dbConnection);
            }

            services.AddSingleton<DbConnection>(_connection);

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var connection = serviceProvider.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
