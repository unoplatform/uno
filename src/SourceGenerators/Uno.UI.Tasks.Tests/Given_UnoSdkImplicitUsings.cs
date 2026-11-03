﻿using System.Diagnostics;
using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// The Uno.Sdk's implicit usings must be removable from the project body, as the .NET SDK's are (#18525), and must
/// not name a namespace the target framework does not get. The in-repo Uno.Sdk is imported by path around a project
/// body, the way SamplesApp does it, and only evaluated: no restore, no build.
/// </summary>
[TestClass]
public class Given_UnoSdkImplicitUsings
{
	private const string Uno = "net10.0";
	private const string Windows = "net10.0-windows10.0.19041";

	private static readonly string[] UnoUsings =
	[
		"System.Windows.Input",
		"Windows.Networking.Connectivity",
		"Windows.Storage",
		"Microsoft.UI.Xaml",
		"Microsoft.UI.Xaml.Controls",
		"Microsoft.UI.Xaml.Media",
		"Microsoft.UI.Xaml.Navigation",
		"Uno.UI",
		"Windows.ApplicationModel",
	];

	/// <summary>Where a test sets its properties.</summary>
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
	[DataRow("enable")]
	[DataRow("true")]
	public void When_ImplicitUsings_Is_Enabled_Then_The_Uno_Usings_Are_Added(string implicitUsings)
	{
		var project = Evaluate(Uno, $"<PropertyGroup><ImplicitUsings>{implicitUsings}</ImplicitUsings></PropertyGroup>");

		project.UnoUsings.Should().BeEquivalentTo(UnoUsings);
	}

	[TestMethod]
	public void When_ImplicitUsings_Is_Disabled_Then_No_Uno_Using_Is_Added()
	{
		var project = Evaluate(Uno, "<PropertyGroup><ImplicitUsings>disable</ImplicitUsings></PropertyGroup>");

		project.Usings.Should().BeEmpty();
	}

	[TestMethod]
	[DataRow(Uno)]
	[DataRow(Windows)]
	public void When_The_Project_Removes_An_Uno_Using_Then_It_Is_Not_Added(string targetFramework)
	{
		var project = Evaluate(targetFramework, """
			<PropertyGroup><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
			<ItemGroup>
				<Using Remove="Uno.UI" />
				<Using Remove="Microsoft.UI.Xaml.Navigation" />
			</ItemGroup>
			""");

		project.UnoUsings.Should().BeEquivalentTo(UnoUsings.Except(["Uno.UI", "Microsoft.UI.Xaml.Navigation"]));
	}

	[TestMethod]
	[DataRow(Placement.Body)]
	[DataRow(Placement.DirectoryBuildTargets)]
	public void When_DisableImplicitUnoNamespaces_Is_Set_Then_Only_The_Uno_Usings_Are_Dropped(Placement placement)
	{
		var project = Evaluate(Uno, placement, """
			<PropertyGroup>
				<ImplicitUsings>enable</ImplicitUsings>
				<DisableImplicitUnoNamespaces>true</DisableImplicitUnoNamespaces>
			</PropertyGroup>
			""");

		project.UnoUsings.Should().BeEmpty();
		project.Usings.Should().Contain("System.Linq");
	}

	[TestMethod]
	[DataRow(Placement.Body)]
	[DataRow(Placement.InnerBuild)]
	[DataRow(Placement.DirectoryBuildTargets)]
	public void When_A_WinAppSdk_Only_Library_Targets_Windows_Then_It_Gets_No_Uno_WinUI_Or_Uno_UI_Using(Placement placement)
	{
		var project = placement == Placement.DirectoryBuildTargets
			// Libraries usually opt in from a shared file, keyed on a property the project body sets.
			? Evaluate(
				Windows,
				"<PropertyGroup><IsPackable>true</IsPackable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>",
				directoryBuildTargets: """
					<PropertyGroup>
						<DisableImplicitUnoWinAppSdkPackages Condition="'$(IsPackable)' == 'true'">true</DisableImplicitUnoWinAppSdkPackages>
					</PropertyGroup>
					""")
			: Evaluate(Windows, placement, WinAppSdkOnly("Library"));

		project.ImplicitPackages.Should().NotContain("Uno.WinUI");
		project.UnoUsings.Should().BeEquivalentTo(UnoUsings.Except(["Uno.UI"]));
	}

	[TestMethod]
	[DataRow(Uno, "Library")]
	[DataRow(Windows, "WinExe")]
	public void When_DisableImplicitUnoWinAppSdkPackages_Does_Not_Apply_Then_The_Uno_UI_Using_Stays(string targetFramework, string outputType)
	{
		var project = Evaluate(targetFramework, WinAppSdkOnly(outputType));

		project.ImplicitPackages.Should().Contain("Uno.WinUI");
		project.UnoUsings.Should().Contain("Uno.UI");
	}

	[TestMethod]
	public void When_UnoFeatures_Are_Left_Out_Of_Windows_Then_Windows_Gets_None_Of_Their_Packages()
	{
		var project = Evaluate(Windows, ToolkitExceptOnWindows);

		project.ImplicitPackages.Should().NotContain("Uno.Toolkit.WinUI");
	}

	[TestMethod]
	public void When_UnoFeatures_Are_Left_Out_Of_Windows_Then_Other_Targets_Keep_Their_Packages()
	{
		var project = Evaluate(Uno, ToolkitExceptOnWindows);

		project.ImplicitPackages.Should().Contain("Uno.Toolkit.WinUI");
	}

	[TestMethod]
	public void When_UnoFeatures_Apply_To_Windows_Then_Their_Packages_Are_Added_There()
	{
		var project = Evaluate(Windows, "<PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup>");

		project.ImplicitPackages.Should().Contain("Uno.Toolkit.WinUI");
	}

	private const string ToolkitExceptOnWindows = """
		<PropertyGroup>
			<UnoFeatures>Toolkit</UnoFeatures>
			<UnoFeatures Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'"></UnoFeatures>
		</PropertyGroup>
		""";

	private static string WinAppSdkOnly(string outputType) => $"""
		<PropertyGroup>
			<OutputType>{outputType}</OutputType>
			<ImplicitUsings>enable</ImplicitUsings>
			<DisableImplicitUnoWinAppSdkPackages>true</DisableImplicitUnoWinAppSdkPackages>
		</PropertyGroup>
		""";

	private static EvaluatedProject Evaluate(string targetFramework, Placement placement, string properties)
		=> placement switch
		{
			Placement.Body => Evaluate(targetFramework, properties),
			Placement.InnerBuild => Evaluate(targetFramework, properties, innerBuild: true),
			_ => Evaluate(targetFramework, body: "", directoryBuildTargets: properties),
		};

	private static EvaluatedProject Evaluate(string targetFramework, string body, string directoryBuildTargets = "", bool innerBuild = false)
	{
		var directory = Path.Join(Path.GetTempPath(), "uno-sdk-implicit-usings", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			// Stop the .NET SDK's upward search for these at the temp directory, whatever sits above it.
			File.WriteAllText(Path.Join(directory, "Directory.Build.props"), "<Project />");
			File.WriteAllText(Path.Join(directory, "Directory.Packages.props"), "<Project />");
			File.WriteAllText(Path.Join(directory, "Directory.Build.targets"), $"<Project>{directoryBuildTargets}</Project>");

			// An inner build of a multi-targeted project gets TargetFramework as a global property; a single-targeted
			// project sets it in the body.
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

			string[] arguments = [projectPath, "-getItem:Using", "-getItem:_UnoProjectSystemPackageReference", "-p:EnableWindowsTargeting=true"];
			if (innerBuild)
			{
				arguments = [.. arguments, $"-p:TargetFramework={targetFramework}"];
			}

			var (exitCode, output) = MSBuild(directory, arguments);
			exitCode.Should().Be(0, output);

			using var json = JsonDocument.Parse(output);
			var items = json.RootElement.GetProperty("Items");
			return new(
				Usings: Identities(items, "Using", _ => true),
				UnoUsings: Identities(items, "Using", item => item.TryGetProperty("Sdk", out var sdk) && sdk.GetString() == "Uno"),
				ImplicitPackages: Identities(items, "_UnoProjectSystemPackageReference", _ => true));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static string[] Identities(JsonElement items, string itemType, Func<JsonElement, bool> filter)
		=> items.TryGetProperty(itemType, out var list)
			? [.. list.EnumerateArray().Where(filter).Select(item => item.GetProperty("Identity").GetString()!)]
			: [];

	private static (int ExitCode, string Output) MSBuild(string workingDirectory, string[] arguments)
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

		return (process.ExitCode, stdout.Result + stderr.Result);
	}

	private sealed record EvaluatedProject(string[] Usings, string[] UnoUsings, string[] ImplicitPackages);
}
