using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class Timeline : DependencyObject, ITimeline, IThemeChangeAware
	{
		private WeakReference<DependencyObject> _targetElement;
		private BindingPath _propertyInfo;
		private List<ITimelineListener> _timelineListeners = new();
		private List<EventHandler<object>> _completedHandlers;

		public event EventHandler<object> Completed
		{
			add => (_completedHandlers ??= new()).Add(value);
			remove => _completedHandlers?.Remove(value);
		}

		public bool AutoReverse
		{
			get => (bool)GetValue(AutoReverseProperty);
			set => SetValue(AutoReverseProperty, value);
		}

		public static DependencyProperty AutoReverseProperty { get; } =
			DependencyProperty.Register("AutoReverse", typeof(bool), typeof(Timeline), new FrameworkPropertyMetadata(false));

		public Timeline()
		{
			IsAutoPropertyInheritanceEnabled = false;

			InitializeBinder();

			State = TimelineState.Stopped;
		}

		internal enum TimelineState
		{
			Active,
			Filling,
			Stopped,
			Paused,
		};

		protected string[] GetTraceProperties()
		{
			return new[] {
				this.GetParent()?.GetType().Name,
				this.GetParent()?.GetDependencyObjectId().ToString(CultureInfo.InvariantCulture),
				Target?.GetType().ToString(),
				PropertyInfo?.Path
			};
		}

		/// <summary>
		/// An internally-used property which is essentially equivalent to <see cref="Storyboard.GetCurrentState"/>, except that it 
		/// distinguishes <see cref="TimelineState.Active"/> from <see cref="TimelineState.Paused"/>.
		/// </summary>
		internal TimelineState State { get; private protected set; }

		public TimeSpan? BeginTime
		{
			get => (TimeSpan?)GetValue(BeginTimeProperty);
			set => SetValue(BeginTimeProperty, value);
		}

		public static DependencyProperty BeginTimeProperty { get; } =
			DependencyProperty.Register("BeginTime", typeof(TimeSpan?), typeof(Timeline), new FrameworkPropertyMetadata(TimeSpan.Zero));

		public Duration Duration
		{
			get => (Duration)GetValue(DurationProperty);
			set => SetValue(DurationProperty, value);
		}

		public static DependencyProperty DurationProperty { get; } =
			DependencyProperty.Register("Duration", typeof(Duration), typeof(Timeline), new FrameworkPropertyMetadata(Duration.Automatic));

		public FillBehavior FillBehavior
		{
			get => (FillBehavior)GetValue(FillBehaviorProperty);
			set => SetValue(FillBehaviorProperty, value);
		}

		public static DependencyProperty FillBehaviorProperty { get; } =
			DependencyProperty.Register("FillBehavior", typeof(FillBehavior), typeof(Timeline), new FrameworkPropertyMetadata(FillBehavior.HoldEnd));

		public RepeatBehavior RepeatBehavior
		{
			get => (RepeatBehavior)GetValue(RepeatBehaviorProperty);
			set => SetValue(RepeatBehaviorProperty, value);
		}

		public static DependencyProperty RepeatBehaviorProperty { get; } =
			DependencyProperty.Register("RepeatBehavior", typeof(RepeatBehavior), typeof(Timeline), new FrameworkPropertyMetadata(new RepeatBehavior()));


		void ITimeline.RegisterListener(ITimelineListener listener)
			=> _timelineListeners.Add(listener);

		void ITimeline.UnregisterListener(ITimelineListener listener)
			=> _timelineListeners.Remove(listener);

		protected void OnCompleted()
		{
			if (_completedHandlers != null)
			{
				for (int i = 0; i < _completedHandlers.Count; i++)
				{
					_completedHandlers[i].Invoke(this, null);
				}
			}

			for (var i = 0; i < _timelineListeners.Count; i++)
			{
				_timelineListeners[i].ChildCompleted(this);
			}
		}

		protected void OnFailed()
		{
			for (var i = 0; i < _timelineListeners.Count; i++)
			{
				_timelineListeners[i].ChildFailed(this);
			}
		}

		/// <summary>
		/// Compute duration of the Timeline. Sometimes it's define by components.
		/// </summary>
		internal virtual TimeSpan GetCalculatedDuration()
		{
			return Duration.Type switch
			{
				DurationType.Forever => TimeSpan.MaxValue,
				DurationType.TimeSpan when Duration.TimeSpan > TimeSpan.Zero => Duration.TimeSpan,
				DurationType.Automatic => TimeSpan.Zero, // this is overriden in xxxUsingKeyFrames implementations
				_ => TimeSpan.Zero
			};
		}

		/// <summary>
		/// The target on which the animated property exists, if set directly by <see cref="Storyboard.SetTarget(Timeline, DependencyObject)"/>. 
		/// </summary>
		internal DependencyObject Target
		{
			get => _targetElement?.GetTarget();
			set => _targetElement = new WeakReference<DependencyObject>(value);
		}

		internal void SetElementNameTarget(ElementNameSubject subject)
		{
			ElementNameSubject.ElementInstanceChangedHandler handler = null;

			handler = (s, e) =>
			{
				// This method is present to handle the change of the target
				// either because the target was not available when the subject
				// was provided, or because the target is an ElementStub. For 
				// the latter, the element stub will be available right away then
				// later updated with the actual control.

				Target = e as DependencyObject;

				if (Target != null)
				{
					if (_propertyInfo != null)
					{
						_propertyInfo.DataContext = Target;
					}

					if (!(e is ElementStub))
					{
						// If the new instance is not an ElementStub, then the value 
						// will never change again, then we can remove the subscription.
						subject.ElementInstanceChanged -= handler;
					}
				}
			};

			Target = subject.ElementInstance as DependencyObject;

			subject.ElementInstanceChanged += handler;
		}

		private protected virtual void InitTarget() { }

		/// <summary>
		/// Path to the target property being animated.
		/// </summary>
		internal BindingPath PropertyInfo
		{
			get
			{
				// Don't use the cached _propertyInfo if TargetProperty or Target has the changed.
				var targetPropertyPath = Storyboard.GetTargetProperty(this);
				InitTarget();
				var target = Target ?? GetTargetFromName();

				if (_propertyInfo == null || _propertyInfo.Path != targetPropertyPath)
				{
					if (_propertyInfo != null)
					{
						_propertyInfo.DataContext = null;
						_propertyInfo.Dispose();
					}

					_propertyInfo = new BindingPath(
						path: targetPropertyPath,
						fallbackValue: null,
						forAnimations: true,
						allowPrivateMembers: false
					)
					{
						DataContext = target,
					};
					return _propertyInfo;
				}

				if (_propertyInfo.DataContext != target)
				{
					_propertyInfo.DataContext = target;
				}

				return _propertyInfo;
			}
		}

		/// <summary>
		/// Returns Target Dependency Object using the Owner Framework Element
		/// </summary>
		protected DependencyObject GetTargetFromName()
		{
			if (GetVisualParent() is FrameworkElement fe)
			{
				return fe.FindName(Storyboard.GetTargetName(this)) as DependencyObject;
			}
			else
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Failed to find target {Storyboard.GetTargetName(this)} on {this.GetParent()?.GetType()}");
				}
			}

			return null;
		}

		private FrameworkElement GetVisualParent()
		{
			object current = this;
			while ((current = current.GetParent()) != null)
			{
				if (current is FrameworkElement fe)
				{
					return fe;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the value of the Target's Property
		/// </summary>
		protected object GetValue()
		{
			return PropertyInfo.Value;
		}

		/// <summary>
		/// Gets the value of the Target's Property under the animated property of the dependency property
		/// value precedence system
		/// </summary>
		protected object GetNonAnimatedValue()
		{
			return PropertyInfo.GetSubstituteValue();
		}

		/// <summary>
		/// Sets The value of the Target's Property
		/// </summary>
		protected void SetValue(object value)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat(
					"Setting [{0}] to [{1} / {2}] current {3:X8}/{4}={5}",
					value,
					Storyboard.GetTargetName(this), Storyboard.GetTargetProperty(this),
					PropertyInfo?.DataContext?.GetHashCode(),
					PropertyInfo?.DataContext?.GetType(),
					PropertyInfo?.Value
				);
			}

			PropertyInfo.Value = value;
		}

		/// <summary>
		/// Clears the animated value of the dependency property value precedence system
		/// </summary>
		protected void ClearValue()
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("Clearing [{0} / {1}]", Storyboard.GetTargetName(this), Storyboard.GetTargetProperty(this));
			}

			PropertyInfo.ClearValue();
		}

		void ITimeline.Begin()
		{
			// Timeline should not be used directly.  Please use derived class.
			ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Begin()");
		}

		void ITimeline.Stop()
		{
			// Timeline should not be used directly.  Please use derived class.
			ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Stop()");
		}

		void ITimeline.Resume()
		{
			// Timeline should not be used directly.  Please use derived class.
			ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Resume()");
		}

		void ITimeline.Pause()
		{
			// Timeline should not be used directly.  Please use derived class.
			ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Pause()");
		}

		void ITimeline.Seek(TimeSpan offset)
		{
			// Timeline should not be used directly.  Please use derived class.
			ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Seek(TimeSpan offset)");
		}

		void ITimeline.SeekAlignedToLastTick(TimeSpan offset)
		{
			// Timeline should not be used directly.  Please use derived class.
			ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void SeekAlignedToLastTick(TimeSpan offset)");
		}

		void ITimeline.SkipToFill()
		{
			// Timeline should not be used directly.  Please use derived class.
			ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void SkipToFill()");
		}

		void ITimeline.Deactivate()
		{
			// Timeline should not be used directly.  Please use derived class.
			ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Deactivate()");
		}

		/// <summary>
		/// Checks if the Timeline will repeat.
		/// </summary>
		/// <returns><c>true</c>, Repeat needed, <c>false</c> otherwise.</returns>
		private protected bool NeedsRepeat(Stopwatch duration, int replayCount)
		{
			var totalTime = duration.Elapsed;

			//3 types of repeat behavors,             
			return ((RepeatBehavior.Type == RepeatBehaviorType.Forever) // Forever: Will always repeat the Timeline
				|| (RepeatBehavior.HasCount && RepeatBehavior.Count > replayCount) // Count: Will repeat the Timeline x times
				|| (RepeatBehavior.HasDuration && RepeatBehavior.Duration - totalTime > TimeSpan.Zero)) // Duration: Will repeat the Timeline for a given duration
				&& State != TimelineState.Stopped;
		}
		/// <summary>
		/// Checks if Target Property is Dependent, If the target property is dependent it should not run the animation unless EnableDependentAnimation is explicitly set.
		///  https://msdn.microsoft.com/en-uS/office/office365/jj819807.aspx#dependent
		/// </summary>
		/// <returns><c>true</c>, property is dependent, <c>false</c> otherwise.</returns>
		internal bool IsTargetPropertyDependant()
		{
			if (PropertyInfo != null)
			{
				var boundProperty = PropertyInfo.GetPathItems().LastOrDefault();

				if (boundProperty != null)
				{
					//https://msdn.microsoft.com/en-uS/office/office365/jj819807.aspx#dependent
					//TODO Projection, Clip

					if (boundProperty.PropertyName.EndsWith("Opacity", StringComparison.Ordinal)
						|| (boundProperty.DataContext is SolidColorBrush && boundProperty.PropertyName.EndsWith("Color", StringComparison.Ordinal))
						|| boundProperty.PropertyName.Equals("Microsoft.UI.Xaml.Controls:Canvas.Top", StringComparison.Ordinal)
						|| boundProperty.PropertyName.Equals("Microsoft.UI.Xaml.Controls:Canvas.Left", StringComparison.Ordinal)
						|| (boundProperty.DataContext is Transform transform)
					)
					{
						//is not dependent if the target is opacity, the color property of a brush, or a Transform property targeting a view as RenderTransform
						// NOTE that the Transform check isn't necessarily a RenderTransform and is not accurate.
						// It's there to handle some cases, e.g, a UIElement having RectangleGeometry Clip that has a Transform
						// Ideally, we want to be specifically checking if the animation is targeting Clip, but no good way to do it so far.
						// The current approach may consider some dependent animations as independent in niche scenario, but that's not an issue for now.
						return false;
					}
				}
			}
			return true;
		}

		private protected virtual void Dispose(bool disposing)
		{
			Target = null;
		}

		internal void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void IThemeChangeAware.OnThemeChanged() => OnThemeChanged();

		private protected virtual void OnThemeChanged() { }

		~Timeline()
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Dispose(false));
		}
	}
}
