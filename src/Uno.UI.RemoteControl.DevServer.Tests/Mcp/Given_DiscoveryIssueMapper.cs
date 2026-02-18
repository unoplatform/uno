using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

[TestClass]
public class Given_DiscoveryIssueMapper
{
	[TestMethod]
	public void WhenNull_ReturnsEmpty()
	{
		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(null);

		issues.Should().BeEmpty();
	}

	[TestMethod]
	public void WhenGlobalJsonMissing_ReportsGlobalJsonNotFound()
	{
		var discovery = new DiscoveryInfo { GlobalJsonPath = null };

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.GlobalJsonNotFound);
		issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenNoUnoSdk_ReportsUnoSdkNotInGlobalJson()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = null,
			UnoSdkVersion = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.UnoSdkNotInGlobalJson);
		issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenSdkNotInCache_ReportsSdkNotInCache()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.SdkNotInCache);
		issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenPackagesJsonMissing_ReportsPackagesJsonNotFound()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = null,
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.PackagesJsonNotFound);
		issues.First(i => i.Code == IssueCode.PackagesJsonNotFound).Severity.Should().Be(ValidationSeverity.Warning);
	}

	[TestMethod]
	public void WhenDevServerNotCached_ReportsDevServerPackageNotCached()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = null,
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.DevServerPackageNotCached);
		issues.First(i => i.Code == IssueCode.DevServerPackageNotCached).Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenHostBinaryMissing_ReportsHostBinaryNotFound()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = "/nuget/uno.winui.devserver/5.5.100",
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
			HostPath = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.HostBinaryNotFound);
		issues.First(i => i.Code == IssueCode.HostBinaryNotFound).Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenDotNetMissing_ReportsDotNetNotFound()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DotNetVersion = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.DotNetNotFound);
		issues.First(i => i.Code == IssueCode.DotNetNotFound).Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenFullyHealthy_ReturnsEmpty()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = "/nuget/uno.winui.devserver/5.5.100",
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
			HostPath = "/nuget/uno.winui.devserver/5.5.100/tools/rc/host/net10.0/Uno.UI.RemoteControl.Host.dll",
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().BeEmpty();
	}

	[TestMethod]
	public void WhenMultipleIssues_ReportsAll()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = null, // Warning
			DotNetVersion = null, // Fatal
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(2);
		issues.Should().Contain(i => i.Code == IssueCode.PackagesJsonNotFound);
		issues.Should().Contain(i => i.Code == IssueCode.DotNetNotFound);
	}
}
