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

	// -------------------------------------------------------------------
	// Add-in degradation scenarios
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("Settings package listed in packages.json but not in NuGet cache → AddInPackageNotCached")]
	public void WhenSettingsPackageNotCached_ReportsAddInPackageNotCached()
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
			HostPath = "/nuget/uno.winui.devserver/5.5.100/tools/rc/host/net10.0/Host.dll",
			SettingsPackageVersion = "1.2.3",
			SettingsPackagePath = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.AddInPackageNotCached);
		issues.First(i => i.Code == IssueCode.AddInPackageNotCached).Severity.Should().Be(ValidationSeverity.Warning);
	}

	[TestMethod]
	[Description("Convention-based add-in discovery threw an exception → AddInDiscoveryFallback")]
	public void WhenDiscoveryFailed_ReportsAddInDiscoveryFallback()
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
			HostPath = "/nuget/uno.winui.devserver/5.5.100/tools/rc/host/net10.0/Host.dll",
			AddInDiscoveryFailed = true,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.AddInDiscoveryFallback);
		issues.First(i => i.Code == IssueCode.AddInDiscoveryFallback).Severity.Should().Be(ValidationSeverity.Warning);
	}

	[TestMethod]
	[Description("No global.json at all → exactly one GlobalJsonNotFound issue, no cascading errors")]
	public void FullDegradedScenario_NoGlobalJson_ReportsExactlyGlobalJsonNotFound()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = null,
			AddInDiscoveryFailed = true, // should be ignored since global.json short-circuits
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.GlobalJsonNotFound);
	}

	[TestMethod]
	[Description("Missing NuGet cache entries produce multiple issues without crashing")]
	public void FullDegradedScenario_MissingNuGetCache_ReportsMultipleIssues()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = null, // Fatal
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
			SettingsPackageVersion = "1.2.3",
			SettingsPackagePath = null, // Warning
			AddInDiscoveryFailed = true, // Warning
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(3);
		issues.Should().Contain(i => i.Code == IssueCode.DevServerPackageNotCached);
		issues.Should().Contain(i => i.Code == IssueCode.AddInPackageNotCached);
		issues.Should().Contain(i => i.Code == IssueCode.AddInDiscoveryFallback);
	}
}
