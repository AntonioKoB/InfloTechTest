using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Messaging;

namespace UserManagement.Data.Tests;

public class UserLogServiceTests
{
    [Fact]
    public async Task RecordAsync_WhenCalledWithBeforeAndAfter_MustPublishALogCommandWithBothSnapshots()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Recording an action builds the finished entry here and hands it to the bus; the write itself is the
        // log command handler's job, off the caller's path.
        var service = CreateService();
        var before = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        var after = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        var published = CapturePublishedCommand();

        // Act: Invokes the method under test with the arranged parameters.
        await service.RecordAsync(5, UserLogAction.Updated, before, after);

        // Assert: Verifies that the action of the method under test behaves as expected.
        published.Value.Should().NotBeNull();
        published.Value!.CommandId.Should().NotBeEmpty();
        var captured = published.Value.Entry;
        captured.UserId.Should().Be(5);
        captured.Action.Should().Be(UserLogAction.Updated);
        captured.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        JsonSerializer.Deserialize<User>(captured.BeforeJson!).Should().BeEquivalentTo(before);
        JsonSerializer.Deserialize<User>(captured.AfterJson!).Should().BeEquivalentTo(after);
    }

    [Fact]
    public async Task RecordAsync_MustNotWriteTheEntryItself()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var after = new User { Id = 5, Forename = "Brand New", Surname = "User", Email = "brandnewuser@example.com", DateOfBirth = new DateOnly(1995, 4, 12) };

        // Act: Invokes the method under test with the arranged parameters.
        await service.RecordAsync(5, UserLogAction.Created, before: null, after: after);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _dataContext.Verify(d => d.CreateAsync(It.IsAny<UserLog>()), Times.Never);
        _messageBus.Verify(b => b.PublishAsync(It.IsAny<RecordUserLogCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_WhenBeforeIsNull_MustPublishALogCommandWithNullBeforeJson()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var after = new User { Id = 5, Forename = "Brand New", Surname = "User", Email = "brandnewuser@example.com", DateOfBirth = new DateOnly(1995, 4, 12) };
        var published = CapturePublishedCommand();

        // Act: Invokes the method under test with the arranged parameters.
        await service.RecordAsync(5, UserLogAction.Created, before: null, after: after);

        // Assert: Verifies that the action of the method under test behaves as expected.
        var captured = published.Value.Should().NotBeNull().And.Subject.As<RecordUserLogCommand>().Entry;
        captured.BeforeJson.Should().BeNull();
        JsonSerializer.Deserialize<User>(captured.AfterJson!).Should().BeEquivalentTo(after);
    }

    [Fact]
    public async Task RecordAsync_MustNeverIncludeThePasswordHashInEitherSnapshot()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Snapshots are rendered on screen as the before/after diff and stay in the log table forever - a
        // credential has no business there, hashed or not. Serializing the whole User must therefore leave
        // PasswordHash out, not just avoid displaying it.
        var service = CreateService();
        var before = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1), PasswordHash = "hash-before" };
        var after = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1), PasswordHash = "hash-after" };
        var published = CapturePublishedCommand();

        // Act: Invokes the method under test with the arranged parameters.
        await service.RecordAsync(5, UserLogAction.Updated, before, after);

        // Assert: Verifies that the action of the method under test behaves as expected.
        var captured = published.Value.Should().NotBeNull().And.Subject.As<RecordUserLogCommand>().Entry;
        captured.BeforeJson.Should().NotContain(nameof(User.PasswordHash)).And.NotContain("hash-before");
        captured.AfterJson.Should().NotContain(nameof(User.PasswordHash)).And.NotContain("hash-after");
    }

    [Fact]
    public async Task RecordAsync_WhenAfterIsNull_MustPublishALogCommandWithNullAfterJson()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var before = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        var published = CapturePublishedCommand();

        // Act: Invokes the method under test with the arranged parameters.
        await service.RecordAsync(5, UserLogAction.Deleted, before: before, after: null);

        // Assert: Verifies that the action of the method under test behaves as expected.
        var captured = published.Value.Should().NotBeNull().And.Subject.As<RecordUserLogCommand>().Entry;
        captured.AfterJson.Should().BeNull();
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

    [Fact]
    public async Task GetPagedAsync_WhenCalled_MustRequestFirstPageFromDataContextAndPopulatePagingInfo()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Pagination is pushed down to IDataContext.GetPageAsync/CountAsync rather than materializing the
        // whole table via GetAllAsync and paging in memory - that push-down's own correctness (ordering,
        // actual skip/take slicing) is proven separately in DataContextTests against a real DataContext.
        // This test only proves UserLogService asks the data layer for the right slice.
        var service = CreateService();
        var pageItems = new[]
        {
            new UserLog { Id = 5, UserId = 1, Action = UserLogAction.Created, Timestamp = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc) },
            new UserLog { Id = 4, UserId = 1, Action = UserLogAction.Created, Timestamp = new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc) }
        };
        _dataContext
            .Setup(s => s.GetPageAsync<UserLog, DateTime>(It.IsAny<Expression<Func<UserLog, DateTime>>>(), true, 0, 2))
            .ReturnsAsync(pageItems);
        _dataContext.Setup(s => s.CountAsync<UserLog>()).ReturnsAsync(5);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetPagedAsync(page: 1, pageSize: 2);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Items.Should().BeEquivalentTo(pageItems, options => options.WithStrictOrdering());
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_WhenRequestingSecondPage_MustSkipByPageSize()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _dataContext
            .Setup(s => s.GetPageAsync<UserLog, DateTime>(It.IsAny<Expression<Func<UserLog, DateTime>>>(), true, 2, 2))
            .ReturnsAsync([]);
        _dataContext.Setup(s => s.CountAsync<UserLog>()).ReturnsAsync(5);

        // Act: Invokes the method under test with the arranged parameters.
        await service.GetPagedAsync(page: 2, pageSize: 2);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _dataContext.Verify(s => s.GetPageAsync<UserLog, DateTime>(It.IsAny<Expression<Func<UserLog, DateTime>>>(), true, 2, 2), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_WhenPageIsPastTheEnd_MustReturnWhateverDataContextReturnsWithTotalCountStillPopulated()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _dataContext
            .Setup(s => s.GetPageAsync<UserLog, DateTime>(It.IsAny<Expression<Func<UserLog, DateTime>>>(), true, 1960, 20))
            .ReturnsAsync([]);
        _dataContext.Setup(s => s.CountAsync<UserLog>()).ReturnsAsync(1);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetPagedAsync(page: 99, pageSize: 20);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenContextReturnsLog_MustReturnSameLog()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var log = new UserLog { Id = 5, UserId = 1, Action = UserLogAction.Created, Timestamp = DateTime.UtcNow };
        _dataContext.Setup(s => s.GetByIdAsync<UserLog>(5L)).ReturnsAsync(log);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(5);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(log);
    }

    [Fact]
    public async Task GetByIdAsync_WhenContextReturnsNull_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _dataContext.Setup(s => s.GetByIdAsync<UserLog>(It.IsAny<object>())).ReturnsAsync((UserLog?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    private sealed class Captured<T> where T : class
    {
        public T? Value { get; set; }
    }

    private Captured<RecordUserLogCommand> CapturePublishedCommand()
    {
        var captured = new Captured<RecordUserLogCommand>();
        _messageBus
            .Setup(b => b.PublishAsync(It.IsAny<ICommand>(), It.IsAny<CancellationToken>()))
            .Callback<ICommand, CancellationToken>((c, _) => captured.Value = c as RecordUserLogCommand)
            .Returns(Task.CompletedTask);
        return captured;
    }

    private readonly Mock<IDataContext> _dataContext = new();
    private readonly Mock<IMessageBus> _messageBus = new();
    private UserLogService CreateService() => new(_dataContext.Object, _messageBus.Object);
}
