using Uno.HotReload.Utils;

namespace Uno.HotReload.Tests.Utils;

/// <summary>
/// Tests for the runtime-descriptor ↔ project-TFM matching rules
/// (<see cref="RoslynExtensions.RuntimeTargetFrameworkMatches"/> and
/// <see cref="RoslynExtensions.TryParseShortTargetFramework"/>).
/// </summary>
[TestClass]
public sealed class Given_RuntimeTargetFrameworkMatching
{
	[TestMethod]
	[DataRow("net10.0", "net10.0", "")]
	[DataRow("net10.0-android", "net10.0", "android")]
	[DataRow("net10.0-android36.0", "net10.0", "android")]
	[DataRow("net10.0-ios26.0", "net10.0", "ios")]
	[DataRow("net10.0-windows10.0.19041.0", "net10.0", "windows")]
	[DataRow("NET10.0-BROWSERWASM", "NET10.0", "BROWSERWASM")]
	public void TryParseShortTargetFramework_ValidInputs(string tfm, string expectedVersion, string expectedPlatform)
	{
		var ok = RoslynExtensions.TryParseShortTargetFramework(tfm, out var version, out var platform);

		Assert.IsTrue(ok);
		Assert.AreEqual(expectedVersion, version);
		Assert.AreEqual(expectedPlatform, platform);
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("   ")]
	[DataRow("netstandard2.0")]
	[DataRow("net10.0-")]
	[DataRow("net10.0-36.0")]
	public void TryParseShortTargetFramework_InvalidInputs(string tfm)
	{
		Assert.IsFalse(RoslynExtensions.TryParseShortTargetFramework(tfm, out _, out _));
	}

	[TestMethod]
	// Exact and platform-version-tolerant matches.
	[DataRow("net10.0-android", "net10.0-android", true)]
	[DataRow("net10.0-android", "net10.0-android36.0", true)]
	[DataRow("net10.0-ios", "net10.0-ios26.0", true)]
	[DataRow("net10.0-IOS", "net10.0-ios26.0", true)]
	// The skia pseudo-platform designates the desktop family — both spellings.
	[DataRow("net10.0-skia", "net10.0-desktop", true)]
	[DataRow("net10.0-skia", "net10.0", true)]
	// Fallback path: an older client reports the build-captured TFM verbatim.
	[DataRow("net10.0-desktop", "net10.0-desktop", true)]
	[DataRow("net10.0-desktop", "net10.0", true)]
	[DataRow("net10.0", "net10.0-desktop", true)]
	// Non-matches: different platform or framework version.
	[DataRow("net10.0-skia", "net10.0-android36.0", false)]
	[DataRow("net10.0-android", "net10.0-ios26.0", false)]
	[DataRow("net9.0-android", "net10.0-android36.0", false)]
	[DataRow("net10.0-browserwasm", "net10.0-desktop", false)]
	[DataRow("net10.0-skia", "net10.0-browserwasm", false)]
	public void RuntimeTargetFrameworkMatches_Cases(string runtime, string project, bool expected)
	{
		Assert.AreEqual(expected, RoslynExtensions.RuntimeTargetFrameworkMatches(runtime, project));
	}
}
