using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.DevServer.Cli.E2E.Tests.Mcp;

[TestClass]
[TestCategory("DevServerMcpE2E")]
[TestCategory("DevServerSourceE2E")]
[DoNotParallelize]
public class Given_McpEndToEndProcess
{
	private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);
	private const string HealthToolName = "uno_health";
	private const string SelectSolutionToolName = "uno_app_select_solution";

	[TestMethod]
	[Description("An unresolved parent directory can become active after roots are provided through MCP, and uno_health reflects the resolved workspace.")]
	public async Task WhenRootsAreProvided_UnoHealthReflectsResolvedWorkspaceAsync()
	{
		using var workspace = TemporaryUnoWorkspaceBuilder.CreateNestedWorkspace();
		await using var harness = McpProcessHarness.Start(GetBuiltCliCommand(), workspace.RepositoryRoot, forceRootsFallback: true);
		var client = new McpJsonRpcClient(harness);
		using var cts = CreateTimeoutSource();

		await client.InitializeAsync(cts.Token);

		var before = await client.ReadHealthAsync(cts.Token);
		before.SelectedSolutionPath.Should().BeNull();
		before.IssueCodes.Should().Contain("HostNotStarted");

		using var setRootsResponse = await client.CallToolAsync(
			"uno_app_initialize",
			new { workspaceDirectory = workspace.PrimaryWorkspaceDirectory },
			cts.Token);

		setRootsResponse.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString().Should().Be("Ok");

		var after = await client.WaitForHealthAsync(
			health => string.Equals(health.SelectedSolutionPath, workspace.PrimarySolutionPath, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(health.ConnectionState, "Launching", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(health.EffectiveWorkspaceDirectory, workspace.PrimaryWorkspaceDirectory, StringComparison.OrdinalIgnoreCase),
			ReadyTimeout,
			cts.Token);

		after.Status.Should().Be("Degraded");
		after.SelectionSource.Should().Be("RootsConfirmed");
	}

	[TestMethod]
	[Description("A real MCP client can list the built-in tools immediately from a valid Uno workspace, even before the upstream host has finished connecting.")]
	public async Task WhenStartedFromResolvedWorkspace_ListToolsReturnsBuiltInsImmediatelyAsync()
	{
		using var workspace = TemporaryUnoWorkspaceBuilder.CreateNestedWorkspace();
		await using var harness = McpProcessHarness.Start(GetBuiltCliCommand(), workspace.PrimaryWorkspaceDirectory, forceRootsFallback: false);
		var client = new McpJsonRpcClient(harness);
		using var cts = CreateTimeoutSource();

		await client.InitializeAsync(cts.Token);

		var tools = await client.ListToolsAsync(cts.Token);

		tools.Should().Contain(HealthToolName);
		tools.Should().Contain(SelectSolutionToolName);
	}

	[TestMethod]
	[Description("An unresolved parent directory can be resolved through explicit MCP solution selection, and uno_health reflects the chosen solution.")]
	public async Task WhenSolutionIsSelected_UnoHealthReflectsExplicitSelectionAsync()
	{
		using var workspace = TemporaryUnoWorkspaceBuilder.CreateNestedWorkspace();
		await using var harness = McpProcessHarness.Start(GetBuiltCliCommand(), workspace.RepositoryRoot, forceRootsFallback: true);
		var client = new McpJsonRpcClient(harness);
		using var cts = CreateTimeoutSource();

		await client.InitializeAsync(cts.Token);

		using var selectionResponse = await client.CallToolAsync(
			"uno_app_select_solution",
			new { solutionPath = workspace.PrimarySolutionPath },
			cts.Token);

		var selection = ReadSelectionResult(selectionResponse.RootElement);
		selection.Status.Should().BeOneOf("started", "already_selected");
		selection.SelectedSolutionPath.Should().Be(workspace.PrimarySolutionPath);
		selection.EffectiveWorkspaceDirectory.Should().Be(workspace.PrimaryWorkspaceDirectory);

		var after = await client.WaitForHealthAsync(
			health => string.Equals(health.SelectedSolutionPath, workspace.PrimarySolutionPath, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(health.ConnectionState, "Launching", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(health.SelectionSource, "UserSelected", StringComparison.OrdinalIgnoreCase),
			ReadyTimeout,
			cts.Token);

		after.Status.Should().Be("Degraded");
		after.SelectionSource.Should().Be("UserSelected");
	}

	[TestMethod]
	[Description("A repository with multiple Uno solutions can still be resolved deterministically through explicit solution selection.")]
	public async Task WhenMultipleUnoSolutionsExist_ExplicitSelectionAttachesToTheRequestedSolutionAsync()
	{
		using var workspace = TemporaryUnoWorkspaceBuilder.CreateAmbiguousWorkspace();
		await using var harness = McpProcessHarness.Start(GetBuiltCliCommand(), workspace.RepositoryRoot, forceRootsFallback: true);
		var client = new McpJsonRpcClient(harness);
		using var cts = CreateTimeoutSource();

		await client.InitializeAsync(cts.Token);

		using var selectionResponse = await client.CallToolAsync(
			"uno_app_select_solution",
			new { solutionPath = workspace.SecondarySolutionPath },
			cts.Token);

		var selection = ReadSelectionResult(selectionResponse.RootElement);
		selection.Status.Should().BeOneOf("started", "already_selected");
		selection.SelectedSolutionPath.Should().Be(workspace.SecondarySolutionPath);
		selection.EffectiveWorkspaceDirectory.Should().Be(workspace.SecondaryWorkspaceDirectory);

		var after = await client.WaitForHealthAsync(
			health => string.Equals(health.SelectedSolutionPath, workspace.SecondarySolutionPath, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(health.ConnectionState, "Launching", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(health.SelectionSource, "UserSelected", StringComparison.OrdinalIgnoreCase),
			ReadyTimeout,
			cts.Token);

		after.Status.Should().Be("Degraded");
		after.SelectionSource.Should().Be("UserSelected");
	}

	[TestMethod]
	[Description("Invalid explicit selection returns a structured rejection and leaves uno_health diagnosable.")]
	public async Task WhenSelectionIsInvalid_UnoHealthRemainsDiagnosableAsync()
	{
		using var workspace = TemporaryUnoWorkspaceBuilder.CreateNestedWorkspaceWithNonUnoSolution();
		await using var harness = McpProcessHarness.Start(GetBuiltCliCommand(), workspace.RepositoryRoot, forceRootsFallback: true);
		var client = new McpJsonRpcClient(harness);
		using var cts = CreateTimeoutSource();

		await client.InitializeAsync(cts.Token);
		using var setRootsResponse = await client.CallToolAsync(
			"uno_app_initialize",
			new { workspaceDirectory = workspace.PrimaryWorkspaceDirectory },
			cts.Token);
		setRootsResponse.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString().Should().Be("Ok");

		using var selectionResponse = await client.CallToolAsync(
			"uno_app_select_solution",
			new { solutionPath = workspace.NonUnoSolutionPath },
			cts.Token);

		var selection = ReadSelectionResult(selectionResponse.RootElement);
		selection.Status.Should().Be("rejected");
		selection.IssueCodes.Should().Contain("WorkspaceNotResolved");

		var after = await client.WaitForHealthAsync(
			health => health.CandidateSolutions.Contains(workspace.PrimarySolutionPath, StringComparer.OrdinalIgnoreCase),
			TimeSpan.FromSeconds(20),
			cts.Token);
		after.IssueCodes.Any(issue => issue is "HostNotStarted" or "HostUnreachable").Should().BeTrue();
		after.CandidateSolutions.Should().Contain(workspace.PrimarySolutionPath);
	}

	private static CliCommandSpec GetBuiltCliCommand()
		=> CliCommandSpec.ForBuiltCli(BuildOutputLocator.ResolveCliDllPath(), []);

	private static CancellationTokenSource CreateTimeoutSource()
	{
#if DEBUG
		return new CancellationTokenSource(TimeSpan.FromSeconds(90));
#else
		return new CancellationTokenSource(TimeSpan.FromMinutes(2));
#endif
	}

	private static McpSelectionSnapshot ReadSelectionResult(JsonElement responseRoot)
	{
		var text = responseRoot
			.GetProperty("result")
			.GetProperty("content")[0]
			.GetProperty("text")
			.GetString();

		text.Should().NotBeNullOrWhiteSpace("uno_app_select_solution must return a JSON selection payload");
		using var json = JsonDocument.Parse(text!);
		var root = json.RootElement;

		var issueCodes = root.TryGetProperty("issues", out var issues) && issues.ValueKind == JsonValueKind.Array
			? issues.EnumerateArray()
				.Select(issue => issue.TryGetProperty("code", out var code) ? code.GetString() : null)
				.WhereNotNullOrWhitespace()
				.ToArray()
			: [];

		return new McpSelectionSnapshot(
			root.GetProperty("status").GetString()!,
			root.TryGetProperty("selectedSolutionPath", out var selectedSolutionPath) && selectedSolutionPath.ValueKind == JsonValueKind.String
				? selectedSolutionPath.GetString()
				: null,
			root.TryGetProperty("effectiveWorkspaceDirectory", out var effectiveWorkspaceDirectory) && effectiveWorkspaceDirectory.ValueKind == JsonValueKind.String
				? effectiveWorkspaceDirectory.GetString()
				: null,
			root.GetProperty("devServerAction").GetString()!,
			issueCodes);
	}

	private sealed record McpSelectionSnapshot(
		string Status,
		string? SelectedSolutionPath,
		string? EffectiveWorkspaceDirectory,
		string DevServerAction,
		IReadOnlyList<string> IssueCodes);
}
