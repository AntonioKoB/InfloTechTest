using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UserManagement.Data.Exceptions;
using UserManagement.Models;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Implementations;

namespace UserManagement.Data.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenContextReturnsEntities_MustReturnSameEntities()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var users = SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetAllAsync();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(users);
    }

    [Fact]
    public async Task FilterByActiveAsync_WhenIsActiveTrue_MustReturnOnlyActiveUsers()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var (activeUsers, _) = SetupMixedUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.FilterByActiveAsync(true);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEquivalentTo(activeUsers);
    }

    [Fact]
    public async Task FilterByActiveAsync_WhenIsActiveFalse_MustReturnOnlyNonActiveUsers()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var (_, nonActiveUsers) = SetupMixedUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.FilterByActiveAsync(false);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEquivalentTo(nonActiveUsers);
    }

    [Fact]
    public async Task GetByIdAsync_WhenContextReturnsUser_MustReturnSameUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = SetupUser();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(user);
    }

    [Fact]
    public async Task GetByIdAsync_WhenContextReturnsNull_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _dataContext
            .Setup(s => s.GetByIdAsync<User>(It.IsAny<object>()))
            .ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WhenContextReturnsUser_MustReturnSameUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Forename = "Johnny", Surname = "User", Email = "juser@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByEmailAsync("juser@example.com");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(user);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenContextReturnsNull_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByEmailAsync("missing@example.com");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenCalled_MustPersistViaDataContext()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        var user = new User
        {
            Forename = "Brand New",
            Surname = "User",
            Email = "brandnewuser@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12)
        };

        // Act: Invokes the method under test with the arranged parameters.
        await service.CreateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _dataContext.Verify(s => s.CreateAsync(user), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyExists_MustThrowEmailAlreadyExistsException()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var existingUser = new User { Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(existingUser);
        var user = new User
        {
            Forename = "Another",
            Surname = "User",
            Email = "existing@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        };

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => service.CreateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
        _dataContext.Verify(s => s.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoOtherUserHasEmail_MustPersistViaDataContext()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        var user = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "updated@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };

        // Act: Invokes the method under test with the arranged parameters.
        await service.UpdateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _dataContext.Verify(s => s.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailBelongsToSameUser_MustPersistViaDataContext()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "unchanged@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        // Act: Invokes the method under test with the arranged parameters.
        await service.UpdateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _dataContext.Verify(s => s.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenDataContextThrowsConcurrencyConflict_MustThrowUserNoLongerExistsException()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Translates the data layer's ConcurrencyConflictException (raised when the row was deleted by
        // another request since being fetched) into a Services-layer exception, so callers of IUserService
        // never need to reference the data layer's exception types directly.
        var service = CreateService();
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _dataContext
            .Setup(s => s.UpdateAsync(It.IsAny<User>()))
            .ThrowsAsync(new ConcurrencyConflictException("gone", new Exception()));
        var user = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "updated@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => service.UpdateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<UserNoLongerExistsException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailBelongsToAnotherUser_MustThrowEmailAlreadyExistsException()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var otherUser = new User { Id = 99, Forename = "Other", Surname = "User", Email = "taken@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(otherUser);
        var updatingUser = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "taken@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => service.UpdateAsync(updatingUser);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
        _dataContext.Verify(s => s.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCalled_MustCallDeleteWhereAsyncWithMatchingIdPredicateWithoutFetchingFirst()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Proves the fetch-then-delete race is gone: DeleteAsync must go straight to a predicate-based bulk
        // delete instead of fetching the entity via GetByIdAsync first - that fetch-then-mutate gap is what
        // let a concurrent second delete find the row already gone and throw DbUpdateConcurrencyException.
        var service = CreateService();
        Expression<Func<User, bool>>? capturedPredicate = null;
        _dataContext
            .Setup(s => s.DeleteWhereAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .Callback<Expression<Func<User, bool>>>(predicate => capturedPredicate = predicate)
            .Returns(Task.CompletedTask);

        // Act: Invokes the method under test with the arranged parameters.
        await service.DeleteAsync(5);

        // Assert: Verifies that the action of the method under test behaves as expected.
        capturedPredicate.Should().NotBeNull();
        capturedPredicate!.Compile()(new User { Id = 5, Forename = "A", Surname = "B", Email = "a@b.com", DateOfBirth = new DateOnly(1990, 1, 1) }).Should().BeTrue();
        capturedPredicate!.Compile()(new User { Id = 6, Forename = "A", Surname = "B", Email = "a@b.com", DateOfBirth = new DateOnly(1990, 1, 1) }).Should().BeFalse();
        _dataContext.Verify(s => s.GetByIdAsync<User>(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_MustNotThrowAndMustStillCallDeleteWhereAsync()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // The fix makes delete idempotent by never creating the race window in the first place: a single
        // predicate-based DELETE affecting zero rows (already-deleted, or never-existed, id) is not an error
        // - unlike the old fetch-then-delete flow, which used to special-case this by checking existence
        // first instead of just letting the underlying delete be a safe no-op.
        var service = CreateService();
        _dataContext
            .Setup(s => s.DeleteWhereAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(Task.CompletedTask);

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => service.DeleteAsync(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().NotThrowAsync();
        _dataContext.Verify(s => s.DeleteWhereAsync<User>(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenEmailDiffersOnlyByCase_PredicateMustStillMatch()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var existingUser = new User { Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        Expression<Func<User, bool>>? capturedPredicate = null;
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .Callback<Expression<Func<User, bool>>>(predicate => capturedPredicate = predicate)
            .ReturnsAsync(existingUser);

        // Act: Invokes the method under test with the arranged parameters.
        await service.GetByEmailAsync("EXISTING@example.com");

        // Assert: Verifies that the action of the method under test behaves as expected.
        capturedPredicate.Should().NotBeNull();
        capturedPredicate!.Compile()(existingUser).Should().BeTrue();
    }

    private User SetupUser(long id = 1, string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateOnly? dateOfBirth = null)
    {
        var user = new User
        {
            Id = id,
            Forename = forename,
            Surname = surname,
            Email = email,
            IsActive = isActive,
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
        };

        _dataContext
            .Setup(s => s.GetByIdAsync<User>(id))
            .ReturnsAsync(user);

        return user;
    }

    private IEnumerable<User> SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = isActive,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _dataContext
            .Setup(s => s.GetAllAsync<User>())
            .ReturnsAsync(users);

        return users;
    }

    private (User[] ActiveUsers, User[] NonActiveUsers) SetupMixedUsers()
    {
        var activeUser = new User { Forename = "Johnny", Surname = "User", Email = "juser@example.com", IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1) };
        var nonActiveUser = new User { Forename = "Jane", Surname = "NonUser", Email = "jnonuser@example.com", IsActive = false, DateOfBirth = new DateOnly(1985, 6, 15) };

        var users = new[] { activeUser, nonActiveUser };

        // Compiles and applies the real predicate FilterByActiveAsync builds against an in-memory array -
        // this proves the service pushes filtering down via WhereAsync rather than materializing everything
        // via GetAllAsync and filtering in C#, not just that some mock returns canned data.
        _dataContext
            .Setup(s => s.WhereAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((Expression<Func<User, bool>> predicate) => users.Where(predicate.Compile()));

        return (new[] { activeUser }, new[] { nonActiveUser });
    }

    private readonly Mock<IDataContext> _dataContext = new();
    private UserService CreateService() => new(_dataContext.Object);
}
