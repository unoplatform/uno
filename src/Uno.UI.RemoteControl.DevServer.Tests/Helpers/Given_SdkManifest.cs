using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class Given_SdkManifest
{
	[TestMethod]
	[DataRow("6.6.29", "6.5.31", true)]   // higher patch line
	[DataRow("6.5.31", "6.4.12", true)]   // higher minor
	[DataRow("7.0.0", "6.9.9", true)]     // higher major
	[DataRow("6.5.31", "6.5.31", false)]  // equal
	[DataRow("6.5.31", "6.6.29", false)]  // candidate older
	[DataRow("6.5.3", "6.5.31", false)]   // 3 < 31 numerically, not lexically
	[DataRow("6.6", "6.6.0", false)]      // missing trailing segment treated as zero
	[DataRow("6.6.0.1", "6.6.0", true)]   // extra trailing segment is newer
	[DataRow("6.10.0", "6.9.99", true)]   // numeric compare per segment (10 > 9)
	public void WhenComparingVersions_IsNewerFollowsNumericOrder(string candidate, string current, bool expected)
		=> SdkManifest.IsNewer(candidate, current).Should().Be(expected);

	[TestMethod]
	public void WhenPrereleaseSuffixPresent_ComparesNumericCoreOnly()
	{
		// The "-dev.N" suffix is ignored: same numeric core is not "newer".
		SdkManifest.IsNewer("6.6.29-dev.5", "6.6.29").Should().BeFalse();
		SdkManifest.IsNewer("6.7.0-dev.1", "6.6.29").Should().BeTrue();
	}

	[TestMethod]
	[DataRow(null, "6.5.31")]
	[DataRow("6.5.31", null)]
	[DataRow("", "6.5.31")]
	[DataRow("   ", "6.5.31")]
	public void WhenEitherVersionMissing_ReturnsFalse(string? candidate, string? current)
		=> SdkManifest.IsNewer(candidate, current).Should().BeFalse();
}
