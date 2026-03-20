using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.DevServer.Cli.E2E.Tests.Mcp;

[TestClass]
[TestCategory("DevServerMcpE2E")]
[TestCategory("DevServerPackageE2E")]
[DoNotParallelize]
public class Given_PackagedDevServerProcess
{
	[TestMethod]
	[Description("The packaged Uno.DevServer tool returns structured health and discovery JSON against a real Uno workspace.")]
	public void WhenPackagedCliIsUsed_HealthAndDiscoReturnExpectedJson()
	{
		using var packagedTool = PackagedDevServerTool.Create();
		using var workspace = TemporaryUnoWorkspaceBuilder.CreateNestedWorkspace();

		var health = CliCommandRunner.Run(
			packagedTool.CreateCommand(["health", "--json", "--solution-dir", workspace.PrimaryWorkspaceDirectory]),
			workspace.RepositoryRoot);
		new[] { 0, 1 }.Should().Contain(
			health.ExitCode,
			"health --json may legitimately report an unhealthy state while still returning valid diagnostics");

		using var healthJson = JsonDocument.Parse(health.StandardOutput);
		healthJson.RootElement.GetProperty("selectedSolutionPath").GetString().Should().Be(workspace.PrimarySolutionPath);
		healthJson.RootElement.GetProperty("effectiveWorkspaceDirectory").GetString().Should().Be(workspace.PrimaryWorkspaceDirectory);

		var disco = CliCommandRunner.Run(
			packagedTool.CreateCommand(["disco", "--json", "--solution-dir", workspace.PrimaryWorkspaceDirectory]),
			workspace.RepositoryRoot);
		disco.EnsureSuccess("packaged uno-devserver disco --json");

		using var discoJson = JsonDocument.Parse(disco.StandardOutput);
		discoJson.RootElement.GetProperty("selectedSolutionPath").GetString().Should().Be(workspace.PrimarySolutionPath);
		discoJson.RootElement.GetProperty("effectiveWorkspaceDirectory").GetString().Should().Be(workspace.PrimaryWorkspaceDirectory);
	}

	[TestMethod]
	[Description("The packaged Uno.DevServer tool supports MCP roots fallback and explicit selection against a nested real workspace.")]
	public async Task WhenPackagedCliRunsInMcpMode_ExplicitSelectionResolvesTheWorkspaceAsync()
	{
		using var packagedTool = PackagedDevServerTool.Create();
		using var workspace = TemporaryUnoWorkspaceBuilder.CreateNestedWorkspace();
		await using var harness = McpProcessHarness.Start(packagedTool.CreateCommand([]), workspace.RepositoryRoot, forceRootsFallback: true);
		var client = new McpJsonRpcClient(harness);
		using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

		await client.InitializeAsync(cts.Token);
		var before = await client.ReadHealthAsync(cts.Token);
		before.IssueCodes.Should().Contain("HostNotStarted");

		using var selectionResponse = await client.CallToolAsync(
			"uno_app_select_solution",
			new { solutionPath = workspace.PrimarySolutionPath },
			cts.Token);

		var selectionText = selectionResponse.RootElement
			.GetProperty("result")
			.GetProperty("content")[0]
			.GetProperty("text")
			.GetString();

		selectionText.Should().NotBeNullOrWhiteSpace();
		using var selectionJson = JsonDocument.Parse(selectionText!);
		selectionJson.RootElement.GetProperty("selectedSolutionPath").GetString().Should().Be(workspace.PrimarySolutionPath);

		var after = await client.WaitForHealthAsync(
			health => string.Equals(health.SelectedSolutionPath, workspace.PrimarySolutionPath, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(health.SelectionSource, "UserSelected", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(health.ConnectionState, "Launching", StringComparison.OrdinalIgnoreCase),
			TimeSpan.FromSeconds(60),
			cts.Token);

		after.EffectiveWorkspaceDirectory.Should().Be(workspace.PrimaryWorkspaceDirectory);
	}
}
