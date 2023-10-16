using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Markup;
using Uno.UI;
using Windows.Foundation.Collections;

using static Microsoft.UI.Xaml.Media.Animation.Timeline.TimelineState;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Microsoft.UI.Xaml
{
	[ContentProperty(Name = "States")]
	public sealed partial class VisualStateGroup : DependencyObject
	{
		/// <summary>
		/// The xaml scope in force at the time the VisualStateGroup was created.
		/// </summary>
		private readonly XamlScope _xamlScope;
		private readonly SerialDisposable _parentLoadedDisposable = new();

		private (VisualState state, VisualTransition transition) _current;

		public event VisualStateChangedEventHandler CurrentStateChanging;

		public event VisualStateChangedEventHandler CurrentStateChanged;

		public VisualStateGroup()
		{
			_xamlScope = ResourceResolver.CurrentScope;

			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();

			this.RegisterParentChangedCallbackStrong(this, OnParentChanged);
		}

		public VisualState CurrentState => _current.state;

		public string Name { get; set; }

		#region States Dependency Property

		public IList<VisualState> States
		{
			get
			{
				var value = (IList<VisualState>)this.GetValue(StatesProperty);

				if (value == null)
				{
					// WARNING: if you remove this initialization, make sure to review 
					// the GetChildrenProviders method, which assumes that the property
					// may return null for performance reasons.
					var collection = new DependencyObjectCollection<VisualState>(parent: this, isAutoPropertyInheritanceEnabled: false);
					value = collection;

					this.SetValue(StatesProperty, value);
				}

				return value;
			}
			internal set { this.SetValue(StatesProperty, value); }
		}

		public static DependencyProperty StatesProperty { get; } =
			DependencyProperty.Register(
				"States",
				typeof(IList<VisualState>),
				typeof(VisualStateGroup),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext,
					propertyChangedCallback: (s, e) => ((VisualStateGroup)s)?.OnStatesChanged(e)
				)
			);

		#endregion

		#region Transitions DependencyProperty

		public IList<VisualTransition> Transitions
		{
			get
			{
				var value = (IList<VisualTransition>)this.GetValue(TransitionsProperty);

				if (value == null)
				{
					value = new DependencyObjectCollection<VisualTransition>(parent: this, isAutoPropertyInheritanceEnabled: false);
					this.SetValue(TransitionsProperty, value);
				}

				return value;
			}
			internal set { this.SetValue(TransitionsProperty, value); }
		}

		public static DependencyProperty TransitionsProperty { get; } =
			DependencyProperty.Register(
				"Transitions",
				typeof(IList<VisualTransition>),
				typeof(VisualStateGroup),
				new FrameworkPropertyMetadata(
					defaultValue: null)
			);

		#endregion

		//Adds Event Handlers when collections changed
		private void OnStatesChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is DependencyObjectCollection<VisualState> oldStates)
			{
				oldStates.VectorChanged -= VisualStateChanged;
			}

			if (e.NewValue is DependencyObjectCollection<VisualState> states)
			{
				states.VectorChanged += VisualStateChanged;
			}

			RefreshStateTriggers();
		}

		//Modifies the Owner FrameworkElement when Collection is modified
		private void VisualStateChanged(object sender, IVectorChangedEventArgs e)
		{
			RefreshStateTriggers();
		}

		private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			RefreshStateTriggers(force: true);

			_parentLoadedDisposable.Disposable = null;
			if (this.GetParent() is IFrameworkElement fe)
			{
				fe.Loaded += OnParentLoaded;
				fe.Unloaded += OnParentUnloaded;
				_parentLoadedDisposable.Disposable = Disposable.Create(() =>
				{
					fe.Loaded -= OnParentLoaded;
					fe.Unloaded -= OnParentUnloaded;
				});
			}
		}

		private void OnParentLoaded(object sender, object args)
		{
			// Notify all states that the parent has been loaded
			for (var stateIndex = 0; stateIndex < States.Count; stateIndex++)
			{
				var state = States[stateIndex];
				for (var triggerIndex = 0; triggerIndex < state.StateTriggers.Count; triggerIndex++)
				{
					var trigger = state.StateTriggers[triggerIndex];

					trigger.OnOwnerElementLoaded();
				}
			}
		}

		private void OnParentUnloaded(object sender, object args)
		{
			// Notify all states that the parent has been loaded
			for (var stateIndex = 0; stateIndex < States.Count; stateIndex++)
			{
				var state = States[stateIndex];
				for (var triggerIndex = 0; triggerIndex < state.StateTriggers.Count; triggerIndex++)
				{
					var trigger = state.StateTriggers[triggerIndex];

					trigger.OnOwnerElementUnloaded();
				}
			}
		}

		internal void RaiseCurrentStateChanging(VisualState oldState, VisualState newState)
		{
			if (this.CurrentStateChanging == null)
			{
				return;
			}

			this.CurrentStateChanging(this, new VisualStateChangedEventArgs() { Control = FindFirstAncestorControl(), NewState = newState, OldState = oldState });
		}

		internal void RaiseCurrentStateChanged(VisualState oldState, VisualState newState)
		{
			if (this.CurrentStateChanged == null)
			{
				return;
			}

			this.CurrentStateChanged(this, new VisualStateChangedEventArgs() { Control = FindFirstAncestorControl(), NewState = newState, OldState = oldState });
		}

		private Control FindFirstAncestorControl()
		{
			// The owner may not be an actual control, so we return the first control we find in the ancestors.
			// This matches the UWP behavior.
			return (this.GetParent() as FrameworkElement)?.FindFirstParent<Control>();
		}

		internal void GoToState(
			IFrameworkElement element,
			VisualState state,
			bool useTransitions,
			Action onStateChanged)
		{
			global::System.Diagnostics.Debug.Assert(state is null || States.Contains(state));

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("Go to state [{0}/{1}] on [{2}]", Name, state?.Name, element);
			}

			var currentValues = _current;
			var targetValues = (state, transition: FindTransition(currentValues.state?.Name, state?.Name));

			// As accessing to VisualState and VisualTransition properties (Storyboard ans Setters) may trigger the materialization of the VisualState,
			// we ensure that this materialization occurs only in the right resource scope.
			// Note: the "current" should have already been materialized.
			(Storyboard transition, Storyboard animation, SetterBaseCollection setters) current, target;
			try
			{
				ResourceResolver.PushNewScope(_xamlScope);

				current = (currentValues.transition?.Storyboard, currentValues.state?.Storyboard, currentValues.state?.Setters);
				target = (targetValues.transition?.Storyboard, targetValues.state?.Storyboard, targetValues.state?.Setters);
			}
			finally
			{
				ResourceResolver.PopScope();
			}

			// Stops running animations (transition or state's storyboard)
			// Note about animations (as of 2021-08-16 win 19043):
			//		Any "running animation", either from the current transition or the current state's storyboard,
			//		is being "paused" for properties that are going to be animated by the "next animation" (again transition or target state's storyboard),
			//		and rollbacked for properties that won't be animated anymore.
			var runningAnimation = (current.transition?.State ?? Stopped) is Stopped
				? current.animation
				: current.transition;
			var nextAnimation = target.transition ?? target.animation;
			if (runningAnimation != null)
			{
				if (nextAnimation is null)
				{
					runningAnimation.Stop();
				}
				else
				{
					runningAnimation.TurnOverAnimationsTo(nextAnimation);
				}
			}

			// Rollback setters that won't be re-set by the target state setters
			// Note about setters and transition (as of 2021-08-16 win 19043):
			//		* if current and target state have setters for the same property,
			//		  the value of the current is kept as is and updated only once at the end of the transition
			//		* if current has a setter which is not updated by the target state
			//		  the value is rollbacked before the transition
			//		* if the target has a setter for a property that was not affected by the current
			//		  the value is applied only at the end of the transition
			if (current.setters is { } currentSetters)
			{
				// This block is a manual enumeration to avoid the foreach pattern
				// See https://github.com/dotnet/runtime/issues/56309 for details
				var settersEnumerator = currentSetters.OfType<Setter>().GetEnumerator();
				while (settersEnumerator.MoveNext())
				{
					var setter = settersEnumerator.Current;

					if (element != null && (target.setters?.OfType<Setter>().Any(o => o.HasSameTarget(setter, DependencyPropertyValuePrecedences.Animations, element)) ?? false))
					{
						// We clear the value of the current setter only if there isn't any setter in the target state
						// which changes the same target property (for perf ... and UWP behavior support regarding transition animation).

						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"Ignoring reset of setter of '{setter.Target?.Path}' as it will be updated again by '{state.Name}'");
						}

						continue;
					}

					setter.ClearValue();
				}
			}

			_current = targetValues;

			// For backward compatibility, we may apply the setters before the end of the transition.
			if (FeatureConfiguration.VisualState.ApplySettersBeforeTransition)
			{
				ApplyTargetStateSetters();
			}

			// Finally effectively apply the target state!
			if (useTransitions && target.transition is { } transitionAnimation)
			{
				// Note: As of 2021-08-16 win 19043, if the transitionAnimation is Repeat=Forever, we actually never apply the state!

				transitionAnimation.Completed += OnTransitionCompleted;
				transitionAnimation.Begin();

				void OnTransitionCompleted(object s, object a)
				{
					transitionAnimation.Completed -= OnTransitionCompleted;

					if (target.animation is { } stateAnimation)
					{
						transitionAnimation.TurnOverAnimationsTo(stateAnimation);
					}

					ApplyTargetState();
				}
			}
			else
			{
				ApplyTargetState();
			}

			void ApplyTargetState()
			{
				// Apply target state setters (the right time to do it!) 
				if (!FeatureConfiguration.VisualState.ApplySettersBeforeTransition)
				{
					ApplyTargetStateSetters();
				}

				// Starts target state animation
				if (target.animation is { } stateAnimation)
				{
					stateAnimation.Begin();
				}

				onStateChanged();
			}

			void ApplyTargetStateSetters()
			{
				if (target.setters is null || element is null)
				{
					return;
				}

				try
				{
					// Setter.ApplyValue can resolve some theme resources.
					// We need to invoke them using the right resource context.
					ResourceResolver.PushNewScope(_xamlScope);

					// This block is a manual enumeration to avoid the foreach pattern
					// See https://github.com/dotnet/runtime/issues/56309 for details
					var settersEnumerator = target.setters.OfType<Setter>().GetEnumerator();

					while (settersEnumerator.MoveNext())
					{
						settersEnumerator.Current.ApplyValue(DependencyPropertyValuePrecedences.Animations, element);


					}
				}
				finally
				{
					ResourceResolver.PopScope();
				}

			}
		}

		private VisualTransition FindTransition(string oldStateName, string newStateName)
		{
			// Only one transition can be run when changing state.
			// The most specific transition wins (i.e. with matching From and To),
			// then we validate for transitions that have only From or To defined which match.

			var hasOld = !oldStateName.IsNullOrEmpty();
			var hasNew = !newStateName.IsNullOrEmpty();

			if (hasOld && hasNew && GetFirstMatch(oldStateName, newStateName) is { } perfectMatch)
			{
				return perfectMatch;
			}

			if (hasOld && GetFirstMatch(oldStateName, null) is { } fromMatch)
			{
				return fromMatch;
			}

			if (hasNew && GetFirstMatch(null, newStateName) is { } newMatch)
			{
				return newMatch;
			}

			return default;

			VisualTransition GetFirstMatch(string from, string to)
			{
				// Avoid using Transitions.FirstOrDefault as it incurs unnecessary Func<VisualTransition, bool> allocations.
				foreach (var transition in Transitions)
				{
					if (Match(transition, from, to))
					{
						return transition;
					}
				}

				return null;
			}

			bool Match(VisualTransition transition, string from, string to)
				=> string.Equals(transition.From, oldStateName) && string.Equals(transition.To, newStateName);
		}

		internal void RefreshStateTriggers(bool force = false)
		{
			var newState = GetActiveTrigger();
			var oldState = CurrentState;
			if (newState == oldState)
			{
				if (!force)
				{
					return;
				}
				else if (newState is null)
				{
					// The 'force' has no effect is both old and new states are 'null'
					// (setting the parent for the first time in control's init)
					// we however raise the state changed for backward compatibility.
					OnStateChanged();
					return;
				}
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[{this}].RefreshStateTriggers() activeState={newState}, oldState={oldState}");
			}

			void OnStateChanged()
			{
				RaiseCurrentStateChanged(oldState, newState);
			}

			var parent = this.GetParent() as IFrameworkElement;
			GoToState(parent, newState, false, OnStateChanged);
		}


		/// <remarks>
		/// This method is not using LINQ for performance considerations.
		/// </remarks>
		private VisualState GetActiveTrigger()
		{
			// As documented there:
			// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.visualstategroup#remarks
			// We're using a priority-based mechanism

			// When using StateTriggers to control visual states, the trigger engine uses the following precedence
			// rules to score triggers and determine which trigger, and the corresponding VisualState, will be active:
			//
			// 1. Custom trigger that derives from StateTriggerBase
			// 2. AdaptiveTrigger activated due to MinWindowWidth
			// 3. AdaptiveTrigger activated due to MinWindowHeight
			//
			// If there are multiple active triggers at a time that have a conflict in scoring(i.e.two active custom
			// triggers), then the first one declared in the markup file takes precedence.

			(VisualState State, double MinWidth, double MinHeight) adaptiveCandidate = (default, -1, -1);

			for (var stateIndex = 0; stateIndex < States.Count; stateIndex++)
			{
				var state = States[stateIndex];
				for (var triggerIndex = 0; triggerIndex < state.StateTriggers.Count; triggerIndex++)
				{
					var trigger = state.StateTriggers[triggerIndex];

					// the first active CustomTrigger is an automatic winner.
					if (trigger.CurrentPrecedence == StateTriggerPrecedence.CustomTrigger)
					{
						return state;
					}

					// between AdaptiveTriggers, they are ranked by MinWindowWidth (descending),
					// then by MinWindowHeight (descending), and finally in the declaration order.
					if (trigger.CurrentPrecedence == StateTriggerPrecedence.AdaptiveTrigger &&
						trigger is AdaptiveTrigger adaptiveTrigger)
					{
						var minWidth = adaptiveTrigger.MinWindowWidth;
						var minHeight = adaptiveTrigger.MinWindowHeight;

						// compare by MinWindowWidth first
						if (minWidth > adaptiveCandidate.MinWidth)
						{
							adaptiveCandidate = (state, minWidth, minHeight);
							break;
						}
						// compare by MinWindowHeight only if MinWindowWidth are equal
						if (minWidth == adaptiveCandidate.MinWidth &&
							minHeight > adaptiveCandidate.MinHeight)
						{
							adaptiveCandidate = (state, minWidth, minHeight);
							break;
						}
						// if MinWindowWidth and MinWindowHeight are both equal,
						// then previous candidate wins for being declared first.
					}
				}
			}

			return adaptiveCandidate.State;
		}

		public override string ToString()
			=> Name ?? $"<unnamed group {GetHashCode()}>";
	}
}
