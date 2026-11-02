using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

[TestClass]
public class Given_XamlTelemetry
{
	[TestMethod]
	[DataRow("true", "")]
	[DataRow("True", "")]
	[DataRow("1", "")]
	[DataRow("", "true")]
	[DataRow("", "TRUE")]
	[DataRow("", "1")]
	[DataRow("false", "1")]
	public void When_Build_Opted_Out_Then_Telemetry_Is_Disabled(string telemetryOptOut, string environmentOptOut)
		=> Assert.IsTrue(XamlCodeGeneration.IsTelemetryDisabled(telemetryOptOut, environmentOptOut, hotReloadHost: "", isDesignTimeBuild: false));

	[TestMethod]
	public void When_Design_Time_Build_Then_Telemetry_Is_Disabled()
		=> Assert.IsTrue(XamlCodeGeneration.IsTelemetryDisabled("", "", hotReloadHost: "", isDesignTimeBuild: true));

	[TestMethod]
	public void When_Hot_Reload_Host_Then_Telemetry_Is_Disabled()
		=> Assert.IsTrue(XamlCodeGeneration.IsTelemetryDisabled("", "", hotReloadHost: "True", isDesignTimeBuild: false));

	[TestMethod]
	[DataRow("", "", "")]
	[DataRow("false", "", "")]
	[DataRow("0", "", "")]
	[DataRow("", "false", "")]
	[DataRow("", "0", "")]
	[DataRow("", "", "False")]
	public void When_Build_Not_Opted_Out_Then_Telemetry_Is_Enabled(string telemetryOptOut, string environmentOptOut, string hotReloadHost)
		=> Assert.IsFalse(XamlCodeGeneration.IsTelemetryDisabled(telemetryOptOut, environmentOptOut, hotReloadHost, isDesignTimeBuild: false));
}
