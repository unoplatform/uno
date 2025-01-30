using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_DispatcherTimer
{
	[TestMethod]
	public async Task When_No_Interval_Set()
	{
		var dispatcherTimer = new DispatcherTimer();
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
		var dispatcherTimer = new DispatcherTimer();
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
			await TestServices.WindowHelper.WaitFor(() => !dispatcherTimer.IsEnabled);

			// The second tick must be scheduled only after the first one finishes completely -
			// around 200ms must have elapsed on the stopwatch.
			Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 180);
		}
		finally
		{
			dispatcherTimer.Stop();
		}
	}

	[TestMethod]
	public async Task When_Change_Interval_Higher()
	{
		var dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromMilliseconds(300);
		try
		{
			Stopwatch stopwatch = new();
			dispatcherTimer.Tick += (s, e) =>
			{
				stopwatch.Stop();
				dispatcherTimer.Stop();
			};
			dispatcherTimer.Start();
			await Task.Delay(100);
			dispatcherTimer.Interval = TimeSpan.FromMilliseconds(600);
			stopwatch.Start();

			await TestServices.WindowHelper.WaitFor(() => !dispatcherTimer.IsEnabled);
			// We increased the interval to 600ms after about 100ms elapsed, so the next tick should be scheduled
			// around 500ms after stopwatch started.
			Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 400);
		}
		finally
		{
			dispatcherTimer.Stop();
		}
	}

	[TestMethod]
	public async Task When_Exception_In_Tick()
	{
		var dispatcherTimer = new DispatcherTimer();
		var simulatedExceptionMessage = "Simulated exception";
		void HandleException(object s, UnhandledExceptionEventArgs e)
		{
			if (e.Exception.Message == simulatedExceptionMessage)
			{
				e.Handled = true;
			}
		}

		Application.Current.UnhandledException += HandleException;

		dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
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
			// Second tick never happens
			Assert.AreEqual(1, tickCounter);
			// Even then, the timer still appears enabled
			Assert.IsTrue(dispatcherTimer.IsEnabled);
		}
		finally
		{
			dispatcherTimer.Stop();
			Application.Current.UnhandledException -= HandleException;
		}
	}
}
