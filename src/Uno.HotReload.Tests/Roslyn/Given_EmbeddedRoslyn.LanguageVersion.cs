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
		"Each dev-server host flavor must embed a compiler that accepts the default C# version of the SDK it " +
		"serves (spec 061 G1). The net11 host shipped on Roslyn 5.6 (C# 14) while SDK 11 defaults to C# 15, " +
		"and every hot reload of a project using C# 15 syntax failed.")]
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
		"project, both versions and the LangVersion workaround (spec 061 R5).")]
	public void When_TargetFrameworkDefaultsToNewerCSharp_Then_WarnsOnce()
	{
		var tooNew = EmbeddedRoslyn.MaxCSharpVersion + 1;
		var solution = CreateNetProject(netMajor: tooNew - 4, LanguageVersion.Default);
		var reporter = new RecordingReporter();

		EmbeddedRoslyn.WarnOnUnsupportedLanguageVersions(solution, reporter);

		Assert.AreEqual(1, reporter.Warnings.Count, string.Join(Environment.NewLine, reporter.Warnings));
		var warning = reporter.Warnings[0];
		StringAssert.Contains(warning, "'Test'");
		StringAssert.Contains(warning, $"default C# version is {tooNew}.0");
		StringAssert.Contains(warning, $"supports up to C# {EmbeddedRoslyn.MaxCSharpVersion}.0");
		StringAssert.Contains(warning, $"<LangVersion>{EmbeddedRoslyn.MaxCSharpVersion}.0</LangVersion>");
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
