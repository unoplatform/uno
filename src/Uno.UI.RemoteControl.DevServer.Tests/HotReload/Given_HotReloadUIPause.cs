using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.HotReload.Client;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.DevServer.Tests.HotReload;

/// <summary>
/// Unit tests for the <c>Uno.HotReload.Client.UIUpdate</c> public API and the
/// <c>PendingUIUpdates</c> state machine that backs it (spec 041).
/// </summary>
[TestClass]
public class Given_HotReloadUIPause
{
	[TestInitialize]
	public void Reset() => PendingUIUpdates.ResetForTest();

	[TestCleanup]
	public void Cleanup() => PendingUIUpdates.ResetForTest();

	// ── Drain observer ───────────────────────────────────────────────────────

	private sealed class DrainObserver : IDisposable
	{
		private readonly List<PendingUIUpdates.DrainEventArgs> _events = new();
		private readonly object _lock = new();

		public DrainObserver() => PendingUIUpdates.DrainRequested += OnDrain;

		private void OnDrain(object? sender, PendingUIUpdates.DrainEventArgs args)
		{
			lock (_lock)
			{
				_events.Add(args);
			}
		}

		public IReadOnlyList<PendingUIUpdates.DrainEventArgs> Events
		{
			get
			{
				lock (_lock) { return _events.ToList(); }
			}
		}

		public void Dispose() => PendingUIUpdates.DrainRequested -= OnDrain;
	}

	// ── Pause / Drain lifecycle ──────────────────────────────────────────────

	[TestMethod]
	public void When_PauseDispose_NoTypes_Then_NoDrain()
	{
		using var obs = new DrainObserver();

		using (UIUpdate.Pause(HotReloadUIPhases.All))
		{
			// nothing queued
		}

		obs.Events.Should().BeEmpty("dispose with no pending types should not raise DrainRequested");
	}

	[TestMethod]
	public void When_PauseEnqueueDispose_Then_DrainsOnce()
	{
		using var obs = new DrainObserver();

		using (UIUpdate.Pause(HotReloadUIPhases.VisualTree))
		{
			PendingUIUpdates.IsPaused(HotReloadUIPhases.VisualTree).Should().BeTrue();
			PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string), typeof(int) });
			PendingUIUpdates.GetPendingCountsForTest().VisualTree.Should().Be(2);
		}

		obs.Events.Should().HaveCount(1);
		obs.Events[0].VisualTree.Should().BeEquivalentTo(new[] { typeof(string), typeof(int) });
		obs.Events[0].ResourceDictionaries.Should().BeEmpty();
		PendingUIUpdates.GetPendingCountsForTest().VisualTree.Should().Be(0);
	}

	[TestMethod]
	public void When_PausePhaseSubset_Then_UnpausedPhase_Does_Not_Accumulate()
	{
		using var obs = new DrainObserver();

		using (UIUpdate.Pause(HotReloadUIPhases.VisualTree))
		{
			PendingUIUpdates.IsPaused(HotReloadUIPhases.VisualTree).Should().BeTrue();
			PendingUIUpdates.IsPaused(HotReloadUIPhases.ResourceDictionaries).Should().BeFalse();

			// Enqueueing into an unpaused phase is a no-op.
			PendingUIUpdates.Enqueue(HotReloadUIPhases.ResourceDictionaries, new[] { typeof(string) });
			PendingUIUpdates.GetPendingCountsForTest().ResourceDictionaries.Should().Be(0);

			// Enqueueing into the paused phase accumulates.
			PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(int) });
			PendingUIUpdates.GetPendingCountsForTest().VisualTree.Should().Be(1);
		}

		obs.Events.Should().HaveCount(1);
		obs.Events[0].VisualTree.Should().BeEquivalentTo(new[] { typeof(int) });
		obs.Events[0].ResourceDictionaries.Should().BeEmpty("RD was not paused, so nothing accumulated");
	}

	[TestMethod]
	public void When_NestedPauses_SamePhase_Then_OnlyOuterReleaseDrains()
	{
		using var obs = new DrainObserver();

		using (UIUpdate.Pause(HotReloadUIPhases.VisualTree))
		{
			PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string) });

			using (UIUpdate.Pause(HotReloadUIPhases.VisualTree))
			{
				PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(int) });
				// Inner dispose: counter drops from 2 → 1, still paused — no drain.
			}

			obs.Events.Should().BeEmpty("inner dispose must not drain while outer pause is still active");
			PendingUIUpdates.IsPaused(HotReloadUIPhases.VisualTree).Should().BeTrue();
		}

		obs.Events.Should().HaveCount(1, "outer dispose drops the counter to 0 and triggers drain");
		obs.Events[0].VisualTree.Should().BeEquivalentTo(new[] { typeof(string), typeof(int) });
	}

	[TestMethod]
	public void When_TwoDifferentPhases_Drain_Fires_Only_When_Both_Released()
	{
		using var obs = new DrainObserver();

		// Acquire two separate handles, one per phase.
		var rdHandle = UIUpdate.Pause(HotReloadUIPhases.ResourceDictionaries);
		var vtHandle = UIUpdate.Pause(HotReloadUIPhases.VisualTree);

		PendingUIUpdates.Enqueue(HotReloadUIPhases.ResourceDictionaries, new[] { typeof(string) });
		PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(int) });

		// Releasing RD: VT is still held → no drain yet.
		rdHandle.Dispose();
		obs.Events.Should().BeEmpty("VT is still paused; drain must not fire while any phase is paused");

		// Releasing VT: all counters are zero → drain fires with both snapshots.
		vtHandle.Dispose();
		obs.Events.Should().HaveCount(1);
		obs.Events[0].ResourceDictionaries.Should().BeEquivalentTo(new[] { typeof(string) });
		obs.Events[0].VisualTree.Should().BeEquivalentTo(new[] { typeof(int) });
	}

	[TestMethod]
	public void When_DropAllTypes_Then_DrainSkipped()
	{
		using var obs = new DrainObserver();

		using (var handle = UIUpdate.Pause(HotReloadUIPhases.VisualTree))
		{
			PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string), typeof(int) });
			handle.Drop(typeof(string), typeof(int));
			PendingUIUpdates.GetPendingCountsForTest().VisualTree.Should().Be(0);
		}

		obs.Events.Should().BeEmpty("after dropping all queued types, drain has nothing to apply");
	}

	[TestMethod]
	public void When_DropSubset_Then_DrainsRemainder()
	{
		using var obs = new DrainObserver();

		using (var handle = UIUpdate.Pause(HotReloadUIPhases.VisualTree))
		{
			PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string), typeof(int), typeof(double) });
			handle.Drop(typeof(int));
		}

		obs.Events.Should().HaveCount(1);
		obs.Events[0].VisualTree.Should().BeEquivalentTo(new[] { typeof(string), typeof(double) });
	}

	[TestMethod]
	public void When_PauseAllEnqueueAndDropFromRDPhase_Then_VT_Remains_In_Drain()
	{
		using var obs = new DrainObserver();

		using (var handle = UIUpdate.Pause(HotReloadUIPhases.All))
		{
			PendingUIUpdates.Enqueue(HotReloadUIPhases.ResourceDictionaries, new[] { typeof(string) });
			PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string), typeof(int) });

			// Drop typeof(string) from both phases.
			handle.Drop(typeof(string));
			PendingUIUpdates.GetPendingCountsForTest().Should().Be(new PendingUIUpdates.PendingCounts(0, 1));
		}

		// RD pending is empty → not included in drain; VT has {int}.
		obs.Events.Should().HaveCount(1);
		obs.Events[0].ResourceDictionaries.Should().BeEmpty("all RD types were dropped");
		obs.Events[0].VisualTree.Should().BeEquivalentTo(new[] { typeof(int) });
	}

	[TestMethod]
	public void When_DoubleDispose_Then_NoDoubleDecrement_And_OneDrain()
	{
		using var obs = new DrainObserver();

		var handle = UIUpdate.Pause(HotReloadUIPhases.VisualTree);
		PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string) });
		handle.Dispose();
		handle.Dispose(); // idempotent

		obs.Events.Should().HaveCount(1, "first dispose drains; second dispose is a no-op");
		PendingUIUpdates.IsPaused(HotReloadUIPhases.VisualTree).Should().BeFalse();
	}

	[TestMethod]
	public void When_DropAfterDispose_Then_DrainAlreadyHappenedIsUnaffected()
	{
		using var obs = new DrainObserver();

		var handle = UIUpdate.Pause(HotReloadUIPhases.VisualTree);
		PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string) });
		handle.Dispose();

		// Drop after Dispose is a no-op — drain already happened.
		handle.Drop(typeof(string));

		obs.Events.Should().HaveCount(1);
		obs.Events[0].VisualTree.Should().BeEquivalentTo(new[] { typeof(string) });
	}

	[TestMethod]
	public void When_NoPauseHeld_Then_EnqueueIsNoOp()
	{
		PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string) });

		PendingUIUpdates.GetPendingCountsForTest().Should().Be(
			new PendingUIUpdates.PendingCounts(0, 0),
			"Enqueue must not accumulate types when the phase is not paused");
	}

	[TestMethod]
	public void When_AcquireResourceDictionaries_Then_VisualTreeUnaffected()
	{
		using (UIUpdate.Pause(HotReloadUIPhases.ResourceDictionaries))
		{
			UIUpdate.IsPaused(HotReloadUIPhases.ResourceDictionaries).Should().BeTrue();
			UIUpdate.IsPaused(HotReloadUIPhases.VisualTree).Should().BeFalse();
			UIUpdate.IsPaused(HotReloadUIPhases.All).Should().BeTrue("IsPaused(All) returns true when any phase is paused");
		}
	}

	[TestMethod]
	public void When_PauseHandleDisposed_From_Different_Thread_Then_Succeeds()
	{
		var handle = UIUpdate.Pause(HotReloadUIPhases.VisualTree);
		PendingUIUpdates.IsPaused(HotReloadUIPhases.VisualTree).Should().BeTrue();

		var disposed = new TaskCompletionSource();
		var thread = new Thread(() =>
		{
			try { handle.Dispose(); disposed.SetResult(); }
			catch (Exception ex) { disposed.SetException(ex); }
		});
		thread.Start();
		thread.Join();

		disposed.Task.IsCompletedSuccessfully.Should().BeTrue("Dispose from any thread must succeed");
		PendingUIUpdates.IsPaused(HotReloadUIPhases.VisualTree).Should().BeFalse();
	}

	[TestMethod]
	public void When_PauseReasonProvided_Then_HandleExposesIt()
	{
		using var handle = UIUpdate.Pause(HotReloadUIPhases.VisualTree, reason: "test-reason");
		handle.Reason.Should().Be("test-reason");
	}

	// ── Multi-thread stress ──────────────────────────────────────────────────

	[TestMethod]
	public void When_PauseFromMultipleThreads_Then_CountsAreConsistent()
	{
		using var obs = new DrainObserver();

		const int threadCount = 64;
		const int iterationsPerThread = 50;

		var threads = Enumerable.Range(0, threadCount)
			.Select(_ => new Thread(() =>
			{
				for (var i = 0; i < iterationsPerThread; i++)
				{
					using var h = UIUpdate.Pause(HotReloadUIPhases.All);
					Thread.SpinWait(10);
				}
			}))
			.ToArray();

		foreach (var t in threads) { t.Start(); }
		foreach (var t in threads) { t.Join(); }

		PendingUIUpdates.IsPaused(HotReloadUIPhases.All).Should().BeFalse("all handles disposed; counters at 0");
		PendingUIUpdates.GetPendingCountsForTest().Should().Be(new PendingUIUpdates.PendingCounts(0, 0));
		obs.Events.Should().BeEmpty("no types were ever enqueued");
	}

	[TestMethod]
	public void When_DropConcurrentWithEnqueue_Then_NoCrash()
	{
		using var obs = new DrainObserver();

		using (var handle = UIUpdate.Pause(HotReloadUIPhases.VisualTree))
		{
			var sharedTypes = new[] { typeof(string), typeof(int), typeof(double), typeof(byte), typeof(bool) };

			var producers = Enumerable.Range(0, 4).Select(_ => new Thread(() =>
			{
				for (var i = 0; i < 100; i++)
				{
					PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, sharedTypes);
					Thread.SpinWait(5);
				}
			})).ToArray();

			var droppers = Enumerable.Range(0, 4).Select(_ => new Thread(() =>
			{
				for (var i = 0; i < 100; i++)
				{
					handle.Drop(sharedTypes);
					Thread.SpinWait(5);
				}
			})).ToArray();

			foreach (var t in producers) { t.Start(); }
			foreach (var t in droppers) { t.Start(); }
			foreach (var t in producers) { t.Join(); }
			foreach (var t in droppers) { t.Join(); }
		}

		// Goal is no exception/crash. Final state must be unpaused.
		PendingUIUpdates.IsPaused(HotReloadUIPhases.VisualTree).Should().BeFalse();
	}

	// ── UpdateRequest.PauseUIPhases integration ──────────────────────────────

	[TestMethod]
	public void When_UpdateRequest_PauseUIPhases_None_Then_No_Pause_Acquired()
	{
		var req = new ClientHotReloadProcessor.UpdateRequest(
			"test.xaml", OldText: null, NewText: "<Page/>", WaitForHotReload: false)
		{
			PauseUIPhases = HotReloadUIPhases.None,
		};

		req.PauseUIPhases.Should().Be(HotReloadUIPhases.None);

		// Verify that PendingUIUpdates stays unpaused when PauseUIPhases is None
		// (the actual pause is only acquired inside TryUpdateFilesAsync when PauseUIPhases != None).
		UIUpdate.IsPaused(HotReloadUIPhases.VisualTree).Should().BeFalse();
	}

	[TestMethod]
	public void When_UpdateRequest_PauseUIPhases_VisualTree_Then_VisualTree_Paused_During_Handle_Scope()
	{
		var req = new ClientHotReloadProcessor.UpdateRequest(
			"test.xaml", OldText: null, NewText: "<Page/>", WaitForHotReload: false)
		{
			PauseUIPhases = HotReloadUIPhases.VisualTree,
			PauseReason = "Test pause",
		};

		req.PauseUIPhases.Should().Be(HotReloadUIPhases.VisualTree);
		req.PauseReason.Should().Be("Test pause");

		// Simulate what TryUpdateFilesAsync does when PauseUIPhases is set.
		using (var handle = UIUpdate.Pause(req.PauseUIPhases, reason: req.PauseReason))
		{
			UIUpdate.IsPaused(HotReloadUIPhases.VisualTree).Should().BeTrue();
			UIUpdate.IsPaused(HotReloadUIPhases.ResourceDictionaries).Should().BeFalse();

			// Types arriving while the pause is held go to pending.
			PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, new[] { typeof(string) });
			PendingUIUpdates.GetPendingCountsForTest().VisualTree.Should().Be(1);

			// Drop (simulating UpdateFile correlated-type drop).
			handle.Drop(typeof(string));
			PendingUIUpdates.GetPendingCountsForTest().VisualTree.Should().Be(0);
		}

		// After dispose, no pending and no pause.
		UIUpdate.IsPaused(HotReloadUIPhases.VisualTree).Should().BeFalse();
		PendingUIUpdates.GetPendingCountsForTest().VisualTree.Should().Be(0);
	}

	[TestMethod]
	public void When_UpdateRequest_PauseUIPhases_All_Then_BothPhases_Paused()
	{
		var req = new ClientHotReloadProcessor.UpdateRequest(
			"test.xaml", OldText: null, NewText: "<Page/>", WaitForHotReload: false)
		{
			PauseUIPhases = HotReloadUIPhases.All,
		};

		using (UIUpdate.Pause(req.PauseUIPhases, reason: req.PauseReason))
		{
			UIUpdate.IsPaused(HotReloadUIPhases.ResourceDictionaries).Should().BeTrue();
			UIUpdate.IsPaused(HotReloadUIPhases.VisualTree).Should().BeTrue();
		}

		UIUpdate.IsPaused(HotReloadUIPhases.All).Should().BeFalse();
	}

	[TestMethod]
	public void When_UpdateRequest_WithCallerInfo_Then_CallerInfo_Preserved()
	{
		var req = new ClientHotReloadProcessor.UpdateRequest(
			"test.xaml", OldText: null, NewText: "<Page/>")
			.WithCallerInfo("MyMethod", "MyFile.cs", 42);

		req.CallerMemberName.Should().Be("MyMethod");
		req.CallerFilePath.Should().Be("MyFile.cs");
		req.CallerLineNumber.Should().Be(42);
	}

	[TestMethod]
	public void When_UpdateRequest_Undo_Then_PauseUIPhases_Preserved()
	{
		var original = new ClientHotReloadProcessor.UpdateRequest(
			"test.xaml", OldText: "A", NewText: "B")
		{
			PauseUIPhases = HotReloadUIPhases.VisualTree,
			PauseReason = "undo-test",
		};

		var undone = original.Undo();

		undone.PauseUIPhases.Should().Be(HotReloadUIPhases.VisualTree,
			"Undo should preserve the PauseUIPhases init property");
		undone.PauseReason.Should().Be("undo-test");
		undone.Edits[0].OldText.Should().Be("B");
		undone.Edits[0].NewText.Should().Be("A");
	}
}
