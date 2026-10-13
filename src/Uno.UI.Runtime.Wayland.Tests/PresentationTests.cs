#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class PresentationTests
{
	[TestMethod]
	public async Task ConcurrentWindowsCannotOverlapSubmissions()
	{
		var syncRoot = new object();
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var begins = 0;
		var coordinator = new WaylandPresentationCoordinator(syncRoot, (command, _) =>
		{
			lock (syncRoot)
			{
				Interlocked.Increment(ref begins);
				command();
			}
		}, () => { });
		var first = Task.Run(() => coordinator.Submit(() =>
		{
			entered.Set();
			Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
			return true;
		}));
		Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
		var second = Task.Run(() => coordinator.Submit(() => true));
		Assert.AreEqual(1, Volatile.Read(ref begins));
		release.Set();
		Assert.IsTrue(await first.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.IsTrue(await second.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.AreEqual(2, begins);
		Assert.IsFalse(coordinator.IsActive);
	}

	[TestMethod]
	public void FailedSubmissionKeepsLastSuccessfulGeometry()
	{
		var state = new WaylandPresentationState();
		var oldLayout = new WaylandDecorationLayout(800, 600, 32, 8, WaylandCaptionButtons.Close);
		var newLayout = new WaylandDecorationLayout(900, 700, 0, 0, WaylandCaptionButtons.Close);
		Assert.IsTrue(state.Submit(1, oldLayout, 1, () => true));
		Assert.IsFalse(state.Submit(2, newLayout, 2, () => false));
		Assert.AreEqual(1L, state.Generation);
		Assert.AreEqual(oldLayout, state.Layout);
		Assert.AreEqual(1d, state.Scale);
		Assert.ThrowsExactly<InvalidOperationException>(() => state.Submit(2, newLayout, 2, () => throw new InvalidOperationException()));
		Assert.AreEqual(1L, state.Generation);
		Assert.IsTrue(state.Submit(2, newLayout, 2, () => true));
		Assert.AreEqual(2L, state.Generation);
		Assert.AreEqual(newLayout, state.Layout);
		Assert.AreEqual(2d, state.Scale);
	}

	[TestMethod]
	public async Task SubmissionWaitsForDispatchAndLeavesProtocolLockAvailable()
	{
		var syncRoot = new object();
		Action? begin = null;
		using var queued = new ManualResetEventSlim();
		var resumed = 0;
		var coordinator = new WaylandPresentationCoordinator(syncRoot, (command, _) =>
		{
			begin = command;
			queued.Set();
		}, () => resumed++);
		var submitted = false;
		var result = Task.Run(() => coordinator.Submit(() =>
		{
			Assert.IsTrue(coordinator.IsActive);
			Assert.IsFalse(Monitor.IsEntered(syncRoot), "Native submission must not hold the protocol lock.");
			Assert.IsTrue(Task.Run(() =>
			{
				lock (syncRoot)
				{
					return true;
				}
			}).Wait(TimeSpan.FromSeconds(5)), "Another reader must be able to acquire the protocol lock.");
			submitted = true;
			return true;
		}));
		Assert.IsTrue(queued.Wait(TimeSpan.FromSeconds(5)));
		Assert.IsFalse(submitted);
		lock (syncRoot)
		{
			begin!();
		}
		Assert.IsTrue(await result.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.IsFalse(coordinator.IsActive);
		Assert.AreEqual(1, resumed);
	}

	[TestMethod]
	public void FailedAndThrowingSubmissionsResumeDispatch()
	{
		var syncRoot = new object();
		var resumed = 0;
		var coordinator = new WaylandPresentationCoordinator(syncRoot, (command, _) =>
		{
			lock (syncRoot)
			{
				command();
			}
		}, () => resumed++);
		Assert.IsFalse(coordinator.Submit(() => false));
		Assert.IsFalse(coordinator.IsActive);
		Assert.ThrowsExactly<InvalidOperationException>(() => coordinator.Submit(() => throw new InvalidOperationException()));
		Assert.IsFalse(coordinator.IsActive);
		Assert.IsTrue(coordinator.Submit(() => true));
		Assert.AreEqual(3, resumed);
	}

	[TestMethod]
	public void RejectedDispatchNeverSubmits()
	{
		var coordinator = new WaylandPresentationCoordinator(new object(), (_, reject) => reject(), () => { });
		Assert.ThrowsExactly<System.IO.IOException>(() => coordinator.Submit(() => throw new AssertFailedException("Unexpected submission.")));
		Assert.IsFalse(coordinator.IsActive);
	}
}
