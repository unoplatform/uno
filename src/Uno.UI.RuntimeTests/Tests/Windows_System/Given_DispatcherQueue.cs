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
	}
}
