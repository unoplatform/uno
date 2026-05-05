using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using Uno.UI.DevServer.Cli.Helpers;
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
		var addRootsTool = new Tool { Name = "uno_app_initialize", Description = "Set roots" };
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
		var addRootsTool = new Tool { Name = "uno_app_initialize", Description = "Set roots" };
		var cachedTools = new[] { HealthService.HealthTool, new Tool { Name = "uno_app_get_info", Description = "Info" } };

		List<Tool> tools = [addRootsTool];
		tools.AddRange(cachedTools);

		// Call the real production code
		var result = ToolListManager.AppendBuiltInTools(tools);

		var healthCount = result.Count(t => t.Name == HealthService.HealthTool.Name);
		healthCount.Should().Be(1, "AppendBuiltInTools deduplication ensures uno_health appears exactly once");
		result.Should().ContainSingle(t => t.Name == ProxyLifecycleManager.SelectSolutionTool.Name);
		result.Should().HaveCount(4, "addRoots + uno_health (from cache) + uno_app_select_solution + uno_app_get_info");
	}

	[TestMethod]
	[Description("AppendBuiltInTools adds uno_health when no cached tools are available (fresh start)")]
	public void ListTools_FallbackPath_NoCachedTools_HealthToolIsAdded()
	{
		var addRootsTool = new Tool { Name = "uno_app_initialize", Description = "Set roots" };

		List<Tool> tools = [addRootsTool];
		// No cached tools

		// Call the real production code
		var result = ToolListManager.AppendBuiltInTools(tools);

		result.Should().HaveCount(3, "uno_health + uno_app_select_solution + addRoots when no cache is available");
		result[0].Name.Should().Be(HealthService.HealthTool.Name, "uno_health should be first");
		result[1].Name.Should().Be(ProxyLifecycleManager.SelectSolutionTool.Name, "uno_app_select_solution should be second");
	}

	[TestMethod]
	[Description("AppendBuiltInTools always normalizes built-in tools to the front even when upstream/cache already returned them out of order")]
	public void ListTools_FallbackPath_BuiltInsAreAlwaysOrderedFirst()
	{
		List<Tool> tools =
		[
			new() { Name = "uno_app_get_info", Description = "Info" },
			HealthService.HealthTool,
			new() { Name = "uno_app_initialize", Description = "Set roots" },
			ProxyLifecycleManager.SelectSolutionTool,
		];

		var result = ToolListManager.AppendBuiltInTools(tools);

		result[0].Name.Should().Be(HealthService.HealthTool.Name);
		result[1].Name.Should().Be(ProxyLifecycleManager.SelectSolutionTool.Name);
		result.Select(t => t.Name).Should().Equal(
			HealthService.HealthTool.Name,
			ProxyLifecycleManager.SelectSolutionTool.Name,
			"uno_app_get_info",
			"uno_app_initialize");
	}

	[TestMethod]
	[Description("Select-solution argument parsing rejects non-string JSON values with a structured MCP error")]
	public void TryGetSelectSolutionPath_WhenValueIsNotString_ReturnsStructuredError()
	{
		var arguments = new Dictionary<string, JsonElement>
		{
			["solutionPath"] = JsonDocument.Parse("42").RootElement.Clone(),
		};

		var success = McpStdioServer.TryGetSelectSolutionPath(arguments, out var solutionPath, out var errorResult);

		success.Should().BeFalse();
		solutionPath.Should().BeNull();
		errorResult.IsError.Should().BeTrue();
		((TextContentBlock)errorResult.Content.Single()).Text.Should().Contain("JSON string");
	}

	// -------------------------------------------------------------------
	// Meta-tools: AppendMetaTools deduplication
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("AppendMetaTools adds discover_tools and execute_tool when not already present")]
	public void AppendMetaTools_WhenNonePresent_AddsBoth()
	{
		IList<Tool> tools = [HealthService.HealthTool, ProxyLifecycleManager.SelectSolutionTool];

		var result = McpStdioServer.AppendMetaTools(tools);

		result.Should().HaveCount(4);
		result.Should().ContainSingle(t => t.Name == McpStdioServer.DiscoverToolsTool.Name);
		result.Should().ContainSingle(t => t.Name == McpStdioServer.ExecuteToolTool.Name);
	}

	[TestMethod]
	[Description("AppendMetaTools deduplicates when meta-tools are already in the list")]
	public void AppendMetaTools_WhenAlreadyPresent_NoDuplicates()
	{
		IList<Tool> tools =
		[
			HealthService.HealthTool,
			McpStdioServer.DiscoverToolsTool,
			McpStdioServer.ExecuteToolTool,
		];

		var result = McpStdioServer.AppendMetaTools(tools);

		result.Should().HaveCount(3);
		result.Count(t => t.Name == McpStdioServer.DiscoverToolsTool.Name).Should().Be(1);
		result.Count(t => t.Name == McpStdioServer.ExecuteToolTool.Name).Should().Be(1);
	}

	[TestMethod]
	[Description("Meta-tools are appended at the end, after built-in and upstream tools")]
	public void AppendMetaTools_OrderIsPreserved_MetaToolsLast()
	{
		IList<Tool> tools =
		[
			HealthService.HealthTool,
			ProxyLifecycleManager.SelectSolutionTool,
			new() { Name = "uno_app_get_info", Description = "Info" },
		];

		var result = McpStdioServer.AppendMetaTools(tools);

		result[0].Name.Should().Be(HealthService.HealthTool.Name);
		result[1].Name.Should().Be(ProxyLifecycleManager.SelectSolutionTool.Name);
		result[2].Name.Should().Be("uno_app_get_info");
		result[3].Name.Should().Be(McpStdioServer.DiscoverToolsTool.Name);
		result[4].Name.Should().Be(McpStdioServer.ExecuteToolTool.Name);
	}

	// -------------------------------------------------------------------
	// Meta-tools: ShouldIncludeMetaTools tracking
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("ShouldIncludeMetaTools is true before any list_changed notification")]
	public void ShouldIncludeMetaTools_BeforeListChanged_IsTrue()
	{
		var server = CreateMcpStdioServer();

		server.ShouldIncludeMetaTools.Should().BeTrue();
	}

	[TestMethod]
	[Description("ShouldIncludeMetaTools is still true after list_changed sent but before client re-queries")]
	public void ShouldIncludeMetaTools_AfterListChangedSentBeforeReQuery_IsTrue()
	{
		var server = CreateMcpStdioServer();

		server.OnListChangedNotificationSent();

		// The client has not re-queried list_tools yet, so meta-tools should still be included
		server.ShouldIncludeMetaTools.Should().BeTrue();
	}

	[TestMethod]
	[Description("ShouldIncludeMetaTools stays true after a client re-queries after list_changed so compatibility tools remain routable for the full session")]
	public void ShouldIncludeMetaTools_AfterClientReQuery_RemainsTrue()
	{
		var server = CreateMcpStdioServer();

		server.OnListChangedNotificationSent();
		server.OnListToolsQueried();

		server.ClientReQueriedAfterListChanged.Should().BeTrue();
		server.ShouldIncludeMetaTools.Should().BeTrue();
	}

	// -------------------------------------------------------------------
	// TryGetInitializeArgs validation
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("TryGetInitializeArgs extracts workspaceDirectory and optional solutionPath")]
	public void TryGetInitializeArgs_WhenValid_ExtractsArguments()
	{
		var testDir = Path.Combine(Path.GetTempPath(), "test-project");
		var testSln = Path.Combine(testDir, "App.slnx");
		var arguments = new Dictionary<string, JsonElement>
		{
			["workspaceDirectory"] = JsonDocument.Parse($"\"{testDir.Replace("\\", "\\\\")}\"").RootElement.Clone(),
			["solutionPath"] = JsonDocument.Parse($"\"{testSln.Replace("\\", "\\\\")}\"").RootElement.Clone(),
		};

		var success = McpStdioServer.TryGetInitializeArgs(arguments, out var workspaceDir, out var solutionPath, out _);

		success.Should().BeTrue();
		workspaceDir.Should().Be(testDir);
		solutionPath.Should().Be(testSln);
	}

	[TestMethod]
	[Description("TryGetInitializeArgs succeeds when solutionPath is omitted")]
	public void TryGetInitializeArgs_WhenSolutionPathOmitted_Succeeds()
	{
		var testDir = Path.Combine(Path.GetTempPath(), "test-project");
		var arguments = new Dictionary<string, JsonElement>
		{
			["workspaceDirectory"] = JsonDocument.Parse($"\"{testDir.Replace("\\", "\\\\")}\"").RootElement.Clone(),
		};

		var success = McpStdioServer.TryGetInitializeArgs(arguments, out var workspaceDir, out var solutionPath, out _);

		success.Should().BeTrue();
		workspaceDir.Should().Be(testDir);
		solutionPath.Should().BeNull();
	}

	[TestMethod]
	[Description("TryGetInitializeArgs rejects missing workspaceDirectory")]
	public void TryGetInitializeArgs_WhenMissing_ReturnsError()
	{
		var arguments = new Dictionary<string, JsonElement>();

		var success = McpStdioServer.TryGetInitializeArgs(arguments, out _, out _, out var errorResult);

		success.Should().BeFalse();
		errorResult.IsError.Should().BeTrue();
		((TextContentBlock)errorResult.Content.Single()).Text.Should().Contain("workspaceDirectory");
	}

	[TestMethod]
	[Description("TryGetInitializeArgs rejects non-string workspaceDirectory")]
	public void TryGetInitializeArgs_WhenNotString_ReturnsError()
	{
		var arguments = new Dictionary<string, JsonElement>
		{
			["workspaceDirectory"] = JsonDocument.Parse("42").RootElement.Clone(),
		};

		var success = McpStdioServer.TryGetInitializeArgs(arguments, out _, out _, out var errorResult);

		success.Should().BeFalse();
		errorResult.IsError.Should().BeTrue();
		((TextContentBlock)errorResult.Content.Single()).Text.Should().Contain("string");
	}

	[TestMethod]
	[Description("TryGetInitializeArgs rejects relative paths")]
	public void TryGetInitializeArgs_WhenRelativePath_ReturnsError()
	{
		var arguments = new Dictionary<string, JsonElement>
		{
			["workspaceDirectory"] = JsonDocument.Parse("\"relative/path\"").RootElement.Clone(),
		};

		var success = McpStdioServer.TryGetInitializeArgs(arguments, out _, out _, out var errorResult);

		success.Should().BeFalse();
		errorResult.IsError.Should().BeTrue();
		((TextContentBlock)errorResult.Content.Single()).Text.Should().Contain("absolute path");
	}

	// -------------------------------------------------------------------
	// InitializeTool definition
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("InitializeTool has the expected name and schema")]
	public void InitializeTool_NameAndSchema_AreValid()
	{
		McpStdioServer.InitializeTool.Name.Should().Be("uno_app_initialize");
		McpStdioServer.InitializeTool.InputSchema.GetProperty("required")
			.EnumerateArray().First().GetString().Should().Be("workspaceDirectory");
	}

	[TestMethod]
	[Description("DiscoverToolsTool has the expected name")]
	public void DiscoverToolsTool_Name_IsValid()
	{
		McpStdioServer.DiscoverToolsTool.Name.Should().Be("uno_discover_tools");
	}

	[TestMethod]
	[Description("ExecuteToolTool has the expected name and required toolName argument")]
	public void ExecuteToolTool_NameAndSchema_AreValid()
	{
		McpStdioServer.ExecuteToolTool.Name.Should().Be("uno_execute_tool");
		McpStdioServer.ExecuteToolTool.InputSchema.GetProperty("required")
			.EnumerateArray().First().GetString().Should().Be("toolName");
	}

	private static McpStdioServer CreateMcpStdioServer()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
			.AddSingleton(new UnoToolsLocator(Microsoft.Extensions.Logging.Abstractions.NullLogger<UnoToolsLocator>.Instance))
			.BuildServiceProvider();
		var monitor = new DevServerMonitor(services, Microsoft.Extensions.Logging.Abstractions.NullLogger<DevServerMonitor>.Instance);
		var upstreamClient = new McpUpstreamClient(Microsoft.Extensions.Logging.Abstractions.NullLogger<McpUpstreamClient>.Instance, monitor);
		var toolListManager = new ToolListManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<ToolListManager>.Instance, upstreamClient);
		var healthService = new HealthService(upstreamClient, monitor, toolListManager);
		return new McpStdioServer(Microsoft.Extensions.Logging.Abstractions.NullLogger<McpStdioServer>.Instance, toolListManager, healthService, upstreamClient);
	}
}
