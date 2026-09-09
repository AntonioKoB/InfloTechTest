using Microsoft.AspNetCore.Components.Forms;

namespace UserManagement.Blazor.Tests;

/// <summary>
/// Stands in for the framework's antiforgery state so the &lt;AntiforgeryToken /&gt; component can render its
/// hidden field under bUnit, which has no HTTP request to derive a real token from.
/// </summary>
internal sealed class FakeAntiforgeryStateProvider : AntiforgeryStateProvider
{
    public const string FieldName = "__RequestVerificationToken";

    public override AntiforgeryRequestToken? GetAntiforgeryToken() => new("test-antiforgery-token", FieldName);
}
