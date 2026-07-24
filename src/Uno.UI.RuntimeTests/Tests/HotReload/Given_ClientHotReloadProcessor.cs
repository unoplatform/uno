#if HAS_UNO
#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.RemoteControl.HotReload;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

// Intentionally NOT platform-gated: unlike the end-to-end hot-reload suites (which spawn a
// desktop HRApp alongside a DevServer and are restricted to skia-desktop), this validates the
// in-process client pieces and must run on every target — the `netX.0-skia` misreport on
// skia-mobile heads (uno-private#2256) shipped precisely because nothing exercised this path
// on device targets.
[TestClass]
public class Given_ClientHotReloadProcessor
{
	[TestMethod]
	public void When_GetRuntimeTargetFramework_Then_PlatformMatchesRuntimeIdentifier()
	{
		// Cross-check the reported platform against the runtime identifier rather than
		// OperatingSystem: the implementation probes OperatingSystem, while the RID is stamped
		// independently by the SDK/runtime pack — a flavor mixup (the uno-runtime asset
		// selection loading a client flavor whose platform detection misfires) desynchronizes
		// the two.
		var rid = RuntimeInformation.RuntimeIdentifier;
		var expectedPlatform =
			rid.StartsWith("android", StringComparison.OrdinalIgnoreCase) ? "android"
			: rid.StartsWith("maccatalyst", StringComparison.OrdinalIgnoreCase) ? "maccatalyst"
			: rid.StartsWith("tvos", StringComparison.OrdinalIgnoreCase) ? "tvos" // incl. tvossimulator
			: rid.StartsWith("ios", StringComparison.OrdinalIgnoreCase) ? "ios" // incl. iossimulator
			: rid.StartsWith("browser", StringComparison.OrdinalIgnoreCase) ? "browserwasm"
			: "skia"; // win-*, linux-*, osx-*: the desktop family reports the `skia` pseudo-platform

		var reported = ClientHotReloadProcessor.GetRuntimeTargetFramework(Microsoft.UI.Xaml.Application.Current.GetType().Assembly);

		var separatorIndex = reported.IndexOf('-');
		Assert.IsTrue(separatorIndex > 0, $"Malformed runtime target framework '{reported}'.");

		var frameworkVersion = reported[..separatorIndex];
		var platform = reported[(separatorIndex + 1)..];

		StringAssert.StartsWith(frameworkVersion, "net");
		Assert.IsTrue(
			Version.TryParse(frameworkVersion.Substring(3), out var version) && version.Major >= 9,
			$"Unexpected framework version in '{reported}'.");
		Assert.AreEqual(expectedPlatform, platform, $"Reported '{reported}' for runtime identifier '{rid}'.");
	}
}
#endif
