using Microsoft.AspNetCore.Components.Forms;

namespace UserManagement.Blazor.Tests;

internal sealed class FakeAntiforgeryStateProvider : AntiforgeryStateProvider
{
    public const string FieldName = "__RequestVerificationToken";

    public override AntiforgeryRequestToken? GetAntiforgeryToken() => new("test-antiforgery-token", FieldName);
}
