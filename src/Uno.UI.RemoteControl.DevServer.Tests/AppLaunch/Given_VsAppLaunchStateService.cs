using Microsoft.Extensions.Time.Testing;
using Uno.UI.RemoteControl.VS.AppLaunch;

namespace Uno.UI.RemoteControl.DevServer.Tests.AppLaunch;

[TestClass]
public class Given_VsAppLaunchStateService
{
	private static VsAppLaunchStateService<TStateDetails> CreateService<TStateDetails>(
		out FakeTimeProvider clock,
		out List<StateChangedEventArgs<TStateDetails>> events,
		TimeSpan? window = null)
	{
		clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var evts = new List<StateChangedEventArgs<TStateDetails>>();
		events = evts;

		var options = new VsAppLaunchStateService<TStateDetails>.Options
		{
			BuildWaitWindow = window ?? TimeSpan.FromSeconds(5)
		};

		var sut = new VsAppLaunchStateService<TStateDetails>(clock, options);
		sut.StateChanged += (_, e) => evts.Add(e);
		return sut;
	}

	[TestMethod]
	public void WhenStartAndNoBuildWithinWindow_ThenTimeoutAndIdle()
	{
		// Arrange
		using var sut = CreateService<Guid>(out var clock, out var events, window: TimeSpan.FromSeconds(5));
		var stateDetails = Guid.NewGuid();

		// Act
		sut.Start(stateDetails);

		// Assert initial transition to PlayInvokedPendingBuild
		events.Should().HaveCount(1);
		events[0].Current.Should().Be(VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild);

		// Act: advance past the wait window to force timeout
		clock.Advance(TimeSpan.FromSeconds(6));

		// Assert: Expect PlayInvokedPendingBuild -> TimedOut -> Idle
		events.Select(e => e.Current).Should()
			.Equal(
				VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild,
				VsAppLaunchStateService<Guid>.LaunchState.TimedOut,
				VsAppLaunchStateService<Guid>.LaunchState.Idle);
	}

	[TestMethod]
	public void WhenBuildBeginsAndSucceeds_ThenBuildSucceededAndIdle()
	{
		// Arrange
		using var sut = CreateService<Guid>(out var clock, out var events, window: TimeSpan.FromSeconds(5));
		var stateDetails = Guid.NewGuid();

		// Act
		sut.Start(stateDetails);

		// Act: Notify build began within window
		sut.NotifyBuild(stateDetails, BuildNotification.Began);

		// Assert
		events.Select(e => e.Current).Should()
			.Equal(
				VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild,
				VsAppLaunchStateService<Guid>.LaunchState.BuildInProgress);

		// Act: Notify success -> BuildSucceeded then Idle
		sut.NotifyBuild(stateDetails, BuildNotification.CompletedSuccess);

		// Assert sequence
		events.Select(e => e.Current).Should()
			.Equal(
				VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild,
				VsAppLaunchStateService<Guid>.LaunchState.BuildInProgress,
				VsAppLaunchStateService<Guid>.LaunchState.BuildSucceeded,
				VsAppLaunchStateService<Guid>.LaunchState.Idle);
	}

	[TestMethod]
	public void WhenNotifyBuildForDifferentKey_ThenIgnored()
	{
		// Arrange
		using var sut = CreateService<Guid>(out var clock, out var events, window: TimeSpan.FromSeconds(5));
		var stateDetails1 = Guid.NewGuid();
		var stateDetails2 = Guid.NewGuid();

		// Act
		sut.Start(stateDetails1);

		// Assert initial event emitted
		events.Should().HaveCount(1);

		// Act: Notify build for another stateDetails -> ignored
		sut.NotifyBuild(stateDetails2, BuildNotification.Began);

		// Assert no change
		events.Should().HaveCount(1);
	}

	[TestMethod]
	public void WhenBuildCanceled_ThenResetsToIdle()
	{
		// Arrange
		using var sut = CreateService<string>(out var clock, out var events, window: TimeSpan.FromSeconds(5));
		var stateDetails = "key42";

		// Act
		sut.Start(stateDetails);

		// Assert initial event
		events.Should().HaveCount(1);

		// Act: cancel build
		sut.NotifyBuild(stateDetails, BuildNotification.Canceled);

		// Assert that it reset to Idle
		events.Select(e => e.Current).Should()
			.Equal(
				VsAppLaunchStateService<string>.LaunchState.PlayInvokedPendingBuild,
				VsAppLaunchStateService<string>.LaunchState.Idle);

		events[0].StateDetails.Should().Be(stateDetails);
		events[1].StateDetails.Should().BeNull(); // Idle should have no active StateDetails
	}
	[TestMethod]
	public void WhenStartWithKeyThenChangeKey_BeforeBuild_ThenOldTimerCanceledAndNewKeyTimesOut()
	{
		// Arrange
		using var sut = CreateService<Guid>(out var clock, out var events, window: TimeSpan.FromSeconds(5));
		var stateDetails1 = Guid.NewGuid();
		var stateDetails2 = Guid.NewGuid();

		// Act: Start first cycle
		sut.Start(stateDetails1);

		// Assert initial PlayInvokedPendingBuild
		events.Should().HaveCount(1);
		events[0].Current.Should().Be(VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild);
		events[0].StateDetails.Should().Be(stateDetails1);

		// Act: advance partial time
		clock.Advance(TimeSpan.FromSeconds(2));

		// Act: Start a new cycle with a different stateDetails before build begins
		sut.Start(stateDetails2);

		// Assert second PlayInvokedPendingBuild for new details
		events.Should().HaveCount(2);
		events[1].Current.Should().Be(VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild);
		events[1].StateDetails.Should().Be(stateDetails2);

		// Act: Advance beyond the wait window to trigger only the latest timeout
		clock.Advance(TimeSpan.FromSeconds(6));

		// Assert exact sequence and that TimedOut has stateDetails2 and Idle clears details
		events.Should().HaveCount(4);
		events[0].Current.Should().Be(VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild);
		events[0].StateDetails.Should().Be(stateDetails1);
		events[1].Current.Should().Be(VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild);
		events[1].StateDetails.Should().Be(stateDetails2);
		events[2].Current.Should().Be(VsAppLaunchStateService<Guid>.LaunchState.TimedOut);
		events[2].StateDetails.Should().Be(stateDetails2);
		events[3].Current.Should().Be(VsAppLaunchStateService<Guid>.LaunchState.Idle);
		events[3].StateDetails.Should().Be(Guid.Empty);

		// Act: Late notifications for the old details are ignored
		sut.NotifyBuild(stateDetails1, BuildNotification.Began);
		// Assert no new events
		events.Should().HaveCount(4);
	}

	[TestMethod]
	public void StartKeepsOriginalInstance_WhenNotifyBuildReceivedWithEqualDifferentInstance()
	{
		// Arrange - use a details type that compares equal only on Key, but carries extra info
		using var sut = CreateService<Details>(out var clock, out var events, window: TimeSpan.FromSeconds(5));
		var original = new Details { Key = 42, Extra = "original" };
		var equivalent = new Details { Key = 42, Extra = "other" };

		// Act
		sut.Start(original);

		// Assert initial event contains the original instance (reference)
		events.Should().HaveCount(1);
		events[0].StateDetails.Should().BeSameAs(original);

		// Act: notify build using an equivalent but different instance
		sut.NotifyBuild(equivalent, BuildNotification.Began);

		// Assert: the state moved to BuildInProgress and the StateDetails kept the original instance
		events.Should().HaveCount(2);
		events[1].Current.Should().Be(VsAppLaunchStateService<Details>.LaunchState.BuildInProgress);
		events[1].StateDetails.Should().BeSameAs(original, "the service must preserve the original instance and not replace it with an equal one");
	}

	private sealed class Details
	{
		public int Key { get; set; }
		public string? Extra { get; set; }

		public override bool Equals(object? obj) => obj is Details d && d.Key == Key;
		public override int GetHashCode() => Key.GetHashCode();
	}

	[TestMethod]
	public void WhenSuccessReportedWithoutBegan_ThenBuildSucceededThenIdleAndKeyCleared()
	{
		// Arrange
		using var sut = CreateService<string>(out var clock, out var events, window: TimeSpan.FromSeconds(3));
		var stateDetails = "k1";

		// Act
		sut.Start(stateDetails);

		// Assert initial
		events.Should().HaveCount(1);
		events[0].Current.Should().Be(VsAppLaunchStateService<string>.LaunchState.PlayInvokedPendingBuild);
		events[0].StateDetails.Should().Be(stateDetails);

		// Act: Report success out of the usual order (no Began)
		sut.NotifyBuild(stateDetails, BuildNotification.CompletedSuccess);

		// Assert: PIPB -> BuildSucceeded -> Idle
		events.Select(e => e.Current).Should()
			.Equal(
				VsAppLaunchStateService<string>.LaunchState.PlayInvokedPendingBuild,
				VsAppLaunchStateService<string>.LaunchState.BuildSucceeded,
				VsAppLaunchStateService<string>.LaunchState.Idle);

		// Key should be present for success, then cleared on Idle
		events[1].StateDetails.Should().Be(stateDetails);
		events[2].StateDetails.Should().BeNull();
	}

	[TestMethod]
	public void WhenTimeoutOccurs_ThenLaterNotificationsForThatKeyAreIgnored()
	{
		// Arrange
		using var sut = CreateService<Guid>(out var clock, out var events, window: TimeSpan.FromMilliseconds(200));
		var stateDetails = Guid.NewGuid();

		// Act
		sut.Start(stateDetails);

		// Assert initial
		events.Should().HaveCount(1);

		// Act: advance past timeout
		clock.Advance(TimeSpan.FromMilliseconds(250));

		// Assert timeout then idle
		events.Select(e => e.Current).Should()
			.Equal(
				VsAppLaunchStateService<Guid>.LaunchState.PlayInvokedPendingBuild,
				VsAppLaunchStateService<Guid>.LaunchState.TimedOut,
				VsAppLaunchStateService<Guid>.LaunchState.Idle);

		// Act: Now any notifications for the old details must be ignored
		sut.NotifyBuild(stateDetails, BuildNotification.Canceled);
		sut.NotifyBuild(stateDetails, BuildNotification.Began);

		// Assert no extra events
		events.Should().HaveCount(3);
	}

	[TestMethod]
	public void WhenStart_ThenFirstNotify_TimeoutBeforeFirstNotify_ButNoTimeoutAfterBeganUntilNewStart()
	{
		// Arrange
		using var sut = CreateService<Guid>(out var clock, out var events, window: TimeSpan.FromMilliseconds(200));
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();

		// Act: First cycle - start and let it timeout
		sut.Start(first);
		clock.Advance(TimeSpan.FromMilliseconds(250));

		// Assert: first cycle timed out
		events.Select(e => e.Current).Should()
			.Contain(VsAppLaunchStateService<Guid>.LaunchState.TimedOut);

		// Act: Second cycle - start and notify Began before window elapses
		sut.Start(second);
		sut.NotifyBuild(second, BuildNotification.Began);

		// Advance beyond the original window - there should be no TimedOut for the second cycle
		clock.Advance(TimeSpan.FromMilliseconds(500));

		// Assert: no additional TimedOut events for the second cycle. We count TimedOut occurrences and
		// ensure only the first cycle produced one.
		var timedOutCount = events.Count(e => e.Current == VsAppLaunchStateService<Guid>.LaunchState.TimedOut);
		timedOutCount.Should().Be(1);

		// Complete the second cycle to get back to Idle
		sut.NotifyBuild(second, BuildNotification.CompletedSuccess);
		events.Last().Current.Should().Be(VsAppLaunchStateService<Guid>.LaunchState.Idle);
	}

	[TestMethod]
	public void Stress_ManyCycles_WithMixedOutcomes_NoLeakAndAlwaysIdleAtEnd()
	{
		using var sut = CreateService<int>(out var clock, out var events, window: TimeSpan.FromMilliseconds(50));

		const int iterations = 1_200;
		for (var i = 0; i < iterations; i++)
		{
			var key = i;
			sut.Start(key);

			var kind = i % 3;
			if (kind == 0)
			{
				// Normal success path
				sut.NotifyBuild(key, BuildNotification.Began);
				sut.NotifyBuild(key, BuildNotification.CompletedSuccess);
			}
			else if (kind == 1)
			{
				// Let the timer expire for timeout
				clock.Advance(TimeSpan.FromMilliseconds(60));
			}
			else
			{
				// Cancel path
				sut.NotifyBuild(key, BuildNotification.Canceled);
			}

			// End-of-iteration assertions
			sut.State.Should().Be(VsAppLaunchStateService<int>.LaunchState.Idle);
			events.Last().Current.Should().Be(VsAppLaunchStateService<int>.LaunchState.Idle);
			events.Last().StateDetails.Should().Be(0);
		}

		// Basic sanity: we produced a lot of events but the final state is Idle with no active key
		sut.State.Should().Be(VsAppLaunchStateService<int>.LaunchState.Idle);
	}
}
