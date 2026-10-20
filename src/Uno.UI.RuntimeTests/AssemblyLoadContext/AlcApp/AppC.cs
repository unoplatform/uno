namespace AlcTestApp;

/// <summary>
/// Minimal <see cref="Application"/> subclass owned exclusively by
/// <c>Given_XamlParseContext_AlcResolution</c>, used to register a secondary-ALC
/// application whose resources that test seeds and reads back.
/// </summary>
/// <remarks>
/// A distinct <c>Type.FullName</c> from <see cref="App"/> and <see cref="AppB"/> is load-bearing,
/// not cosmetic. <c>ResourceResolver.TryTopLevelRetrieval</c> re-resolves a parse-context match
/// through <c>Application.GetLatestSecondaryApplicationForType</c> (the hot-reload bump), which is
/// keyed on <c>Type.FullName</c>. Sharing a type with another test class would let that class's
/// registration — in a different ALC, without this test's seeded keys — win the bump and silently
/// turn this test's control assertion into a host fallback.
///
/// Like <see cref="AppB"/>, it carries no XAML and no <c>InitializeComponent</c>: the base
/// <c>Application</c> constructor's <c>Current = this</c> is what registers the instance into the
/// per-ALC registry, so a reflection-driven <c>Activator.CreateInstance</c> inside a secondary ALC
/// is enough to make it discoverable.
/// </remarks>
public sealed class AppC : Application
{
	public AppC()
	{
	}
}
