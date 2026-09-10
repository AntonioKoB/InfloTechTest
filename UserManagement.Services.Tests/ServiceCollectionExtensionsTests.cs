using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

/// <summary>
/// Pins the one thing the decorator order guarantees: the user cache sits beneath the audit, so a read
/// served from memory is still recorded as a view. Resolves IUserService through the real registration with
/// only the data access mocked, because the order is decided in AddDomainServices and nowhere else.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddDomainServices_WhenAUserIsReadTwiceAsViewed_MustHitTheDatabaseOnceAndRecordTwoViews()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var user = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        var dataContext = new Mock<IDataContext>();
        dataContext.Setup(d => d.GetByIdAsync<User>(It.IsAny<object>())).ReturnsAsync(user);
        using var provider = new ServiceCollection().AddSingleton(dataContext.Object).AddDomainServices().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Act: Invokes the method under test with the arranged parameters.
        await userService.GetByIdAsync(5, recordAsViewed: true);
        await userService.GetByIdAsync(5, recordAsViewed: true);

        // Assert: Verifies that the action of the method under test behaves as expected.
        dataContext.Verify(d => d.GetByIdAsync<User>(It.IsAny<object>()), Times.Once);
        dataContext.Verify(d => d.CreateAsync(It.Is<UserLog>(l => l.UserId == 5 && l.Action == UserLogAction.Viewed)), Times.Exactly(2));
    }
}
