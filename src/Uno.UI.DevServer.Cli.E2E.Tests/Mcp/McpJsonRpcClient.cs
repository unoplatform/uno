using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.DevServer.Cli.E2E.Tests.Mcp;

internal sealed class McpJsonRpcClient(McpProcessHarness harness)
{
	private int _nextRequestId;

	public async Task InitializeAsync(CancellationToken ct)
	{
		var requestId = GetNextRequestId();
		var request = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			id = requestId,
			method = "initialize",
			@params = new
			{
				protocolVersion = "2024-11-05",
				capabilities = new { },
				clientInfo = new
				{
					name = "uno-devserver-e2e-test",
					version = "1.0.0"
				}
			}
		});

		await harness.WriteLineAsync(request, ct);
		using var response = await harness.ReadResponseAsync(requestId, TimeSpan.FromSeconds(20));
		response.RootElement.TryGetProperty("result", out _).Should().BeTrue("MCP initialize must return a successful result payload");

		var initializedNotification = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			method = "notifications/initialized"
		});

		await harness.WriteLineAsync(initializedNotification, ct);
	}

	public async Task<IReadOnlyList<string>> ListToolsAsync(CancellationToken ct)
	{
		var requestId = GetNextRequestId();
		var request = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			id = requestId,
			method = "tools/list",
			@params = new { }
		});

		await harness.WriteLineAsync(request, ct);
		using var response = await harness.ReadResponseAsync(requestId, TimeSpan.FromSeconds(20));
		return response.RootElement
			.GetProperty("result")
			.GetProperty("tools")
			.EnumerateArray()
			.Select(tool => tool.GetProperty("name").GetString()!)
			.ToArray();
	}

	public async Task<JsonDocument> CallToolAsync(string toolName, object? arguments, CancellationToken ct)
	{
		var requestId = GetNextRequestId();
		var request = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			id = requestId,
			method = "tools/call",
			@params = new
			{
				name = toolName,
				arguments
			}
		});

		await harness.WriteLineAsync(request, ct);
		return await harness.ReadResponseAsync(requestId, TimeSpan.FromSeconds(30));
	}

	public async Task<McpHealthSnapshot> ReadHealthAsync(CancellationToken ct)
	{
		using var response = await CallToolAsync("uno_health", new { }, ct);
		var text = response.RootElement
			.GetProperty("result")
			.GetProperty("content")[0]
			.GetProperty("text")
			.GetString();

		text.Should().NotBeNullOrWhiteSpace("uno_health must return a JSON health payload");
		using var json = JsonDocument.Parse(text!);
		return McpHealthSnapshot.FromJson(json.RootElement);
	}

	public async Task<McpHealthSnapshot> WaitForHealthAsync(Func<McpHealthSnapshot, bool> predicate, TimeSpan timeout, CancellationToken ct)
	{
		var deadline = DateTime.UtcNow + timeout;
		McpHealthSnapshot? last = null;

		while (DateTime.UtcNow < deadline)
		{
			ct.ThrowIfCancellationRequested();

			last = await ReadHealthAsync(ct);
			if (predicate(last))
			{
				return last;
			}

			await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
		}

		last ??= await ReadHealthAsync(ct);
		throw new TimeoutException(
			$"Timed out waiting for MCP health condition. Last status: {last.Status}, connectionState: {last.ConnectionState}, upstreamConnected: {last.UpstreamConnected}, resolution: {last.ResolutionKind}, selectionSource: {last.SelectionSource}, selectedSolution: {last.SelectedSolutionPath}, issues: {string.Join(", ", last.IssueCodes)}");
	}

	private int GetNextRequestId() => Interlocked.Increment(ref _nextRequestId);
}

internal sealed record McpHealthSnapshot(
	string Status,
	string? ConnectionState,
	bool UpstreamConnected,
	string? EffectiveWorkspaceDirectory,
	string? SelectedSolutionPath,
	string? ResolutionKind,
	string? SelectionSource,
	IReadOnlyList<string> CandidateSolutions,
	IReadOnlyList<string> IssueCodes)
{
	public static McpHealthSnapshot FromJson(JsonElement root)
	{
		static string? ReadOptionalString(JsonElement element, string propertyName)
			=> element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
				? value.GetString()
				: null;

		static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
		{
			if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
			{
				return [];
			}

			return value.EnumerateArray()
				.Where(item => item.ValueKind == JsonValueKind.String)
				.Select(item => item.GetString()!)
				.ToArray();
		}

		static IReadOnlyList<string> ReadIssueCodes(JsonElement element)
		{
			if (!element.TryGetProperty("issues", out var issues) || issues.ValueKind != JsonValueKind.Array)
			{
				return [];
			}

			return issues.EnumerateArray()
				.Select(issue => issue.TryGetProperty("code", out var code) ? code.GetString() : null)
				.WhereNotNullOrWhitespace()
				.ToArray();
		}

		return new McpHealthSnapshot(
			root.GetProperty("status").GetString()!,
			ReadOptionalString(root, "connectionState"),
			root.TryGetProperty("upstreamConnected", out var upstreamConnected) && upstreamConnected.ValueKind == JsonValueKind.True,
			ReadOptionalString(root, "effectiveWorkspaceDirectory"),
			ReadOptionalString(root, "selectedSolutionPath"),
			ReadOptionalString(root, "resolutionKind"),
			ReadOptionalString(root, "selectionSource"),
			ReadStringArray(root, "candidateSolutions"),
			ReadIssueCodes(root));
	}
}

internal static class McpJsonValueExtensions
{
	public static IEnumerable<string> WhereNotNullOrWhitespace(this IEnumerable<string?> values)
		=> values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value!);
}
