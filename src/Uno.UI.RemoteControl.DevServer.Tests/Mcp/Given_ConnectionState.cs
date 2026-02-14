using System.Text.Json;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for <see cref="ConnectionState"/> enum serialization and
/// <see cref="HealthReport"/> integration with the new ConnectionState field.
/// </summary>
[TestClass]
public class Given_ConnectionState
{
	[TestMethod]
	[Description("ConnectionState enum values serialize as strings for LLM readability")]
	public void ConnectionState_SerializesAsString()
	{
		foreach (var state in Enum.GetValues<ConnectionState>())
		{
			var json = JsonSerializer.Serialize(state, McpJsonUtilities.DefaultOptions);
			json.Should().Contain($"\"{state}\"", $"ConnectionState.{state} should serialize as string");

			var deserialized = JsonSerializer.Deserialize<ConnectionState>(json, McpJsonUtilities.DefaultOptions);
			deserialized.Should().Be(state);
		}
	}

	[TestMethod]
	[Description("HealthReport with ConnectionState roundtrips through JSON correctly")]
	public void HealthReport_WithConnectionState_Roundtrip()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UpstreamConnected = true,
			ToolCount = 5,
			ConnectionState = ConnectionState.Connected,
			Issues = [],
		};

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		json.Should().Contain("\"Connected\"");

		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
		deserialized.Should().NotBeNull();
		deserialized!.ConnectionState.Should().Be(ConnectionState.Connected);
	}

	[TestMethod]
	[Description("HealthReport with null ConnectionState roundtrips correctly for backward compatibility")]
	public void HealthReport_WithNullConnectionState_Roundtrip()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UpstreamConnected = false,
			Issues = [],
		};

		report.ConnectionState.Should().BeNull();

		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
		deserialized!.ConnectionState.Should().BeNull();
	}

	[TestMethod]
	[Description("All ConnectionState enum values survive JSON roundtrip")]
	public void AllConnectionStates_RoundtripThroughJson()
	{
		foreach (var state in Enum.GetValues<ConnectionState>())
		{
			var report = new HealthReport
			{
				Status = HealthStatus.Healthy,
				UpstreamConnected = false,
				ConnectionState = state,
				Issues = [],
			};

			var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
			var deserialized = JsonSerializer.Deserialize<HealthReport>(json, McpJsonUtilities.DefaultOptions);
			deserialized!.ConnectionState.Should().Be(state, $"ConnectionState.{state} should roundtrip");
		}
	}
}
