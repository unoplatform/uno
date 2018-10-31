using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Markup;
using Uno.UI;
using Windows.Foundation.Collections;

#if XAMARIN_IOS
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml
{
	[ContentProperty(Name = "States")]
	public sealed partial class VisualStateGroup : DependencyObject
	{
		public VisualStateGroup()
		{
			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();

			this.RegisterParentChangedCallback(this, OnParentChanged);
		}

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

		// Using a DependencyProperty as the backing store for States.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StatesProperty =
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

		// Using a DependencyProperty as the backing store for Transitions.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TransitionsProperty =
			DependencyProperty.Register(
				"Transitions",
				typeof(IList<VisualTransition>),
				typeof(VisualStateGroup),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((VisualStateGroup)s)?.OnTransitionsChanged(e)
				)
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

		//Adds Event Handlers when collections changed
		private void OnTransitionsChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		public VisualState CurrentState { get; internal set; }

		public event VisualStateChangedEventHandler CurrentStateChanging;

		public event VisualStateChangedEventHandler CurrentStateChanged;

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

		internal void GoToState(IFrameworkElement element, VisualState state, VisualState originalState, bool useTransitions, Action onStateChanged)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("Go to state [{0}/{1}] on [{2}]", Name, state?.Name, element);
			}

			var transition = FindTransition(originalState?.Name, state?.Name);

			EventHandler<object> onComplete = null;

			onComplete = (s, a) =>
			{
				onStateChanged();

				if (state?.Storyboard == null)
				{
					return;
				}

				state.Storyboard.Completed -= onComplete;
			};

			EventHandler<object> onTransitionComplete = null;

			onTransitionComplete = (s, a) =>
			{
				if (transition?.Storyboard != null && useTransitions)
				{
					transition.Storyboard.Completed -= onTransitionComplete;

					if (state?.Storyboard != null)
					{
						transition.Storyboard.TurnOverAnimationsTo(state.Storyboard);
					}
				}

				//Starts Storyboard Animation
				if (state?.Storyboard == null)
				{
					onComplete(this, null);
				}
				else if(state != null)
				{
					state.Storyboard.Completed += onComplete;
					state.Storyboard.Begin();
				}
			};

			//Stops Previous Storyboard Animation
			if (originalState != null)
			{
				if (originalState.Storyboard != null)
				{
					if (transition?.Storyboard != null)
					{
						originalState.Storyboard.TurnOverAnimationsTo(transition.Storyboard);
					}
					else if (state?.Storyboard != null)
					{
						originalState.Storyboard.TurnOverAnimationsTo(state.Storyboard);
					}
					else
					{
						originalState.Storyboard.Stop();
					}
				}

				foreach (var setter in this.CurrentState.Setters.OfType<Setter>())
				{
					setter.ClearValue();
				}
			}

			this.CurrentState = state;
			if (this.CurrentState != null)
			{
				foreach (var setter in this.CurrentState.Setters.OfType<Setter>())
				{
					setter.ApplyValue(DependencyPropertyValuePrecedences.Animations, element);
				}
			}

			if (transition?.Storyboard == null || !useTransitions)
			{
				onTransitionComplete(this, null);
			}
			else
			{
				transition.Storyboard.Completed += onTransitionComplete;
				transition.Storyboard.Begin();
			}
		}

		private VisualTransition FindTransition(string oldStateName, string newStateName)
		{
			var perfectMatch = Transitions.FirstOrDefault(vt =>
				string.Equals(vt.From, oldStateName) &&
				string.Equals(vt.To, newStateName));

			if (perfectMatch != null)
			{
				return perfectMatch;
			}

			var fromMatch = Transitions.FirstOrDefault(vt =>
				string.Equals(vt.From, oldStateName) &&
				vt.To == null);

			if (fromMatch != null)
			{
				return fromMatch;
			}

			var toMatch = Transitions.FirstOrDefault(vt =>
				vt.From == null &&
				string.Equals(vt.To, newStateName));

			return toMatch;
		}

		internal void RefreshStateTriggers()
		{
			var activeVisualState = GetActiveTrigger();
			var oldState = CurrentState;

			if (this.GetParent() is IFrameworkElement parent)
			{
				// The parent may be null when the VisualStateGroup is being built.

				if (CurrentState != null || activeVisualState != null)
				{
					GoToState(parent, activeVisualState, CurrentState, false, () => RaiseCurrentStateChanged(oldState, activeVisualState));
				}
			}
		}

		private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			RefreshStateTriggers();
		}

		/// <remarks>
		/// This method is not using LINQ for performance considerations.
		/// </remarks>
		private VisualState GetActiveTrigger()
		{
			for (int index = States.Count - 1; index >= 0; index--)
			{
				foreach (var trigger in States[index].StateTriggers)
				{
					if (trigger.InternalIsActive)
					{
						return trigger.Owner;
					}
				}
			}

			return null;
		}
	}
}
