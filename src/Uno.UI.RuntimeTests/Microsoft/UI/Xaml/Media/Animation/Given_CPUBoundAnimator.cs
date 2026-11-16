#if HAS_UNO
#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_CPUBoundAnimator
{
	// Generous, so a slow lane doesn't turn a late end into a failure: the tests check that the end happens at all.
	private static readonly TimeSpan EndTimeout = TimeSpan.FromSeconds(5);

	[TestMethod]
	public async Task When_No_Frames_Then_Ends_At_Its_Duration()
	{
		using var sut = new FramelessAnimator(from: 0, to: 100) { Duration = 100 };

		sut.Start();

		Assert.IsTrue(await sut.WaitForEnd(EndTimeout), "The animation never ended without frames.");
		Assert.AreEqual(1, sut.EndCount);
		Assert.AreEqual(100d, sut.AnimatedValue);
		Assert.AreEqual(100L, sut.CurrentPlayTime);
		Assert.IsFalse(sut.IsRunning);
	}

	[TestMethod]
	public async Task When_No_Frames_And_StartDelay_Then_Ends_After_Delay_And_Duration()
	{
		using var sut = new FramelessAnimator(from: 0, to: 100) { Duration = 100, StartDelay = 200 };

		sut.Start();

		Assert.IsTrue(await sut.WaitForEnd(EndTimeout), "The animation never ended without frames.");
		Assert.IsTrue(sut.EndedAfter >= TimeSpan.FromMilliseconds(300), $"Ended after {sut.EndedAfter.TotalMilliseconds}ms, before its delay and duration.");
		Assert.AreEqual(100d, sut.AnimatedValue);
	}

	[TestMethod]
	public async Task When_No_Frames_And_Paused_Then_Ends_Only_After_Resume()
	{
		using var sut = new FramelessAnimator(from: 0, to: 100) { Duration = 100 };

		sut.Start();
		sut.Pause();

		Assert.IsFalse(await sut.WaitForEnd(TimeSpan.FromMilliseconds(400)), "A paused animation ended.");

		sut.Resume();

		Assert.IsTrue(await sut.WaitForEnd(EndTimeout), "The resumed animation never ended without frames.");
		Assert.AreEqual(1, sut.EndCount);
	}

	[TestMethod]
	public async Task When_No_Frames_And_Cancelled_Then_Never_Ends()
	{
		using var sut = new FramelessAnimator(from: 0, to: 100) { Duration = 100 };

		sut.Start();
		sut.Cancel();

		Assert.IsFalse(await sut.WaitForEnd(TimeSpan.FromMilliseconds(400)), "A cancelled animation ended.");
	}

	[TestMethod]
	public async Task When_A_Frame_Ends_It_Then_Ends_Once()
	{
		using var sut = new FramelessAnimator(from: 0, to: 100) { Duration = 100 };

		sut.Start();
		// Blocks the UI thread past the end, so the end timer's tick is queued behind the frame and runs after it.
		Thread.Sleep(150);
		sut.DeliverFrame();

		Assert.AreEqual(1, sut.EndCount);

		await Task.Delay(400);

		Assert.AreEqual(1, sut.EndCount, "The end was raised again after a frame had already ended the animation.");
	}

	/// <summary>An animator whose host delivers no frames, as when a window is hidden or minimized.</summary>
	private sealed class FramelessAnimator : CPUBoundAnimator<double>
	{
		private readonly Stopwatch _clock = new();
		private TaskCompletionSource? _ended;

		public FramelessAnimator(double from, double to)
			: base(from, to)
		{
			AnimationEnd += (_, _) =>
			{
				EndCount++;
				EndedAfter = _clock.Elapsed;
				_ended?.TrySetResult();
			};
		}

		public new long Duration
		{
			get => base.Duration;
			init => SetDuration(value);
		}

		public new long StartDelay
		{
			get => base.StartDelay;
			init => base.StartDelay = value;
		}

		public int EndCount { get; private set; }

		public TimeSpan EndedAfter { get; private set; }

		public new void Start()
		{
			_clock.Restart();
			base.Start();
		}

		public async Task<bool> WaitForEnd(TimeSpan timeout)
		{
			if (EndCount > 0)
			{
				return true;
			}

			_ended = new TaskCompletionSource();
			return await Task.WhenAny(_ended.Task, Task.Delay(timeout)) == _ended.Task;
		}

		public void DeliverFrame() => OnFrame(this, EventArgs.Empty);

		protected override void EnableFrameReporting()
		{
		}

		protected override void DisableFrameReporting()
		{
		}

		protected override double GetUpdatedValue(long frame, double from, double to)
			=> from + (to - from) * frame / Math.Max(1, base.Duration);
	}
}
#endif
