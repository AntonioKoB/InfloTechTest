using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using UserManagement.Models;
using UserManagement.Services.Caching;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

/// <summary>
/// Caches a single user by id beneath the auditing decorator, so a read served from memory is still audited
/// by the layer above (see ServiceCollectionExtensionsTests for the order). Lists are cached at the HTTP
/// layer by the API's output caching instead, so they and the email lookup pass straight through here. The
/// cache hands out copies: callers mutate the user they were given before saving it, and that must never
/// reach the cached one.
/// </summary>
public class CachingUserServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenCalledTwice_MustQueryTheInnerServiceOnce()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = SetupUser();

        // Act: Invokes the method under test with the arranged parameters.
        var first = await service.GetByIdAsync(user.Id);
        var second = await service.GetByIdAsync(user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.GetByIdAsync(user.Id, It.IsAny<bool>()), Times.Once);
        first.Should().BeEquivalentTo(user);
        second.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTheRecordAsViewedFlagDiffersBetweenCalls_MustStillServeTheSecondCallFromCache()
    {
        // Arrange: the flag only matters to the auditing decorator above; the cached user is the same either way.
        var service = CreateService();
        var user = SetupUser();

        // Act: Invokes the method under test with the arranged parameters.
        await service.GetByIdAsync(user.Id, recordAsViewed: true);
        await service.GetByIdAsync(user.Id, recordAsViewed: false);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.GetByIdAsync(user.Id, It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_MustNotCacheTheMiss()
    {
        // Arrange: an id that does not exist now may exist after the next create, so a miss is never remembered.
        var service = CreateService();
        _inner.Setup(s => s.GetByIdAsync(999, It.IsAny<bool>())).ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var first = await service.GetByIdAsync(999);
        var second = await service.GetByIdAsync(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        first.Should().BeNull();
        second.Should().BeNull();
        _inner.Verify(s => s.GetByIdAsync(999, It.IsAny<bool>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledForDifferentUsers_MustCacheEachSeparately()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var first = SetupUser(id: 5, forename: "First");
        var second = SetupUser(id: 7, forename: "Second");

        // Act: Invokes the method under test with the arranged parameters.
        var firstResult = await service.GetByIdAsync(5);
        var secondResult = await service.GetByIdAsync(7);
        await service.GetByIdAsync(5);
        await service.GetByIdAsync(7);

        // Assert: Verifies that the action of the method under test behaves as expected.
        firstResult.Should().BeEquivalentTo(first);
        secondResult.Should().BeEquivalentTo(second);
        _inner.Verify(s => s.GetByIdAsync(5, It.IsAny<bool>()), Times.Once);
        _inner.Verify(s => s.GetByIdAsync(7, It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCallerMutatesTheReturnedUser_MustNotAffectTheCachedCopy()
    {
        // Arrange: the API's update flow mutates the fetched user in place before saving it.
        var service = CreateService();
        var user = SetupUser();
        var originalForename = user.Forename;

        // Act: Invokes the method under test with the arranged parameters.
        var first = await service.GetByIdAsync(user.Id);
        first!.Forename = "Tampered";
        var second = await service.GetByIdAsync(user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        second.Should().NotBeSameAs(first);
        second!.Forename.Should().Be(originalForename);
    }

    [Fact]
    public async Task UpdateAsync_WhenCalled_MustInvalidateThatUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = SetupUser();
        await service.GetByIdAsync(user.Id);

        // Act: Invokes the method under test with the arranged parameters.
        await service.UpdateAsync(user);
        await service.GetByIdAsync(user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.UpdateAsync(user), Times.Once);
        _inner.Verify(s => s.GetByIdAsync(user.Id, It.IsAny<bool>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_WhenCalled_MustLeaveOtherUsersCached()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var updated = SetupUser(id: 5);
        var other = SetupUser(id: 7);
        await service.GetByIdAsync(updated.Id);
        await service.GetByIdAsync(other.Id);

        // Act: Invokes the method under test with the arranged parameters.
        await service.UpdateAsync(updated);
        await service.GetByIdAsync(other.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.GetByIdAsync(other.Id, It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenInnerThrows_MustStillInvalidateThatUser()
    {
        // Arrange: a failed save can mean the row changed or vanished underneath (UserNoLongerExistsException),
        // so a write attempt invalidates whether or not it succeeded - the cost is one extra read.
        var service = CreateService();
        var user = SetupUser();
        await service.GetByIdAsync(user.Id);
        _inner.Setup(s => s.UpdateAsync(user)).ThrowsAsync(new EmailAlreadyExistsException(user.Email));

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => service.UpdateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
        await service.GetByIdAsync(user.Id);
        _inner.Verify(s => s.GetByIdAsync(user.Id, It.IsAny<bool>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_WhenCalled_MustInvalidateThatUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = SetupUser();
        await service.GetByIdAsync(user.Id);

        // Act: Invokes the method under test with the arranged parameters.
        await service.DeleteAsync(user.Id);
        await service.GetByIdAsync(user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.DeleteAsync(user.Id), Times.Once);
        _inner.Verify(s => s.GetByIdAsync(user.Id, It.IsAny<bool>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_WhenCalled_MustPassThrough()
    {
        // Arrange: a brand-new id was never cached (misses are not remembered), so there is nothing to invalidate.
        var service = CreateService();
        var user = new User { Forename = "Brand New", Surname = "User", Email = "brandnewuser@example.com", DateOfBirth = new DateOnly(1995, 4, 12) };

        // Act: Invokes the method under test with the arranged parameters.
        await service.CreateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.CreateAsync(user), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalledTwice_MustPassThroughBothTimes()
    {
        // Arrange: the list is cached at the HTTP layer, not here.
        var service = CreateService();
        _inner.Setup(s => s.GetAllAsync()).ReturnsAsync([SetupUser()]);

        // Act: Invokes the method under test with the arranged parameters.
        await service.GetAllAsync();
        await service.GetAllAsync();

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.GetAllAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task FilterByActiveAsync_WhenCalledTwice_MustPassThroughBothTimes()
    {
        // Arrange: the filtered lists are cached at the HTTP layer, not here.
        var service = CreateService();
        _inner.Setup(s => s.FilterByActiveAsync(true)).ReturnsAsync([SetupUser()]);

        // Act: Invokes the method under test with the arranged parameters.
        await service.FilterByActiveAsync(true);
        await service.FilterByActiveAsync(true);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.FilterByActiveAsync(true), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByEmailAsync_WhenCalledTwice_MustPassThroughBothTimes()
    {
        // Arrange: the email lookup backs uniqueness checks and sign-in, which must always see the database.
        var service = CreateService();
        var user = SetupUser();
        _inner.Setup(s => s.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        // Act: Invokes the method under test with the arranged parameters.
        await service.GetByEmailAsync(user.Email);
        await service.GetByEmailAsync(user.Email);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.GetByEmailAsync(user.Email), Times.Exactly(2));
    }

    private User SetupUser(long id = 5, string forename = "Existing", string surname = "User", string email = "existing@example.com")
    {
        var user = new User { Id = id, Forename = forename, Surname = surname, Email = email, IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1) };
        _inner.Setup(s => s.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync(user);
        return user;
    }

    private readonly Mock<IUserService> _inner = new();
    private CachingUserService CreateService() => new(_inner.Object, new MemoryCacheAdapter(new MemoryCache(new MemoryCacheOptions())));
}
