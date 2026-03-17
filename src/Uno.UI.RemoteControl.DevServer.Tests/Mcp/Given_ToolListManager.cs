using System.IO.Pipelines;
using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

// Reproduces the uno_health freeze: ToolListManager calls ListToolsAsync
// without a bounded timeout, blocking the entire MCP pipeline when the
// upstream DevServer is slow to respond.
[TestClass]
public class Given_ToolListManager
{
	// Proves ListToolsAsync without CT hangs forever on a slow server.
	// This is the exact pattern at ToolListManager.RefreshCachedToolsFromUpstreamAsync:183
	//   var list = await upstreamClient.ListToolsAsync();
	[TestMethod]
	[Timeout(15_000)]
	public async Task ListToolsAsync_WithoutCT_HangsOnSlowServer()
	{
		await using var fixture = await SlowMcpFixture.CreateAsync();

		// Call without CT — simulates the bug
		var listTask = fixture.Client.ListToolsAsync();

		// Should hang (not complete within 3s), proving the bug exists
		var winner = await Task.WhenAny(listTask.AsTask(), Task.Delay(3_000));

		winner.Should().NotBeSameAs(
			listTask.AsTask(),
			"ListToolsAsync without CT hangs on a slow server");
	}

	// Proves ListToolsAsync WITH a timeout CT cancels properly.
	// This is the pattern the fix should use.
	[TestMethod]
	[Timeout(15_000)]
	public async Task ListToolsAsync_WithTimeout_CancelsOnSlowServer()
	{
		await using var fixture = await SlowMcpFixture.CreateAsync();
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

		var act = async () =>
		{
			var unused = await fixture.Client.ListToolsAsync(cancellationToken: cts.Token);
		};

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	// Proves CallToolAsync without timeout hangs forever on a slow server.
	// This is the pattern at McpStdioServer.cs:136 where call_tool is forwarded:
	//   var result = await upstreamClient.CallToolAsync(name, args, ct);
	[TestMethod]
	[Timeout(15_000)]
	public async Task CallToolAsync_WithoutTimeout_HangsOnSlowServer()
	{
		await using var fixture = await SlowMcpFixture.CreateAsync();

		// Call a tool — upstream accepts but never responds
		var callTask = fixture.Client.CallToolAsync(
			"uno_app_start",
			new Dictionary<string, object?>());

		// Should hang (not complete within 3s), proving the bug exists
		var winner = await Task.WhenAny(callTask.AsTask(), Task.Delay(3_000));

		winner.Should().NotBeSameAs(
			callTask.AsTask(),
			"CallToolAsync without timeout hangs on a slow server");
	}

	// Proves CallToolAsync WITH a timeout CT cancels properly.
	[TestMethod]
	[Timeout(15_000)]
	public async Task CallToolAsync_WithTimeout_CancelsOnSlowServer()
	{
		await using var fixture = await SlowMcpFixture.CreateAsync();
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

		var act = async () =>
		{
			var unused = await fixture.Client.CallToolAsync(
				"uno_app_start",
				new Dictionary<string, object?>(),
				cancellationToken: cts.Token);
		};

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	// In-memory MCP client/server pair where tools/list and tools/call never respond.
	private sealed class SlowMcpFixture : IAsyncDisposable
	{
		private readonly McpServer _server;

		public McpClient Client { get; }

		private SlowMcpFixture(McpServer server, McpClient client)
		{
			_server = server;
			Client = client;
		}

		public static async Task<SlowMcpFixture> CreateAsync()
		{
			Pipe clientToServer = new(), serverToClient = new();

			var server = McpServer.Create(
				new StreamServerTransport(
					clientToServer.Reader.AsStream(),
					serverToClient.Writer.AsStream()),
				new McpServerOptions
				{
					ServerInfo = new Implementation
					{
						Name = "slow-test",
						Version = "1.0.0"
					},
					Capabilities = new ServerCapabilities
					{
						Tools = new()
					},
					Handlers =
					{
						ListToolsHandler = async (_, ct) =>
						{
							await Task.Delay(Timeout.Infinite, ct);
							return new ListToolsResult();
						},
						CallToolHandler = async (_, ct) =>
						{
							await Task.Delay(Timeout.Infinite, ct);
							return new CallToolResult();
						}
					}
				});

			_ = server.RunAsync();

			var client = await McpClient.CreateAsync(
				new StreamClientTransport(
					clientToServer.Writer.AsStream(),
					serverToClient.Reader.AsStream()),
				new McpClientOptions
				{
					ClientInfo = new Implementation
					{
						Name = "test-client",
						Version = "1.0.0"
					}
				});

			return new SlowMcpFixture(server, client);
		}

		public async ValueTask DisposeAsync()
		{
			await Client.DisposeAsync();
			await _server.DisposeAsync();
		}
	}
}
