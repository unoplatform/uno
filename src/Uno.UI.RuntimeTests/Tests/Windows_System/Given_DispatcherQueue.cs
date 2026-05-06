using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_System
{
	[TestClass]
	public class Given_DispatcherQueue
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // https://github.com/unoplatform/uno/issues/22862
		public void When_GetForCurrentThreadFromDispatcher()
		{
			Assert.IsNotNull(DispatcherQueue.GetForCurrentThread());
		}

#if !__WASM__ // Wasm does not have bg threads yet ...
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public void When_GetForCurrentThreadFromBackgroundThread()
		{
			Assert.IsNull(DispatcherQueue.GetForCurrentThread());
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_NativeDispatcherSynchronizationContext_Continuation_Scheduling()
		{
			var list = new List<int>();
			DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.High, async () =>
			{
				list.Add(1);
				await Task.Yield();
				list.Add(2);
			});
			DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Normal, () => list.Add(3));
			await UITestHelper.WaitForIdle();
			CollectionAssert.AreEqual(new[] { 1, 3, 2 }, list);
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public async Task When_Low_Priority_With_Active_Rendering_Subscriber_Does_Not_Starve()
		{
			// Regression test: subscribing to CompositionTarget.Rendering activates the
			// self-perpetuating 60fps render cycle (RequestNewFrame + unconditional
			// InvalidateRender at the end of every Render() in CompositionTarget.Rendering.skia.cs).
			// The cycle queues a render in NativeDispatcher._compositionTargets at every
			// vsync. Before the fix, the render gate (normalItemsToProcessBeforeNextRenderAction)
			// only counted Normal queue items, so a Low-priority item enqueued under render
			// saturation could starve indefinitely. The fix counts Normal+Low so Low items
			// always get a chance to run before the next render.
			var lowRan = new TaskCompletionSource<bool>();
			EventHandler<object> renderingHandler = (_, _) => { /* keep _isRenderingActive true */ };

			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += renderingHandler;
			try
			{
				// Let the render cycle reach steady state.
				await Task.Delay(100);

				var enqueued = DispatcherQueue.GetForCurrentThread().TryEnqueue(
					DispatcherQueuePriority.Low,
					() => lowRan.TrySetResult(true));
				Assert.IsTrue(enqueued, "TryEnqueue should accept the Low-priority item.");

				// Allow up to 2s for the Low item to be dispatched. With the bug present, it
				// never runs because every DispatchItems call dispatches a render first.
				var completed = await Task.WhenAny(lowRan.Task, Task.Delay(2000));
				Assert.AreSame(lowRan.Task, completed,
					"Low-priority item starved while CompositionTarget.Rendering was active.");
			}
			finally
			{
				Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= renderingHandler;
			}
		}
	}
}
