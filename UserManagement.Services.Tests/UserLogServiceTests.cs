using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;

namespace UserManagement.Data.Tests;

public class UserLogServiceTests
{
    [Fact]
    public async Task RecordAsync_WhenCalledWithBeforeAndAfter_MustPersistLogWithBothSnapshots()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var before = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        var after = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        UserLog? captured = null;
        _dataContext
            .Setup(s => s.CreateAsync(It.IsAny<UserLog>()))
            .Callback<UserLog>(log => captured = log)
            .Returns(Task.CompletedTask);

        // Act: Invokes the method under test with the arranged parameters.
        await service.RecordAsync(5, UserLogAction.Updated, before, after);

        // Assert: Verifies that the action of the method under test behaves as expected.
        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(5);
        captured.Action.Should().Be(UserLogAction.Updated);
        captured.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        JsonSerializer.Deserialize<User>(captured.BeforeJson!).Should().BeEquivalentTo(before);
        JsonSerializer.Deserialize<User>(captured.AfterJson!).Should().BeEquivalentTo(after);
    }

    [Fact]
    public async Task RecordAsync_WhenBeforeIsNull_MustPersistLogWithNullBeforeJson()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var after = new User { Id = 5, Forename = "Brand New", Surname = "User", Email = "brandnewuser@example.com", DateOfBirth = new DateOnly(1995, 4, 12) };
        UserLog? captured = null;
        _dataContext
            .Setup(s => s.CreateAsync(It.IsAny<UserLog>()))
            .Callback<UserLog>(log => captured = log)
            .Returns(Task.CompletedTask);

        // Act: Invokes the method under test with the arranged parameters.
        await service.RecordAsync(5, UserLogAction.Created, before: null, after: after);

        // Assert: Verifies that the action of the method under test behaves as expected.
        captured.Should().NotBeNull();
        captured!.BeforeJson.Should().BeNull();
        JsonSerializer.Deserialize<User>(captured.AfterJson!).Should().BeEquivalentTo(after);
    }

    [Fact]
    public async Task RecordAsync_WhenAfterIsNull_MustPersistLogWithNullAfterJson()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var before = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        UserLog? captured = null;
        _dataContext
            .Setup(s => s.CreateAsync(It.IsAny<UserLog>()))
            .Callback<UserLog>(log => captured = log)
            .Returns(Task.CompletedTask);

        // Act: Invokes the method under test with the arranged parameters.
        await service.RecordAsync(5, UserLogAction.Deleted, before: before, after: null);

        // Assert: Verifies that the action of the method under test behaves as expected.
        captured.Should().NotBeNull();
        captured!.AfterJson.Should().BeNull();
        JsonSerializer.Deserialize<User>(captured.BeforeJson!).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task GetForUserAsync_WhenCalled_MustReturnOnlyThatUsersLogsNewestFirst()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var older = new UserLog { Id = 1, UserId = 5, Action = UserLogAction.Created, Timestamp = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc) };
        var newer = new UserLog { Id = 2, UserId = 5, Action = UserLogAction.Updated, Timestamp = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc) };
        var otherUsersLog = new UserLog { Id = 3, UserId = 6, Action = UserLogAction.Created, Timestamp = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc) };
        _dataContext
            .Setup(s => s.GetAllAsync<UserLog>())
            .ReturnsAsync([older, newer, otherUsersLog]);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetForUserAsync(5);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().ContainInOrder(newer, older);
        result.Should().NotContain(otherUsersLog);
    }

    private readonly Mock<IDataContext> _dataContext = new();
    private UserLogService CreateService() => new(_dataContext.Object);
}
