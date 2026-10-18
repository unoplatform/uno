using System.Diagnostics;
using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// WinAppSDK renders with WinUI, so nothing implies 'skia' on a Windows target. A head that lists the legacy
/// 'SkiaRenderer' token keeps the Skia packages there (#25117), but a WinAppSDK-only library, which keeps its Windows
/// build free of Uno packages, does not take the token as a request for them: only an explicit 'Skia' does. The
/// in-repo Uno.Sdk is imported by path around a project body, the way SamplesApp does it, and only evaluated: no
/// restore, no build.
/// </summary>
[TestClass]
public class Given_UnoSdkRendererFeatures
{
	private const string Uno = "net10.0";
	private const string Windows = "net10.0-windows10.0.19041";

	private static readonly string[] WindowsSkiaPackages = ["Uno.WinUI.Graphics2DSK", "SkiaSharp.Views.WinUI", "Uno.Toolkit.Skia.WinUI"];

	/// <summary>Where a test sets DisableImplicitUnoWinAppSdkPackages.</summary>
	public enum Placement
	{
		/// <summary>In the body of a project that sets TargetFramework.</summary>
		Body,

		/// <summary>In the body of a project with TargetFrameworks, evaluated as an inner build.</summary>
		InnerBuild,

		/// <summary>In a Directory.Build.targets, which the .NET SDK imports after the Uno.Sdk targets.</summary>
		DirectoryBuildTargets,
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void When_Windows_Names_No_Renderer_Then_It_Gets_No_Skia_Package(bool innerBuild)
	{
		var packages = Evaluate(Windows, Features("Toolkit"), innerBuild: innerBuild);

		packages.Should().NotContain(WindowsSkiaPackages);
	}

	[TestMethod]
	[DataRow("WinExe", true)]
	[DataRow("Library", false)]
	public void When_Windows_Lists_SkiaRenderer_Outside_A_WinAppSdk_Only_Library_Then_It_Gets_The_Skia_Packages(string outputType, bool disableImplicitUnoWinAppSdkPackages)
	{
		var packages = Evaluate(Windows, $"""
			<PropertyGroup>
				<OutputType>{outputType}</OutputType>
				<DisableImplicitUnoWinAppSdkPackages>{disableImplicitUnoWinAppSdkPackages}</DisableImplicitUnoWinAppSdkPackages>
			</PropertyGroup>
			{Features("SkiaRenderer;Toolkit")}
			""");

		packages.Should().Contain(WindowsSkiaPackages);
	}

	[TestMethod]
	[DataRow(Placement.Body)]
	[DataRow(Placement.InnerBuild)]
	[DataRow(Placement.DirectoryBuildTargets)]
	public void When_A_WinAppSdk_Only_Library_Lists_SkiaRenderer_Then_Windows_Gets_No_Skia_Package(Placement placement)
	{
		var packages = EvaluateWinAppSdkOnlyLibrary(Windows, placement, "SkiaRenderer;Toolkit");

		packages.Should().NotContain(WindowsSkiaPackages);
		packages.Should().Contain("Uno.Toolkit.WinUI", "the other features still apply");
	}

	[TestMethod]
	[DataRow("Skia;Toolkit")]
	[DataRow("SkiaRenderer;Skia;Toolkit")]
	public void When_A_WinAppSdk_Only_Library_Lists_Skia_Then_Windows_Gets_The_Skia_Packages(string features)
	{
		var packages = EvaluateWinAppSdkOnlyLibrary(Windows, Placement.Body, features);

		packages.Should().Contain(WindowsSkiaPackages);
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("Skia;WebGpu")]
	[DataRow("SkiaRenderer;WebGpu")]
	public void When_An_Uno_Target_Names_Skia_Or_No_Renderer_Then_It_Gets_Skia(string features)
	{
		var packages = Evaluate(Uno, Features(features), innerBuild: true);

		packages.Should().Contain(["Uno.WinUI.Composition.Skia", "Uno.WinUI.Graphics2DSK"]);
	}

	[TestMethod]
	public void When_A_WinAppSdk_Only_Library_Lists_SkiaRenderer_Then_Its_Uno_Targets_Still_Get_Skia()
	{
		var packages = EvaluateWinAppSdkOnlyLibrary(Uno, Placement.InnerBuild, "SkiaRenderer;WebGpu");

		packages.Should().Contain(["Uno.WinUI.Composition.Skia", "Uno.WinUI.Graphics2DSK"]);
	}

	private static string Features(string features) => $"<PropertyGroup><UnoFeatures>{features}</UnoFeatures></PropertyGroup>";

	private static string[] EvaluateWinAppSdkOnlyLibrary(string targetFramework, Placement placement, string features)
	{
		const string Property = "<DisableImplicitUnoWinAppSdkPackages>true</DisableImplicitUnoWinAppSdkPackages>";

		return placement switch
		{
			Placement.Body => Evaluate(targetFramework, $"<PropertyGroup>{Property}</PropertyGroup>{Features(features)}"),
			Placement.InnerBuild => Evaluate(targetFramework, $"<PropertyGroup>{Property}</PropertyGroup>{Features(features)}", innerBuild: true),

			// Libraries usually opt in from a shared file, keyed on a property the project body sets.
			_ => Evaluate(
				targetFramework,
				$"<PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>{Features(features)}",
				directoryBuildTargets: """
					<PropertyGroup>
						<DisableImplicitUnoWinAppSdkPackages Condition="'$(IsPackable)' == 'true'">true</DisableImplicitUnoWinAppSdkPackages>
					</PropertyGroup>
					"""),
		};
	}

	private static string[] Evaluate(string targetFramework, string body, string directoryBuildTargets = "", bool innerBuild = false)
	{
		var directory = Path.Join(Path.GetTempPath(), "uno-sdk-renderer-features", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			// Stop the .NET SDK's upward search for these at the temp directory, whatever sits above it.
			File.WriteAllText(Path.Join(directory, "Directory.Build.props"), "<Project />");
			File.WriteAllText(Path.Join(directory, "Directory.Packages.props"), "<Project />");
			File.WriteAllText(Path.Join(directory, "Directory.Build.targets"), $"<Project>{directoryBuildTargets}</Project>");

			// An inner build of a multi-targeted project gets TargetFramework as a global property, before any of
			// the Uno.Sdk is evaluated; a single-targeted project sets it in the body, after the Uno.Sdk props.
			var targetFrameworks = innerBuild
				? $"<TargetFrameworks>{Uno};{Windows}</TargetFrameworks>"
				: $"<TargetFramework>{targetFramework}</TargetFramework>";

			var projectPath = Path.Join(directory, "Lib.csproj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "Sdk", "Sdk.props")}" />
					<PropertyGroup>
						{targetFrameworks}
						<UnoSingleProject>true</UnoSingleProject>
					</PropertyGroup>
					{body}
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "Sdk", "Sdk.targets")}" />
				</Project>
				""");

			string[] arguments = [projectPath, "-getItem:PackageReference", "-p:EnableWindowsTargeting=true"];
			if (innerBuild)
			{
				arguments = [.. arguments, $"-p:TargetFramework={targetFramework}"];
			}

			var (exitCode, standardOutput, output) = MSBuild(directory, arguments);
			exitCode.Should().Be(0, output);

			using var json = JsonDocument.Parse(standardOutput);
			return json.RootElement.GetProperty("Items").TryGetProperty("PackageReference", out var items)
				? [.. items.EnumerateArray().Select(item => item.GetProperty("Identity").GetString()!)]
				: [];
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static (int ExitCode, string StandardOutput, string Output) MSBuild(string workingDirectory, string[] arguments)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			WorkingDirectory = workingDirectory,
		};
		string[] allArguments = ["msbuild", "-nologo", "-nodeReuse:false", .. arguments];
		foreach (var argument in allArguments)
		{
			startInfo.ArgumentList.Add(argument);
		}
		startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

		using var process = Process.Start(startInfo)!;
		var stdout = process.StandardOutput.ReadToEndAsync();
		var stderr = process.StandardError.ReadToEndAsync();
		process.WaitForExit();

		return (process.ExitCode, stdout.Result, stdout.Result + stderr.Result);
	}
}
