using System;
using System.IO;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests;

[TestClass]
public class Given_ProjectAssetsParser
{
	private string _root = null!;

	[TestInitialize]
	public void TestInitialize()
	{
		_root = Path.Join(Path.GetTempPath(), "uno-tests-assets-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best effort: a leftover temp directory does not affect other tests.
		}
	}

	private string FullPath(string relative) => Path.Join(_root, relative.Replace('/', Path.DirectorySeparatorChar));

	private string Write(string relative, string content)
	{
		var path = FullPath(relative);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
		return path;
	}

	private string Restore(string projectRelative)
	{
		Write(projectRelative, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
		return Write(Path.Join(Path.GetDirectoryName(projectRelative), "obj", "project.assets.json"), """{ "version": 3, "libraries": {} }""");
	}

	private string WriteSlnx(string relative, params string[] projectPaths)
		=> Write(relative, "<Solution>" + string.Concat(projectPaths.Select(p => $"<Project Path=\"{p}\" />")) + "</Solution>");

	[TestMethod]
	[Description("Issue #24311: the app's assets sit at src/MyApp/obj, two levels below the workspace root, so the " +
		"folder scan misses them and Uno.UI.App.Mcp never reaches --addins. The solution names the project.")]
	public async Task When_ProjectIsTwoLevelsBelowTheWorkspace_Then_SolutionFindsItsAssets()
	{
		var assets = Restore("src/MyApp/MyApp.csproj");
		var slnx = WriteSlnx("src/MyApp.slnx", "MyApp/MyApp.csproj");

		ProjectAssetsParser.FindProjectAssetsFiles(_root, NullLogger.Instance).Should().BeEmpty();

		var found = await ProjectAssetsParser.FindProjectAssetsFilesAsync(slnx, _root, NullLogger.Instance);

		found.Should().Equal(assets);
	}

	[TestMethod]
	[Description("Issue #25524 layout: a library sits within the folder scan, the selected app solution four levels " +
		"down. Only the selected solution's assets are read, so the library's versions do not leak in.")]
	public async Task When_ASolutionIsSelected_Then_AssetsOutsideItAreNotRead()
	{
		Restore("Lib/Lib.csproj");
		var app = Restore("a/b/c/d/App/App.csproj");
		var sln = WriteSlnx("a/b/c/d/App.slnx", "App/App.csproj");

		var found = await ProjectAssetsParser.FindProjectAssetsFilesAsync(sln, _root, NullLogger.Instance);

		found.Should().Equal(app);
	}

	[TestMethod]
	public async Task When_SolutionIsSln_Then_BackslashPathsAndSiblingFoldersResolve()
	{
		var app = Restore("src/App/App.csproj");
		var shared = Restore("shared/Shared/Shared.csproj");
		var sln = Write("src/App.sln", """
			Microsoft Visual Studio Solution File, Format Version 12.00
			# Visual Studio Version 17
			Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App", "App\App.csproj", "{11111111-1111-1111-1111-111111111111}"
			EndProject
			Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Shared", "..\shared\Shared\Shared.csproj", "{22222222-2222-2222-2222-222222222222}"
			EndProject
			Global
			EndGlobal
			""");

		var found = await ProjectAssetsParser.FindProjectAssetsFilesFromSolutionAsync(sln, NullLogger.Instance);

		found.Should().Equal(app, shared);
	}

	[TestMethod]
	public async Task When_SolutionProjectsAreNotRestored_Then_FallsBackToTheFolderScan()
	{
		Write("src/MyApp/MyApp.csproj", "<Project />");
		var slnx = WriteSlnx("src/MyApp.slnx", "MyApp/MyApp.csproj");
		var rootAssets = Restore("Tool/Tool.csproj");

		var found = await ProjectAssetsParser.FindProjectAssetsFilesAsync(slnx, _root, NullLogger.Instance);

		found.Should().Equal(rootAssets);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("missing.slnx")]
	[DataRow("broken.slnx")]
	public async Task When_SolutionIsMissingOrUnreadable_Then_FallsBackWithoutThrowing(string? solution)
	{
		var rootAssets = Restore("App/App.csproj");
		Write("broken.slnx", "<Solution><Project");
		var solutionPath = string.IsNullOrEmpty(solution) ? solution : FullPath(solution);

		var found = await ProjectAssetsParser.FindProjectAssetsFilesAsync(solutionPath, _root, NullLogger.Instance);

		found.Should().Equal(rootAssets);
	}

	[TestMethod]
	public async Task When_TwoProjectsShareAFolder_Then_TheirAssetsAreReadOnce()
	{
		var app = Restore("App/App.csproj");
		Write("App/App.Other.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
		var slnx = WriteSlnx("App.slnx", "App/App.csproj", "App/App.Other.csproj");

		var found = await ProjectAssetsParser.FindProjectAssetsFilesFromSolutionAsync(slnx, NullLogger.Instance);

		found.Should().Equal(app);
	}
}
