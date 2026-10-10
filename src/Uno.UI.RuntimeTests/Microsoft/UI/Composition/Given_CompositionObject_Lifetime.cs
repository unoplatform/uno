#if __SKIA__
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

// Each test blocks the UI thread while collecting, so anything a dead visual queued on the dispatcher
// would keep it (and its subgraph) alive.
[TestClass]
[RunsOnUIThread]
public class Given_CompositionObject_Lifetime
{
	[TestMethod]
	public void When_Elements_Dropped_Then_Visuals_Collected_Without_Dispatcher()
	{
		var visualRefs = CreateDroppedElementVisuals(100);

		CollectFully();

		Assert.AreEqual(0, visualRefs.Count(r => r.IsAlive), "Dead visuals were kept alive by work queued on the dispatcher.");
	}

	[TestMethod]
	public void When_Visual_Disposed_Off_Thread_Then_Nothing_Queued()
	{
		var visualRef = DisposeOffThread();

		CollectFully();

		Assert.IsFalse(visualRef.IsAlive, "Disposing a visual without animations queued work on the dispatcher.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference[] CreateDroppedElementVisuals(int count)
	{
		var refs = new WeakReference[count];
		for (var i = 0; i < count; i++)
		{
			Border border = new() { Child = new TextBlock { Text = "Item" } };
			// Long weak refs: a short one clears before the finalizer runs and can't see a resurrection.
			refs[i] = new(border.Visual, trackResurrection: true);
		}

		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference DisposeOffThread()
	{
		var visual = TestServices.WindowHelper.XamlRoot.Compositor.CreateSpriteVisual();
		// Only the explicit Dispose path is under test here.
		GC.SuppressFinalize(visual);
		WaitWithoutPumping(Task.Run(visual.Dispose));
		return new(visual);
	}

	// Managed waits pump messages on an STA UI thread, which would drain the dispatcher; Thread.Sleep doesn't.
	private static void WaitWithoutPumping(Task task)
	{
		while (!task.IsCompleted)
		{
			Thread.Sleep(1);
		}

		task.GetAwaiter().GetResult();
	}

	private static void CollectFully() => WaitWithoutPumping(Task.Run(CollectFullyCore));

	private static void CollectFullyCore()
	{
		for (var i = 0; i < 3; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}
}
#endif
