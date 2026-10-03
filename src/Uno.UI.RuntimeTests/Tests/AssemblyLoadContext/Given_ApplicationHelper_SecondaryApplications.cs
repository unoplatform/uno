#if HAS_UNO
#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

/// <summary>
/// <see cref="ApplicationHelper.EnableSecondaryApplicationSupport"/> is the host's way to honour the
/// <c>Application.HasSecondaryApps</c> contract — set before the first secondary load context is
/// loaded — without depending on the implicit latch inside the secondary <see cref="Application"/>
/// constructor, which runs only after that context's assemblies have already registered resources.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ApplicationHelper_SecondaryApplications
{
	private bool _hadSecondaryApps;

	[TestInitialize]
	public void Initialize()
	{
		_hadSecondaryApps = Application.HasSecondaryApps;
	}

	[TestCleanup]
	public void Cleanup()
	{
		// Matches the other ALC suites: the flag is a one-way latch gating every secondary-app
		// resource path, so leave it as this test found it.
		Application.HasSecondaryApps = _hadSecondaryApps;
	}

	[TestMethod]
	public void When_HostDeclaresSecondaryApplicationSupport_Then_FlagIsSet()
	{
		Application.HasSecondaryApps = false;

		ApplicationHelper.EnableSecondaryApplicationSupport();

		Assert.IsTrue(
			Application.HasSecondaryApps,
			"Declaring secondary-application support must set the flag that gates ALC-aware resource registration and resolution.");
	}

	[TestMethod]
	public void When_AlreadyDeclared_Then_DeclaringAgainKeepsIt()
	{
		Application.HasSecondaryApps = true;

		ApplicationHelper.EnableSecondaryApplicationSupport();

		Assert.IsTrue(Application.HasSecondaryApps, "The declaration is idempotent.");
	}
}
#endif
