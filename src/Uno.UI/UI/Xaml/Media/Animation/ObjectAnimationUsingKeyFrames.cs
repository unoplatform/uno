using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Windows.UI.Xaml.Markup;
using Windows.UI.Core;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Media.Animation
{
	[ContentProperty(Name = "KeyFrames")]
	public sealed partial class ObjectAnimationUsingKeyFrames : Timeline, ITimeline
	{
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private EventActivity _traceActivity;

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{9EBBD06A-ADA3-464F-93C6-C850AB62A41D}");

			public const int Start = 1;
			public const int Stop = 2;
			public const int Pause = 3;
			public const int Resume = 4;
		}

		private KeyFrameScheduler<object> _frameScheduler;
		private (int count, TimeSpan time) _playStatus;

		public ObjectAnimationUsingKeyFrames()
		{
			KeyFrames = new ObjectKeyFrameCollection(owner: this, isAutoPropertyInheritanceEnabled: false);
		}

		#region KeyFrames DependencyProperty

		public ObjectKeyFrameCollection KeyFrames
		{
			get => (ObjectKeyFrameCollection)GetValue(KeyFramesProperty);
			internal set => SetValue(KeyFramesProperty, value);
		}

		/// <remarks>
		/// This property is not exposed as a DP in UWP, but it is required
		/// to be one for the DataContext/TemplatedParent to flow properly.
		/// </remarks>
		internal static DependencyProperty KeyFramesProperty { get; } =
			DependencyProperty.Register(
				name: "KeyFrames",
				propertyType: typeof(ObjectKeyFrameCollection),
				ownerType: typeof(ObjectAnimationUsingKeyFrames),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null
				)
			);

		#endregion

		internal override TimeSpan GetCalculatedDuration()
		{
			var duration = Duration;
			if (duration != Duration.Automatic)
			{
				return base.GetCalculatedDuration();
			}

			if (KeyFrames.Any())
			{
				var lastKeyTime = KeyFrames.Max(kf => kf.KeyTime);
				return lastKeyTime.TimeSpan;
			}

			return base.GetCalculatedDuration();
		}

		void ITimeline.Begin()
		{
			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Start,
					EventOpcode.Start,
					payload: GetTraceProperties()
				);
			}

			Reset();

			State = TimelineState.Active;

			_playStatus = default;
			_frameScheduler = new KeyFrameScheduler<object>(
				BeginTime,
				Duration.HasTimeSpan ? Duration.TimeSpan : default(TimeSpan?),
				default,
				KeyFrames,
				OnFrame,
				OnFramesEnd);
			_frameScheduler.Start();
		}

		void ITimeline.Stop()
		{
			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Stop,
					EventOpcode.Stop,
					payload: GetTraceProperties()
				);
			}

			// We explicitly call the Stop of the _frameScheduler before the Reset dispose it,
			// so the EndReason will be Stopped instead of Aborted.
			_frameScheduler?.Stop();

			Reset();
			ClearValue();
		}

		void ITimeline.Resume()
		{
			if (State != TimelineState.Paused)
			{
				return;
			}

			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Resume,
					EventOpcode.Send,
					payload: GetTraceProperties()
				);
			}

			State = TimelineState.Active;
			_frameScheduler!.Resume();
		}

		void ITimeline.Pause()
		{
			if (State != TimelineState.Active)
			{
				return;
			}

			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Pause,
					EventOpcode.Send,
					payload: GetTraceProperties()
				);
			}

			State = TimelineState.Paused;
			_frameScheduler!.Pause();
		}

		void ITimeline.Seek(TimeSpan offset)
		{
			_frameScheduler?.Seek(offset);
		}

		void ITimeline.SeekAlignedToLastTick(TimeSpan offset)
		{
			// Same as Seek
			((ITimeline)this).Seek(offset);
		}

		void ITimeline.SkipToFill()
		{
			// Set value to last keytime and set state to filling
			_frameScheduler?.Dispose();
			_frameScheduler = null;

			var fillFrame = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).Last();

			SetValue(fillFrame.Value);
			State = TimelineState.Stopped;
		}

		void ITimeline.Deactivate()
		{
			Reset();
		}

		/// <summary>
		/// Brings the Timeline to its initial state
		/// </summary>
		private void Reset()
		{
			_frameScheduler?.Dispose();
			_frameScheduler = null;

			State = TimelineState.Stopped;
		}

		private IDisposable OnFrame(object currentValue, IKeyFrame<object> frame, TimeSpan duration)
		{
			SetValue(frame.Value);
			return null;
		}

		private void OnFramesEnd(KeyFrameScheduler<object>.EndReason endReason)
		{
			_playStatus = (_playStatus.count + 1, _playStatus.time + _frameScheduler!.Elapsed);

			if (endReason != KeyFrameScheduler<object>.EndReason.EndOfFrames)
			{
				return;
			}

			if (RepeatBehavior.ShouldRepeat(_playStatus.time, _playStatus.count))
			{
				Replay();
				return;
			}

			if (FillBehavior == FillBehavior.HoldEnd)//Two types of fill behaviors : HoldEnd - Keep displaying the last frame
			{
				Fill();
			}
			else// Stop - Put back the initial state
			{
				Reset();
				ClearValue();
			}

			OnCompleted();
		}

		/// <summary>
		/// Fills the animation: the final frame is shown and left visible
		/// </summary>
		private void Fill()
		{
			var lastKeyFrame = KeyFrames.MaxBy(k => k.KeyTime.TimeSpan);

			_frameScheduler?.Dispose();
			_frameScheduler = null;

			State = TimelineState.Filling;
			SetValue(lastKeyFrame.Value);
		}

		/// <summary>
		/// Replays the Timeline
		/// </summary>
		private void Replay()
		{
			_frameScheduler?.Dispose();

			ClearValue();


			_frameScheduler = new KeyFrameScheduler<object>(
				BeginTime,
				Duration.HasTimeSpan ? Duration.TimeSpan : default(TimeSpan?),
				default,
				KeyFrames,
				OnFrame,
				OnFramesEnd);
			_frameScheduler.Start();
		}

		/// <summary>
		/// Destroys the animation
		/// </summary>
		/// <param name="disposing"></param>
		private protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_frameScheduler != null)
			{
				_frameScheduler.Dispose();
				_frameScheduler = null;
			}
		}
	}
}
