#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class LifetimeTests
{
	[TestMethod]
	public async Task DispatcherStopSuppressesTheRestOfACapturedBatch()
	{
		var dispatcher = new WaylandDispatcher();
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var stop = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
		var callbacks = 0;
		dispatcher.Schedule(() =>
		{
			entered.Set();
			Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
		});
		Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
		// Both actions enter the next captured batch before the loop resumes.
		dispatcher.Schedule(() =>
		{
			var completion = dispatcher.Stop();
			Assert.IsFalse(completion.IsCompleted);
			stop.SetResult(completion);
		});
		dispatcher.Schedule(() => callbacks++);
		release.Set();
		await stop.Task.Unwrap().WaitAsync(TimeSpan.FromSeconds(5));
		dispatcher.Schedule(() => callbacks++);
		Assert.AreEqual(0, callbacks);
	}

	[TestMethod]
	public async Task DispatcherStopWaitsForTheRunningCallback()
	{
		var dispatcher = new WaylandDispatcher();
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		dispatcher.Schedule(() =>
		{
			entered.Set();
			Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
		});
		Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
		var completion = dispatcher.Stop();
		Assert.IsFalse(completion.IsCompleted);
		release.Set();
		await completion.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[TestMethod]
	public async Task DispatcherStopBeforeFirstCallbackDoesNotHang()
		=> await new WaylandDispatcher().Stop().WaitAsync(TimeSpan.FromSeconds(5));

	[TestMethod]
	public async Task FailedConstructionCleanupStillReleasesLaterResources()
	{
		var released = new List<string>();
		using var thread = new Rendering.WaylandRenderThread("construction rollback", _ => { });
		thread.Complete(() => WaylandCleanup.Run(
			() =>
			{
				released.Add("graphics");
				throw new InvalidOperationException("graphics cleanup failed");
			},
			() => released.Add("protocols")));
		try
		{
			await thread.Completion.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Fail("Rollback failure was not retained for shutdown.");
		}
		catch (AggregateException error)
		{
			Assert.AreEqual("graphics cleanup failed", error.InnerExceptions[0].Message);
		}
		CollectionAssert.AreEqual(new[] { "graphics", "protocols" }, released);
	}

	[TestMethod]
	public void PartialProtocolCleanupAttemptsEveryRemainingObject()
	{
		var released = new List<int>();
		try
		{
			WaylandCleanup.Run(
				() => throw new InvalidOperationException("decoration destruction failed"),
				() => released.Add(1),
				() => throw new InvalidOperationException("toplevel destruction failed"),
				() => released.Add(2));
			Assert.Fail("Protocol cleanup errors were swallowed.");
		}
		catch (AggregateException error)
		{
			Assert.AreEqual(2, error.InnerExceptions.Count);
		}
		CollectionAssert.AreEqual(new[] { 1, 2 }, released);
	}
}
