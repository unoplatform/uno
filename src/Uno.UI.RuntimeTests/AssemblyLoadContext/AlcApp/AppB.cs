namespace AlcTestApp;

/// <summary>
/// Minimal sibling Application subclass used by the secondary-ALC sibling-iteration
/// regression test. Distinct <c>Type.FullName</c> from <see cref="App"/> so the dedupe
/// in <c>Application.EnumerateSecondaryApplications</c> does not collapse the two
/// registrations when both are loaded into separate ALCs.
/// </summary>
/// <remarks>
/// Intentionally has no XAML / no <c>InitializeComponent</c>: the test seeds the
/// distinguishing key into <see cref="Application.Resources"/> directly. The base
/// <c>Application</c> constructor still registers the instance in the per-ALC registry
/// via <c>Current = this</c>, so a reflection-driven <c>Activator.CreateInstance</c>
/// from inside a secondary ALC is enough to make the sibling discoverable.
/// </remarks>
public sealed class AppB : Application
{
	public AppB()
	{
	}
}
