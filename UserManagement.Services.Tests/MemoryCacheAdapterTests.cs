using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using UserManagement.Models;
using UserManagement.Services.Caching;

namespace UserManagement.Data.Tests;

public class MemoryCacheAdapterTests
{
    [Fact]
    public async Task GetAsync_WhenKeyWasSet_MustReturnTheValue()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var cache = CreateCache();
        var user = CreateUser();
        await cache.SetAsync("users:5", user, TimeSpan.FromMinutes(5));

        // Act: Invokes the method under test with the arranged parameters.
        var result = await cache.GetAsync<User>("users:5");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task GetAsync_WhenKeyWasNeverSet_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var cache = CreateCache();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await cache.GetAsync<User>("users:999");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WhenKeyWasSet_MustMakeGetReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var cache = CreateCache();
        await cache.SetAsync("users:5", CreateUser(), TimeSpan.FromMinutes(5));

        // Act: Invokes the method under test with the arranged parameters.
        await cache.RemoveAsync("users:5");

        // Assert: Verifies that the action of the method under test behaves as expected.
        (await cache.GetAsync<User>("users:5")).Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenTheEntryHasOutlivedItsTimeToLive_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var clock = new TestClock { UtcNow = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero) };
        var cache = CreateCache(clock);
        await cache.SetAsync("users:5", CreateUser(), TimeSpan.FromMinutes(5));

        // Act: Invokes the method under test with the arranged parameters.
        clock.UtcNow += TimeSpan.FromMinutes(6);
        var result = await cache.GetAsync<User>("users:5");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    private static MemoryCacheAdapter CreateCache(ISystemClock? clock = null)
        => new(new MemoryCache(new MemoryCacheOptions { Clock = clock }));

    private static User CreateUser()
        => new() { Id = 5, Forename = "Cached", Surname = "User", Email = "cached@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
