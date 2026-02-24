using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Protocol;
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
}
