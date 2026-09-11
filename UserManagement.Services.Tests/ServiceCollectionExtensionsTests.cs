using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Services.Messaging;

namespace UserManagement.Data.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddDomainServices_WhenUserReadTwiceAsViewed_MustHitDatabaseOnceAndLogTwice()
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
        var published = await provider.GetRequiredService<IMessageBus>().ReadAsync(3, TimeSpan.FromMilliseconds(300));
        published.Should().HaveCount(2).And.AllSatisfy(c =>
            c.Should().BeOfType<RecordUserLogCommand>().Which.Entry.Should().Match<UserLog>(l => l.UserId == 5 && l.Action == UserLogAction.Viewed));
        dataContext.Verify(d => d.CreateAsync(It.IsAny<UserLog>()), Times.Never);
    }

    [Fact]
    public void AddDomainServices_MustResolveAHandlerForEveryCommand()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        using var provider = new ServiceCollection().AddSingleton(new Mock<IDataContext>().Object).AddDomainServices().BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act: Invokes the method under test with the arranged parameters.
        var handlers = new object[]
        {
            scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateUserCommand>>(),
            scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateUserCommand>>(),
            scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteUserCommand>>(),
            scope.ServiceProvider.GetRequiredService<ICommandHandler<RecordUserLogCommand>>()
        };

        // Assert: Verifies that the action of the method under test behaves as expected.
        handlers.Should().AllSatisfy(h => h.Should().NotBeNull());
    }

    [Fact]
    public async Task AddDomainServices_WhenCreateHandled_MustPublishOneLogCommandOnly()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var dataContext = new Mock<IDataContext>();
        dataContext.Setup(d => d.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);
        dataContext.Setup(d => d.CreateAsync(It.IsAny<User>())).Callback<User>(u => u.Id = 7).Returns(Task.CompletedTask);
        using var provider = new ServiceCollection().AddSingleton(dataContext.Object).AddDomainServices().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateUserCommand>>();
        var command = new CreateUserCommand(Guid.NewGuid(), "Brand New", "User", "brandnewuser@example.com", new DateOnly(1995, 4, 12), true, "hashed");

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        var published = await provider.GetRequiredService<IMessageBus>().ReadAsync(2, TimeSpan.FromMilliseconds(300));
        published.Should().ContainSingle().Which.Should().BeOfType<RecordUserLogCommand>()
            .Which.Entry.Should().Match<UserLog>(l => l.UserId == 7 && l.Action == UserLogAction.Created);
        dataContext.Verify(d => d.CreateAsync(It.IsAny<UserLog>()), Times.Never);
    }
}
