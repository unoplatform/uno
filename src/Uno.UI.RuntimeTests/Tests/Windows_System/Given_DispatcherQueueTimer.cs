using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Uno.UI.RuntimeTests.Tests.Windows_System
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_DispatcherQueueTimer
	{
		[TestMethod]
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
				Assert.AreEqual(3, count);
			}
			finally
			{
				timer.Stop();
			}
		}

		[TestMethod]
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

#if !WINAPPSDK
		[TestMethod]
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
		public void When_SetNegativeInterval()
		{
			var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			var negativeTimeSpan = TimeSpan.FromMilliseconds(-100);
			Assert.Throws<ArgumentException>(() => timer.Interval = negativeTimeSpan);
		}

		[TestMethod]
		public async Task When_No_Interval_Set()
		{
			var dispatcherTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			try
			{
				int tickCounter = 0;
				dispatcherTimer.Tick += (s, e) =>
				{
					tickCounter++;
				};
				dispatcherTimer.Start();
				await TestServices.WindowHelper.WaitFor(() => tickCounter > 0);
				await TestServices.WindowHelper.WaitFor(() => tickCounter > 5);
			}
			finally
			{
				dispatcherTimer.Stop();
			}
		}

		[TestMethod]
		public async Task When_Sleep_In_Tick()
		{
			var dispatcherTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			dispatcherTimer.Interval = TimeSpan.FromMilliseconds(200);
			try
			{
				var firstTick = true;
				Stopwatch stopwatch = new Stopwatch();
				dispatcherTimer.Tick += (s, e) =>
				{
					if (!firstTick)
					{
						stopwatch.Stop();
						dispatcherTimer.Stop();
					}
					Thread.Sleep(100);
					if (firstTick)
					{
						stopwatch.Start();
						firstTick = false;
					}
				};
				dispatcherTimer.Start();
				await TestServices.WindowHelper.WaitFor(() => !dispatcherTimer.IsRunning);

				// The second tick must be scheduled only after the first one finishes completely -
				// around 200ms must have elapsed on the stopwatch.
				Assert.IsGreaterThanOrEqual(180, stopwatch.ElapsedMilliseconds);
			}
			finally
			{
				dispatcherTimer.Stop();
			}
		}

		[TestMethod]
		public async Task When_Change_Interval_Higher()
		{
			var dispatcherTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			dispatcherTimer.Interval = TimeSpan.FromMilliseconds(300);
			try
			{
				int repeats = 0;
				Stopwatch stopwatch = new();
				dispatcherTimer.Tick += (s, e) =>
				{
					repeats++;
					if (repeats == 1)
					{
						// We increased the interval to 600ms after about 100ms elapsed,
						// but this should not reset the existing scheduled tick.
						Assert.IsLessThanOrEqual(400, stopwatch.ElapsedMilliseconds);
						stopwatch.Restart();
					}
					else if (repeats == 2)
					{
						// The second tick should be scheduled after 600ms
						Assert.IsGreaterThanOrEqual(400, stopwatch.ElapsedMilliseconds);
						stopwatch.Stop();
						dispatcherTimer.Stop();
					}
				};
				dispatcherTimer.Start();
				await Task.Delay(100);
				dispatcherTimer.Interval = TimeSpan.FromMilliseconds(600);
				stopwatch.Start();

				await TestServices.WindowHelper.WaitFor(() => !dispatcherTimer.IsRunning, timeoutMS: 2000);
			}
			finally
			{
				dispatcherTimer.Stop();
			}
		}

		[TestMethod]
		public async Task When_Exception_In_Tick()
		{
			var dispatcherTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			var simulatedExceptionMessage = "Simulated exception";
			void HandleException(object s, UnhandledExceptionEventArgs e)
			{
				if (e.Exception.Message == simulatedExceptionMessage)
				{
					e.Handled = true;
				}
			}

			Application.Current.UnhandledException += HandleException;

			dispatcherTimer.Interval = TimeSpan.FromMilliseconds(50);
			try
			{
				int tickCounter = 0;
				dispatcherTimer.Tick += (s, e) =>
				{
					tickCounter++;
					throw new InvalidOperationException(simulatedExceptionMessage);
				};
				dispatcherTimer.Start();
				await TestServices.WindowHelper.WaitFor(() => tickCounter > 0);
				await Task.Delay(200);

				// The time keeps ticking even after an exception
				Assert.IsTrue(dispatcherTimer.IsRunning);
				Assert.IsTrue(dispatcherTimer.IsRepeating);
				Assert.IsGreaterThan(1, tickCounter);
			}
			finally
			{
				dispatcherTimer.Stop();
				Application.Current.UnhandledException -= HandleException;
			}
		}
	}
}
