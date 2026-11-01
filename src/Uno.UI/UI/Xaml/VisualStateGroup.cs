using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Markup;
using Uno.UI;
using Windows.Foundation.Collections;
using System.Buffers;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	[ContentProperty(Name = "States")]
	public sealed partial class VisualStateGroup : DependencyObject, IXNameProvider
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

		public string Name { get; internal set; }

		void IXNameProvider.SetXName(string name) => Name = name;

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
			OnOwnerElementChanged();
			RefreshStateTriggers();
		}

		private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			RefreshStateTriggers(force: true);

			_parentLoadedDisposable.Disposable = null;
			if (this.GetParent() is IFrameworkElement fe)
			{
				OnOwnerElementChanged();
				// On Skia, we match WinUI and initialize the visual triggers of a Control in
				// EmterImpl and/or InvokeApplyTemplate. Somehow the state triggers are also
				// applied when the VisualStates are being set on a non-Control. We fallback
				// to listening to Loaded to evaluate the triggers in that case.
				if (fe is not Control)
				{
					fe.Loaded += OnOwnerElementLoaded;
				}
				fe.Unloaded += OnOwnerElementUnloaded;
				_parentLoadedDisposable.Disposable = Disposable.Create(() =>
				{
					if (fe is not Control)
					{
						fe.Loaded -= OnOwnerElementLoaded;
					}
					fe.Unloaded -= OnOwnerElementUnloaded;
				});
			}
		}

		private void OnOwnerElementChanged()
		{
			ExecuteOnTriggers(t => t.OnOwnerElementChanged());
		}

		internal void OnOwnerElementLoaded(object sender, RoutedEventArgs args)
		{
			ExecuteOnTriggers(t => t.OnOwnerElementLoaded());
		}

		private void OnOwnerElementUnloaded(object sender, RoutedEventArgs args) =>
			ExecuteOnTriggers(t => t.OnOwnerElementUnloaded());

		private void ExecuteOnTriggers(Action<StateTriggerBase> action)
		{
			for (var stateIndex = 0; stateIndex < States.Count; stateIndex++)
			{
				var state = States[stateIndex];
				if (state.StateTriggersOrDefault is not { } triggers)
				{
					continue;
				}

				for (var triggerIndex = 0; triggerIndex < triggers.Count; triggerIndex++)
				{
					var trigger = triggers[triggerIndex];

					action(trigger);
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

			// As accessing to VisualState and VisualTransition properties (Storyboard and Setters) may trigger the materialization of the VisualState,
			// we ensure that this materialization occurs only in the right resource scope.
			// Note: the "current" should have already been materialized.
			//
			// VisualState Storyboards/Setters materialized here resolve {ThemeResource} against the
			// element's own theme via ResolveOwnerTheme.
			(Storyboard transition, Storyboard animation, SetterBaseCollection setters) current, target;
			try
			{
				ResourceResolver.PushNewScope(_xamlScope);

				// Scope the owner's effective theme so a lazy-materialized VisualState's keyframes/setters
				// resolve the part's own theme, not the stale ambient (ResolveOwnerTheme; WinUI themes the
				// storyboard at Enter). Skipped during a theme walk — the core slot already carries the new
				// theme while the owner's per-object theme is still stale, so a re-entrant GoToState (a
				// StateTrigger flipping mid-walk) keeps the walk theme.
				var ownerIsProcessingThemeWalk = (element as DependencyObject) is DependencyObject provider
					&& provider.IsProcessingThemeWalk;
				using var themeScope = ownerIsProcessingThemeWalk
					? default
					: Uno.UI.Xaml.Core.CoreServices.Instance
						.ScopeRequestedThemeForSubTree(ThemeResolution.ResolveOwnerTheme(element as DependencyObject));

				current = (currentValues.transition?.Storyboard, currentValues.state?.Storyboard, currentValues.state?.Setters);
				target = (targetValues.transition?.Storyboard, targetValues.state?.Storyboard, targetValues.state?.Setters);
			}
			finally
			{
				ResourceResolver.PopScope();
			}

			// The storyboards were materialized after the element entered the tree, so they missed the theme WinUI gives
			// them at Enter (CDependencyObject::EnterImpl theme block). Without it, a later resolution of their {ThemeResource}
			// references (e.g. on Loading) falls back to the application theme, and the animations apply that value.
			if (element is DependencyObject { IsProcessingThemeWalk: false } themedOwner && themedOwner.GetTheme() is var ownerTheme && ownerTheme != Theme.None)
			{
				target.transition?.EstablishTheme(ownerTheme);
				target.animation?.EstablishTheme(ownerTheme);
			}

			// As in WinUI (VisualStateManagerActuator), the storyboards of the target state start before the running ones
			// are stopped: an animation of the new state takes over the properties it shares with a running one (handoff),
			// and stopping the old storyboard restores only the properties nothing animates anymore.
			_current = targetValues;

			// Finally effectively apply the target state!
			var transitionAnimation = useTransitions ? target.transition : null;

			// A zero-duration transition doesn't delay the target state: both start right away (VisualTransition::IsZeroDuration).
			var zeroDurationTransition = transitionAnimation is null || IsZeroDuration(transitionAnimation);

			if (!zeroDurationTransition)
			{
				ClearPropertiesNotSetInNextState(
					element,
					current.setters,
					null,
					transitionAnimation);

				// Note: As of 2021-08-16 win 19043, if the transitionAnimation is Repeat=Forever, we actually never apply the state!

				transitionAnimation!.Completed += OnTransitionCompleted;

				void OnTransitionCompleted(object s, object a)
				{
					transitionAnimation.Completed -= OnTransitionCompleted;

					ApplyTargetState();

					// The new state's storyboard starts before the transition stops, for smooth handoff.
					StopStoryboards(transitionAnimation, null, target.animation, null);
				}
			}
			else
			{
				ClearPropertiesNotSetInNextState(
					element,
					current.setters,
					target.setters,
					target.animation);
			}

			// Finally we're far enough along to attempt to actually start all these storyboards. These
			// actions can potentially fail if they can't resolve their TargetName properties.
			if (transitionAnimation is not null)
			{
				AttemptStartStoryboard(transitionAnimation);
			}

			if (zeroDurationTransition)
			{
				ApplyTargetState();
			}

			StopStoryboards(current.transition, current.animation, transitionAnimation, zeroDurationTransition ? target.animation : null);

			void ApplyTargetState()
			{
				// Apply target state setters (the right time to do it!)
				ApplyTargetStateSetters();

				// Starts target state animation
				// TODO Uno: Uno re-enters the current state on forced trigger refreshes, where WinUI's GoToState is a no-op:
				// a running storyboard of the re-entered state keeps running rather than restarting.
				if (target.animation is { } stateAnimation
					&& (stateAnimation != current.animation || stateAnimation.IsInStoppedState()))
				{
					AttemptStartStoryboard(stateAnimation);
				}

				onStateChanged();
			}

			void ApplyTargetStateSetters()
			{
				if (target.setters is null || element is null)
				{
					return;
				}

				// Setter.ApplyValue resolves {ThemeResource} setters against the element's own theme
				// (ResourceResolver.ApplyThemeResource → ResolveOwnerTheme).
				try
				{
					// Setter.ApplyValue can resolve some theme resources.
					// We need to invoke them using the right resource context.
					ResourceResolver.PushNewScope(_xamlScope);

					foreach (var setterBase in target.setters)
					{
						if (setterBase is Setter setter)
						{
							setter.ApplyValue(element);
							setter.EnqueueDeferredSetterOperation(element, DeferredAnimationOperation.DeferredOperation.Set, () => ReapplySetter(setter, element));
						}
					}
				}
				finally
				{
					ResourceResolver.PopScope();
				}

			}
		}

		// Older note from 17/08/2021
		/*****************************************************************/
		// Rollback setters that won't be re-set by the target state setters
		// Note about setters and transition (as of 2021-08-16 win 19043):
		//		* if current and target state have setters for the same property,
		//		  the value of the current is kept as is and updated only once at the end of the transition
		//		* if current has a setter which is not updated by the target state
		//		  the value is rollbacked before the transition
		//		* if the target has a setter for a property that was not affected by the current
		//		  the value is applied only at the end of the transition
		/*****************************************************************/

		// Newer note from 25/04/2021
		/*****************************************************************/
		// WinUI rolls back properties that are set by the current state that won't be set by the
		// setters of the next state and won't be set by the nextAnimation.
		// It doesn't matter how it's set by the current state (setters or transition or animation) or
		// how it will possibly be set by the new state. Simply put, if the next state (or transition) doesn't touch
		// the property in any way, roll it back. Similarly, if the animation of the new state
		// doesn't set a property but it's set in the transition, it should be cleared after the
		// transition.
		// In other words, one can think of it as actually manipulating 3 states
		// "Active" State (Setter & runningAnimation) --> Transition (if exists) --> New State (Setter & target.animation)
		// Moving from one to the next rolls back the properties of the one before it.
		// Note that "touching the property" also includes modifying a subproperty. Meaning, if the previous state sets
		// the property Prop and the "next state" sets "Prop.SubProp", then we don't roll back Prop.
		/*****************************************************************/
		private static void ClearPropertiesNotSetInNextState(
			IFrameworkElement element,
			SetterBaseCollection prevSetters,
			SetterBaseCollection nextSetters,
			Storyboard nextAnimation)
		{
			if (element is null || prevSetters is null)
			{
				return;
			}

			var propertiesNotToClear = ArrayPool<BindingPath>.Shared.Rent((nextSetters?.Count ?? 0) + (nextAnimation?.Children.Items.Count ?? 0));
			var propsLength = 0;

			if (nextSetters is { })
			{
				foreach (var setterBase in nextSetters)
				{
					if (setterBase is Setter setter)
					{
						propertiesNotToClear[propsLength++] = setter.TryGetOrCreateBindingPath(element);
					}
				}
			}

			if (nextAnimation is { })
			{
				foreach (var item in nextAnimation.Children.Items)
				{
					propertiesNotToClear[propsLength++] = item.PropertyInfo;
				}
			}

			if (prevSetters is { })
			{
				foreach (var setterBase in prevSetters)
				{
					// Setters with a null path are always cleared.
					if (setterBase is Setter setter &&
						(setter.TryGetOrCreateBindingPath(element) is not { } prevPath || !propertiesNotToClear.Any(path => prevPath.PrefixOfOrEqualTo(path))))
					{
						setter.ClearValue();
						setter.EnqueueDeferredSetterOperation(element, DeferredAnimationOperation.DeferredOperation.Clear);
					}
				}
			}
		}

		// Applies a {ThemeResource} setter again, in the resource context of the visual states.
		private void ReapplySetter(Setter setter, IFrameworkElement element)
		{
			try
			{
				ResourceResolver.PushNewScope(_xamlScope);
				setter.ApplyValue(element);
			}
			finally
			{
				ResourceResolver.PopScope();
			}
		}

		// VisualStateManagerActuator::AttemptStartStoryboard
		private void AttemptStartStoryboard(Storyboard storyboard)
		{
			try
			{
				storyboard.BeginPrivate(true);
			}
			catch (InvalidOperationException e)
			{
				// TODO Uno: WinUI lets GoToState fail when a storyboard target can't be resolved. Uno still resolves some
				// targets lazily (x:Load, deferred elements), so the failure is logged instead.
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Failed to begin a storyboard of the visual state group {Name}: {e.Message}");
				}

				return;
			}

			// We do this next bit because of course VSM is a special snowflake and unlike when
			// Storyboards are typically started, VSM applies their first-frame values synchronously.
			Storyboard.ZeroSeekAlignedToLastTick(storyboard);

			// TODO Uno: When animations aren't enabled, WinUI skips all non-essential transition storyboards to their end frame.
		}

		// VisualStateManagerActuator::StopAndRemoveStoryboards: stops the running storyboards, but the ones just started.
		private static void StopStoryboards(Storyboard first, Storyboard second, Storyboard keep1, Storyboard keep2)
		{
			if (first is not null && first != keep1 && first != keep2)
			{
				first.StopPrivate();
			}

			if (second is not null && second != first && second != keep1 && second != keep2)
			{
				second.StopPrivate();
			}
		}

		// CVisualTransition::IsZeroDuration
		private static bool IsZeroDuration(Storyboard storyboard)
		{
			// TODO Uno: VisualTransition.GeneratedDuration is not implemented, so it always counts as zero.
			const bool visualTransitionZeroDuration = true;

			// We use the GetDuration method to ensure that we take into account offshifted
			// start times.
			storyboard.GetDuration(out _, out var rDuration);
			var storyboardNullOrZeroDuration = rDuration == 0.0f;

			return visualTransitionZeroDuration && storyboardNullOrZeroDuration;
		}

		private VisualTransition FindTransition(string oldStateName, string newStateName)
		{
			// Only one transition can be run when changing state.
			// The most specific transition wins (i.e. with matching From and To),
			// then we validate for transitions that have only From or To defined which match.

			var transitions = Transitions;
			var hasOld = !oldStateName.IsNullOrEmpty();
			var hasNew = !newStateName.IsNullOrEmpty();

			if (hasOld && hasNew && GetFirstMatch(transitions, oldStateName, newStateName) is { } perfectMatch)
			{
				return perfectMatch;
			}

			if (hasOld && GetFirstMatch(transitions, oldStateName, null) is { } fromMatch)
			{
				return fromMatch;
			}

			if (hasNew && GetFirstMatch(transitions, null, newStateName) is { } newMatch)
			{
				return newMatch;
			}

			return default;

			static VisualTransition GetFirstMatch(IList<VisualTransition> transitions, string from, string to)
			{
				// Avoid using Transitions.FirstOrDefault as it incurs unnecessary Func<VisualTransition, bool> allocations.
				foreach (var transition in transitions)
				{
					if (Match(transition, from, to))
					{
						return transition;
					}
				}

				return null;
			}

			static bool Match(VisualTransition transition, string from, string to)
				=> string.Equals(transition.From, from) && string.Equals(transition.To, to);
		}

		internal void RefreshStateTriggers(bool force = false)
		{
			// If no visual state in this group has triggers, we should not try to overwrite
			// the currently set state with "null" - skip the refresh.
			if (!HasStateTriggers())
			{
				return;
			}

			var newState = GetActiveTrigger();
			var oldState = CurrentState;

			var currentStateHasTriggers = oldState?.StateTriggersOrDefault is { Count: > 0 };
			if (newState is null && !currentStateHasTriggers)
			{
				// We didn't activate any trigger state, we should keep the current non-trigger state.
				return;
			}

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

		private bool HasStateTriggers()
		{
			for (var stateIndex = 0; stateIndex < States.Count; stateIndex++)
			{
				var state = States[stateIndex];
				if (state.StateTriggersOrDefault is { Count: > 0 })
				{
					return true;
				}
			}
			return false;
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
				if (state.StateTriggersOrDefault is not { } triggers)
				{
					continue;
				}

				for (var triggerIndex = 0; triggerIndex < triggers.Count; triggerIndex++)
				{
					var trigger = triggers[triggerIndex];

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
