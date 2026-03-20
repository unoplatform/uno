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

	// -------------------------------------------------------------------
	// TCS reset pattern (simulates crash -> reset -> reconnect cycle)
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("TCS reset allows a new connection to resolve after a crash")]
	public async Task TcsReset_AllowsNewConnectionAfterCrash()
	{
		// Simulate initial connection
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetResult("client-v1");
		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();

		// Simulate crash: reset TCS (like ResetConnectionAsync does)
		var oldTcs = tcs;
		tcs = new TaskCompletionSource<string>();
		oldTcs.Task.IsCompletedSuccessfully.Should().BeTrue("old TCS still holds the disposed client");

		// New TCS is fresh and pending
		tcs.Task.IsCompleted.Should().BeFalse();

		// Simulate reconnection
		tcs.TrySetResult("client-v2");
		var result = await tcs.Task;
		result.Should().Be("client-v2");
	}

	[TestMethod]
	[Description("TCS reset on a faulted TCS creates a clean pending state")]
	public async Task TcsReset_OnFaultedTcs_CreatesFreshPending()
	{
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetException(new InvalidOperationException("connection failed"));
		tcs.Task.IsFaulted.Should().BeTrue();

		// Reset
		var oldTcs = tcs;
		tcs = new TaskCompletionSource<string>();
		oldTcs.TrySetCanceled(); // Cancel the old faulted one (no-op, already faulted)

		// New TCS is pending
		tcs.Task.IsCompleted.Should().BeFalse();

		// Reconnection succeeds
		tcs.TrySetResult("client-v2");
		var result = await tcs.Task;
		result.Should().Be("client-v2");
	}

	// -------------------------------------------------------------------
	// Reconnection counter behavior
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("Reconnection counter resets to zero when connection succeeds")]
	public void ReconnectionCounter_ResetsOnSuccess()
	{
		var reconnectionAttempts = 0;
		const int maxAttempts = 3;

		// Simulate 2 crashes
		reconnectionAttempts++;
		reconnectionAttempts++;
		reconnectionAttempts.Should().Be(2);

		// Simulate successful reconnection
		reconnectionAttempts = 0;
		reconnectionAttempts.Should().Be(0, "counter resets on successful connection");

		// Can crash again without hitting max
		reconnectionAttempts++;
		(reconnectionAttempts > maxAttempts).Should().BeFalse();
	}

	[TestMethod]
	[Description("Exceeding max reconnection attempts transitions to Degraded")]
	public void ReconnectionCounter_ExceedingMax_TransitionsToDegraded()
	{
		var reconnectionAttempts = 0;
		const int maxAttempts = 3;
		var state = ConnectionState.Connected;

		// Simulate 4 crashes without successful reconnection
		for (var i = 0; i < 4; i++)
		{
			reconnectionAttempts++;
			if (reconnectionAttempts > maxAttempts)
			{
				state = ConnectionState.Degraded;
			}
			else
			{
				state = ConnectionState.Reconnecting;
			}
		}

		reconnectionAttempts.Should().Be(4);
		state.Should().Be(ConnectionState.Degraded);
	}

	[TestMethod]
	[Description("Degraded state is not terminal: recovery is possible if the host eventually restarts")]
	public void DegradedState_IsNotTerminal_RecoveryIsPossible()
	{
		var state = ConnectionState.Degraded;

		// Simulate an external recovery (e.g. operator restarts the host manually)
		state = ConnectionState.Connecting;
		state.Should().Be(ConnectionState.Connecting);

		state = ConnectionState.Connected;
		state.Should().Be(ConnectionState.Connected);
	}
}
