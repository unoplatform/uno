#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class RenderThreadTests
{
	[TestMethod]
	public async Task CompletionWaitsForCleanupAndRejectsLaterWork()
	{
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var errors = new List<Exception>();
		using var thread = new WaylandRenderThread("test renderer", errors.Add);
		var cleaned = false;
		thread.Post(() =>
		{
			entered.Set();
			Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
		});
		Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
		thread.Post(() => cleaned = true);
		thread.Dispose();
		Assert.IsFalse(thread.Completion.IsCompleted);
		thread.Post(() => Assert.Fail("Work posted after completion was accepted."));
		release.Set();
		await thread.Completion.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.IsTrue(cleaned);
		Assert.AreEqual(0, errors.Count);
		thread.Dispose();
		thread.Post(() => Assert.Fail("Work posted after queue disposal was accepted."));
	}

	[TestMethod]
	public async Task FailedWorkDoesNotSkipQueuedCleanup()
	{
		var errors = 0;
		using var thread = new WaylandRenderThread("test renderer", _ => errors++);
		var cleaned = false;
		thread.Post(() => throw new InvalidOperationException("render failure"));
		thread.Post(() => cleaned = true);
		thread.Dispose();
		await thread.Completion.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.IsTrue(cleaned);
		Assert.AreEqual(1, errors);
	}

	[TestMethod]
	public async Task FailedCleanupFaultsCompletion()
	{
		var errors = 0;
		using var thread = new WaylandRenderThread("test cleanup", _ => errors++);
		thread.Complete(() => throw new InvalidOperationException("cleanup failure"));
		try
		{
			await thread.Completion.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Fail("Cleanup failure was swallowed.");
		}
		catch (InvalidOperationException error)
		{
			Assert.AreEqual("cleanup failure", error.Message);
		}
	}
}
