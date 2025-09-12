using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Uno.UI.RemoteControl.Server.AppLaunch;

namespace Uno.UI.RemoteControl.DevServer.Tests.AppLaunch
{
	[TestClass]
	public class Given_ApplicationLaunchMonitor
	{
		private static ApplicationLaunchMonitor CreateMonitor(
			out FakeTimeProvider clock,
			out List<ApplicationLaunchMonitor.LaunchEvent> registered,
			out List<ApplicationLaunchMonitor.LaunchEvent> timeouts,
			out List<ApplicationLaunchMonitor.LaunchEvent> connections,
			TimeSpan? timeout = null)
		{
			// Use local lists inside callbacks, then assign them to out parameters
			var registeredList = new List<ApplicationLaunchMonitor.LaunchEvent>();
			var timeoutsList = new List<ApplicationLaunchMonitor.LaunchEvent>();
			var connectionsList = new List<ApplicationLaunchMonitor.LaunchEvent>();

			var options = new ApplicationLaunchMonitor.Options
			{
				Timeout = timeout ?? TimeSpan.FromSeconds(10),
				OnRegistered = e => registeredList.Add(e),
				OnTimeout = e => timeoutsList.Add(e),
				OnConnected = e => connectionsList.Add(e),
			};

			clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
			registered = registeredList;
			timeouts = timeoutsList;
			connections = connectionsList;

			return new ApplicationLaunchMonitor(clock, options);
		}

		[TestMethod]
		public void WhenLaunchRegisteredAndNotTimedOut_ThenRegisteredCallbackOnly()
		{
			// Arrange
			using var sut = CreateMonitor(out var clock, out var registered, out var timeouts, out var connections,
				TimeSpan.FromSeconds(10));
			var mvid = Guid.NewGuid();

			// Act
			sut.RegisterLaunch(mvid, "Wasm", isDebug: true);
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
			using var sut = CreateMonitor(out var clock, out var registered, out var timeouts, out var connections);
			var mvid = Guid.NewGuid();
			sut.RegisterLaunch(mvid, "Wasm", isDebug: true);

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
			using var sut = CreateMonitor(out var clock, out _, out var timeouts, out var connections, TimeSpan.FromMilliseconds(500));
			var mvid = Guid.NewGuid();

			// First registration - will be expired
			sut.RegisterLaunch(mvid, "Wasm", isDebug: true);
			// Advance beyond timeout so the first one expires
			clock.Advance(TimeSpan.FromMilliseconds(600));

			// Two active registrations that should remain in FIFO order
			sut.RegisterLaunch(mvid, "Wasm", isDebug: true);
			clock.Advance(TimeSpan.FromMilliseconds(1));
			sut.RegisterLaunch(mvid, "Wasm", isDebug: true);

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

			using var sut = CreateMonitor(out var clock, out _, out var timeouts, out var connections, TimeSpan.FromMilliseconds(100));
			var mvid = Guid.NewGuid();

			// Register K entries which will be expired
			for (int i = 0; i < K; i++)
			{
				sut.RegisterLaunch(mvid, "Wasm", isDebug: false);
			}

			// Advance to let those K entries time out
			clock.Advance(TimeSpan.FromMilliseconds(150));

			// Register L active entries without advancing the global clock so they remain within the timeout window
			for (int i = 0; i < L; i++)
			{
				sut.RegisterLaunch(mvid, "Wasm", isDebug: false);
				// Do NOT advance the clock here: advancing would make early active entries expire before we report connections.
			}

			// Act: report L times
			for (int i = 0; i < L; i++)
			{
				sut.ReportConnection(mvid, "Wasm", isDebug: false);
			}

			// Assert: at least K should have timed out, and we should have L connections in FIFO order
			timeouts.Count.Should().BeGreaterOrEqualTo(K);
			connections.Should().HaveCount(L);
			for (int i = 1; i < connections.Count; i++)
			{
				connections[i - 1].RegisteredAt.Should().BeOnOrBefore(connections[i].RegisteredAt);
			}
		}

		[TestMethod]
		public void WhenRegisteredLaunchTimeoutExpires_ThenTimeoutCallbackInvoked()
		{
			// Arrange
			using var sut = CreateMonitor(out var clock, out _, out var timeouts, out _, TimeSpan.FromSeconds(10));
			var mvid = Guid.NewGuid();
			sut.RegisterLaunch(mvid, "Wasm", isDebug: false);

			// Act
			clock.Advance(TimeSpan.FromSeconds(11));

			// Assert
			timeouts.Should().HaveCount(1);
		}

		[TestMethod]
		public void WhenTimeoutExpiresWithMixedExpiredAndActive_ThenOnlyExpiredAreRemoved()
		{
			// Arrange
			using var sut = CreateMonitor(out var clock, out _, out var timeouts, out var connections);
			var mvid = Guid.NewGuid();
			sut.RegisterLaunch(mvid, "Wasm", isDebug: false); // will expire
			clock.Advance(TimeSpan.FromSeconds(5));
			sut.RegisterLaunch(mvid, "Wasm", isDebug: false); // still active

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
			using var sut = CreateMonitor(out var clock, out _, out _, out _);
			var mvid = Guid.NewGuid();

			sut.Invoking(m => m.RegisterLaunch(mvid, null!, true)).Should().Throw<ArgumentException>();
			sut.Invoking(m => m.RegisterLaunch(mvid, string.Empty, true)).Should().Throw<ArgumentException>();
			sut.Invoking(m => m.ReportConnection(mvid, null!, true)).Should().Throw<ArgumentException>();
			sut.Invoking(m => m.ReportConnection(mvid, string.Empty, true)).Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void WhenPlatformDiffersByCaseOnReportConnection_ThenItDoesNotMatch()
		{
			using var sut = CreateMonitor(out var clock, out _, out _, out var connections);
			var mvid = Guid.NewGuid();
			sut.RegisterLaunch(mvid, "Wasm", true);
			clock.Advance(TimeSpan.FromSeconds(1)); // bellow timeout

			sut.ReportConnection(mvid, "wasm", true);

			connections.Should().BeEmpty();
		}
	}
}
