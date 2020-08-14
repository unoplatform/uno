using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.System
{
	[TestClass]
	public class Given_DispatcherQueueTimer
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ScheduleWorkItem()
		{
			var tcs = new TaskCompletionSource<object>();
			var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();

			try
			{
				timer.Interval = TimeSpan.FromMilliseconds(10);
				timer.Tick += (snd, e) => tcs.TrySetResult(default);

				timer.Start();

				Assert.IsTrue(timer.IsRunning);

				await Task.WhenAny(tcs.Task, Task.Delay(30000));

				Assert.IsTrue(tcs.Task.IsCompleted);
			}
			finally
			{
				timer.Stop();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ScheduleRepeatingWorkItem()
		{
			var tcs = new TaskCompletionSource<object>();
			var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			var count = 0;

			try
			{
				timer.Interval = TimeSpan.FromMilliseconds(10);
				timer.Tick += (snd, e) =>
				{
					if (++count >= 3)
					{
						tcs.TrySetResult(default);
					}
				};

				timer.Start();

				Assert.IsTrue(timer.IsRunning);
				Assert.IsTrue(timer.IsRepeating);

				await Task.WhenAny(tcs.Task, Task.Delay(30000));

				Assert.IsTrue(tcs.Task.IsCompleted);
				Assert.AreEqual(count, 3);
			}
			finally
			{
				timer.Stop();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ScheduleNonRepeatingWorkItem()
		{
			var tcs = new TaskCompletionSource<bool>();
			var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();

			try
			{
				timer.IsRepeating = false;
				timer.Interval = TimeSpan.FromMilliseconds(10);
				timer.Tick += (snd, e) => tcs.TrySetResult(snd.IsRunning);

				timer.Start();

				Assert.IsTrue(timer.IsRunning);
				Assert.IsFalse(timer.IsRepeating);

				await Task.WhenAny(tcs.Task, Task.Delay(30000));

				Assert.IsTrue(tcs.Task.IsCompleted);
				Assert.IsFalse(tcs.Task.Result);
				Assert.IsFalse(timer.IsRunning);
			}
			finally
			{
				timer.Stop();
			}
		}

#if !WINDOWS_UWP && !__WASM__ // CoreDispatcher.Main.HasThreadAccess is always false on WASM ...
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Tick_Then_RunningOnDispatcher()
		{
			var tcs = new TaskCompletionSource<object>();
			var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			var hasThreadAccess = false;

			try
			{
				timer.Interval = TimeSpan.FromMilliseconds(100);
				timer.Tick += (snd, e) =>
				{
					hasThreadAccess = CoreDispatcher.Main.HasThreadAccess;
					tcs.TrySetResult(default);
				};

				timer.Start();

				await Task.WhenAny(tcs.Task, Task.Delay(1000));

				Assert.IsTrue(hasThreadAccess);
			}
			finally
			{
				timer.Stop();
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_StartAndStopFromBackgroundThread()
		{
			var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();

			try
			{
				timer.Interval = TimeSpan.FromMilliseconds(100);

				Assert.IsFalse(timer.IsRunning);
				await Task.Run(() => timer.Start());
				Assert.IsTrue(timer.IsRunning);
				await Task.Run(() => timer.Stop());
				Assert.IsFalse(timer.IsRunning);
			}
			finally
			{
				timer.Stop();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[ExpectedException(typeof(ArgumentException))]
		public async Task When_SetNegativeInterval()
		{
			var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();

			timer.Interval = TimeSpan.FromMilliseconds(-100);
		}
	}
}
