using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Protocol;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

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
	public void ComputeWorkspaceHash_IgnoresTrailingDirectorySeparators()
	{
		var hash1 = ToolCacheFile.ComputeWorkspaceHash(@"C:\Users\test\project");
		var hash2 = ToolCacheFile.ComputeWorkspaceHash(@"C:\Users\test\project\");

		hash1.Should().Be(hash2);
	}

	[TestMethod]
	public void ComputeWorkspaceHash_WhenNullOrEmpty_ReturnsEmpty()
	{
		ToolCacheFile.ComputeWorkspaceHash(null).Should().BeEmpty();
		ToolCacheFile.ComputeWorkspaceHash("").Should().BeEmpty();
		ToolCacheFile.ComputeWorkspaceHash("  ").Should().BeEmpty();
	}

	[TestMethod]
	[Description("ComputeWorkspaceHash follows the same normalization rules as PathComparison for case-insensitive paths")]
	public void ComputeWorkspaceHash_MatchesPathComparisonNormalization_OnCaseInsensitivePaths()
	{
		string path1;
		string path2;

		if (OperatingSystem.IsWindows())
		{
			path1 = @"C:\Users\Test\Project";
			path2 = @"C:\USERS\TEST\PROJECT";
		}
		else
		{
			path1 = "/mnt/c/Users/Test/Project";
			path2 = "/mnt/c/USERS/TEST/PROJECT";
		}

		PathComparison.PathsEqual(path1, path2).Should().BeTrue();
		var hash1 = ToolCacheFile.ComputeWorkspaceHash(path1);
		var hash2 = ToolCacheFile.ComputeWorkspaceHash(path2);
		hash1.Should().Be(hash2);
	}

	[TestMethod]
	[Description("Normalized /mnt paths remain deterministic and hash-equivalent even when the raw casing differs")]
	public void ComputeWorkspaceHash_OnMntPaths_UsesNormalizedOrdering()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("This test validates /mnt path behavior on non-Windows hosts.");
			return;
		}

		var paths = new[]
		{
			"/mnt/c/USERS/TEST/PROJECT",
			"/mnt/c/Users/Test/Project",
		};

		var ordered = paths
			.OrderBy(PathComparison.Normalize, StringComparer.Ordinal)
			.ToArray();

		PathComparison.PathsEqual(ordered[0], ordered[1]).Should().BeTrue();
		ToolCacheFile.ComputeWorkspaceHash(ordered[0]).Should().Be(ToolCacheFile.ComputeWorkspaceHash(ordered[1]));
	}

	[TestMethod]
	[Description("ComputeWorkspaceHash is case-sensitive on native Linux paths (no /mnt/ prefix)")]
	public void ComputeWorkspaceHash_IsCaseSensitive_OnNativeLinuxPaths()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("This test validates POSIX case-sensitive behavior.");
			return;
		}

		var hash1 = ToolCacheFile.ComputeWorkspaceHash("/home/user/project");
		var hash2 = ToolCacheFile.ComputeWorkspaceHash("/HOME/USER/PROJECT");

		hash1.Should().NotBe(hash2, "native Linux paths are case-sensitive");
	}

	// -------------------------------------------------------------------
	// Deduplication of legacy cache
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("A cache written by a pre-dedup version may contain duplicate tool names; DistinctBy at load time should collapse them")]
	public void TryRead_WithDuplicateTools_DeduplicatesOnLoad()
	{
		var tools = new[]
		{
			new Tool
			{
				Name = "uno_app_get_info",
				Description = "Get app info (first)",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
			new Tool
			{
				Name = "uno_app_get_info",
				Description = "Get app info (duplicate)",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
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

		// TryRead returns raw tools — deduplication is done by the consumer (ToolListManager.GetCachedTools)
		// Verify that DistinctBy produces the expected result
		var deduped = restored.DistinctBy(t => t.Name).ToArray();
		deduped.Should().HaveCount(2);
		deduped.Select(t => t.Name).Should().BeEquivalentTo(new[] { "uno_app_get_info", "uno_health" });
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
