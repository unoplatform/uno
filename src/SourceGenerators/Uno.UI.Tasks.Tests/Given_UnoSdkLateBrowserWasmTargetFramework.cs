using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Sdk.props picks the WebAssembly SDK from a global TargetFramework only (-f, or the inner build of TargetFrameworks).
/// A head that sets a single browserwasm TargetFramework itself is moved to TargetFrameworks, so it builds as an outer
/// build whose inner build gets the WebAssembly SDK. A head that can't be moved fails with UNOB0032 instead of silently
/// building as a desktop executable.
/// </summary>
[TestClass]
public class Given_UnoSdkLateBrowserWasmTargetFramework
{
	private const string BrowserWasm = "net10.0-browserwasm";
	private const string Desktop = "net10.0-desktop";

	[TestMethod]
	[DataRow("Exe", BrowserWasm, false, true)]
	[DataRow("WinExe", BrowserWasm, false, true)]
	[DataRow("Exe", BrowserWasm, true, false)]
	[DataRow("Library", BrowserWasm, false, false)]
	[DataRow("Exe", Desktop, false, false)]
	public void When_A_Head_Sets_A_Single_BrowserWasm_TargetFramework_Then_It_Is_Moved_To_TargetFrameworks(string outputType, string targetFramework, bool isGlobal, bool expected)
	{
		var properties = Evaluate(new() { OutputType = outputType, TargetFramework = targetFramework, IsGlobal = isGlobal });

		if (expected)
		{
			properties["TargetFramework"].Should().BeEmpty();
			properties["TargetFrameworks"].Should().Be(targetFramework);
			properties["IsCrossTargetingBuild"].Should().Be("true");
		}
		else
		{
			properties["TargetFramework"].Should().Be(targetFramework);
			properties["TargetFrameworks"].Should().BeEmpty();
			properties["IsCrossTargetingBuild"].Should().BeEmpty();
		}
	}

	[TestMethod]
	public void When_A_Head_Also_Sets_TargetFrameworks_Then_Only_The_BrowserWasm_TargetFramework_Is_Kept()
	{
		// The .NET SDK builds only the TargetFramework when both are set, so the moved one replaces the list.
		var properties = Evaluate(new() { OutputType = "Exe", TargetFramework = BrowserWasm, BodyProperties = $"<TargetFrameworks>{Desktop};{BrowserWasm}</TargetFrameworks>" });

		properties["TargetFramework"].Should().BeEmpty();
		properties["TargetFrameworks"].Should().Be(BrowserWasm);
	}

	[TestMethod]
	public void When_The_TargetFramework_Is_Moved_Then_The_Inner_Build_Uses_The_WebAssembly_Sdk()
	{
		// The inner build of the moved TargetFramework, as the outer build dispatches it.
		var properties = Evaluate(new() { OutputType = "Exe", TargetFramework = BrowserWasm, IsGlobal = true });

		properties["_DefaultMicrosoftNETSdk"].Should().Be("Microsoft.NET.Sdk.WebAssembly");
	}

	[TestMethod]
	public void When_The_TargetFramework_Is_Moved_Then_The_Outer_Build_Computes_The_Inner_Run_Command()
	{
		// A file-based `dotnet run app.cs` asks the outer build. On the plain SDK, the run command would be the apphost.
		var properties = Evaluate(new() { OutputType = "Exe", TargetFramework = BrowserWasm }, ["-t:ComputeRunArguments"], "RunCommand");

		properties["RunCommand"].Should().Be("dotnet");
	}

	[TestMethod]
	[DataRow(BrowserWasm, false, "false")]
	[DataRow(BrowserWasm, true, "false")]
	[DataRow(Desktop, false, "true")]
	public void When_A_File_Based_App_Targets_BrowserWasm_Then_PublishAot_Is_Off(string targetFramework, bool isGlobal, string expected)
	{
		var properties = Evaluate(new() { OutputType = "Exe", TargetFramework = targetFramework, IsGlobal = isGlobal, BodyProperties = "<FileBasedProgram>true</FileBasedProgram><PublishAot>true</PublishAot>" }, [], "PublishAot");

		properties["PublishAot"].Should().Be(expected);
	}

	[TestMethod]
	[DataRow("Exe", BrowserWasm, false, false, true)]
	[DataRow("WinExe", BrowserWasm, false, false, true)]
	[DataRow("Exe", BrowserWasm, false, true, false)]
	[DataRow("Exe", BrowserWasm, true, false, false)]
	[DataRow("Library", BrowserWasm, false, false, false)]
	[DataRow("Exe", Desktop, false, false, false)]
	public void When_A_Head_Is_Only_Known_After_The_Project_Body_Then_A_BrowserWasm_TargetFramework_Is_An_Error(string outputType, string targetFramework, bool isGlobal, bool inBody, bool expected)
	{
		// An OutputType after the SDK targets stands for one set in Directory.Build.targets, too late to move the TargetFramework.
		var result = RunProject(new() { OutputType = outputType, TargetFramework = targetFramework, IsGlobal = isGlobal, OutputTypeAfterSdk = !inBody }, ["-t:UnoFailOnLateBrowserWasmTargetFramework"]);

		if (expected)
		{
			result.ExitCode.Should().NotBe(0, result.Output);
			result.Output.Should().Contain("error UNOB0032");
		}
		else
		{
			result.ExitCode.Should().Be(0, result.Output);
			result.Output.Should().NotContain("UNOB0032");
		}
	}

	private record Project
	{
		public required string OutputType { get; init; }

		public required string TargetFramework { get; init; }

		/// <summary>Passes the TargetFramework as a global property too, as -f or an outer build does.</summary>
		public bool IsGlobal { get; init; }

		public bool OutputTypeAfterSdk { get; init; }

		public string BodyProperties { get; init; } = "";
	}

	private static Dictionary<string, string> Evaluate(Project project, string[]? arguments = null, params string[] names)
	{
		names = names is [] ? ["TargetFramework", "TargetFrameworks", "IsCrossTargetingBuild", "_DefaultMicrosoftNETSdk"] : names;

		var result = RunProject(project, [.. arguments ?? [], .. names.Select(name => $"-getProperty:{name}")]);
		result.ExitCode.Should().Be(0, result.Output);

		// A single -getProperty prints the bare value rather than JSON.
		if (names is [var name])
		{
			return new() { [name] = result.StandardOutput.Trim() };
		}

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
			var properties = json.RootElement.GetProperty("Properties");
			return names.ToDictionary(name => name, name => properties.GetProperty(name).GetString()!);
		}
	}

	private static DotnetMSBuild.Result RunProject(Project project, string[] arguments)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-browserwasm-").FullName;
		try
		{
			var outputType = $"<PropertyGroup><OutputType>{project.OutputType}</OutputType></PropertyGroup>";

			// Imports the SDK files the way an MSBuild SDK does, so the project body is evaluated between them.
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "Sdk", "Sdk.props")}" />
					<PropertyGroup>
						<TargetFramework>{project.TargetFramework}</TargetFramework>
						<UnoSingleProject>true</UnoSingleProject>
						{project.BodyProperties}
					</PropertyGroup>
					{(project.OutputTypeAfterSdk ? "" : outputType)}
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "Sdk", "Sdk.targets")}" />
					{(project.OutputTypeAfterSdk ? outputType : "")}
				</Project>
				""");

			string[] global = project.IsGlobal ? [$"-p:TargetFramework={project.TargetFramework}"] : [];

			// From the repository root, so its global.json picks the SDK.
			return DotnetMSBuild.Run(RepositoryPaths.Root, [projectPath, .. global, .. arguments]);
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}
