#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Samples.Tests;

/// <summary>
/// Emits a periodic heartbeat while runtime tests execute, so a CI run that freezes
/// leaves evidence of *what* froze rather than just going silent until the job timeout.
/// </summary>
/// <remarks>
/// The heartbeat runs on a dedicated thread rather than a timer or the thread pool: if the
/// pool is starved the heartbeat must still be emitted, otherwise it cannot be distinguished
/// from a fully frozen process. Each tick probes the UI dispatcher and the thread pool
/// separately, which separates "UI thread blocked" from "thread pool starved" from
/// "whole process stopped" (no heartbeat line at all).
/// </remarks>
internal sealed class TestRunStallMonitor : IDisposable
{
	internal const string LogPrefix = "[stall-monitor]";

	private const string IntervalVariable = "UNO_TEST_STALL_MONITOR_INTERVAL_SECONDS";
	private const string StallThresholdVariable = "UNO_TEST_STALL_MONITOR_THRESHOLD_SECONDS";

	private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

	private readonly TimeSpan _interval;
	private readonly TimeSpan _stallThreshold;
	private readonly Func<Task>? _dispatcherProbe;
	private readonly Stopwatch _runElapsed = Stopwatch.StartNew();
	private readonly CancellationTokenSource _cts = new();
	private readonly Thread _thread;

	private volatile string _currentTest = "(none)";
	private long _currentTestStartedAt;
	private bool _disposed;

	private TestRunStallMonitor(TimeSpan interval, TimeSpan stallThreshold, Func<Task>? dispatcherProbe)
	{
		_interval = interval;
		_stallThreshold = stallThreshold;
		_dispatcherProbe = dispatcherProbe;
		_currentTestStartedAt = _runElapsed.ElapsedMilliseconds;

		_thread = new Thread(Loop)
		{
			IsBackground = true,
			Name = "UnoTestStallMonitor",
			Priority = ThreadPriority.AboveNormal,
		};
		_thread.Start();
	}

	/// <summary>
	/// Starts a monitor, or returns <c>null</c> when disabled. Enabled by default on CI;
	/// set <c>UNO_TEST_STALL_MONITOR_INTERVAL_SECONDS</c> to opt in locally, or to 0 to disable.
	/// </summary>
	public static TestRunStallMonitor? TryStart(Func<Task>? dispatcherProbe)
	{
		var interval = GetSeconds(IntervalVariable, defaultSeconds: DefaultIntervalSeconds);
		if (interval <= 0)
		{
			return null;
		}

		var threshold = GetSeconds(StallThresholdVariable, defaultSeconds: 120);

		Console.WriteLine(
			$"{LogPrefix} enabled: interval={interval}s stallThreshold={threshold}s " +
			$"pid={GetProcessIdSafe()} os={RuntimeDescription()}");

		return new TestRunStallMonitor(
			TimeSpan.FromSeconds(interval),
			TimeSpan.FromSeconds(threshold),
			dispatcherProbe);
	}

	private static int DefaultIntervalSeconds =>
#if IS_CI
		30;
#else
		0;
#endif

	public void SetCurrentTest(string testName)
	{
		_currentTest = testName;
		Interlocked.Exchange(ref _currentTestStartedAt, _runElapsed.ElapsedMilliseconds);
	}

	private void Loop()
	{
		var previousGcPause = TimeSpan.Zero;

		while (!_cts.IsCancellationRequested)
		{
			try
			{
				if (_cts.Token.WaitHandle.WaitOne(_interval))
				{
					return;
				}

				var inCurrentTest = TimeSpan.FromMilliseconds(
					_runElapsed.ElapsedMilliseconds - Interlocked.Read(ref _currentTestStartedAt));

				var (dispatcher, threadPool) = (ProbeDispatcher(), ProbeThreadPool());
				var gcPause = TotalGcPause();
				var gcDelta = gcPause - previousGcPause;
				previousGcPause = gcPause;

				var stalled = inCurrentTest >= _stallThreshold;

				Console.WriteLine(
					$"{LogPrefix}{(stalled ? " STALL" : "")} " +
					$"elapsed={Format(_runElapsed.Elapsed)} " +
					$"inTest={Format(inCurrentTest)} " +
					$"dispatcher={dispatcher} " +
					$"threadPool={threadPool} " +
					$"gcPauseDelta={gcDelta.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture)}ms " +
					$"test='{_currentTest}'");
			}
			catch (Exception e)
			{
				// The monitor must never be able to fail a run it is only observing.
				Console.WriteLine($"{LogPrefix} probe error: {e.GetType().Name}: {e.Message}");
			}
		}
	}

	private string ProbeDispatcher()
	{
		if (_dispatcherProbe is null)
		{
			return "n/a";
		}

		var sw = Stopwatch.StartNew();
		try
		{
			var task = _dispatcherProbe();
			return task.Wait(ProbeTimeout)
				? $"{sw.ElapsedMilliseconds}ms"
				: $"BLOCKED(>{ProbeTimeout.TotalSeconds:F0}s)";
		}
		catch (Exception e)
		{
			return $"error({e.GetType().Name})";
		}
	}

	private static string ProbeThreadPool()
	{
		var sw = Stopwatch.StartNew();
		var signal = new ManualResetEventSlim(false);

		if (!ThreadPool.UnsafeQueueUserWorkItem(_ => signal.Set(), null))
		{
			return "queue-failed";
		}

		return signal.Wait(ProbeTimeout)
			? $"{sw.ElapsedMilliseconds}ms"
			: $"STARVED(>{ProbeTimeout.TotalSeconds:F0}s)";
	}

	private static TimeSpan TotalGcPause()
	{
#if NET7_0_OR_GREATER
		return GC.GetTotalPauseDuration();
#else
		return TimeSpan.Zero;
#endif
	}

	private static int GetSeconds(string variable, int defaultSeconds)
		=> int.TryParse(Environment.GetEnvironmentVariable(variable), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
			? value
			: defaultSeconds;

	private static string Format(TimeSpan value)
		=> value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

	private static string GetProcessIdSafe()
	{
		try
		{
			return Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			return "?";
		}
	}

	private static string RuntimeDescription()
		=> $"{Environment.OSVersion.Platform}/{System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}";

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_cts.Cancel();
		_thread.Join(TimeSpan.FromSeconds(2));
		_cts.Dispose();

		Console.WriteLine($"{LogPrefix} stopped after {Format(_runElapsed.Elapsed)}");
	}
}
