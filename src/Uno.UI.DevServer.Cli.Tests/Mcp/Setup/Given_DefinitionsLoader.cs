using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_DefinitionsLoader
{
	[TestMethod]
	public void Load_EmbeddedResources_ReturnsAllIdeProfiles()
	{
		var defs = DefinitionsLoader.Load();

		defs.Ides.Should().ContainKey("vscode");
		defs.Ides.Should().ContainKey("cursor");
		defs.Ides.Should().ContainKey("windsurf");
		defs.Ides.Should().ContainKey("kiro");
		defs.Ides.Should().ContainKey("trae");
		defs.Ides.Should().ContainKey("antigravity");
		defs.Ides.Should().ContainKey("rider");
		defs.Ides.Should().ContainKey("claude-code");
		defs.Ides.Should().ContainKey("opencode");
		defs.Ides.Should().ContainKey("aider");
	}

	[TestMethod]
	public void Load_EmbeddedResources_ReturnsAllServerDefinitions()
	{
		var defs = DefinitionsLoader.Load();

		defs.Servers.Should().ContainKey("UnoApp");
		defs.Servers.Should().ContainKey("UnoDocs");
	}

	[TestMethod]
	public void Load_EmbeddedResources_UnoAppHasThreeVariants()
	{
		var defs = DefinitionsLoader.Load();
		var unoApp = defs.Servers["UnoApp"];

		unoApp.Transport.Should().Be("stdio");
		unoApp.Variants.Should().ContainKey("stable");
		unoApp.Variants.Should().ContainKey("prerelease");
		unoApp.Variants.Should().ContainKey("pinned");
	}

	[TestMethod]
	public void Load_EmbeddedResources_UnoDocsIsHttp()
	{
		var defs = DefinitionsLoader.Load();
		var unoDocs = defs.Servers["UnoDocs"];

		unoDocs.Transport.Should().Be("http");
		unoDocs.Variants["stable"]["url"]!.GetValue<string>().Should().Be("https://mcp.platform.uno/v1");
	}

	[TestMethod]
	public void Load_EmbeddedResources_VsCodeUsesServersRootKey()
	{
		var defs = DefinitionsLoader.Load();
		var vscode = defs.Ides["vscode"];

		vscode.JsonRootKey.Should().Be("servers");
		vscode.WriteTarget.Should().Contain("{workspace}");
		vscode.ConfigPaths.Length.Should().BeGreaterThan(1);
	}

	[TestMethod]
	public void Load_EmbeddedResources_CursorUsesMcpServersRootKey()
	{
		var defs = DefinitionsLoader.Load();
		var cursor = defs.Ides["cursor"];

		cursor.JsonRootKey.Should().Be("mcpServers");
	}

	[TestMethod]
	public void Load_EmbeddedResources_DetectionPatternsPresent()
	{
		var defs = DefinitionsLoader.Load();
		var unoApp = defs.Servers["UnoApp"];

		unoApp.Detection.KeyPatterns.Should().NotBeEmpty();
		unoApp.Detection.CommandPatterns.Should().NotBeNull();
		unoApp.Detection.UrlPatterns.Should().NotBeNull();
	}

	[TestMethod]
	public void Load_ExternalFile_OverridesEmbedded()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/custom/ides.json", """
		{
		  "test-ide": {
		    "configPaths": ["{workspace}/.test/mcp.json"],
		    "writeTarget": "{workspace}/.test/mcp.json",
		    "jsonRootKey": "mcpServers"
		  }
		}
		""");

		var defs = DefinitionsLoader.Load(fs, ideDefinitionsPath: "/custom/ides.json");

		defs.Ides.Should().ContainKey("test-ide");
		defs.Ides.Should().NotContainKey("vscode");
	}

	[TestMethod]
	public void Load_ExternalFileNotFound_Throws()
	{
		var fs = new InMemoryFileSystem();

		var act = () => DefinitionsLoader.Load(fs, ideDefinitionsPath: "/missing/file.json");
		act.Should().Throw<FileNotFoundException>();
	}
}
