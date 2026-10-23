using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// The renderer is the app's choice: a library that implied 'skia' (or mapped the legacy 'SkiaRenderer' to it)
/// would make SkiaSharp a dependency of every app using it, a WebGPU-only one included.
/// </summary>
[TestClass]
public class Given_UnoSdkRendererFeature
{
	private const string Windows = "net10.0-windows10.0.19041.0";

	private static readonly string[] SkiaPackages = ["Uno.WinUI.Composition.Skia", "Uno.WinUI.Graphics2DSK", "SkiaSharp.Views.WinUI", "Uno.Toolkit.Skia.WinUI"];

	[TestMethod]
	[DataRow("", "net10.0")]
	[DataRow("SkiaRenderer", "net10.0")]
	[DataRow("SkiaRenderer;Toolkit", "net10.0")]
	[DataRow("SkiaRenderer", "net10.0-desktop")]
	[DataRow("SkiaRenderer", Windows)]
	public void When_Library_Does_Not_List_Skia_Then_Skia_Is_Not_Referenced(string features, string targetFramework)
	{
		var (unoFeatures, packages) = Evaluate("Library", features, targetFramework);

		unoFeatures.Should().NotContain(";skia;");
		packages.Should().NotContain(SkiaPackages);
		packages.Should().Contain("Uno.WinUI");
	}

	[TestMethod]
	[DataRow("Skia", "net10.0", new[] { "Uno.WinUI.Composition.Skia", "Uno.WinUI.Graphics2DSK" })]
	[DataRow("Skia;Toolkit", "net10.0", new[] { "Uno.WinUI.Composition.Skia", "Uno.WinUI.Graphics2DSK", "Uno.Toolkit.Skia.WinUI" })]
	[DataRow("Skia", Windows, new[] { "Uno.WinUI.Graphics2DSK", "SkiaSharp.Views.WinUI" })]
	public void When_Library_Lists_Skia_Then_Skia_Is_Referenced(string features, string targetFramework, string[] expected)
	{
		var (unoFeatures, packages) = Evaluate("Library", features, targetFramework);

		unoFeatures.Should().Contain(";skia;");
		packages.Should().Contain(expected);
	}

	[TestMethod]
	[DataRow("Exe", "net10.0", true)]
	[DataRow("WinExe", "net10.0", true)]
	[DataRow("Exe", "net10.0-desktop", true)]
	[DataRow("Exe", "net10.0-android", true)]
	[DataRow("Exe", "net10.0", false)]
	public void When_Head_Names_No_Renderer_Then_Skia_Is_Implied(string outputType, string targetFramework, bool singleProject)
	{
		var (unoFeatures, packages) = Evaluate(outputType, "", targetFramework, singleProject);

		unoFeatures.Should().Contain(";skia;");
		packages.Should().Contain(["Uno.WinUI.Composition.Skia", "Uno.WinUI.Graphics2DSK"]);
	}

	[TestMethod]
	[DataRow("SkiaRenderer", new[] { "Uno.WinUI.Composition.Skia" })]
	[DataRow("SkiaRenderer;WebGpu", new[] { "Uno.WinUI.Composition.Skia", "Uno.WinUI.Composition.WebGpu" })]
	public void When_Head_Lists_SkiaRenderer_Then_It_Maps_To_Skia(string features, string[] expected)
	{
		var (unoFeatures, packages) = Evaluate("Exe", features);

		unoFeatures.Should().Contain(";skia;");
		packages.Should().Contain(expected);
	}

	[TestMethod]
	[DataRow("net10.0", false)]
	[DataRow("net10.0-desktop", true)]
	public void When_OutputType_Is_Exe_On_Some_Target_Frameworks_Only_Then_The_Others_Are_Libraries(string targetFramework, bool isHead)
	{
		var (unoFeatures, _) = Evaluate("Exe", "", targetFramework, outputTypeCondition: "'$(TargetFramework)' != 'net10.0'");

		if (isHead)
		{
			unoFeatures.Should().Contain(";skia;");
		}
		else
		{
			unoFeatures.Should().NotContain(";skia;");
		}
	}

	[TestMethod]
	public void When_Head_Lists_WebGpu_Only_Then_Skia_Is_Not_Referenced()
	{
		var (unoFeatures, packages) = Evaluate("Exe", "WebGpu");

		unoFeatures.Should().NotContain(";skia;");
		packages.Should().NotContain(SkiaPackages);
		packages.Should().Contain("Uno.WinUI.Composition.WebGpu");
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("WebGpuCanvas")]
	public void When_Windows_Head_Sets_TargetFramework_In_Its_Body_Then_No_Renderer_Is_Implied(string features)
	{
		var (unoFeatures, packages) = Evaluate("WinExe", features, Windows);

		unoFeatures.Should().NotContain(";skia;").And.NotContain(";webgpu;");
		packages.Should().NotContain(SkiaPackages);
	}

	[TestMethod]
	public void When_Head_Lists_WebGpuCanvas_Then_WebGpu_Is_Added_Next_To_Skia()
	{
		var (unoFeatures, packages) = Evaluate("Exe", "WebGpuCanvas");

		unoFeatures.Should().Contain(";skia;").And.Contain(";webgpu;");
		packages.Should().Contain(["Uno.WinUI.Composition.Skia", "Uno.WinUI.Composition.WebGpu", "Uno.WinUI.Graphics3DWebGpu"]);
	}

	[TestMethod]
	public void When_Library_Lists_WebGpuCanvas_Then_Only_WebGpu_Is_Added()
	{
		var (unoFeatures, packages) = Evaluate("Library", "WebGpuCanvas");

		unoFeatures.Should().Contain(";webgpu;").And.NotContain(";skia;");
		packages.Should().NotContain(SkiaPackages);
		packages.Should().Contain(["Uno.WinUI.Composition.WebGpu", "Uno.WinUI.Graphics3DWebGpu"]);
	}

	[TestMethod]
	[DataRow("Library", "SkiaRenderer", true)]
	[DataRow("Library", "Skia;SkiaRenderer", true)]
	[DataRow("Library", "Skia", false)]
	[DataRow("Exe", "SkiaRenderer", false)]
	public void When_Compiling_Then_An_Ignored_SkiaRenderer_Is_Reported(string outputType, string features, bool expected)
	{
		var output = Compile(outputType, features);

		if (expected)
		{
			output.Should().Contain("warning UNOB0030");
		}
		else
		{
			output.Should().NotContain("UNOB0030");
		}
	}

	private static (string UnoFeatures, string[] Packages) Evaluate(string outputType, string features, string targetFramework = "net10.0", bool singleProject = true, string outputTypeCondition = "")
	{
		var result = RunProject(outputType, features, targetFramework, singleProject, ["-getProperty:UnoFeatures", "-getItem:PackageReference"], outputTypeCondition);

		JsonDocument json;
		try
		{
			json = JsonDocument.Parse(result.StandardOutput);
		}
		catch (JsonException e)
		{
			throw new InvalidOperationException($"dotnet msbuild did not print JSON:{Environment.NewLine}{result.Output}", e);
		}

		using (json)
		{
			var unoFeatures = json.RootElement.GetProperty("Properties").GetProperty("UnoFeatures").GetString()!;
			var packages = json.RootElement.GetProperty("Items").GetProperty("PackageReference")
				.EnumerateArray()
				.Select(item => item.GetProperty("Identity").GetString()!)
				.ToArray();

			return (unoFeatures, packages);
		}
	}

	private static string Compile(string outputType, string features)
		=> RunProject(outputType, features, "net10.0", singleProject: true, ["-t:CoreCompile"]).Output;

	private static DotnetMSBuild.Result RunProject(string outputType, string features, string targetFramework, bool singleProject, string[] arguments, string outputTypeCondition = "")
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-renderer-").FullName;
		try
		{
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{targetFramework}</TargetFramework>
						<OutputType Condition="{outputTypeCondition}">{outputType}</OutputType>
						<UnoSingleProject>{singleProject}</UnoSingleProject>
						<UnoFeatures>{features}</UnoFeatures>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
					<Target Name="CoreCompile" />
				</Project>
				""");

			// From the repository root, so its global.json picks the SDK.
			var result = DotnetMSBuild.Run(RepositoryPaths.Root, [projectPath, .. arguments]);

			result.ExitCode.Should().Be(0, result.Output);
			return result;
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}
