using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using UserManagement.Blazor.Components.Pages;

namespace UserManagement.Blazor.Tests;

public class LoginPageTests : BunitContext
{
    public LoginPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<AntiforgeryStateProvider, FakeAntiforgeryStateProvider>();
    }

    [Fact]
    public void Render_MustShowEmailAndPasswordInputsAndASignInButton()
    {
        // Act
        var cut = Render<LoginPage>();

        // Assert
        cut.Find("#input-email").GetAttribute("type").Should().Be("email");
        cut.Find("#input-password").GetAttribute("type").Should().Be("password");
        cut.Find("#login-button").Should().NotBeNull();
    }

    [Fact]
    public void Render_MustPostTheFormToTheLoginEndpointWithAnAntiforgeryToken()
    {
        // Arrange
        // Login is a genuine HTTP form post to the Blazor host (not a SignalR event) so the server can issue
        // the auth cookie. The field names bind to LoginRequest; the hidden antiforgery field is what lets
        // the endpoint validate that the post came from this page.

        // Act
        var cut = Render<LoginPage>();

        // Assert
        var form = cut.Find("form");
        form.GetAttribute("method").Should().BeEquivalentTo("post");
        form.GetAttribute("action").Should().Be("/login");
        cut.Find("#input-email").GetAttribute("name").Should().Be("Email");
        cut.Find("#input-password").GetAttribute("name").Should().Be("Password");
        cut.Find($"input[type=hidden][name={FakeAntiforgeryStateProvider.FieldName}]").GetAttribute("value").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Render_WithoutAnError_MustNotShowAnErrorMessage()
    {
        // Act
        var cut = Render<LoginPage>();

        // Assert
        cut.FindAll("#login-error").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithTheErrorQueryParameter_MustShowAnInvalidCredentialsMessage()
    {
        // Arrange
        Services.GetRequiredService<NavigationManager>().NavigateTo("/login?error=1");

        // Act
        var cut = Render<LoginPage>();

        // Assert
        cut.Find("#login-error").TextContent.Should().Contain("email or password");
    }
}
