using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Markup;
using Windows.UI.Core;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Media.Animation
{
	[ContentProperty(Name = "KeyFrames")]
	public sealed partial class ObjectAnimationUsingKeyFrames : Timeline, ITimeline, IKeyFramesProvider
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
		private bool _isReversing;
		private object _startingValue;

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
			_isReversing = false;
			_startingValue = GetValue();

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

			// With AutoReverse, the final value is the starting value (after reversing back)
			if (AutoReverse)
			{
				SetValue(_startingValue);
			}
			else
			{
				var fillFrame = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).Last();
				SetValue(fillFrame.Value);
			}

			State = FillBehavior == FillBehavior.HoldEnd ? TimelineState.Filling : TimelineState.Stopped;
			OnCompleted();
		}

		/// <summary>
		/// Begins the animation in reverse, playing from the end value back to the start value.
		/// Used by Storyboard-level AutoReverse to signal child animations to play in reverse.
		/// </summary>
		void ITimeline.BeginReversed()
		{
			Reset();

			State = TimelineState.Active;
			_isReversing = true;
			_startingValue = GetValue();

			PlayReversed();
		}

		/// <summary>
		/// Skips to the fill state as if the animation had played in reverse.
		/// Sets the animated property to its starting value (the "reversed" end state).
		/// </summary>
		void ITimeline.SkipToFillReversed()
		{
			_frameScheduler?.Dispose();
			_frameScheduler = null;

			// Set to the starting value (the "reversed" end state)
			SetValue(_startingValue);

			State = TimelineState.Filling;
			OnCompleted();
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

			// Handle AutoReverse: if enabled and we just finished the forward animation, reverse it
			if (AutoReverse && !_isReversing)
			{
				_isReversing = true;
				PlayReversed();
				return;
			}

			// If we were reversing, we've now completed both forward and reverse
			if (_isReversing)
			{
				_isReversing = false;
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
		/// Play keyframes in reverse order
		/// </summary>
		private void PlayReversed()
		{
			_frameScheduler?.Dispose();

			// Create reversed keyframe sequence
			// For discrete keyframe animations, we play the keyframes in reverse order
			var reversedKeyFrames = CreateReversedKeyFrames();

			_frameScheduler = new KeyFrameScheduler<object>(
				BeginTime,
				Duration.HasTimeSpan ? Duration.TimeSpan : default(TimeSpan?),
				default,
				reversedKeyFrames,
				OnFrame,
				OnFramesEnd);
			_frameScheduler.Start();
		}

		/// <summary>
		/// Creates a reversed sequence of keyframes for reverse playback.
		/// </summary>
		private IEnumerable<IKeyFrame<object>> CreateReversedKeyFrames()
		{
			var orderedKeyFrames = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).ToList();
			if (orderedKeyFrames.Count == 0)
			{
				yield break;
			}

			var totalDuration = Duration.HasTimeSpan
				? Duration.TimeSpan
				: orderedKeyFrames.Last().KeyTime.TimeSpan;

			// Yield keyframes in reverse order with adjusted times
			for (int i = orderedKeyFrames.Count - 1; i >= 0; i--)
			{
				var originalFrame = orderedKeyFrames[i];
				var originalTime = originalFrame.KeyTime.TimeSpan;
				var reversedTime = totalDuration - originalTime;

				// Determine the value - in reverse, we want to go to the previous value (or starting value for the last)
				object value;
				if (i == 0)
				{
					// The last keyframe in reverse should return to the starting value
					value = _startingValue;
				}
				else
				{
					// Otherwise, go to the previous keyframe's value
					value = orderedKeyFrames[i - 1].Value;
				}

				yield return new DiscreteObjectKeyFrame
				{
					KeyTime = KeyTime.FromTimeSpan(reversedTime),
					Value = value
				};
			}
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

		IEnumerable IKeyFramesProvider.GetKeyFrames() => KeyFrames;
	}
}
