using Microsoft.Extensions.Time.Testing;
using Uno.UI.RemoteControl.Server.AppLaunch;

namespace Uno.UI.RemoteControl.DevServer.Tests.AppLaunch;

[TestClass]
public class Given_ApplicationLaunchMonitor
{
	private static ApplicationLaunchMonitor CreateMonitor(
		out FakeTimeProvider clock,
		out List<ApplicationLaunchMonitor.LaunchEvent> registered,
		out List<ApplicationLaunchMonitor.LaunchEvent> timeouts,
		out List<ApplicationLaunchMonitor.LaunchEvent> connections,
		out List<bool> connectionWasTimedOut,
		TimeSpan? timeout = null,
		TimeSpan? retention = null,
		TimeSpan? scavengeInterval = null)
	{
		// Use local lists inside callbacks, then assign them to out parameters
		var registeredList = new List<ApplicationLaunchMonitor.LaunchEvent>();
		var timeoutsList = new List<ApplicationLaunchMonitor.LaunchEvent>();
		var connectionsList = new List<ApplicationLaunchMonitor.LaunchEvent>();
		var connectionsTimedOutList = new List<bool>();

		var options = new ApplicationLaunchMonitor.Options
		{
			Timeout = timeout ?? TimeSpan.FromSeconds(10),
			OnRegistered = e => registeredList.Add(e),
			OnTimeout = e => timeoutsList.Add(e),
			OnConnected = (e, wasTimedOut) =>
			{
				connectionsList.Add(e);
				connectionsTimedOutList.Add(wasTimedOut);
			},
			Retention = retention ?? TimeSpan.FromHours(1),
			ScavengeInterval = scavengeInterval ?? TimeSpan.FromMinutes(5),
		};

		clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
		registered = registeredList;
		timeouts = timeoutsList;
		connections = connectionsList;
		connectionWasTimedOut = connectionsTimedOutList;

		return new ApplicationLaunchMonitor(clock, options);
	}

	[TestMethod]
	public void WhenLaunchRegisteredAndNotTimedOut_ThenRegisteredCallbackOnly()
	{
		// Arrange
		using var sut = CreateMonitor(
			out var clock,
			out var registered,
			out var timeouts,
			out var connections,
			out _,
			timeout: TimeSpan.FromSeconds(10));
		var mvid = Guid.NewGuid();

		// Act
		sut.RegisterLaunch(mvid, "Wasm", isDebug: true, ide: "UnitTestIDE", plugin: "unit-plugin");
		clock.Advance(TimeSpan.FromSeconds(5));

		// Assert
		registered.Should().HaveCount(1);
		timeouts.Should().BeEmpty();
		connections.Should().BeEmpty();
	}

	[TestMethod]
	public void WhenMatchingConnectionReportedForRegisteredLaunch_ThenConnectedInvokedOnceAndConsumed()
	{
		// Arrange
		using var sut = CreateMonitor(
			out var clock,
			out _,
			out var timeouts,
			out var connections,
			out _);
		var mvid = Guid.NewGuid();
		sut.RegisterLaunch(mvid, "Wasm", isDebug: true, ide: "UnitTestIDE", plugin: "unit-plugin");

		// Act
		sut.ReportConnection(mvid, "Wasm", isDebug: true);
		clock.Advance(TimeSpan.FromSeconds(500));

		// Assert
		connections.Should().HaveCount(1);
		timeouts.Should().BeEmpty();

		// And: second report should not match (consumed)
		sut.ReportConnection(mvid, "Wasm", isDebug: true);
		connections.Should().HaveCount(1);
	}

	[TestMethod]
	public void WhenConnectionsArriveForMultipleRegistrations_ThenFifoOrderIsPreserved()
	{
		// Arrange: ensure timeouts can expire earlier registrations and FIFO is preserved for active ones
		using var sut = CreateMonitor(
			out var clock,
			out _,
			out var timeouts,
			out var connections,
			out _,
			timeout: TimeSpan.FromMilliseconds(500));
		var mvid = Guid.NewGuid();

		// First registration - will be expired
		sut.RegisterLaunch(mvid, "Wasm", isDebug: true, ide: "UnitTestIDE", plugin: "unit-plugin");
		// Advance beyond timeout so the first one expires
		clock.Advance(TimeSpan.FromMilliseconds(600));

		// Two active registrations that should remain in FIFO order
		sut.RegisterLaunch(mvid, "Wasm", isDebug: true, ide: "UnitTestIDE", plugin: "unit-plugin");
		clock.Advance(TimeSpan.FromMilliseconds(1));
		sut.RegisterLaunch(mvid, "Wasm", isDebug: true, ide: "UnitTestIDE", plugin: "unit-plugin");

		// Act
		sut.ReportConnection(mvid, "Wasm", isDebug: true);
		sut.ReportConnection(mvid, "Wasm", isDebug: true);

		// Assert
		// First registration should have timed out
		timeouts.Should().HaveCount(1);
		connections.Should().HaveCount(2);
		connections[0].RegisteredAt.Should().BeOnOrBefore(connections[1].RegisteredAt);
	}

	[TestMethod]
	public void WhenManyRegistrationsWithMixedTimeouts_FIFOOrderStillPreserved_Stress()
	{
		// Stress test: large number of registrations where many expire, then a batch of active registrations
		const int K = 9000; // number to expire
		const int L = 1000; // number to remain active and validate FIFO on

		using var sut = CreateMonitor(
			out var clock,
			out _,
			out var timeouts,
			out var connections,
			out _,
			timeout: TimeSpan.FromMilliseconds(100));
		var mvid = Guid.NewGuid();

		// Register K entries which will be expired
		for (var i = 0; i < K; i++)
		{
			sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin");
			clock.Advance(TimeSpan.FromTicks(1));
		}

		// Advance to let those K entries time out
		clock.Advance(TimeSpan.FromMilliseconds(150));

		// Register L active entries without advancing the global clock so they remain within the timeout window
		for (var i = 0; i < L; i++)
		{
			sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin");
			clock.Advance(TimeSpan.FromTicks(1));
			// Do NOT advance the clock here: advancing would make early active entries expire before we report connections.
		}

		// Act: report L times
		for (var i = 0; i < L; i++)
		{
			sut.ReportConnection(mvid, "Wasm", isDebug: false);
			clock.Advance(TimeSpan.FromTicks(1));
		}

		// Assert: at least K should have timed out, and we should have L connections in FIFO order
		timeouts.Count.Should().BeGreaterThanOrEqualTo(K);
		connections.Should().HaveCount(L);
		for (var i = 1; i < connections.Count; i++)
		{
			connections[i - 1].RegisteredAt.Should().BeOnOrBefore(connections[i].RegisteredAt);
		}
	}

	[TestMethod]
	public void WhenRegisteredLaunchTimeoutExpires_ThenTimeoutCallbackInvoked()
	{
		// Arrange
		using var sut = CreateMonitor(
			out var clock,
			out _,
			out var timeouts,
			out _,
			out _,
			timeout: TimeSpan.FromSeconds(10));
		var mvid = Guid.NewGuid();
		sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin");

		// Act
		clock.Advance(TimeSpan.FromSeconds(11));

		// Assert
		timeouts.Should().HaveCount(1);
	}

	[TestMethod]
	public void WhenTimeoutExpiresWithMixedExpiredAndActive_ThenOnlyExpiredAreRemoved()
	{
		// Arrange
		using var sut = CreateMonitor(
			out var clock,
			out _,
			out var timeouts,
			out var connections,
			out _);
		var mvid = Guid.NewGuid();
		sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin"); // will expire
		clock.Advance(TimeSpan.FromSeconds(5));
		sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin"); // still active

		// Act
		clock.Advance(TimeSpan.FromSeconds(6)); // first expired, second still active

		// Assert
		timeouts.Should().HaveCount(1);
		// Active one should still be connectable
		sut.ReportConnection(mvid, "Wasm", isDebug: false);
		connections.Should().HaveCount(1);
	}

	[TestMethod]
	public void WhenPlatformIsNullOrEmptyInRegisterOrReport_ThenThrowsArgumentException()
	{
		using var sut = CreateMonitor(out _, out _, out _, out _, out _);
		var mvid = Guid.NewGuid();

		sut.Invoking(m => m.RegisterLaunch(mvid, string.Empty, true, ide: "UnitTestIDE", plugin: "unit-plugin")).Should().Throw<ArgumentException>();
		sut.Invoking(m => m.ReportConnection(mvid, string.Empty, true)).Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void WhenPlatformDiffersByCaseOnReportConnection_ThenItDoesNotMatch()
	{
		using var sut = CreateMonitor(out var clock, out _, out _, out var connections, out _);
		var mvid = Guid.NewGuid();
		sut.RegisterLaunch(mvid, "Wasm", true, ide: "UnitTestIDE", plugin: "unit-plugin");
		clock.Advance(TimeSpan.FromSeconds(1)); // bellow timeout

		sut.ReportConnection(mvid, "wasm", true);

		connections.Should().BeEmpty();
	}

	[TestMethod]
	public void ReportConnection_ReturnsBooleanIndicatingMatch()
	{
		using var sut = CreateMonitor(out var clock, out _, out _, out var connections, out _);
		var mvid = Guid.NewGuid();
		// No registration yet -> false
		sut.ReportConnection(mvid, "Wasm", isDebug: true).Should().BeFalse();

		// Register and then report -> true
		sut.RegisterLaunch(mvid, "Wasm", isDebug: true, ide: "UnitTestIDE", plugin: "unit-plugin");
		sut.ReportConnection(mvid, "Wasm", isDebug: true).Should().BeTrue();

		// Already consumed -> false
		sut.ReportConnection(mvid, "Wasm", isDebug: true).Should().BeFalse();
	}

	[TestMethod]
	public void WhenConnectionArrivesAfterTimeout_ThenStillConnects()
	{
		using var sut = CreateMonitor(
			out var clock,
			out _,
			out var timeouts,
			out var connections,
			out _,
			timeout: TimeSpan.FromSeconds(5));
		var mvid = Guid.NewGuid();
		sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin");

		// Advance past timeout so OnTimeout is invoked
		clock.Advance(TimeSpan.FromSeconds(6));
		timeouts.Should().HaveCount(1);

		// Even though it timed out, a later connection should still be accepted
		var result = sut.ReportConnection(mvid, "Wasm", isDebug: false);
		result.Should().BeTrue();
		connections.Should().HaveCount(1);
		// OnTimeout should still have been invoked
		timeouts.Should().HaveCount(1);
	}

	[TestMethod]
	public void WhenScavengerRunsRepeatedly_ThenVeryOldEntriesAreRemoved()
	{
		// Arrange: Timeout long, retention short, scavenge frequent
		using var sut = CreateMonitor(
			out var clock,
			out _,
			out _,
			out var connections,
			out _,
			timeout: TimeSpan.FromMinutes(10),
			retention: TimeSpan.FromSeconds(2),
			scavengeInterval: TimeSpan.FromSeconds(1));

		var mvid = Guid.NewGuid();
		sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin");

		// Advance time past retention and run multiple scavenge intervals to ensure the periodic scavenge executed more than once
		clock.Advance(TimeSpan.FromSeconds(3)); // now > retention
		clock.Advance(TimeSpan.FromSeconds(1));
		clock.Advance(TimeSpan.FromSeconds(1));

		// Act: after scavenging, the entry should be removed and cannot be connected
		var result = sut.ReportConnection(mvid, "Wasm", isDebug: false);
		result.Should().BeFalse();
		connections.Should().BeEmpty();
	}

	[TestMethod]
	public void WhenScavengerRunsMultipleTimes_ThenOnlyOldEntriesRemovedAndNewerRemain()
	{
		using var sut = CreateMonitor(
			out var clock,
			out _,
			out _,
			out var connections,
			out _,
			timeout: TimeSpan.FromMinutes(10),
			retention: TimeSpan.FromSeconds(2),
			scavengeInterval: TimeSpan.FromSeconds(1));

		var mvid = Guid.NewGuid();

		// Register first entry
		sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin");
		clock.Advance(TimeSpan.FromSeconds(1));

		// Register second entry (newer)
		sut.RegisterLaunch(mvid, "Wasm", isDebug: false, ide: "UnitTestIDE", plugin: "unit-plugin");

		// We only advance enough time so multiple scavenge passes run (scavenge interval = 1s)
		// but the newer entry (registered 1s after the first) is still within the retention window.
		// Advance two seconds so scavenge runs (at t=2 and earlier), but newer entry (registered at t=1)
		// remains within the retention window (cutoff = now - 2s => 0s, older entry removed, newer kept).
		clock.Advance(TimeSpan.FromSeconds(2));

		// Act: reporting twice should yield a single match (the newer entry), then no more
		var first = sut.ReportConnection(mvid, "Wasm", isDebug: false);
		var second = sut.ReportConnection(mvid, "Wasm", isDebug: false);

		first.Should().BeTrue();
		second.Should().BeFalse();
		connections.Should().HaveCount(1);
	}
}
