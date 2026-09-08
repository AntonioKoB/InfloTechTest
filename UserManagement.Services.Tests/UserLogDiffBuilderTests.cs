using System;
using System.Text.Json;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;

namespace UserManagement.Data.Tests;

public class UserLogDiffBuilderTests
{
    [Fact]
    public void Build_WhenBeforeIsNull_MustShowAllAfterFieldsAsAdded()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var builder = CreateBuilder();
        var after = new User { Id = 1, Forename = "Brand New", Surname = "User", Email = "brandnewuser@example.com", IsActive = true, DateOfBirth = new DateOnly(1995, 4, 12) };
        var log = new UserLog { UserId = 1, Action = UserLogAction.Created, Timestamp = DateTime.UtcNow, BeforeJson = null, AfterJson = JsonSerializer.Serialize(after) };

        // Act: Invokes the method under test with the arranged parameters.
        var result = builder.Build(log);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().Contain(c => c.PropertyName == nameof(User.Forename) && c.OldValue == null && c.NewValue == after.Forename);
        result.Should().Contain(c => c.PropertyName == nameof(User.Surname) && c.OldValue == null && c.NewValue == after.Surname);
        result.Should().Contain(c => c.PropertyName == nameof(User.Email) && c.OldValue == null && c.NewValue == after.Email);
        result.Should().Contain(c => c.PropertyName == nameof(User.IsActive) && c.OldValue == null && c.NewValue == after.IsActive.ToString());
        result.Should().Contain(c => c.PropertyName == nameof(User.DateOfBirth) && c.OldValue == null && c.NewValue == after.DateOfBirth.ToString());
        result.Should().NotContain(c => c.PropertyName == nameof(User.Id));
    }

    [Fact]
    public void Build_WhenAfterIsNull_MustShowAllBeforeFieldsAsRemoved()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var builder = CreateBuilder();
        var before = new User { Id = 1, Forename = "Existing", Surname = "User", Email = "existing@example.com", IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1) };
        var log = new UserLog { UserId = 1, Action = UserLogAction.Deleted, Timestamp = DateTime.UtcNow, BeforeJson = JsonSerializer.Serialize(before), AfterJson = null };

        // Act: Invokes the method under test with the arranged parameters.
        var result = builder.Build(log);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().Contain(c => c.PropertyName == nameof(User.Forename) && c.OldValue == before.Forename && c.NewValue == null);
        result.Should().Contain(c => c.PropertyName == nameof(User.Surname) && c.OldValue == before.Surname && c.NewValue == null);
        result.Should().Contain(c => c.PropertyName == nameof(User.Email) && c.OldValue == before.Email && c.NewValue == null);
        result.Should().Contain(c => c.PropertyName == nameof(User.IsActive) && c.OldValue == before.IsActive.ToString() && c.NewValue == null);
        result.Should().Contain(c => c.PropertyName == nameof(User.DateOfBirth) && c.OldValue == before.DateOfBirth.ToString() && c.NewValue == null);
        result.Should().NotContain(c => c.PropertyName == nameof(User.Id));
    }

    [Fact]
    public void Build_WhenBothPresent_MustOnlyShowChangedFields()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var builder = CreateBuilder();
        var before = new User { Id = 1, Forename = "Old", Surname = "User", Email = "unchanged@example.com", IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1) };
        var after = new User { Id = 1, Forename = "New", Surname = "User", Email = "unchanged@example.com", IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1) };
        var log = new UserLog { UserId = 1, Action = UserLogAction.Updated, Timestamp = DateTime.UtcNow, BeforeJson = JsonSerializer.Serialize(before), AfterJson = JsonSerializer.Serialize(after) };

        // Act: Invokes the method under test with the arranged parameters.
        var result = builder.Build(log);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = nameof(User.Forename), OldValue = "Old", NewValue = "New" });
    }

    [Fact]
    public void Build_MustNeverIncludeIdFieldEvenWhenOnlyOneSnapshotExists()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Id is identity, not a business field - including it would show a pointless "Id: 1 -> (none)" row
        // whenever only one snapshot exists (Created/Viewed/Deleted). When both snapshots exist (Updated),
        // Id would already be excluded by the skip-if-equal rule since it never changes - this test covers
        // the one case that rule doesn't naturally handle.
        var builder = CreateBuilder();
        var after = new User { Id = 42, Forename = "Brand New", Surname = "User", Email = "brandnewuser@example.com", IsActive = true, DateOfBirth = new DateOnly(1995, 4, 12) };
        var log = new UserLog { UserId = 42, Action = UserLogAction.Created, Timestamp = DateTime.UtcNow, BeforeJson = null, AfterJson = JsonSerializer.Serialize(after) };

        // Act: Invokes the method under test with the arranged parameters.
        var result = builder.Build(log);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().NotContain(c => c.PropertyName == nameof(User.Id));
    }

    [Fact]
    public void Build_WhenActionIsViewed_MustReturnNoChanges()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Viewed stores an After snapshot (for display on the View screen) even though nothing changed -
        // without this, the generic Before-is-null rule would show it as a full "nothing -> everything" diff,
        // identical to Created's shape, which is misleading since a view isn't a change.
        var builder = CreateBuilder();
        var after = new User { Id = 1, Forename = "Existing", Surname = "User", Email = "existing@example.com", IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1) };
        var log = new UserLog { UserId = 1, Action = UserLogAction.Viewed, Timestamp = DateTime.UtcNow, BeforeJson = null, AfterJson = JsonSerializer.Serialize(after) };

        // Act: Invokes the method under test with the arranged parameters.
        var result = builder.Build(log);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEmpty();
    }

    private static UserLogDiffBuilder CreateBuilder() => new();
}
