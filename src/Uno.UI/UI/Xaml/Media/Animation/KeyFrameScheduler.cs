#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.System;
using Uno.Disposables;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media.Animation
{
	internal class KeyFrameScheduler<TValue> : IDisposable
	{
		public delegate IDisposable? OnFrame(TValue current, IKeyFrame<TValue> frame, TimeSpan duration);

		private readonly Stopwatch _watch = new Stopwatch();
		private readonly OnFrame _onFrame;
		private readonly Action<EndReason> _onCompleted;
		private readonly TimeSpan? _beginTime;
		private readonly TimeSpan? _duration;
		private readonly List<IKeyFrame<TValue>> _frames;

		private DispatcherQueueTimer? _timer;
		private int _frameId = -1;
		private SerialDisposable? _frameSubscription; // Lazy init only if needed (not used for the common discrete key frames)
		private TimeSpan _seekOffset;

		private int _state = States.NotRunning;

		private static class States
		{
			public const int NotRunning = 0;
			public const int Running = 1;
			public const int Paused = 2;
			public const int Ended = int.MaxValue; // a.k.a. Stoped / Disposed : This Scheduler can run frames only once!
		}

		public enum EndReason
		{
			/// <summary>
			/// Reached the last frame and wait for the configured duration
			/// </summary>
			EndOfFrames,

			/// <summary>
			/// Stopped by the user
			/// </summary>
			Stopped,

			/// <summary>
			/// Aborted
			/// </summary>
			Disposed
		}

		public KeyFrameScheduler(
			TimeSpan? beginTime,
			TimeSpan? duration,
			TValue initialValue,
			IEnumerable<IKeyFrame<TValue>>? frames,
			OnFrame onFrame,
			Action<EndReason> onCompleted)
		{
			CurrentValue = initialValue;

			frames ??= Enumerable.Empty<IKeyFrame<TValue>>();
			frames = duration.HasValue
				? frames.Where(k => k != null && k.KeyTime.TimeSpan <= duration.Value)
				: frames.Trim();

			_frames = frames.ToList();
			_frames.Sort(KeyFrameComparer<TValue>.Instance);

			_beginTime = beginTime;
			_duration = duration;
			_onFrame = onFrame;
			_onCompleted = onCompleted;
		}

		public TValue CurrentValue { get; private set; }

		public TimeSpan Elapsed => _seekOffset + _watch.Elapsed;

		public void Start()
		{
			if (Interlocked.CompareExchange(ref _state, States.Running, States.NotRunning) == States.NotRunning)
			{
				_watch.Restart();

				var nextFrame = GetNextFrameDueIn();
				if (nextFrame > TimeSpan.Zero)
				{
					ScheduleNextFrame(nextFrame);
				}
				else
				{
					RunNextFrame();
				}
			}
		}

		public void Pause()
		{
			if (Interlocked.CompareExchange(ref _state, States.Paused, States.Running) == States.Running)
			{
				_watch.Stop();
				_timer?.Stop();
			}
		}

		public void Resume()
		{
			if (Interlocked.CompareExchange(ref _state, States.Running, States.Paused) == States.Paused)
			{
				_watch.Start();

				var nextFrame = GetNextFrameDueIn();
				if (nextFrame > TimeSpan.Zero)
				{
					ScheduleNextFrame(nextFrame);
				}
				else
				{
					RunNextFrame();
				}
			}
		}

		public void Stop()
		{
			Complete(EndReason.Stopped);
		}

		public void Seek(TimeSpan offset)
		{
			if (_state == States.Ended)
			{
				return;
			}

			_seekOffset += offset;

			// Search for the first last frame before the updated Elapsed
			_frameId = 0;
			for (var i = 0; i < _frames.Count; i++)
			{
				if (_frames[i].KeyTime.TimeSpan > Elapsed)
				{
					break;
				}
				_frameId = i;
			}

			// And request to apply it right now if running
			if (_state == States.Running)
			{
				RunNextFrame();
			}
		}

		private void ScheduleNextFrame(TimeSpan delay)
		{
			if (_timer is null)
			{
				_timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
				_timer.State = this;
				_timer.Tick += RunNextFrame;
			}

			_timer.Interval = delay;
			_timer.Start();
		}

		private static void RunNextFrame(DispatcherQueueTimer timer, object state)
			=> ((KeyFrameScheduler<TValue>)timer.State).RunNextFrame();

		private void RunNextFrame()
		{
			_timer?.Stop();

			if (_state != States.Running)
			{
				return;
			}

			if (++_frameId >= _frames.Count)
			{
				Complete(EndReason.EndOfFrames);
				return;
			}

			var frame = _frames[_frameId]!; // ! We trim the collection in ctor
			var duration = GetNextFrameDueIn(); // The duration of the current before moving to next frame

			var oldValue = CurrentValue;
			CurrentValue = frame.Value;

			var frameSubscription = _onFrame(oldValue, frame, duration);
			if (frameSubscription != null)
			{
				(_frameSubscription ??= new SerialDisposable()).Disposable = frameSubscription;
			}

			if (duration > TimeSpan.Zero)
			{
				ScheduleNextFrame(duration);
			}
			else
			{
				RunNextFrame();
			}
		}

		/// <summary>
		/// Gets the delay to wait before moving to the next frame
		/// </summary>
		private TimeSpan GetNextFrameDueIn()
		{
			var nextFrameId = _frameId + 1;
			var nextFrameDueTime = nextFrameId < _frames.Count
				? _frames[nextFrameId].KeyTime.TimeSpan
				: _duration ?? TimeSpan.Zero;

			if (_beginTime.HasValue)
			{
				nextFrameDueTime += _beginTime.Value;
			}

			return nextFrameDueTime - Elapsed;
		}

		private void Complete(EndReason reason)
		{
			if (_timer is { } timer)
			{
				timer.Stop(); // Safety only, should already have been stopped!
				timer.State = null;
				timer.Tick -= RunNextFrame;

				_timer = null;
			}

			_watch.Stop();
			_frameSubscription?.Dispose();

			if (Interlocked.Exchange(ref _state, States.Ended) != States.Ended)
			{
				_onCompleted(reason);
			}
		}

		/// <inheritdoc />
		public void Dispose()
			=> Complete(EndReason.Disposed);
	}
}
