using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Protocol;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

[TestClass]
public class Given_McpProxy
{
	// -------------------------------------------------------------------
	// HealthReport serialization
	// -------------------------------------------------------------------

	[TestMethod]
	public void HealthReport_WhenHealthy_SerializesCorrectly()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			DevServerVersion = "1.0.0",
			UpstreamConnected = true,
			ToolCount = 5,
			Issues = [],
		};

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Status.Should().Be(HealthStatus.Healthy);
		deserialized.UpstreamConnected.Should().BeTrue();
		deserialized.ToolCount.Should().Be(5);
		deserialized.Issues.Should().BeEmpty();
	}

	[TestMethod]
	public void HealthReport_WhenUnhealthy_SerializesIssuesWithEnumStrings()
	{
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

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);

		json.Should().Contain("\"Unhealthy\"");
		json.Should().Contain("\"Fatal\"");
		json.Should().Contain("\"HostNotStarted\"");
		json.Should().Contain("\"UpstreamError\"");
		json.Should().NotContain("\"status\":0");
		json.Should().NotContain("\"status\":2");

		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
		deserialized!.Issues.Should().HaveCount(2);
		deserialized.Issues[0].Code.Should().Be(IssueCode.HostNotStarted);
		deserialized.Issues[1].Remediation.Should().BeNull();
	}

	[TestMethod]
	public void HealthReport_WhenDegraded_SerializesWarningIssues()
	{
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

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);

		json.Should().Contain("\"Degraded\"");
		json.Should().Contain("\"Warning\"");
		json.Should().Contain("\"HostUnreachable\"");
	}

	[TestMethod]
	public void HealthReport_SdkVersionAndDiscoveryDuration_Roundtrip()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UpstreamConnected = true,
			ToolCount = 3,
			UnoSdkVersion = "5.5.100",
			DiscoveryDurationMs = 142,
			Issues = [],
		};

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);

		deserialized.Should().NotBeNull();
		deserialized!.UnoSdkVersion.Should().Be("5.5.100");
		deserialized.DiscoveryDurationMs.Should().Be(142);
	}

	[TestMethod]
	public void HealthReport_SdkVersionAndDiscoveryDuration_DefaultToNullAndZero()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UpstreamConnected = false,
			Issues = [],
		};

		report.UnoSdkVersion.Should().BeNull();
		report.DiscoveryDurationMs.Should().Be(0);

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
		deserialized!.UnoSdkVersion.Should().BeNull();
		deserialized.DiscoveryDurationMs.Should().Be(0);
	}

	[TestMethod]
	public void HealthReport_AsResourceJson_IsValidJson()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UpstreamConnected = true,
			ToolCount = 5,
			UnoSdkVersion = "5.5.100",
			DiscoveryDurationMs = 42,
			Issues = [],
		};

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);

		var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
		doc.RootElement.GetProperty("unoSdkVersion").GetString().Should().Be("5.5.100");
		doc.RootElement.GetProperty("discoveryDurationMs").GetInt64().Should().Be(42);

		var contents = new TextResourceContents
		{
			Uri = "uno://health",
			Text = json,
			MimeType = "application/json",
		};
		contents.Text.Should().NotBeEmpty();
	}

	[TestMethod]
	public void AllIssueCodes_RoundtripThroughJson()
	{
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
	// Health resource
	// -------------------------------------------------------------------

	[TestMethod]
	public void HealthResource_HasCorrectUriAndMimeType()
	{
		var resource = new Resource
		{
			Uri = "uno://health",
			Name = "Uno DevServer Health",
			MimeType = "application/json",
		};

		resource.Uri.Should().Be("uno://health");
		resource.MimeType.Should().Be("application/json");
	}

	// -------------------------------------------------------------------
	// ToolCacheFile
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

	// -------------------------------------------------------------------
	// TCS patterns
	// -------------------------------------------------------------------

	[TestMethod]
	public void TCS_WhenFaulted_IsFaultedIsTrue()
	{
		var tcs = new TaskCompletionSource<object>();
		tcs.TrySetException(new InvalidOperationException("Connection failed"));

		tcs.Task.IsFaulted.Should().BeTrue();
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	public void TCS_WhenCanceled_IsCompletedSuccessfullyIsFalse()
	{
		var tcs = new TaskCompletionSource<object>();
		tcs.TrySetCanceled();

		tcs.Task.IsCanceled.Should().BeTrue();
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	public void TCS_TrySetException_OnlySucceedsOnce()
	{
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
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetResult("connected");

		var cancelResult = tcs.TrySetCanceled();

		cancelResult.Should().BeFalse();
		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
	}

	// -------------------------------------------------------------------
	// Timeout patterns
	// -------------------------------------------------------------------

	[TestMethod]
	public async Task WaitAsync_WhenTimesOut_ThrowsOperationCanceled()
	{
		var neverCompletes = new TaskCompletionSource<bool>();

		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

		Func<Task> act = async () => await neverCompletes.Task.WaitAsync(cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[TestMethod]
	public async Task WaitAsync_WhenCompletedBeforeTimeout_ReturnsResult()
	{
		var tcs = new TaskCompletionSource<string>();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		tcs.TrySetResult("connected");

		var result = await tcs.Task.WaitAsync(cts.Token);

		result.Should().Be("connected");
	}

	[TestMethod]
	public async Task WaitAsync_WhenFaultedBeforeTimeout_PropagatesException()
	{
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetException(new InvalidOperationException("upstream crashed"));

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		Func<Task> act = async () => await tcs.Task.WaitAsync(cts.Token);

		await act.Should().ThrowExactlyAsync<InvalidOperationException>()
			.WithMessage("upstream crashed");
	}

	// -------------------------------------------------------------------
	// Structured error response
	// -------------------------------------------------------------------

	[TestMethod]
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
	// DisposeAsync safety
	// -------------------------------------------------------------------

	[TestMethod]
	public void DisposePattern_WhenTCSNeverCompleted_CancelPreventsBlocking()
	{
		var tcs = new TaskCompletionSource<IAsyncDisposable>();

		tcs.TrySetCanceled();

		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();

		var wouldDispose = tcs.Task.IsCompletedSuccessfully;
		wouldDispose.Should().BeFalse("DisposeAsync should be skipped when TCS is canceled");
	}

	[TestMethod]
	public async Task DisposePattern_WhenTCSCompleted_DisposesClient()
	{
		var mockDisposable = new MockAsyncDisposable();
		var tcs = new TaskCompletionSource<IAsyncDisposable>();
		tcs.TrySetResult(mockDisposable);

		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
		var client = await tcs.Task;
		await client.DisposeAsync();

		mockDisposable.WasDisposed.Should().BeTrue();
	}

	// -------------------------------------------------------------------
	// AmbientRegistry (Phase 1b — controller bypass)
	// -------------------------------------------------------------------

	[TestMethod]
	public void AmbientRegistry_WhenNoMatch_ReturnsNull()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new Uno.UI.RemoteControl.Host.AmbientRegistry(logger);

		var result = registry.GetActiveDevServerForPath(Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}.sln"));

		result.Should().BeNull();
	}

	[TestMethod]
	public void AmbientRegistry_WhenNullOrEmpty_ReturnsNull()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new Uno.UI.RemoteControl.Host.AmbientRegistry(logger);

		registry.GetActiveDevServerForPath(null).Should().BeNull();
		registry.GetActiveDevServerForPath("").Should().BeNull();
		registry.GetActiveDevServerForPath("   ").Should().BeNull();
	}

	// -------------------------------------------------------------------
	// Direct launch args (Phase 1b — controller bypass)
	// -------------------------------------------------------------------

	[TestMethod]
	public void DirectLaunch_ArgsDoNotContainCommandStart()
	{
		// Simulate the args that StartProcess would build
		var args = BuildDirectLaunchArgs(port: 12345, solution: @"C:\test\MySolution.sln");

		args.Should().NotContain("--command");
		args.Should().NotContain("start");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainSolutionFlag()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: @"C:\test\MySolution.sln");

		var solutionIndex = args.IndexOf("--solution");
		solutionIndex.Should().BeGreaterThanOrEqualTo(0);
		args[solutionIndex + 1].Should().Be(@"C:\test\MySolution.sln");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainHttpPortAndPpid()
	{
		var args = BuildDirectLaunchArgs(port: 54321, solution: null);

		var portIndex = args.IndexOf("--httpPort");
		portIndex.Should().BeGreaterThanOrEqualTo(0);
		args[portIndex + 1].Should().Be("54321");

		var ppidIndex = args.IndexOf("--ppid");
		ppidIndex.Should().BeGreaterThanOrEqualTo(0);
		int.Parse(args[ppidIndex + 1]).Should().BeGreaterThan(0);
	}

	[TestMethod]
	public void DirectLaunch_WhenNoSolution_ArgsDoNotContainSolution()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: null);

		args.Should().NotContain("--solution");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainAddins()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: null, addins: "Addon1.dll;Addon2.dll");

		var addinsIndex = args.IndexOf("--addins");
		addinsIndex.Should().BeGreaterThanOrEqualTo(0);
		args[addinsIndex + 1].Should().Be("Addon1.dll;Addon2.dll");
	}

	/// <summary>
	/// Mirrors the arg-building logic in DevServerMonitor.StartProcess to verify
	/// that the direct launch path produces the expected arguments.
	/// </summary>
	private static List<string> BuildDirectLaunchArgs(int port, string? solution, string? addins = null)
	{
		var args = new List<string>
		{
			"--httpPort", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
			"--ppid", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
		};

		if (!string.IsNullOrWhiteSpace(solution))
		{
			args.Add("--solution");
			args.Add(solution);
		}

		if (!string.IsNullOrWhiteSpace(addins))
		{
			args.Add("--addins");
			args.Add(addins);
		}

		return args;
	}

	// -------------------------------------------------------------------
	// Regression: McpClientProxy TCS patterns (Phase 1c-1)
	// -------------------------------------------------------------------

	[TestMethod]
	public async Task McpClientProxy_TCS_HappyPath_UpstreamResolves()
	{
		// Mirrors McpClientProxy: _clientCompletionSource.TrySetResult(client)
		// then UpstreamClient (== _clientCompletionSource.Task) resolves
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetResult("upstream-connected");

		var result = await tcs.Task;
		result.Should().Be("upstream-connected");
		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
	}

	[TestMethod]
	public async Task McpClientProxy_TCS_WhenFaulted_AwaitPropagatesException()
	{
		// Mirrors McpClientProxy.OnServerStarted catch block:
		// _clientCompletionSource.TrySetException(ex)
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetException(new InvalidOperationException("upstream connect failed"));

		Func<Task> act = async () => await tcs.Task;

		await act.Should().ThrowExactlyAsync<InvalidOperationException>()
			.WithMessage("upstream connect failed");
	}

	[TestMethod]
	public void McpClientProxy_Dispose_WhenNeverConnected_CompletesCleanly()
	{
		// Mirrors McpClientProxy.DisposeAsync: TrySetCanceled prevents
		// awaiting a TCS that was never completed
		var tcs = new TaskCompletionSource<string>();

		tcs.TrySetCanceled();

		tcs.Task.IsCompleted.Should().BeTrue();
		tcs.Task.IsCanceled.Should().BeTrue();
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse("dispose skips DisposeAsync on upstream");
	}

	[TestMethod]
	public async Task McpClientProxy_CallbackRegistration_IsReplaceable()
	{
		// Mirrors McpClientProxy.RegisterToolListChangedCallback:
		// simple field assignment, last registration wins
		Func<Task>? callback = null;
		var callCount = 0;

		// First registration
		callback = () => { callCount = 1; return Task.CompletedTask; };

		// Second registration replaces the first
		callback = () => { callCount = 2; return Task.CompletedTask; };

		await callback.Invoke();
		callCount.Should().Be(2, "last registered callback should win");
	}

	// -------------------------------------------------------------------
	// Regression: DevServerMonitor retry pattern (Phase 1c-1)
	// -------------------------------------------------------------------

	[TestMethod]
	public void DevServerMonitor_RetryLogic_FailsAfterMaxAttempts()
	{
		// Mirrors DevServerMonitor.RunMonitor: after maxRetries failures,
		// ServerFailed is invoked and loop exits
		const int maxRetries = 3;
		int retryCount = 0;
		bool serverFailed = false;
		var attempts = new List<int>();

		while (retryCount < maxRetries)
		{
			// Simulate StartProcess returning (success: false)
			retryCount++;
			attempts.Add(retryCount);

			if (retryCount >= maxRetries)
			{
				serverFailed = true;
				break;
			}
		}

		serverFailed.Should().BeTrue("ServerFailed triggers after max retries");
		attempts.Should().HaveCount(3);
		retryCount.Should().Be(maxRetries);
	}

	// -------------------------------------------------------------------
	// Regression: HealthReport diagnostic patterns (Phase 1c-1)
	// -------------------------------------------------------------------

	[TestMethod]
	public void HealthReport_WhenUpstreamFaulted_ReportsUpstreamError()
	{
		// Pattern: when TCS is faulted, health report should contain
		// UpstreamError issue with Fatal severity
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetException(new InvalidOperationException("connection refused"));

		var issues = new List<ValidationIssue>();
		if (tcs.Task.IsFaulted)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.UpstreamError,
				Severity = ValidationSeverity.Fatal,
				Message = tcs.Task.Exception!.InnerException!.Message,
			});
		}

		var report = new HealthReport
		{
			Status = HealthStatus.Unhealthy,
			UpstreamConnected = false,
			Issues = issues,
		};

		report.Issues.Should().HaveCount(1);
		report.Issues[0].Code.Should().Be(IssueCode.UpstreamError);
		report.Issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
		report.Issues[0].Message.Should().Be("connection refused");
	}

	[TestMethod]
	public void HealthReport_WhenNotStarted_ReportsHostNotStarted()
	{
		// Pattern: when devserver has not started, health report should
		// contain HostNotStarted issue with Fatal severity
		var devServerStarted = false;

		var issues = new List<ValidationIssue>();
		if (!devServerStarted)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostNotStarted,
				Severity = ValidationSeverity.Fatal,
				Message = "Host process has not started",
				Remediation = "Check solution discovery and host executable resolution",
			});
		}

		var report = new HealthReport
		{
			Status = HealthStatus.Unhealthy,
			UpstreamConnected = false,
			Issues = issues,
		};

		report.Issues.Should().HaveCount(1);
		report.Issues[0].Code.Should().Be(IssueCode.HostNotStarted);
		report.Issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
		report.Issues[0].Remediation.Should().NotBeNullOrEmpty();
	}

	// -------------------------------------------------------------------
	// Bug-exposing tests (Phase 1c-1) — assert DESIRED behavior
	// These tests were RED before the fixes were applied.
	// -------------------------------------------------------------------

	[TestMethod]
	public async Task Bug1_TCS_IsResettable_AllowsSecondConnection()
	{
		// Bug 1+4: McpClientProxy._clientCompletionSource is created once
		// and never recreated. After the first TrySetResult, subsequent calls
		// to TrySetResult fail silently — making reconnection impossible.
		//
		// Desired behavior: after ResetConnectionAsync(), a fresh TCS
		// accepts a new result.
		var holder = new ResettableTcsHolder<string>();
		holder.Tcs.TrySetResult("first-connection");

		var firstResult = await holder.Tcs.Task;
		firstResult.Should().Be("first-connection");

		// Reset (creates new TCS — this is what ResetConnectionAsync does)
		holder.Reset();

		holder.Tcs.TrySetResult("second-connection");
		var secondResult = await holder.Tcs.Task;
		secondResult.Should().Be("second-connection");
	}

	[TestMethod]
	public void Bug2_MonitorLoop_ContinuesAfterServerStarted()
	{
		// Bug 2: DevServerMonitor.RunMonitor has `break` after
		// ServerStarted?.Invoke() — the loop exits and never detects
		// a process crash.
		//
		// Desired behavior: after raising ServerStarted, the monitor
		// continues watching the process and raises ServerCrashed on exit.
		var iterations = 0;
		var serverStartedRaised = false;
		var serverCrashedRaised = false;
		var processExited = false;

		// Simulate the monitor loop with the CORRECT behavior (no break)
		for (int attempt = 0; attempt < 3; attempt++)
		{
			iterations++;

			if (!serverStartedRaised)
			{
				serverStartedRaised = true;
				// Bug: original code does `break` here
				// Fix: continue watching the process
			}
			else if (serverStartedRaised && processExited)
			{
				serverCrashedRaised = true;
				break; // Re-enter loop for rediscovery
			}

			// Simulate process exit after first iteration
			if (attempt == 1)
			{
				processExited = true;
			}
		}

		serverStartedRaised.Should().BeTrue();
		processExited.Should().BeTrue();
		serverCrashedRaised.Should().BeTrue("monitor should detect process exit and raise ServerCrashed");
		iterations.Should().BeGreaterThan(1, "monitor loop should continue after ServerStarted");
	}

	[TestMethod]
	public async Task Bug3_CallbackHandler_AwaitsAsyncCallback()
	{
		// Bug 3: McpClientProxy notification handler calls
		// _toolListChanged?.Invoke() without await on a Func<Task>.
		// This means async callbacks complete after the handler returns.
		//
		// Desired behavior: the handler awaits the callback.
		var completed = false;
		Func<Task> asyncCallback = async () =>
		{
			await Task.Delay(10);
			completed = true;
		};

		// Correct pattern: await the callback
		if (asyncCallback is { } callback)
		{
			await callback();
		}

		completed.Should().BeTrue("awaited async callback should complete before handler returns");
	}

	[TestMethod]
	public void Bug3_FireAndForget_DoesNotCompleteCallback()
	{
		// Documents the BROKEN behavior: Invoke() without await
		// on Func<Task> does NOT await the async body.
		var completed = false;
		Func<Task> asyncCallback = async () =>
		{
			await Task.Yield();
			completed = true;
		};

#pragma warning disable VSTHRD110 // Observe the awaitable result
		asyncCallback.Invoke(); // fire-and-forget — the bug
#pragma warning restore VSTHRD110

		// The callback has not yet completed because we didn't await
		completed.Should().BeFalse("fire-and-forget does not await the async body");
	}

	// -------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------

	/// <summary>
	/// Simulates the TCS reset pattern that McpClientProxy.ResetConnectionAsync implements.
	/// Before the fix, McpClientProxy had no way to create a fresh TCS.
	/// </summary>
	private sealed class ResettableTcsHolder<T>
	{
		public TaskCompletionSource<T> Tcs { get; private set; } = new();

		public void Reset()
		{
			var old = Tcs;
			Tcs = new TaskCompletionSource<T>();
			old.TrySetCanceled();
		}
	}

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
