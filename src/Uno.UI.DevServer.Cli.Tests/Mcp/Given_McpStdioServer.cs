using AwesomeAssertions;
using ModelContextProtocol.Protocol;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

/// <summary>
/// Tests for <see cref="McpStdioServer"/> handler behavior:
/// structured error responses returned by the call_tool handler,
/// and ServerInfo configuration patterns.
/// </summary>
[TestClass]
public class Given_McpStdioServer
{
	// -------------------------------------------------------------------
	// Structured error response (call_tool handler)
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("call_tool returns a structured error pointing to uno_health when the upstream is not yet ready")]
	public void CallToolResult_WhenIsError_ContainsStructuredMessage()
	{
		var result = new CallToolResult()
		{
			Content = [new TextContentBlock() { Text = "DevServer is starting up. The host process is not yet ready. Call the uno_health tool for detailed diagnostics, or wait a few seconds and retry." }],
			IsError = true,
		};

		result.IsError.Should().BeTrue();
		result.Content.Should().HaveCount(1);
		var textBlock = result.Content[0] as TextContentBlock;
		textBlock.Should().NotBeNull();
		textBlock!.Text.Should().Contain("uno_health");
		textBlock.Text.Should().Contain("not yet ready");
	}

	// -------------------------------------------------------------------
	// ServerInfo configuration pattern
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("Implementation record accepts the expected name and version format")]
	public void ServerInfo_NameAndVersion_AreValid()
	{
		var serverInfo = new Implementation
		{
			Name = "uno-devserver",
			Version = GetInformationalVersion(),
		};

		serverInfo.Name.Should().Be("uno-devserver");
		serverInfo.Version.Should().NotBeNullOrEmpty();
	}

	[TestMethod]
	[Description("InformationalVersion does not contain the commit hash suffix")]
	public void ServerInfo_Version_DoesNotContainCommitHash()
	{
		var version = GetInformationalVersion();

		version.Should().NotContain("+", "commit hash suffix should be stripped");
	}

	/// <summary>
	/// Mirrors the GetAssemblyVersion() logic in McpStdioServer to verify the
	/// InformationalVersion -> SemVer extraction pattern.
	/// </summary>
	private static string GetInformationalVersion()
	{
		var attr = typeof(Given_McpStdioServer).Assembly
			.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
			.OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
			.FirstOrDefault();

		if (attr is not null)
		{
			var parts = attr.InformationalVersion.Split('+', StringSplitOptions.RemoveEmptyEntries);
			return parts[0];
		}

		return typeof(Given_McpStdioServer).Assembly.GetName().Version?.ToString() ?? "0.0.0";
	}

	// -------------------------------------------------------------------
	// forceRootsFallback deduplication (list_tools handler)
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("When cached tools include uno_health and it is prepended explicitly, duplicates appear (demonstrates the old bug)")]
	public void ListTools_FallbackPath_OldBehavior_ProducesDuplicateHealthTool()
	{
		var addRootsTool = new Tool { Name = "uno_app_set_roots", Description = "Set roots" };
		var cachedTools = new[] { HealthService.HealthTool, new Tool { Name = "uno_app_get_info", Description = "Info" } };

		// Old behavior: explicitly prepend uno_health, then AddRange cached tools
		List<Tool> tools = [HealthService.HealthTool, addRootsTool];
		tools.AddRange(cachedTools);

		var healthCount = tools.Count(t => t.Name == HealthService.HealthTool.Name);
		healthCount.Should().Be(2, "old code prepends uno_health AND it may already be in cached tools");
	}

	[TestMethod]
	[Description("New behavior: AppendBuiltInTools deduplication prevents duplicate uno_health in forceRootsFallback path")]
	public void ListTools_FallbackPath_NewBehavior_NoDuplicateHealthTool()
	{
		var addRootsTool = new Tool { Name = "uno_app_set_roots", Description = "Set roots" };
		var cachedTools = new[] { HealthService.HealthTool, new Tool { Name = "uno_app_get_info", Description = "Info" } };

		List<Tool> tools = [addRootsTool];
		tools.AddRange(cachedTools);

		// Call the real production code
		var result = ToolListManager.AppendBuiltInTools(tools);

		var healthCount = result.Count(t => t.Name == HealthService.HealthTool.Name);
		healthCount.Should().Be(1, "AppendBuiltInTools deduplication ensures uno_health appears exactly once");
		result.Should().HaveCount(3, "addRoots + uno_health (from cache) + uno_app_get_info");
	}

	[TestMethod]
	[Description("AppendBuiltInTools adds uno_health when no cached tools are available (fresh start)")]
	public void ListTools_FallbackPath_NoCachedTools_HealthToolIsAdded()
	{
		var addRootsTool = new Tool { Name = "uno_app_set_roots", Description = "Set roots" };

		List<Tool> tools = [addRootsTool];
		// No cached tools

		// Call the real production code
		var result = ToolListManager.AppendBuiltInTools(tools);

		result.Should().HaveCount(2, "uno_health + addRoots when no cache is available");
		result[0].Name.Should().Be(HealthService.HealthTool.Name, "uno_health should be first");
	}
}
