using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.HotReload.Roslyn;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tests.Utils;

namespace Uno.HotReload.Tests.Roslyn;

public sealed partial class Given_EmbeddedRoslyn
{
	[TestMethod]
	[Description(
		"The host flavor matching this test project's target framework must embed a compiler that accepts the " +
		"default C# version of the SDK building it (spec 061 G1). The net11 host shipped on Roslyn 5.6 (C# 14) " +
		"while SDK 11 defaults to C# 15, and every hot reload of a project using C# 15 syntax failed.")]
	public void When_SdkDefaultLanguageVersion_Then_EmbeddedCompilerSupportsIt()
	{
		var sdkMax = typeof(Given_EmbeddedRoslyn).Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.Single(a => a.Key == "SdkMaxSupportedLangVersion")
			.Value;
		Assert.IsFalse(string.IsNullOrEmpty(sdkMax), "MaxSupportedLangVersion was not stamped by the build.");

		var sdkMaxMajor = int.Parse(sdkMax.Split('.')[0], System.Globalization.CultureInfo.InvariantCulture);

		Assert.IsTrue(
			EmbeddedRoslyn.MaxCSharpVersion >= sdkMaxMajor,
			$"The embedded Roslyn {EmbeddedRoslyn.Version.ToString(2)} supports up to C# {EmbeddedRoslyn.MaxCSharpVersion}, " +
			$"but the SDK defaults this target framework to C# {sdkMax}: bump DevServerRoslynVersion in src/Uno.DevServer.Roslyn.props.");
	}

	[TestMethod]
	[Description(
		"A project whose target framework defaults to a C# version newer than the embedded compiler loads " +
		"silently on an older C# (the workspace drops the unparseable /langversion): one warning must name the " +
		"project, both versions and the LangVersion workaround (spec 061 R5). Latest, LatestMajor and Preview " +
		"resolve against the compiler too, so they also mean an older C# here than in the build.")]
	[DataRow(LanguageVersion.Default)]
	[DataRow(LanguageVersion.Latest)]
	[DataRow(LanguageVersion.LatestMajor)]
	[DataRow(LanguageVersion.Preview)]
	public void When_TargetFrameworkDefaultsToNewerCSharp_Then_WarnsOnce(LanguageVersion languageVersion)
	{
		var tooNew = EmbeddedRoslyn.MaxCSharpVersion + 1;
		var solution = CreateNetProject(netMajor: tooNew - 4, languageVersion);
		var reporter = new RecordingReporter();

		EmbeddedRoslyn.WarnOnUnsupportedLanguageVersions(solution, reporter);

		Assert.AreEqual(1, reporter.Warnings.Count, string.Join(Environment.NewLine, reporter.Warnings));
		var warning = reporter.Warnings[0];
		StringAssert.Contains(warning, "'Test'");
		StringAssert.Contains(warning, $"defaults to C# {tooNew}.0");
		StringAssert.Contains(warning, $"supports up to C# {EmbeddedRoslyn.MaxCSharpVersion}.0");
		StringAssert.Contains(warning, $"<LangVersion>{EmbeddedRoslyn.MaxCSharpVersion}.0</LangVersion>");
	}

	[TestMethod]
	[Description(
		"A platform-specific target framework (net11.0-desktop, net11.0-android…) has the same default C# version " +
		"as its base framework.")]
	public void When_PlatformTargetFrameworkDefaultsToNewerCSharp_Then_Warns()
	{
		var netMajor = EmbeddedRoslyn.MaxCSharpVersion + 1 - 4;
		var refPack = Given_TryGetTargetFramework.FakeRefPackPath($"Microsoft.Android.Ref.net{netMajor}.0_36.0", "36.0.42", $"net{netMajor}.0", "Microsoft.Android.dll");
		var project = Given_TryGetTargetFramework.CreateProject([refPack], []);
		var reporter = new RecordingReporter();

		EmbeddedRoslyn.WarnOnUnsupportedLanguageVersions(project.Solution, reporter);

		Assert.AreEqual(1, reporter.Warnings.Count, string.Join(Environment.NewLine, reporter.Warnings));
		StringAssert.Contains(reporter.Warnings[0], $"net{netMajor}.0-android");
	}

	[TestMethod]
	[Description(
		"The blocked-reload line of a project on a too-new C# version is followed by a hint saying its errors " +
		"are not build errors (spec 061 R5), and no hint is produced for a supported project.")]
	public void When_UnsupportedLanguageVersion_Then_BlockedHintNamesBothVersions()
	{
		var tooNew = EmbeddedRoslyn.MaxCSharpVersion + 1;
		var unsupported = CreateNetProject(netMajor: tooNew - 4, LanguageVersion.Default).Projects.Single();
		var supported = CreateNetProject(netMajor: tooNew - 5, LanguageVersion.Default).Projects.Single();

		var hint = EmbeddedRoslyn.GetUnsupportedLanguageVersionHint(unsupported);

		Assert.IsNotNull(hint);
		StringAssert.Contains(hint, $"'Test' defaults to C# {tooNew}.0");
		StringAssert.Contains(hint, $"supports up to C# {EmbeddedRoslyn.MaxCSharpVersion}.0");
		Assert.IsNull(EmbeddedRoslyn.GetUnsupportedLanguageVersionHint(supported));
	}

	[TestMethod]
	[Description("A target framework whose default C# version the embedded compiler supports must not warn.")]
	public void When_TargetFrameworkDefaultIsSupported_Then_NoWarning()
	{
		var solution = CreateNetProject(netMajor: EmbeddedRoslyn.MaxCSharpVersion - 4, LanguageVersion.Default);
		var reporter = new RecordingReporter();

		EmbeddedRoslyn.WarnOnUnsupportedLanguageVersions(solution, reporter);

		Assert.AreEqual(0, reporter.Warnings.Count, string.Join(Environment.NewLine, reporter.Warnings));
	}

	[TestMethod]
	[Description(
		"An explicit LangVersion the embedded compiler can parse is honored as-is, so the documented " +
		"workaround (pinning an older LangVersion) must silence the warning.")]
	public void When_ExplicitSupportedLangVersion_Then_NoWarning()
	{
		var solution = CreateNetProject(netMajor: EmbeddedRoslyn.MaxCSharpVersion - 3, LanguageVersion.CSharp12);
		var reporter = new RecordingReporter();

		EmbeddedRoslyn.WarnOnUnsupportedLanguageVersions(solution, reporter);

		Assert.AreEqual(0, reporter.Warnings.Count, string.Join(Environment.NewLine, reporter.Warnings));
	}

	private static Solution CreateNetProject(int netMajor, LanguageVersion languageVersion)
	{
		var refPack = Given_TryGetTargetFramework.FakeRefPackPath("Microsoft.NETCore.App.Ref", $"{netMajor}.0.0", $"net{netMajor}.0", "System.Runtime.dll");
		var project = Given_TryGetTargetFramework.CreateProject([refPack], []);

		return project.Solution.WithProjectParseOptions(project.Id, new CSharpParseOptions(languageVersion));
	}
}
