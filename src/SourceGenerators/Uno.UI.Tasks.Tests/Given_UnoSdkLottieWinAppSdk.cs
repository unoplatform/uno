using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Lottie is built into Uno.WinUI, but WinAppSDK takes LottieVisualSource from CommunityToolkit.WinUI.Lottie: XAML using
/// its namespace on a Windows target without that package gets UNOB0034 instead of only the XAML compiler's WMC0001.
/// </summary>
[TestClass]
public class Given_UnoSdkLottieWinAppSdk
{
	private const string Windows = "net10.0-windows10.0.19041.0";

	private const string LottieXaml = """
		<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:lottie="using:CommunityToolkit.WinUI.Lottie">
			<AnimatedVisualPlayer>
				<lottie:LottieVisualSource UriSource="ms-appx:///Assets/a.json" />
			</AnimatedVisualPlayer>
		</Page>
		""";

	private const string PlainXaml = """
		<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
			<AnimatedVisualPlayer />
		</Page>
		""";

	[TestMethod]
	public void When_Xaml_Uses_Lottie_On_Windows_Without_The_Package_Then_It_Warns()
	{
		var result = RunProject(Windows, LottieXaml, referencesLottie: false);

		result.ExitCode.Should().Be(0, result.Output);
		result.Output.Should().Contain("warning UNOB0034");
		result.Output.Should().Contain("MainPage.xaml");
	}

	[TestMethod]
	[DataRow(Windows, true, true)]
	[DataRow(Windows, false, false)]
	[DataRow("net10.0-desktop", true, false)]
	public void When_The_Package_Is_Referenced_Or_Lottie_Is_Unused_Or_Not_Windows_Then_It_Does_Not_Warn(string targetFramework, bool usesLottie, bool referencesLottie)
	{
		var result = RunProject(targetFramework, usesLottie ? LottieXaml : PlainXaml, referencesLottie);

		result.ExitCode.Should().Be(0, result.Output);
		result.Output.Should().NotContain("UNOB0034");
	}

	private static DotnetMSBuild.Result RunProject(string targetFramework, string xaml, bool referencesLottie)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-lottie-").FullName;
		try
		{
			File.WriteAllText(Path.Join(directory, "MainPage.xaml"), xaml);

			var reference = referencesLottie
				? """<ReferencePath Include="$(MSBuildThisFileDirectory)CommunityToolkit.WinUI.Lottie.dll" />"""
				: "";

			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{targetFramework}</TargetFramework>
						<OutputType>Exe</OutputType>
						<UnoSingleProject>true</UnoSingleProject>
					</PropertyGroup>
					<ItemGroup>
						<Page Include="MainPage.xaml" />
					</ItemGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
					<Target Name="ResolveAssemblyReferences">
						<ItemGroup>
							{reference}
						</ItemGroup>
					</Target>
					<Target Name="MarkupCompilePass1" />
				</Project>
				""");

			// From the repository root, so its global.json picks the SDK.
			return DotnetMSBuild.Run(RepositoryPaths.Root, [projectPath, "-t:MarkupCompilePass1"]);
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}
