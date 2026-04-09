using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml.Data;
using Uno.Diagnostics.Eventing;
using Microsoft.UI.Xaml.Markup;
using System.Threading;
using Windows.UI.Core;
using Uno.Disposables;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.UI.Xaml.Media.Animation
{
	[ContentProperty(Name = "Children")]
	public sealed partial class Storyboard : Timeline, ITimeline, IAdditionalChildrenProvider, ITimelineListener
	{
		private static readonly IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private EventActivity _traceActivity;

		public static class TraceProvider
		{
			public static readonly Guid Id = Guid.Parse("{57A7F5D4-8AA9-453F-A2D5-9F9DCA48BF54}");

			public const int StoryBoard_Start = 1;
			public const int StoryBoard_Stop = 2;
			public const int StoryBoard_Pause = 3;
			public const int StoryBoard_Resume = 4;
		}

		private readonly Stopwatch _activeDuration = new Stopwatch();
		private int _replayCount = 1;
		private int _runningChildren;
		private bool _hasFillingChildren;
		private bool _hasScheduledCompletion;

		public Storyboard()
		{
			Children = new TimelineCollection(owner: this, isAutoPropertyInheritanceEnabled: false);
		}

		public TimelineCollection Children { get; }

		#region TargetName Attached Property
		public static string GetTargetName(Timeline element) => (string)element.GetValue(TargetNameProperty);

		public static void SetTargetName(Timeline element, string name) => element.SetValue(TargetNameProperty, name);

		// Using a DependencyProperty as the backing store for TargetName.  This enables animation, styling, binding, etc...
		public static DependencyProperty TargetNameProperty
		{
			[DynamicDependency(nameof(GetTargetName))]
			[DynamicDependency(nameof(SetTargetName))]
			get;
		} = DependencyProperty.RegisterAttached(
				"TargetName",
				typeof(string),
				typeof(Storyboard),
				new FrameworkPropertyMetadata(string.Empty));
		#endregion

		#region TargetProperty Attached Property
		public static string GetTargetProperty(Timeline element) => (string)element.GetValue(TargetPropertyProperty);

		public static void SetTargetProperty(Timeline element, string path) => element.SetValue(TargetPropertyProperty, path);

		// Using a DependencyProperty as the backing store for TargetProperty.  This enables animation, styling, binding, etc...
		public static DependencyProperty TargetPropertyProperty
		{
			[DynamicDependency(nameof(GetTargetProperty))]
			[DynamicDependency(nameof(SetTargetProperty))]
			get;
		} = DependencyProperty.RegisterAttached(
				"TargetProperty",
				typeof(string),
				typeof(Storyboard),
				new FrameworkPropertyMetadata(string.Empty));
		#endregion

		public static void SetTarget(Timeline timeline, DependencyObject target) => timeline.Target = target;

		/// <summary>
		/// Explicitly sets the target using an ElementNameSubject, in case of lazy
		/// evaluation of the target element.
		/// </summary>
		public static void SetTarget(Timeline timeline, ElementNameSubject target) => timeline.SetElementNameTarget(target);

		/// <summary>
		/// Disposes event subscriptions for an <see cref="ITimeline"/>
		/// </summary>
		/// <param name="child"></param>
		private void DisposeChildRegistrations(ITimeline child)
		{
			child.UnregisterListener(this);
		}

		/// <summary>
		/// Replay this animation.
		/// </summary>
		private void Replay()
		{
			_replayCount++;

			Play();
		}

		public void Begin()
		{
			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.StoryBoard_Start,
					EventOpcode.Start,
					payload: new[] {
						this.GetParent()?.GetType().Name,
						this.GetParent()?.GetDependencyObjectId().ToString(CultureInfo.InvariantCulture),
					}
				);
			}

			State = TimelineState.Active;
			_hasFillingChildren = false;
			_replayCount = 1;
			_activeDuration.Restart();

#if __SKIA__
			// MUX: CStoryboard::BeginPrivate — register with TimeManager and set timing flags.
			// The TimeManager will call ComputeState() on the next tick (before layout).
			// Only register if there are children — empty storyboards complete via dispatcher callback.
			if (Children is { Count: > 0 } && Uno.UI.FeatureConfiguration.Timeline.UseTimeManager)
			{
				_isStopped = false;
				_isBeginning = true;
				_lastParentTime = double.MaxValue;
				_isPausedForTimeManager = false;
				_isResuming = false;
				_isSeeking = false;
				_completedEventFiredByTimeManager = false;
				_computedClockState = InternalClockState.NotStarted;

				if (!_isRegisteredWithTimeManager)
				{
					TimeManager.Instance.AddTimeline(this);
				}

				// Request an immediate tick so the first ComputeState happens before the next layout.
				Uno.UI.Xaml.Core.CoreServices.RequestAdditionalFrame();
			}
#endif

			Play();
		}

		private void Play()
		{
			_runningChildren = Children?.Count ?? 0;
			if (_runningChildren > 0)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					ITimeline child = Children[i];

					DisposeChildRegistrations(child);

					child.RegisterListener(this);

					child.Begin();
				}
			}
			else
			{
				_hasScheduledCompletion = true;
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					_hasScheduledCompletion = false;
					if (State == TimelineState.Stopped)
					{
						// If the storyboard was force-stopped,
						// we don't stop again and don't trigger Completed.
						return;
					}

					// No children, so we complete immediately
					State = TimelineState.Stopped;
					OnCompleted();
				});
			}
		}

		public void Stop()
		{
			if (_trace.IsEnabled)
			{
				_trace.WriteEventActivity(
					eventId: TraceProvider.StoryBoard_Stop,
					opCode: EventOpcode.Stop,
					activity: _traceActivity,
					payload: new object[] { Target?.GetType().ToString(), PropertyInfo?.Path }
				);
			}

#if __SKIA__
			_isStopped = true;
			_isPausedForTimeManager = false;
			_isResuming = false;
			_isBeginning = false;
			_isSeeking = false;

			if (_isRegisteredWithTimeManager)
			{
				TimeManager.Instance.RemoveTimeline(this);
			}
#endif

			State = TimelineState.Stopped;
			_hasFillingChildren = false;

			if (Children != null)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					ITimeline child = Children[i];

					child.Stop();
					DisposeChildRegistrations(child);
				}
			}
			_runningChildren = 0;
		}

		public void Resume()
		{
			if (_trace.IsEnabled)
			{
				_trace.WriteEventActivity(
					eventId: TraceProvider.StoryBoard_Resume,
					opCode: EventOpcode.Send,
					activity: _traceActivity,
					payload: new object[] { Target?.GetType().ToString(), PropertyInfo?.Path }
				);
			}

			if (_hasScheduledCompletion)
			{
				return;
			}

#if __SKIA__
			// MUX: CStoryboard::Resume — set resuming flag for next ComputeState tick.
			_isPausedForTimeManager = false;
			_isResuming = true;
			Uno.UI.Xaml.Core.CoreServices.RequestAdditionalFrame();
#endif

			if (Children != null && Children.Count > 0)
			{
				State = TimelineState.Active;

				for (int i = 0; i < Children.Count; i++)
				{
					ITimeline child = Children[i];

					child.Resume();
				}
			}
		}

		public void Pause()
		{
			if (_trace.IsEnabled)
			{
				_trace.WriteEventActivity(
					eventId: TraceProvider.StoryBoard_Pause,
					opCode: EventOpcode.Send,
					activity: _traceActivity,
					payload: new object[] { Target?.GetType().ToString(), PropertyInfo?.Path }
				);
			}

			if (_hasScheduledCompletion)
			{
				return;
			}

#if __SKIA__
			// MUX: CStoryboard::Pause — set paused flag for ComputeState.
			// m_lastParentTime is already snapped and will serve as the pause start time.
			_isPausedForTimeManager = true;
#endif

			State = TimelineState.Paused;

			if (Children != null)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					ITimeline child = Children[i];

					child.Pause();
				}
			}
		}

		public void Seek(TimeSpan offset)
		{
#if __SKIA__
			if (_isRegisteredWithTimeManager)
			{
				// MUX: CStoryboard::SeekInternal — queue asynchronous seek (takes effect on next tick).
				_pendingSeekTime = offset.TotalSeconds;
				_isSeeking = true;
				Uno.UI.Xaml.Core.CoreServices.RequestAdditionalFrame();
				return;
			}
#endif
			if (Children != null)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					ITimeline child = Children[i];

					child.Seek(offset);
				}
			}
		}

		public void SeekAlignedToLastTick(TimeSpan offset)
		{
#if __SKIA__
			if (_isRegisteredWithTimeManager)
			{
				// MUX: CStoryboard::SeekAlignedToLastTick (storyboard.cpp lines 625-697)
				// Synchronously applies animation values at the seeked time.
				// Uses m_lastParentTime (snapped) instead of current global time.

				// Set the pending seek.
				_pendingSeekTime = offset.TotalSeconds;
				_isSeeking = true;

				// Synchronously compute state at the last known parent time.
				// This immediately updates animation values so they're readable
				// right after this call returns (required by VSM SkipToFill pattern).
				var syncParams = new ComputeStateParams()
				{
					HasTime = true,
					Time = _lastParentTime != double.MaxValue ? _lastParentTime : 0.0,
					IsPaused = _isPausedForTimeManager,
				};
				ComputeState(syncParams);

				// Queue another seek for the next real tick to correct the time delta
				// with the actual global clock.
				// MUX: storyboard.cpp line 691
				_pendingSeekTime = offset.TotalSeconds;
				_isSeeking = true;

				return;
			}
#endif
			if (Children != null)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					ITimeline child = Children[i];

					child.SeekAlignedToLastTick(offset);
				}
			}
		}
		public void SkipToFill()
		{
#if __SKIA__
			if (_isRegisteredWithTimeManager)
			{
				// MUX: SkipToFill uses SeekAlignedToLastTick to seek to the end.
				// This synchronously applies the final animation values.
				var duration = GetNaturalDurationFromChildren();
				SeekAlignedToLastTick(duration);
				return;
			}
#endif
			if (Children != null)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					ITimeline child = Children[i];

					child.SkipToFill();
				}
			}
		}

		internal void Deactivate()
		{
#if __SKIA__
			_isStopped = true;
			_isPausedForTimeManager = false;
			_isResuming = false;
			_isBeginning = false;
			_isSeeking = false;

			if (_isRegisteredWithTimeManager)
			{
				TimeManager.Instance.RemoveTimeline(this);
			}
#endif

			State = TimelineState.Stopped;
			_hasFillingChildren = false;

			if (Children != null)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					ITimeline child = Children[i];

					child.Deactivate();
					DisposeChildRegistrations(child);
				}

				_runningChildren = 0;
			}
		}

		internal void TurnOverAnimationsTo(Storyboard storyboard)
		{
			var affectedProperties = storyboard.Children.TargetedProperties;

			for (int i = 0; i < Children.Count; i++)
			{
				var child = Children[i];

				var id = child.GetTimelineTargetFullName();

				if (affectedProperties.Contains(id))
				{
					((ITimeline)child).Deactivate();
				}
				else
				{
					((ITimeline)child).Stop();
				}
			}

#if __SKIA__
			// Remove from TimeManager — this storyboard is being replaced by another.
			_isStopped = true;
			if (_isRegisteredWithTimeManager)
			{
				TimeManager.Instance.RemoveTimeline(this);
			}
#endif

			State = TimelineState.Stopped;
		}

		public ClockState GetCurrentState()
		{
			switch (State)
			{
				case TimelineState.Filling:
					return ClockState.Filling;
				case TimelineState.Stopped:
					return ClockState.Stopped;
				default:
				case TimelineState.Active:
				case TimelineState.Paused:
					return ClockState.Active;
			}
		}

		public TimeSpan GetCurrentTime()
		{
#if __SKIA__
			if (_isRegisteredWithTimeManager)
			{
				return TimeSpan.FromSeconds(_computedCurrentTime);
			}
#endif
			throw new NotImplementedException();
		}

		void ITimelineListener.ChildFailed(Timeline child)
		{
			DisposeChildRegistrations(child);

			Interlocked.Decrement(ref _runningChildren);

			// OnFailed is not called here because it relates to an individual
			// child, where completed relates to all children being completed.
		}

		void ITimelineListener.ChildCompleted(Timeline child)
		{
			DisposeChildRegistrations(child);

			Interlocked.Decrement(ref _runningChildren);
			_hasFillingChildren |= (child.FillBehavior != FillBehavior.Stop);

			if (_runningChildren == 0)
			{
				if (NeedsRepeat(_activeDuration, _replayCount))
				{
					Replay(); // replay the animation
					return;
				}
				if (State == TimelineState.Active)
				{
					State = _hasFillingChildren ? TimelineState.Filling : TimelineState.Stopped;
				}

#if __SKIA__
				// Prevent the TimeManager path from firing Completed again for the same run.
				_completedEventFiredByTimeManager = true;
#endif
				OnCompleted();
			}
		}

		private protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (Children != null)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					Children[i].Dispose();
				}
			}
		}

		IEnumerable<DependencyObject> IAdditionalChildrenProvider.GetAdditionalChildObjects() => Children;

		private protected override void OnThemeChanged()
		{
			if (State == TimelineState.Filling)
			{
				// If we're filling, reapply the fill to reapply values that may have changed with the app theme
				SkipToFill();
			}
		}
	}
}
