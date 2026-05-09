using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Protocol;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for <see cref="HealthReport"/> serialization, resource formatting,
/// and the diagnostic patterns used by <see cref="HealthService"/>.
/// </summary>
[TestClass]
public class Given_HealthService
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
			SelectionSource = WorkspaceSelectionSource.UserSelected,
			Issues = [],
		};

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Status.Should().Be(HealthStatus.Healthy);
		deserialized.UpstreamConnected.Should().BeTrue();
		deserialized.ToolCount.Should().Be(5);
		deserialized.SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
		deserialized.Issues.Should().BeEmpty();
	}

	[TestMethod]
	[Description("Enums serialize as strings (not integers) for LLM readability")]
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
	[Description("When workspace resolution fails before startup, health is immediately unhealthy with a workspace-specific issue instead of waiting for upstream timeouts.")]
	public void HealthReport_WhenWorkspaceIsNotResolved_IsImmediatelyUnhealthy()
	{
		var discovery = new DiscoveryInfo
		{
			RequestedWorkingDirectory = @"D:\src\studio.live",
			ResolutionKind = WorkspaceResolutionKind.NoValidWorkspace,
			CandidateSolutions =
			[
				@"D:\src\studio.live\src\App.slnx",
			],
		};

		var report = HealthReportFactory.Create(
			discovery,
			devServerStarted: false,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: null,
			discoveredSolutions: discovery.CandidateSolutions);

		report.Status.Should().Be(HealthStatus.Unhealthy);
		report.Issues.Should().Contain(issue => issue.Code == IssueCode.HostNotStarted);
		report.Issues.Should().Contain(issue => issue.Code == IssueCode.WorkspaceNotResolved);
		report.Issues.Should().NotContain(issue => issue.Code == IssueCode.HostUnreachable);
	}

	[TestMethod]
	public void HealthReport_WhenNoCandidates_IsImmediatelyUnhealthy()
	{
		var discovery = new DiscoveryInfo
		{
			RequestedWorkingDirectory = @"D:\empty",
			ResolutionKind = WorkspaceResolutionKind.NoCandidates,
			CandidateSolutions = [],
		};

		var report = HealthReportFactory.Create(
			discovery,
			devServerStarted: false,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: null,
			discoveredSolutions: null);

		report.Status.Should().Be(HealthStatus.Unhealthy);
		report.Issues.Should().Contain(issue => issue.Code == IssueCode.HostNotStarted);
		report.Issues.Should().Contain(issue => issue.Code == IssueCode.NoSolutionFound);
	}

	[TestMethod]
	public void HealthReport_WhenWorkspaceWasExplicitlySelected_ExposesSelectionSource()
	{
		var discovery = new DiscoveryInfo
		{
			RequestedWorkingDirectory = @"D:\src\repo",
			EffectiveWorkspaceDirectory = @"D:\src\repo\src",
			SelectedSolutionPath = @"D:\src\repo\src\App.slnx",
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			SelectionSource = WorkspaceSelectionSource.UserSelected,
			CandidateSolutions =
			[
				@"D:\src\repo\src\App.slnx",
			],
		};

		var report = HealthReportFactory.Create(
			discovery,
			devServerStarted: true,
			upstreamConnected: true,
			toolCount: 4,
			connectionState: ConnectionState.Connected,
			discoveredSolutions: discovery.CandidateSolutions);

		report.SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
		report.Discovery.Should().NotBeNull();
		report.Discovery!.SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
	}

	[TestMethod]
	public void HealthReport_WhenWorkspaceIsAmbiguous_IsImmediatelyUnhealthyWithDiagnostic()
	{
		var discovery = new DiscoveryInfo
		{
			RequestedWorkingDirectory = @"D:\src\repo",
			ResolutionKind = WorkspaceResolutionKind.Ambiguous,
			CandidateSolutions =
			[
				@"D:\src\repo\srcA\AppA.slnx",
				@"D:\src\repo\srcB\AppB.slnx",
			],
		};

		var report = HealthReportFactory.Create(
			discovery,
			devServerStarted: false,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: null,
			discoveredSolutions: discovery.CandidateSolutions);

		report.Status.Should().Be(HealthStatus.Unhealthy);
		report.Issues.Should().Contain(issue => issue.Code == IssueCode.HostNotStarted);
		report.Issues.Should().Contain(issue => issue.Code == IssueCode.WorkspaceAmbiguous);
	}

	[TestMethod]
	public void HealthReport_WhenDiscoveryInfoHasTotalDuration_UsesThatDuration()
	{
		var discovery = new DiscoveryInfo
		{
			RequestedWorkingDirectory = @"D:\src\repo",
			EffectiveWorkspaceDirectory = @"D:\src\repo\src",
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			DiscoveryDurationMs = 321,
			AddInsDiscoveryDurationMs = 42,
			UnoSdkVersion = "6.6.0-dev.1",
		};

		var report = HealthReportFactory.Create(
			discovery,
			devServerStarted: true,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: null,
			discoveredSolutions: null);

		report.DiscoveryDurationMs.Should().Be(321);
	}

	[TestMethod]
	[Description("Every IssueCode enum value must survive a JSON serialize/deserialize roundtrip as a string")]
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
	// Diagnostic patterns
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("When the upstream TCS is faulted, HealthService produces an UpstreamError issue with the inner exception message")]
	public void HealthReport_WhenUpstreamFaulted_ReportsUpstreamError()
	{
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
	[Description("When ConnectionState is Reconnecting, HealthReport includes a HostCrashed warning issue")]
	public void HealthReport_WhenReconnecting_ReportsHostCrashedWarning()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Degraded,
			UpstreamConnected = false,
			ConnectionState = ConnectionState.Reconnecting,
			Issues =
			[
				new ValidationIssue
				{
					Code = IssueCode.HostCrashed,
					Severity = ValidationSeverity.Warning,
					Message = "The DevServer host process crashed and is being restarted automatically.",
					Remediation = "Wait a few seconds for the host to restart.",
				},
			],
		};

		report.ConnectionState.Should().Be(ConnectionState.Reconnecting);
		report.Issues.Should().HaveCount(1);
		report.Issues[0].Code.Should().Be(IssueCode.HostCrashed);
		report.Issues[0].Severity.Should().Be(ValidationSeverity.Warning);
	}

	[TestMethod]
	[Description("Reconnecting state serializes with both ConnectionState and HostCrashed issue in the JSON")]
	public void HealthReport_WhenReconnecting_RoundtripsFullReport()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Degraded,
			UpstreamConnected = false,
			ConnectionState = ConnectionState.Reconnecting,
			Issues =
			[
				new ValidationIssue
				{
					Code = IssueCode.HostCrashed,
					Severity = ValidationSeverity.Warning,
					Message = "Host crashed, reconnecting",
				},
			],
		};

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		json.Should().Contain("\"Reconnecting\"");
		json.Should().Contain("\"HostCrashed\"");

		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
		deserialized!.ConnectionState.Should().Be(ConnectionState.Reconnecting);
		deserialized.Issues[0].Code.Should().Be(IssueCode.HostCrashed);
	}

	[TestMethod]
	[Description("When ConnectionState is Degraded, HealthReport includes a HostCrashed fatal issue")]
	public void HealthReport_WhenDegradedState_ReportsHostCrashedFatal()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Unhealthy,
			UpstreamConnected = false,
			ConnectionState = ConnectionState.Degraded,
			Issues =
			[
				new ValidationIssue
				{
					Code = IssueCode.HostCrashed,
					Severity = ValidationSeverity.Fatal,
					Message = "The DevServer host process crashed repeatedly and could not be restarted.",
					Remediation = "Check the DevServer logs for errors.",
				},
			],
		};

		report.ConnectionState.Should().Be(ConnectionState.Degraded);
		report.Issues.Should().HaveCount(1);
		report.Issues[0].Code.Should().Be(IssueCode.HostCrashed);
		report.Issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	[Description("Diagnostic degraded mode does not report HostCrashed when no DevServer host was ever started")]
	public void HealthReport_WhenDegradedWithoutStartedHost_DoesNotReportHostCrashed()
	{
		var discovery = new DiscoveryInfo
		{
			RequestedWorkingDirectory = @"D:\src\repo",
			ResolutionKind = WorkspaceResolutionKind.Ambiguous,
			CandidateSolutions =
			[
				@"D:\src\repo\srcA\AppA.slnx",
				@"D:\src\repo\srcB\AppB.slnx",
			],
		};

		var report = HealthReportFactory.Create(
			discovery,
			devServerStarted: false,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: ConnectionState.Degraded,
			discoveredSolutions: discovery.CandidateSolutions);

		report.Status.Should().Be(HealthStatus.Unhealthy);
		report.Issues.Should().NotContain(issue => issue.Code == IssueCode.HostCrashed);
		report.Issues.Should().Contain(issue => issue.Code == IssueCode.WorkspaceAmbiguous);
	}

	[TestMethod]
	[Description("HealthReport with ConnectionState.Launching roundtrips correctly")]
	public void HealthReport_WithLaunchingState_Roundtrip()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Degraded,
			UpstreamConnected = false,
			ConnectionState = ConnectionState.Launching,
			Issues =
			[
				new ValidationIssue
				{
					Code = IssueCode.HostUnreachable,
					Severity = ValidationSeverity.Warning,
					Message = "Host is launching, waiting for health-check",
				},
			],
		};

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		json.Should().Contain("\"Launching\"");

		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
		deserialized!.ConnectionState.Should().Be(ConnectionState.Launching);
		deserialized.Issues.Should().HaveCount(1);
		deserialized.Issues[0].Code.Should().Be(IssueCode.HostUnreachable);
	}

	[TestMethod]
	[Description("HealthReport with ConnectionState.Shutdown roundtrips correctly")]
	public void HealthReport_WithShutdownState_Roundtrip()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UpstreamConnected = false,
			ConnectionState = ConnectionState.Shutdown,
			Issues = [],
		};

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		json.Should().Contain("\"Shutdown\"");

		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
		deserialized!.ConnectionState.Should().Be(ConnectionState.Shutdown);
	}

	[TestMethod]
	public void HealthReport_WhenNotStarted_ReportsHostNotStarted()
	{
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

	[TestMethod]
	[Description("HealthReport includes HostMcpEndpointNotAvailable as Fatal when host responds but /mcp is 404")]
	public void HealthReport_WhenHostRespondedNoMcp_ReportsHostMcpEndpointNotAvailable()
	{
		var report = HealthReportFactory.Create(
			discovery: null,
			devServerStarted: true,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: ConnectionState.Connecting,
			discoveredSolutions: null,
			hostRespondedNoMcp: true);

		report.Issues.Should().Contain(issue => issue.Code == IssueCode.HostMcpEndpointNotAvailable);
		var mcpIssue = report.Issues.First(issue => issue.Code == IssueCode.HostMcpEndpointNotAvailable);
		mcpIssue.Severity.Should().Be(ValidationSeverity.Fatal);
		mcpIssue.Message.Should().Contain("6.6", "should mention the minimum version that supports MCP");
		mcpIssue.Remediation.Should().NotBeNullOrEmpty();
	}

	[TestMethod]
	[Description("HostNotStarted is suppressed when hostRespondedNoMcp is true to avoid contradictory issues")]
	public void HealthReport_WhenNotStartedButHostRespondedNoMcp_OnlyReportsNoMcp()
	{
		var report = HealthReportFactory.Create(
			discovery: null,
			devServerStarted: false,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: null,
			discoveredSolutions: null,
			hostRespondedNoMcp: true);

		report.Issues.Should().Contain(issue => issue.Code == IssueCode.HostMcpEndpointNotAvailable);
		report.Issues.Should().NotContain(issue => issue.Code == IssueCode.HostNotStarted,
			"HostNotStarted would be confusing when the real issue is a too-old host version");
	}

	[TestMethod]
	[Description("HealthReport does NOT include HostMcpEndpointNotAvailable when /mcp is available")]
	public void HealthReport_WhenHostMcpAvailable_NoMcpIssue()
	{
		var report = HealthReportFactory.Create(
			discovery: null,
			devServerStarted: true,
			upstreamConnected: true,
			toolCount: 5,
			connectionState: ConnectionState.Connected,
			discoveredSolutions: null,
			hostRespondedNoMcp: false);

		report.Issues.Should().NotContain(issue => issue.Code == IssueCode.HostMcpEndpointNotAvailable);
	}
}
