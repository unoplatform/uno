#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_CompositionTargetFrameDispatcher
{
	[TestMethod]
	public void When_Dispatch_Then_All_Handlers_Invoked()
	{
		var dispatcher = new CompositionTargetFrameDispatcher();
		var invoked = new List<int>();
		var handlers = new List<EventHandler<object>>
		{
			(_, _) => invoked.Add(0),
			(_, _) => invoked.Add(1),
			(_, _) => invoked.Add(2),
		};

		dispatcher.Dispatch(handlers);

		CollectionAssert.AreEqual(new[] { 0, 1, 2 }, invoked);
	}

	[TestMethod]
	public void When_Dispatch_Completes_Then_Buffer_Is_Cleared()
	{
		var dispatcher = new CompositionTargetFrameDispatcher();
		var handlers = new List<EventHandler<object>> { (_, _) => { }, (_, _) => { } };

		dispatcher.Dispatch(handlers);

		Assert.IsTrue(
			dispatcher.Snapshot.All(h => h is null),
			"The reused snapshot buffer must not root any handler past its dispatch.");
	}

	[TestMethod]
	public void When_Handler_Throws_Then_Remaining_Handlers_Run_And_Nothing_Escapes()
	{
		// A throwing Rendering handler must be contained: the exception must not escape (on WASM it
		// would cross the frame-callback boundary and halt the frame loop for the process
		// lifetime), the remaining handlers must still run, and the buffer must still be cleared.
		var dispatcher = new CompositionTargetFrameDispatcher();
		var laterInvoked = false;
		var handlers = new List<EventHandler<object>>
		{
			(_, _) => throw new InvalidOperationException("boom"),
			(_, _) => laterInvoked = true,
		};

		dispatcher.Dispatch(handlers);

		Assert.IsTrue(laterInvoked, "A throwing handler must not skip the remaining handlers of the same frame.");
		Assert.IsTrue(
			dispatcher.Snapshot.All(h => h is null),
			"A throwing handler must not leave any handler rooted in the static buffer.");
	}

	[TestMethod]
	public void When_Handler_List_Shrinks_Then_Previous_Frame_Handler_Not_Retained()
	{
		// The observable property that matters: the reused buffer must not keep a handler (and
		// anything it roots — e.g. a collectible-ALC object) alive past its dispatch. Asserted via
		// a WeakReference to the handler's target rather than by pinning the buffering strategy.
		var dispatcher = new CompositionTargetFrameDispatcher();

		var weakTarget = DispatchLargeFrameAndDropHandlers(dispatcher);

		// A subsequent smaller frame must not resurrect/retain the large frame's delegates either.
		dispatcher.Dispatch(new List<EventHandler<object>> { (_, _) => { } });

		Assert.IsTrue(
			TryWaitUntilCollected(weakTarget),
			"After its dispatch (and a subsequent smaller frame), a handler's target must be collectible; the reused snapshot buffer must not root it.");
		Assert.IsTrue(
			dispatcher.Snapshot.All(h => h is null),
			"Entries beyond the current handler count must not keep stale delegate references from a previous, larger frame.");
	}

	[TestMethod]
	public void When_Handler_Reenters_Dispatch_Then_Outer_Frame_Completes()
	{
		// A Rendering handler that synchronously re-enters the frame loop (directly, or via JS
		// re-invoking the frame callback) must not clear the shared buffer underneath the outer
		// invocation loop: the nested dispatch falls back to a local snapshot, and the outer
		// loop's remaining handlers still run.
		var dispatcher = new CompositionTargetFrameDispatcher();
		var invoked = new List<string>();
		var nested = new List<EventHandler<object>>
		{
			(_, _) => invoked.Add("nested"),
		};

		var handlers = new List<EventHandler<object>>();
		handlers.Add((_, _) =>
		{
			invoked.Add("outer-first");
			dispatcher.Dispatch(nested);
		});
		handlers.Add((_, _) => invoked.Add("outer-second"));

		dispatcher.Dispatch(handlers);

		CollectionAssert.AreEqual(
			new[] { "outer-first", "nested", "outer-second" },
			invoked,
			"A reentrant dispatch must not disturb the outer invocation loop (no NRE from a cleared shared buffer, no skipped handler).");
		Assert.IsTrue(
			dispatcher.Snapshot.All(h => h is null),
			"The shared buffer must be cleared once the outer dispatch completes.");
	}

	/// <summary>
	/// The strong handler/target references must be confined to a separate, non-inlined frame:
	/// locals in the frame that runs the GC keep their objects alive on some runtimes.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference DispatchLargeFrameAndDropHandlers(CompositionTargetFrameDispatcher dispatcher)
	{
		var target = new HandlerTarget();
		var large = new List<EventHandler<object>>
		{
			target.OnRendering,
			(_, _) => { },
			(_, _) => { },
			(_, _) => { },
		};

		dispatcher.Dispatch(large);

		return new WeakReference(target);
	}

	private static bool TryWaitUntilCollected(WeakReference reference)
	{
		for (var i = 0; i < 10 && reference.IsAlive; i++)
		{
			GC.Collect(2, GCCollectionMode.Forced, blocking: true);
			GC.WaitForPendingFinalizers();
		}

		return !reference.IsAlive;
	}

	private sealed class HandlerTarget
	{
		public void OnRendering(object? sender, object e)
		{
		}
	}
}
