using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class ProjectAssetsParserTests
{
	private static ILogger _logger = null!;
	private string _tempDir = null!;

	[ClassInitialize]
	public static void ClassInitialize(TestContext _)
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
			builder.SetMinimumLevel(LogLevel.Trace);
		});
		_logger = loggerFactory.CreateLogger<ProjectAssetsParserTests>();
	}

	[TestInitialize]
	public void TestInitialize()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "ProjectAssetsParserTests_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		try
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, recursive: true);
			}
		}
		catch
		{
			// Best effort cleanup
		}
	}

	// -------------------------------------------------------------------
	// ParseLibraries tests
	// -------------------------------------------------------------------

	[TestMethod]
	public void ParseLibraries_ValidFile_ExtractsPackages()
	{
		// Arrange
		var assetsPath = CreateProjectAssetsJson(_tempDir,
			("Uno.UI.App.Mcp", "6.5.100"),
			("Newtonsoft.Json", "13.0.3"),
			("Uno.Settings.DevServer", "1.2.3"));

		// Act
		var result = ProjectAssetsParser.ParseLibraries(assetsPath, _logger);

		// Assert
		result.Should().HaveCount(3);
		result.Should().Contain(("Uno.UI.App.Mcp", "6.5.100"));
		result.Should().Contain(("Newtonsoft.Json", "13.0.3"));
		result.Should().Contain(("Uno.Settings.DevServer", "1.2.3"));
	}

	[TestMethod]
	public void ParseLibraries_FiltersProjectReferences()
	{
		// Arrange - mix of package and project references
		var assetsPath = Path.Combine(_tempDir, "project.assets.json");
		File.WriteAllText(assetsPath, """
		{
			"version": 3,
			"libraries": {
				"Newtonsoft.Json/13.0.3": {
					"type": "package",
					"path": "newtonsoft.json/13.0.3"
				},
				"MyApp.Core/1.0.0": {
					"type": "project",
					"path": "../MyApp.Core/MyApp.Core.csproj"
				},
				"Uno.UI/6.0.0": {
					"type": "package",
					"path": "uno.ui/6.0.0"
				}
			}
		}
		""");

		// Act
		var result = ProjectAssetsParser.ParseLibraries(assetsPath, _logger);

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(("Newtonsoft.Json", "13.0.3"));
		result.Should().Contain(("Uno.UI", "6.0.0"));
		result.Should().NotContain(r => r.packageName == "MyApp.Core");
	}

	[TestMethod]
	public void ParseLibraries_MalformedJson_ReturnsEmpty()
	{
		// Arrange
		var assetsPath = Path.Combine(_tempDir, "project.assets.json");
		File.WriteAllText(assetsPath, "this is { not valid json <<<");

		// Act
		var result = ProjectAssetsParser.ParseLibraries(assetsPath, _logger);

		// Assert
		result.Should().BeEmpty();
	}

	[TestMethod]
	public void ParseLibraries_MissingLibraries_ReturnsEmpty()
	{
		// Arrange - valid JSON but no "libraries" key
		var assetsPath = Path.Combine(_tempDir, "project.assets.json");
		File.WriteAllText(assetsPath, """
		{
			"version": 3,
			"targets": {}
		}
		""");

		// Act
		var result = ProjectAssetsParser.ParseLibraries(assetsPath, _logger);

		// Assert
		result.Should().BeEmpty();
	}

	[TestMethod]
	public void ParseLibraries_MalformedKey_SkipsEntry()
	{
		// Arrange - a key without "/" separator
		var assetsPath = Path.Combine(_tempDir, "project.assets.json");
		File.WriteAllText(assetsPath, """
		{
			"version": 3,
			"libraries": {
				"ValidPackage/1.0.0": {
					"type": "package",
					"path": "validpackage/1.0.0"
				},
				"MalformedKeyNoSlash": {
					"type": "package",
					"path": "malformed"
				}
			}
		}
		""");

		// Act
		var result = ProjectAssetsParser.ParseLibraries(assetsPath, _logger);

		// Assert
		result.Should().HaveCount(1);
		result.Should().Contain(("ValidPackage", "1.0.0"));
	}

	// -------------------------------------------------------------------
	// FindProjectAssetsFiles tests
	// -------------------------------------------------------------------

	[TestMethod]
	public void FindProjectAssetsFiles_InRootObj_Finds()
	{
		// Arrange - {dir}/obj/project.assets.json
		var objDir = Path.Combine(_tempDir, "obj");
		Directory.CreateDirectory(objDir);
		File.WriteAllText(Path.Combine(objDir, "project.assets.json"), "{}");

		// Act
		var result = ProjectAssetsParser.FindProjectAssetsFiles(_tempDir, _logger);

		// Assert
		result.Should().HaveCount(1);
		result[0].Should().Contain(Path.Combine("obj", "project.assets.json"));
	}

	[TestMethod]
	public void FindProjectAssetsFiles_InSubdirObj_Finds()
	{
		// Arrange - {dir}/App/obj/project.assets.json
		var subObjDir = Path.Combine(_tempDir, "App", "obj");
		Directory.CreateDirectory(subObjDir);
		File.WriteAllText(Path.Combine(subObjDir, "project.assets.json"), "{}");

		// Act
		var result = ProjectAssetsParser.FindProjectAssetsFiles(_tempDir, _logger);

		// Assert
		result.Should().HaveCount(1);
		result[0].Should().Contain(Path.Combine("App", "obj", "project.assets.json"));
	}

	[TestMethod]
	public void FindProjectAssetsFiles_Multiple_FindsAll()
	{
		// Arrange - two projects
		var obj1 = Path.Combine(_tempDir, "obj");
		var obj2 = Path.Combine(_tempDir, "SubProject", "obj");
		Directory.CreateDirectory(obj1);
		Directory.CreateDirectory(obj2);
		File.WriteAllText(Path.Combine(obj1, "project.assets.json"), "{}");
		File.WriteAllText(Path.Combine(obj2, "project.assets.json"), "{}");

		// Act
		var result = ProjectAssetsParser.FindProjectAssetsFiles(_tempDir, _logger);

		// Assert
		result.Should().HaveCount(2);
	}

	[TestMethod]
	public void FindProjectAssetsFiles_None_ReturnsEmpty()
	{
		// Arrange - empty directory
		// _tempDir already exists and is empty

		// Act
		var result = ProjectAssetsParser.FindProjectAssetsFiles(_tempDir, _logger);

		// Assert
		result.Should().BeEmpty();
	}

	#region Test Helpers

	private static string CreateProjectAssetsJson(
		string directory,
		params (string name, string version)[] packages)
	{
		var libraries = new Dictionary<string, object>();
		foreach (var (name, version) in packages)
		{
			libraries[$"{name}/{version}"] = new
			{
				type = "package",
				path = $"{name.ToLowerInvariant()}/{version}"
			};
		}

		var json = System.Text.Json.JsonSerializer.Serialize(new
		{
			version = 3,
			libraries
		});

		var assetsPath = Path.Combine(directory, "project.assets.json");
		File.WriteAllText(assetsPath, json);
		return assetsPath;
	}

	#endregion
}
