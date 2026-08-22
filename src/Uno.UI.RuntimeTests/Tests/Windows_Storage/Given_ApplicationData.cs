using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage;

[TestClass]
public class Given_ApplicationData
{
	private uint _originalVersion;

	[TestInitialize]
	public void Initialize() => _originalVersion = ApplicationData.Current.Version;

	/// <summary>
	/// The version is persisted, so a run that leaves it raised moves the baseline for every later run
	/// on the same machine.
	/// </summary>
	[TestCleanup]
	public async Task Cleanup() =>
		await ApplicationData.Current.SetVersionAsync(_originalVersion, _ => { });

	[TestMethod]
	public async Task When_SetVersion()
	{
		// Arrange
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;
		var desiredVersion = currentVersion + 5;
		// Act
		await appData.SetVersionAsync(desiredVersion, (e) => { });

		// Assert
		appData.Version.Should().Be(desiredVersion);
	}

	[TestMethod]
	public async Task When_SetVersion_Handler()
	{
		// Arrange
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;
		var desiredVersion = currentVersion + 5;

		// Act
		await appData.SetVersionAsync(desiredVersion, new ApplicationDataSetVersionHandler((setVersionRequest) =>
		{
			// Assert
			setVersionRequest.CurrentVersion.Should().Be(currentVersion);
			setVersionRequest.DesiredVersion.Should().Be(desiredVersion);
		}));

		appData.Version.Should().Be(desiredVersion);
	}

	[TestMethod]
	public async Task When_SetVersion_Deferred()
	{
		// Arrange
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;
		var desiredVersion = currentVersion + 5;

		SetVersionDeferral deferral = null;
		// Act
		var task = appData.SetVersionAsync(desiredVersion, new ApplicationDataSetVersionHandler((setVersionRequest) =>
		{
			// Assert
			setVersionRequest.CurrentVersion.Should().Be(currentVersion);
			setVersionRequest.DesiredVersion.Should().Be(desiredVersion);

			deferral = setVersionRequest.GetDeferral();
		})).AsTask();

		await TestServices.WindowHelper.WaitFor(() => deferral != null);

		Assert.IsFalse(task.IsCompleted);

		deferral.Complete();

		await TestServices.WindowHelper.WaitFor(() => task.IsCompleted);
	}

	[TestMethod]
	public async Task When_SetVersion_Downgrade()
	{
		// Arrange - ensure we have a version > 0 to test downgrade
		var appData = ApplicationData.Current;
		var baseVersion = appData.Version;
		var higherVersion = baseVersion + 10;

		// First set to a higher version
		await appData.SetVersionAsync(higherVersion, (e) => { });
		appData.Version.Should().Be(higherVersion);

		// Act - downgrade to base version (WinUI allows this)
		await appData.SetVersionAsync(baseVersion, (e) => { });

		// Assert - version should be downgraded
		appData.Version.Should().Be(baseVersion);
	}

	[TestMethod]
	public async Task When_SetVersion_MultipleVersionJump_HandlerCalledOnce()
	{
		// Arrange
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;
		var desiredVersion = currentVersion + 10; // Jump by 10 versions

		int handlerCallCount = 0;
		uint reportedCurrentVersion = 0;
		uint reportedDesiredVersion = 0;

		// Act
		await appData.SetVersionAsync(desiredVersion, new ApplicationDataSetVersionHandler((setVersionRequest) =>
		{
			handlerCallCount++;
			reportedCurrentVersion = setVersionRequest.CurrentVersion;
			reportedDesiredVersion = setVersionRequest.DesiredVersion;
		}));

		// Assert - This test validates the TODO: "Is the request handled version by version?"
		// If handler is called once: handlerCallCount == 1, reportedCurrent == currentVersion, reportedDesired == desiredVersion
		// If handler is called version-by-version: handlerCallCount == 10
		Assert.AreEqual(1, handlerCallCount, "Handler should be called exactly once (not version-by-version)");
		Assert.AreEqual(currentVersion, reportedCurrentVersion);
		Assert.AreEqual(desiredVersion, reportedDesiredVersion);
	}

	[TestMethod]
	public async Task When_SetVersion_SameVersion()
	{
		// Arrange
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;

		int handlerCallCount = 0;
		uint reportedCurrentVersion = 0;
		uint reportedDesiredVersion = 0;

		// Act - set to same version
		await appData.SetVersionAsync(currentVersion, new ApplicationDataSetVersionHandler((setVersionRequest) =>
		{
			handlerCallCount++;
			reportedCurrentVersion = setVersionRequest.CurrentVersion;
			reportedDesiredVersion = setVersionRequest.DesiredVersion;
		}));

		// Assert - validate behavior when setting same version
		appData.Version.Should().Be(currentVersion);
		Assert.AreEqual(1, handlerCallCount, "Handler should be called exactly once");
		Assert.AreEqual(currentVersion, reportedCurrentVersion);
		Assert.AreEqual(currentVersion, reportedDesiredVersion);
	}

#if HAS_UNO
	// Uno-only: asserts Uno's DeferralManager cancellation. Native WinUI leaves the set-version
	// operation pending on an uncompleted deferral even after Cancel(), which wedges
	// ApplicationData for every later test in the process.
	[TestMethod]
	public async Task When_SetVersion_Deferral_Never_Completed()
	{
		// A handler that takes a deferral and never completes it must not wait forever: the operation
		// stays pending, and cancelling it has to end the wait.
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;

		var operation = appData.SetVersionAsync(currentVersion + 1, request => _ = request.GetDeferral());
		var task = operation.AsTask();

		Assert.AreNotEqual(TaskStatus.RanToCompletion, task.Status, "The operation must not complete while a deferral is outstanding.");

		operation.Cancel();

		// A regression would hang here rather than fail, so the wait is bounded.
		var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
		Assert.AreSame(task, completed, "Cancelling the operation must end the wait on the outstanding deferral.");
		await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await task);

		appData.Version.Should().Be(currentVersion);
	}
#endif
}
