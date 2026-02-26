using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class NormalizeNuGetVersionTests
{
	[TestMethod]
	[DataRow("1.8-dev.19", "1.8.0-dev.19")]
	[DataRow("1.8", "1.8.0")]
	[DataRow("6.6.0-dev.4442.g5731140fba", "6.6.0-dev.4442.g5731140fba")]
	[DataRow("10.0.100", "10.0.100")]
	[DataRow("5.6.54", "5.6.54")]
	[DataRow("1.0-beta", "1.0.0-beta")]
	public void NormalizeNuGetVersion_NormalizesCorrectly(string input, string expected)
	{
		var result = NuGetCacheHelper.NormalizeNuGetVersion(input);

		result.Should().Be(expected);
	}
}
