using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Protocol;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for <see cref="ToolCacheFile"/> validation, serialization,
/// checksum integrity, workspace hash, and atomic write patterns.
/// </summary>
[TestClass]
public class Given_ToolCacheFile
{
	// -------------------------------------------------------------------
	// Validation
	// -------------------------------------------------------------------

	[TestMethod]
	public void ToolCacheFile_ValidatesUnoHealthToolName()
	{
		var tool = new Tool
		{
			Name = "uno_health",
			Description = "Health check tool",
			InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
		};

		var isValid = ToolCacheFile.TryValidateCachedTools([tool], out var reason);

		isValid.Should().BeTrue();
		reason.Should().BeNull();
	}

	[TestMethod]
	public void ToolCacheFile_MixedToolsWithHealthTool_ValidationPasses()
	{
		var tools = new[]
		{
			new Tool
			{
				Name = "uno_health",
				Description = "Health check",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
			new Tool
			{
				Name = "uno_app_get_info",
				Description = "Get app info",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{"app_id":{"type":"string"}}}"""),
			},
		};

		var isValid = ToolCacheFile.TryValidateCachedTools(tools, out var reason);

		isValid.Should().BeTrue();
		reason.Should().BeNull();
	}

	// -------------------------------------------------------------------
	// Create and roundtrip
	// -------------------------------------------------------------------

	[TestMethod]
	public void ToolCacheFile_CreateEntry_ProducesValidCacheWithChecksum()
	{
		var tools = new[]
		{
			new Tool
			{
				Name = "uno_health",
				Description = "Health check",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
		};

		var entry = ToolCacheFile.CreateEntry(tools);
		var json = JsonSerializer.Serialize(entry, McpJsonUtilities.DefaultOptions);

		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var success = ToolCacheFile.TryRead(json, "test-cache.json", logger, out var restored);

		success.Should().BeTrue();
		restored.Should().HaveCount(1);
		restored[0].Name.Should().Be("uno_health");
	}

	// -------------------------------------------------------------------
	// Metadata matching
	// -------------------------------------------------------------------

	[TestMethod]
	public void ToolCacheEntry_WithMetadata_RoundtripsCorrectly()
	{
		var tools = new[]
		{
			new Tool
			{
				Name = "uno_test_tool",
				Description = "A test tool",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
		};

		var entry = ToolCacheFile.CreateEntry(tools, workspaceHash: "ABCDEF0123456789", unoSdkVersion: "5.5.100");
		var json = JsonSerializer.Serialize(entry, McpJsonUtilities.DefaultOptions);

		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var success = ToolCacheFile.TryRead(
			json,
			"test-cache.json",
			logger,
			out var restored,
			expectedWorkspaceHash: "ABCDEF0123456789",
			expectedUnoSdkVersion: "5.5.100");

		success.Should().BeTrue();
		restored.Should().HaveCount(1);
		restored[0].Name.Should().Be("uno_test_tool");
	}

	[TestMethod]
	public void TryRead_WhenWorkspaceHashMismatch_ReturnsFalse()
	{
		var tools = new[]
		{
			new Tool
			{
				Name = "uno_test_tool",
				Description = "A test tool",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
		};

		var entry = ToolCacheFile.CreateEntry(tools, workspaceHash: "AAAA000000000000", unoSdkVersion: "5.5.100");
		var json = JsonSerializer.Serialize(entry, McpJsonUtilities.DefaultOptions);

		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var success = ToolCacheFile.TryRead(
			json,
			"test-cache.json",
			logger,
			out _,
			expectedWorkspaceHash: "BBBB111111111111",
			expectedUnoSdkVersion: "5.5.100");

		success.Should().BeFalse();
	}

	[TestMethod]
	public void TryRead_WhenSdkVersionMismatch_ReturnsFalse()
	{
		var tools = new[]
		{
			new Tool
			{
				Name = "uno_test_tool",
				Description = "A test tool",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
		};

		var entry = ToolCacheFile.CreateEntry(tools, workspaceHash: "AAAA000000000000", unoSdkVersion: "5.5.100");
		var json = JsonSerializer.Serialize(entry, McpJsonUtilities.DefaultOptions);

		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var success = ToolCacheFile.TryRead(
			json,
			"test-cache.json",
			logger,
			out _,
			expectedWorkspaceHash: "AAAA000000000000",
			expectedUnoSdkVersion: "6.0.0");

		success.Should().BeFalse();
	}

	[TestMethod]
	public void TryRead_WhenNullMetadata_AcceptsLegacyCache()
	{
		var tools = new[]
		{
			new Tool
			{
				Name = "uno_test_tool",
				Description = "A test tool",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
		};

		var entry = ToolCacheFile.CreateEntry(tools);
		var json = JsonSerializer.Serialize(entry, McpJsonUtilities.DefaultOptions);

		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var success = ToolCacheFile.TryRead(
			json,
			"test-cache.json",
			logger,
			out var restored,
			expectedWorkspaceHash: "AAAA000000000000",
			expectedUnoSdkVersion: "5.5.100");

		success.Should().BeTrue();
		restored.Should().HaveCount(1);
	}

	// -------------------------------------------------------------------
	// Workspace hash
	// -------------------------------------------------------------------

	[TestMethod]
	public void ComputeWorkspaceHash_NormalizesPathSeparators()
	{
		var hash1 = ToolCacheFile.ComputeWorkspaceHash(@"C:\Users\test\project");
		var hash2 = ToolCacheFile.ComputeWorkspaceHash("C:/Users/test/project");

		hash1.Should().Be(hash2);
		hash1.Should().HaveLength(16);
	}

	[TestMethod]
	public void ComputeWorkspaceHash_WhenNullOrEmpty_ReturnsEmpty()
	{
		ToolCacheFile.ComputeWorkspaceHash(null).Should().BeEmpty();
		ToolCacheFile.ComputeWorkspaceHash("").Should().BeEmpty();
		ToolCacheFile.ComputeWorkspaceHash("  ").Should().BeEmpty();
	}

	[TestMethod]
	[Description("ComputeWorkspaceHash is case-insensitive on all platforms (including Linux/WSL)")]
	public void ComputeWorkspaceHash_IsCaseInsensitiveOnAllPlatforms()
	{
		var hash1 = ToolCacheFile.ComputeWorkspaceHash("/home/user/project");
		var hash2 = ToolCacheFile.ComputeWorkspaceHash("/HOME/USER/PROJECT");
		var hash3 = ToolCacheFile.ComputeWorkspaceHash("/Home/User/Project");

		hash1.Should().Be(hash2, "hashes for different casings of the same path should match");
		hash1.Should().Be(hash3, "hashes for different casings of the same path should match");
		hash1.Should().HaveLength(16);
	}

	// -------------------------------------------------------------------
	// Atomic cache writes
	// -------------------------------------------------------------------

	[TestMethod]
	public void AtomicWrite_WhenTargetExists_OverwriteSucceeds()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"mcp-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		try
		{
			var targetPath = Path.Combine(tempDir, "cache.json");
			var tempPath = targetPath + ".tmp";

			File.WriteAllText(targetPath, """{"version":1}""");

			File.WriteAllText(tempPath, """{"version":2}""");
			File.Move(tempPath, targetPath, overwrite: true);

			var content = File.ReadAllText(targetPath);
			content.Should().Contain("\"version\":2");
			File.Exists(tempPath).Should().BeFalse();
		}
		finally
		{
			Directory.Delete(tempDir, recursive: true);
		}
	}
}
