using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using UserManagement.Blazor.Components.Layout;

namespace UserManagement.Blazor.Tests;

public class NavMenuTests : BunitContext
{
    public NavMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<AntiforgeryStateProvider, FakeAntiforgeryStateProvider>();
    }

    [Fact]
    public void WhenSignedIn_MustShowNavigationTheUserNameAndALogoutFormPostingToTheLogoutEndpoint()
    {
        // Arrange
        // Logout changes who the browser is signed in as, so like login it is a real form post carrying the
        // antiforgery token, not a client-side click handler.
        var authorization = AddAuthorization();
        authorization.SetAuthorized("Peter Loew");

        // Act
        var cut = Render<NavMenu>();

        // Assert
        cut.Find("#nav-users").Should().NotBeNull();
        cut.Find("#signed-in-user").TextContent.Should().Contain("Peter Loew");
        var form = cut.Find("#logout-form");
        form.GetAttribute("method").Should().BeEquivalentTo("post");
        form.GetAttribute("action").Should().Be("/logout");
        cut.Find($"#logout-form input[type=hidden][name={FakeAntiforgeryStateProvider.FieldName}]").Should().NotBeNull();
    }

    [Fact]
    public void WhenSignedOut_MustShowNeitherNavigationNorTheUserNorLogout()
    {
        // Arrange
        var authorization = AddAuthorization();
        authorization.SetNotAuthorized();

        // Act
        var cut = Render<NavMenu>();

        // Assert
        cut.FindAll("#nav-users").Should().BeEmpty();
        cut.FindAll("#signed-in-user").Should().BeEmpty();
        cut.FindAll("#logout-form").Should().BeEmpty();
    }
}
