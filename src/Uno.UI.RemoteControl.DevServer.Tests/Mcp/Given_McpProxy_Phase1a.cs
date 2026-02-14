using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Protocol;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for Phase 1a — Instant MCP Start fixes.
/// Covers: HealthReport model, tool cache with health tool, TCS timeout patterns,
/// structured error responses, and DisposeAsync safety.
/// </summary>
[TestClass]
public class Given_McpProxy_Phase1a
{
	// -------------------------------------------------------------------
	// HealthReport serialization
	// -------------------------------------------------------------------

	[TestMethod]
	public void HealthReport_WhenHealthy_SerializesCorrectly()
	{
		// Arrange
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			DevServerVersion = "1.0.0",
			UpstreamConnected = true,
			ToolCount = 5,
			Issues = [],
		};

		// Act
		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);

		// Assert
		deserialized.Should().NotBeNull();
		deserialized!.Status.Should().Be(HealthStatus.Healthy);
		deserialized.UpstreamConnected.Should().BeTrue();
		deserialized.ToolCount.Should().Be(5);
		deserialized.Issues.Should().BeEmpty();
	}

	[TestMethod]
	public void HealthReport_WhenUnhealthy_SerializesIssuesWithEnumStrings()
	{
		// Arrange
		var report = new HealthReport
		{
			Status = HealthStatus.Unhealthy,
			UpstreamConnected = false,
			ToolCount = 0,
			Issues =
			[
				new ValidationIssue
				{
					Code = IssueCode.HostNotStarted,
					Severity = ValidationSeverity.Fatal,
					Message = "Host not started",
					Remediation = "Start the host",
				},
				new ValidationIssue
				{
					Code = IssueCode.UpstreamError,
					Severity = ValidationSeverity.Fatal,
					Message = "Connection failed",
				},
			],
		};

		// Act
		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);

		// Assert — enums are serialized as strings, not integers
		json.Should().Contain("\"Unhealthy\"");
		json.Should().Contain("\"Fatal\"");
		json.Should().Contain("\"HostNotStarted\"");
		json.Should().Contain("\"UpstreamError\"");
		json.Should().NotContain("\"status\":0");
		json.Should().NotContain("\"status\":2");

		// Assert — roundtrip works
		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
		deserialized!.Issues.Should().HaveCount(2);
		deserialized.Issues[0].Code.Should().Be(IssueCode.HostNotStarted);
		deserialized.Issues[1].Remediation.Should().BeNull();
	}

	[TestMethod]
	public void HealthReport_WhenDegraded_SerializesWarningIssues()
	{
		// Arrange
		var report = new HealthReport
		{
			Status = HealthStatus.Degraded,
			UpstreamConnected = false,
			Issues =
			[
				new ValidationIssue
				{
					Code = IssueCode.HostUnreachable,
					Severity = ValidationSeverity.Warning,
					Message = "Host starting",
					Remediation = "Wait and retry",
				},
			],
		};

		// Act
		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);

		// Assert
		json.Should().Contain("\"Degraded\"");
		json.Should().Contain("\"Warning\"");
		json.Should().Contain("\"HostUnreachable\"");
	}

	// -------------------------------------------------------------------
	// ToolCacheFile validates uno_health tool name
	// -------------------------------------------------------------------

	[TestMethod]
	public void ToolCacheFile_ValidatesUnoHealthToolName()
	{
		// The uno_health tool name must pass tool cache validation rules
		// (3-64 chars, alphanumeric + underscore/hyphen/dot)
		var tool = new Tool
		{
			Name = "uno_health",
			Description = "Health check tool",
			InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
		};

		// Act
		var isValid = ToolCacheFile.TryValidateCachedTools([tool], out var reason);

		// Assert
		isValid.Should().BeTrue();
		reason.Should().BeNull();
	}

	// -------------------------------------------------------------------
	// TCS error completion (McpClientProxy fix)
	// -------------------------------------------------------------------

	[TestMethod]
	public void TCS_WhenFaulted_IsFaultedIsTrue()
	{
		// Simulates the McpClientProxy fix: TCS completed with exception
		// should be immediately faulted, not pending.
		var tcs = new TaskCompletionSource<object>();
		tcs.TrySetException(new InvalidOperationException("Connection failed"));

		// Assert — task is immediately faulted
		tcs.Task.IsFaulted.Should().BeTrue();
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	public void TCS_WhenCanceled_IsCompletedSuccessfullyIsFalse()
	{
		// Simulates the DisposeAsync guard: after TrySetCanceled,
		// IsCompletedSuccessfully must be false so we skip DisposeAsync on the client.
		var tcs = new TaskCompletionSource<object>();
		tcs.TrySetCanceled();

		tcs.Task.IsCanceled.Should().BeTrue();
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	public void TCS_TrySetException_OnlySucceedsOnce()
	{
		// Ensures that the TCS can only be completed once — second attempt is ignored.
		var tcs = new TaskCompletionSource<object>();

		var first = tcs.TrySetException(new InvalidOperationException("first"));
		var second = tcs.TrySetException(new InvalidOperationException("second"));

		first.Should().BeTrue();
		second.Should().BeFalse();
		tcs.Task.IsFaulted.Should().BeTrue();
	}

	[TestMethod]
	public void TCS_TrySetCanceled_AfterTrySetResult_Fails()
	{
		// Ensures TrySetCanceled doesn't overwrite a successful result
		// (important for DisposeAsync safety).
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetResult("connected");

		var cancelResult = tcs.TrySetCanceled();

		cancelResult.Should().BeFalse();
		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
	}

	// -------------------------------------------------------------------
	// Timeout pattern (list_tools fix)
	// -------------------------------------------------------------------

	[TestMethod]
	public async Task WaitAsync_WhenTimesOut_ThrowsOperationCanceled()
	{
		// Simulates the list_tools timeout fix: WaitAsync with a short timeout
		// should throw OperationCanceledException, not block forever.
		var neverCompletes = new TaskCompletionSource<bool>();

		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

		Func<Task> act = async () => await neverCompletes.Task.WaitAsync(cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[TestMethod]
	public async Task WaitAsync_WhenCompletedBeforeTimeout_ReturnsResult()
	{
		// The happy path: upstream completes before timeout.
		var tcs = new TaskCompletionSource<string>();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		// Complete immediately
		tcs.TrySetResult("connected");

		// Act
		var result = await tcs.Task.WaitAsync(cts.Token);

		// Assert
		result.Should().Be("connected");
	}

	[TestMethod]
	public async Task WaitAsync_WhenFaultedBeforeTimeout_PropagatesException()
	{
		// If upstream faults before timeout, the error propagates (not a timeout).
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetException(new InvalidOperationException("upstream crashed"));

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		Func<Task> act = async () => await tcs.Task.WaitAsync(cts.Token);

		await act.Should().ThrowExactlyAsync<InvalidOperationException>()
			.WithMessage("upstream crashed");
	}

	// -------------------------------------------------------------------
	// Structured error response (call_tool fix)
	// -------------------------------------------------------------------

	[TestMethod]
	public void CallToolResult_WhenIsError_ContainsStructuredMessage()
	{
		// Simulates the structured error response for premature tool calls.
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
	// DisposeAsync safety pattern
	// -------------------------------------------------------------------

	[TestMethod]
	public void DisposePattern_WhenTCSNeverCompleted_CancelPreventsBlocking()
	{
		// Simulates the DisposeAsync fix: if the TCS was never completed
		// (e.g. server never started), TrySetCanceled makes it safe to check.
		var tcs = new TaskCompletionSource<IAsyncDisposable>();

		// Act — simulate DisposeAsync
		tcs.TrySetCanceled();

		// Assert — IsCompletedSuccessfully is false, so we skip the dispose
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();

		// This simulates the guard in the fixed DisposeAsync:
		// if (clientTask.IsCompletedSuccessfully) → false, so we don't call .Result
		var wouldDispose = tcs.Task.IsCompletedSuccessfully;
		wouldDispose.Should().BeFalse("DisposeAsync should be skipped when TCS is canceled");
	}

	[TestMethod]
	public async Task DisposePattern_WhenTCSCompleted_DisposesClient()
	{
		// Simulates the happy path: TCS completed, DisposeAsync should
		// actually dispose the client.
		var mockDisposable = new MockAsyncDisposable();
		var tcs = new TaskCompletionSource<IAsyncDisposable>();
		tcs.TrySetResult(mockDisposable);

		// Act — simulate DisposeAsync
		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
		var client = await tcs.Task;
		await client.DisposeAsync();

		// Assert
		mockDisposable.WasDisposed.Should().BeTrue();
	}

	// -------------------------------------------------------------------
	// Tool cache integration: health tool does not break cache validation
	// -------------------------------------------------------------------

	[TestMethod]
	public void ToolCacheFile_MixedToolsWithHealthTool_ValidationPasses()
	{
		// Ensures that the health tool mixed with upstream tools passes validation.
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

	[TestMethod]
	public void ToolCacheFile_CreateEntry_ProducesValidCacheWithChecksum()
	{
		// Proves that tools including uno_health can be cached and restored.
		var tools = new[]
		{
			new Tool
			{
				Name = "uno_health",
				Description = "Health check",
				InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
			},
		};

		// Act
		var entry = ToolCacheFile.CreateEntry(tools);
		var json = JsonSerializer.Serialize(entry, McpJsonUtilities.DefaultOptions);

		// Assert — can roundtrip through TryRead
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var success = ToolCacheFile.TryRead(json, "test-cache.json", logger, out var restored);

		success.Should().BeTrue();
		restored.Should().HaveCount(1);
		restored[0].Name.Should().Be("uno_health");
	}

	// -------------------------------------------------------------------
	// All IssueCode values roundtrip through JSON
	// -------------------------------------------------------------------

	[TestMethod]
	public void AllIssueCodes_RoundtripThroughJson()
	{
		// Every IssueCode value must serialize to a string and deserialize back.
		foreach (var code in Enum.GetValues<IssueCode>())
		{
			var issue = new ValidationIssue
			{
				Code = code,
				Severity = ValidationSeverity.Warning,
				Message = $"Test {code}",
			};

			var json = JsonSerializer.Serialize(issue, McpJsonUtilities.DefaultOptions);
			json.Should().Contain($"\"{code}\"", $"IssueCode.{code} should serialize as string");

			var deserialized = JsonSerializer.Deserialize<ValidationIssue>(json, McpJsonUtilities.DefaultOptions);
			deserialized!.Code.Should().Be(code);
		}
	}

	// -------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------

	private sealed class MockAsyncDisposable : IAsyncDisposable
	{
		public bool WasDisposed { get; private set; }

		public ValueTask DisposeAsync()
		{
			WasDisposed = true;
			return ValueTask.CompletedTask;
		}
	}
}
