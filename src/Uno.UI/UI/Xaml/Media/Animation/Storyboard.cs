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
using Windows.Graphics.Printing.PrintSupport;

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
		private DispatcherTimer _durationTimer;
		private bool _isDurationExpired;

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

			Play();
		}

		private void Play()
		{
			_runningChildren = Children?.Count ?? 0;
			_isDurationExpired = false;

			// Start storyboard duration timer if explicit Duration is set
			StartDurationTimer();

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
			else if (_durationTimer == null)
			{
				// No children AND no duration timer → complete immediately (existing behavior)
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
			// else: empty storyboard with duration timer → wait for timer
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

			StopDurationTimer();
			_isDurationExpired = false;

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

			// Restart duration timer with remaining time
			if (_durationTimer != null && Duration.HasTimeSpan)
			{
				var remaining = Duration.TimeSpan - _activeDuration.Elapsed;
				if (remaining > TimeSpan.Zero)
				{
					_durationTimer.Interval = remaining;
					_durationTimer.Start();
				}
				else
				{
					// Duration already elapsed during pause — expire now
					OnDurationTimerTick(null, null);
					return;
				}
			}

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

			_durationTimer?.Stop();

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
			StopDurationTimer();
			_isDurationExpired = false;

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
				// If storyboard has explicit Duration and it hasn't elapsed yet, wait for the timer
				if (!_isDurationExpired && (Duration.Type == DurationType.TimeSpan || Duration.Type == DurationType.Forever))
				{
					return;
				}

				HandleCompletion();
			}
		}

		private void HandleCompletion()
		{
			if (NeedsRepeat(_activeDuration, _replayCount))
			{
				Replay();
				return;
			}

			if (State == TimelineState.Active)
			{
				State = _hasFillingChildren ? TimelineState.Filling : TimelineState.Stopped;
			}

			OnCompleted();
		}

		private void StartDurationTimer()
		{
			StopDurationTimer();

			if (Duration.Type == DurationType.TimeSpan && Duration.TimeSpan > TimeSpan.Zero)
			{
				_durationTimer = new DispatcherTimer { Interval = Duration.TimeSpan };
				_durationTimer.Tick += OnDurationTimerTick;
				_durationTimer.Start();
			}
		}

		private void StopDurationTimer()
		{
			if (_durationTimer != null)
			{
				_durationTimer.Stop();
				_durationTimer.Tick -= OnDurationTimerTick;
				_durationTimer = null;
			}
		}

		private void OnDurationTimerTick(object sender, object e)
		{
			StopDurationTimer();
			_isDurationExpired = true;

			// Deactivate any still-running children (holds their current animated value)
			if (Children != null && _runningChildren > 0)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					var child = Children[i];
					if (child.State == TimelineState.Active || child.State == TimelineState.Paused)
					{
						((ITimeline)child).Deactivate();
						DisposeChildRegistrations(child);
						_hasFillingChildren = true; // deactivated children hold values
					}
				}
				_runningChildren = 0;
			}

			HandleCompletion();
		}

		private protected override void Dispose(bool disposing)
		{
			StopDurationTimer();
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
