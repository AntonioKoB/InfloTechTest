using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Blazor.Components.Auth;

namespace UserManagement.Blazor.Tests;

public class RedirectToLoginTests : BunitContext
{
    [Fact]
    public void Render_MustNavigateToTheLoginPage()
    {
        // Arrange
        var navigation = Services.GetRequiredService<NavigationManager>();

        // Act
        Render<RedirectToLogin>();

        // Assert
        navigation.Uri.Should().EndWith("/login");
    }
}
