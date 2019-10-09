using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;
using Uno.Logging;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class Timeline : DependencyObject, ITimeline
	{
		private WeakReference<DependencyObject> _targetElement;
		private BindingPath _propertyInfo;

		public Timeline()
		{
			IsAutoPropertyInheritanceEnabled = false;

			InitializeBinder();

			State = TimelineState.Stopped;
		}

		protected enum TimelineState
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
				this.GetParent()?.GetDependencyObjectId().ToString(),
				Target?.GetType().ToString(),
				PropertyInfo?.Path
			};
		}

		/// <summary>
		/// An internally-used property which is essentially equivalent to <see cref="Storyboard.GetCurrentState"/>, except that it 
		/// distinguishes <see cref="TimelineState.Active"/> from <see cref="TimelineState.Paused"/>.
		/// </summary>
		protected TimelineState State { get; set; }

		public Nullable<TimeSpan> BeginTime
		{
			get => (Nullable<TimeSpan>) GetValue(BeginTimeProperty);
			set => SetValue(BeginTimeProperty, value);
		}

		public static readonly DependencyProperty BeginTimeProperty =
			DependencyProperty.Register("BeginTime", typeof(Nullable<TimeSpan>), typeof(Timeline), new PropertyMetadata(TimeSpan.Zero));

		public Duration Duration
		{
			get => (Duration) GetValue(DurationProperty);
			set => SetValue(DurationProperty, value);
		}

		public static readonly DependencyProperty DurationProperty =
			DependencyProperty.Register("Duration", typeof(Duration), typeof(Timeline), new PropertyMetadata(new Duration()));

		public FillBehavior FillBehavior
		{
			get => (FillBehavior) GetValue(FillBehaviorProperty);
			set => SetValue(FillBehaviorProperty, value);
		}

		public static readonly DependencyProperty FillBehaviorProperty =
			DependencyProperty.Register("FillBehavior", typeof(FillBehavior), typeof(Timeline), new PropertyMetadata(FillBehavior.HoldEnd));

		public RepeatBehavior RepeatBehavior
		{
			get => (RepeatBehavior) GetValue(RepeatBehaviorProperty);
			set => SetValue(RepeatBehaviorProperty, value);
		}

		public static readonly DependencyProperty RepeatBehaviorProperty =
			DependencyProperty.Register("RepeatBehavior", typeof(RepeatBehavior), typeof(Timeline), new PropertyMetadata(new RepeatBehavior()));


		public event EventHandler<object> Completed;

		protected void OnCompleted()
		{
			this.Completed?.Invoke(this, null);
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

		/// <summary>
		/// Path to the target property being animated.
		/// </summary>
		internal BindingPath PropertyInfo
		{
			get
			{
				if (_propertyInfo == null)
				{
					var target = Target ?? GetTargetFromName();

					_propertyInfo = new BindingPath(
						path: Storyboard.GetTargetProperty(this),
						fallbackValue: null,
						precedence: DependencyPropertyValuePrecedences.Animations,
						allowPrivateMembers: false
					);

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
				return fe.FindName(Storyboard.GetTargetName(this));
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("Clearing [{0} / {1}]", Storyboard.GetTargetName(this), Storyboard.GetTargetProperty(this));
			}

			PropertyInfo.ClearValue();
		}

		void ITimeline.Begin()
		{
			// Timeline should not be used directly.  Please use derived class.
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Begin()");
		}

		void ITimeline.Stop()
		{
			// Timeline should not be used directly.  Please use derived class.
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Stop()");
		}

		void ITimeline.Resume()
		{
			// Timeline should not be used directly.  Please use derived class.
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Resume()");
		}

		void ITimeline.Pause()
		{
			// Timeline should not be used directly.  Please use derived class.
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Pause()");
		}

		void ITimeline.Seek(TimeSpan offset)
		{
			// Timeline should not be used directly.  Please use derived class.
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Seek(TimeSpan offset)");
		}

		void ITimeline.SeekAlignedToLastTick(TimeSpan offset)
		{
			// Timeline should not be used directly.  Please use derived class.
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void SeekAlignedToLastTick(TimeSpan offset)");
		}

		void ITimeline.SkipToFill()
		{
			// Timeline should not be used directly.  Please use derived class.
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void SkipToFill()");
		}

		void ITimeline.Deactivate()
		{
			// Timeline should not be used directly.  Please use derived class.
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(GetType().FullName, "void Deactivate()");
		}


		/// <summary>
		/// Checks if the Timeline will repeat.
		/// </summary>
		/// <returns><c>true</c>, Repeat needed, <c>false</c> otherwise.</returns>
		protected bool NeedsRepeat(DateTimeOffset lastBeginTime, int replayCount)
		{
			var totalTime = DateTimeOffset.Now - lastBeginTime;

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
					//TODO Projection, Clip, Canvas.Left or Canvas.Top

					if (boundProperty.PropertyName.EndsWith("Opacity")
						|| (boundProperty.DataContext is SolidColorBrush && boundProperty.PropertyName == "Color")
						|| (boundProperty.DataContext is Transform)
					)
					{
						//is not dependent if the target is opacity, the color property of a brush, or a Transform property
						return false;
					}
				}
			}
			return true;
		}

		protected virtual void Dispose(bool disposing)
		{
			Target = null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Timeline()
		{
			Dispose(true);
		}
	}
}
