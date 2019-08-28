using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;
using System.Threading.Tasks;
using Uno.UI;
#if XAMARIN_IOS
using UIKit;
#endif

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class SelectorItem : ContentControl
	{
		private static class CommonStates
		{
			public const string Selected = "Selected";
			public const string Normal = "Normal";
			public const string Over = "PointerOver";
			public const string Pressed = "Pressed";
			public const string OverSelected = "PointerOverSelected"; // "SelectedPointerOver" for ListBoxItem, ComboBoxItem and PivotHeaderItem
			public const string OverPressed = "PointerOverPressed"; // Only for ListViewItem and GridViewItem
			public const string PressedSelected = "PressedSelected"; // "SelectedPressed" for ListBoxItem, ComboBoxItem and PivotHeaderItem
		}

		private static class DisabledStates
		{
			public const string Enabled = "Enabled";
			public const string Disabled = "Disabled";
		}

		private enum ManipulationUpdateKind
		{
			None = 0,
			Begin,
			End,
			Clicked
		}

		/// <summary>
		/// Delay time before setting the pressed state of an item to false, to allow time for the Pressed visual state to be drawn and perceived. 
		/// </summary>
		private static readonly TimeSpan MinTimeBetweenPressStates = TimeSpan.FromMilliseconds(100);

		/// <summary>
		/// Whether the SelectorItem will handle touches. This can be set to false for compatibility with controls where the parent 
		/// handles touches (ComboBox-Android, legacy ListView/GridView). 
		/// </summary>
		internal bool ShouldHandlePressed { get; set; } = true;

		/// <remarks>
		/// Ensure that the ContentControl will create its children even
		/// if it has no parent view. This is critical for the recycling panels,
		/// where the content is databound before being assigned to its
		/// parent and displayed.
		/// </remarks>
		protected override bool CanCreateTemplateWithoutParent { get; } = true;

		/// <summary>
		/// Indicates if the SelectorItem has the "PointerOverPressed" visual state (GridViewItem and ListViewItem only)
		/// </summary>
		internal virtual bool HasPointerOverPressedState => false;

		private string _currentState;
		private uint _goToStateRequest;
		private DateTime _pauseStateUpdateUntil;

		public SelectorItem()
		{

		}

		private Selector Selector => ItemsControl.ItemsControlFromItemContainer(this) as Selector;

		private bool IsItemClickEnabled
		{
			get
			{
				if (!(Selector is ListViewBase listViewBase))
				{
					return true;
				}

				return listViewBase.IsItemClickEnabled || listViewBase.SelectionMode != ListViewSelectionMode.None;
			}
		}

		protected override void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
			var disabledStates = newValue ? DisabledStates.Enabled : DisabledStates.Disabled;
			VisualStateManager.GoToState(this, disabledStates, true);

			base.OnIsEnabledChanged(oldValue, newValue);
		}

		/// <summary>
		/// Set appropriate visual state from MultiSelectStates group. (https://msdn.microsoft.com/en-us/library/windows/apps/mt299136.aspx?f=255&MSPPError=-2147217396)
		/// </summary>
		internal void ApplyMultiSelectState(bool isSelectionMultiple)
		{
			if (isSelectionMultiple)
			{
				// We can safely always go to multiselect state
				VisualStateManager.GoToState(this, "MultiSelectEnabled", useTransitions: true);
			}
			else
			{
				// Retrieve the current state (which 'lives' on the SelectorItem's template root, and may change if it is retemplated)
				var currentState = VisualStateManager.GetCurrentState(this, "MultiSelectStates")?.Name;

				if (currentState == "MultiSelectEnabled")
				{
					// The MultiSelectDisabled state goes through VisibleRect then collapsed, which means Disabled state can't be
					// invoked if the state is already disabled without having the selected check box appearing briefly. (Issue #403)
					VisualStateManager.GoToState(this, "MultiSelectDisabled", useTransitions: true);
				}
			}
		}

		partial void OnIsSelectedChangedPartial(bool oldIsSelected, bool newIsSelected)
		{
			UpdateCommonStates();
		}

		private void UpdateCommonStatesWithoutNeedsLayout(ManipulationUpdateKind manipulationUpdate = ManipulationUpdateKind.None)
		{
			using (InterceptSetNeedsLayout())
			{
				UpdateCommonStates(manipulationUpdate);
			}
		}

		private void UpdateCommonStates(ManipulationUpdateKind manipulationUpdate = ManipulationUpdateKind.None)
		{
			var state = GetState(IsSelected, IsPointerOver, IsPointerPressed);

			// On Windows, the pressed state appears only after a few, and won't appear at all if you quickly start to scroll with the finger.
			// So here we make sure to delay the beginning of a manipulation to match this behavior (and avoid flickering when scrolling)
			// We also make sure that when user taps (Enter->Pressed->Move*->Release->Exit) on the item, he is able to see the pressed (selected) state.
			var request = ++_goToStateRequest;

			TimeSpan delay; // delay to apply the 'state'
			bool pause; // should we force a pause after applying the 'state'
			if (manipulationUpdate == ManipulationUpdateKind.Clicked
				&& _currentState != CommonStates.PressedSelected
				&& _currentState != CommonStates.OverPressed
				&& _currentState != CommonStates.Pressed)
			{
				// When clicked (i.e. pointer released), but not yet in pressed state, we force to go immediately in pressed state
				// Then we let the standard go to state process to reach the final expected state.

				var pressedState = GetState(IsSelected, IsPointerOver, isPressed: true);
				_currentState = pressedState;
				VisualStateManager.GoToState(this, pressedState, true);

				_pauseStateUpdateUntil = DateTime.Now + MinTimeBetweenPressStates;

				delay = MinTimeBetweenPressStates;
				pause = false;
			}
			else if (manipulationUpdate == ManipulationUpdateKind.Begin)
			{
				// We delay the beginning of a manipulation to avoid flickers, but not for "exact" devices

				delay = MinTimeBetweenPressStates;
				pause = true;
			}
			else
			{
				delay = _pauseStateUpdateUntil - DateTime.Now;
				pause = false;
			}

			if (delay < TimeSpan.Zero)
			{
				_currentState = state;
				VisualStateManager.GoToState(this, state, true);
			}
			else
			{
				CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					await Task.Delay(delay);

					if (_goToStateRequest != request)
					{
						return;
					}

					_currentState = state;
					VisualStateManager.GoToState(this, state, true);

					if (pause)
					{
						_pauseStateUpdateUntil = DateTime.Now + MinTimeBetweenPressStates;
					}
				});
			}
		}

		private string GetState(bool isSelected, bool isOver, bool isPressed)
		{
			var state = CommonStates.Normal;
			if (isSelected && isPressed)
			{
				state = CommonStates.PressedSelected;
			}
			else if (FeatureConfiguration.SelectorItem.UseOverStates
				&& isSelected && isOver)
			{
				state = CommonStates.OverSelected;
			}
			else if (FeatureConfiguration.SelectorItem.UseOverStates && HasPointerOverPressedState
				&& isOver && isPressed)
			{
				state = CommonStates.OverPressed;
			}
			else if (isSelected)
			{
				state = CommonStates.Selected;
			}
			else if (isPressed)
			{
				state = CommonStates.Pressed;
			}
			else if (FeatureConfiguration.SelectorItem.UseOverStates
				&& isOver)
			{
				state = CommonStates.Over;
			}

			return state;
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();
#if __ANDROID__
			Focusable = true;
			FocusableInTouchMode = true;
#endif

			UpdateCommonStates();
		}

#if __IOS__
		private bool _pressedOverride;
		private new bool IsPointerPressed => _pressedOverride || base.IsPointerPressed;

		/// <summary>
		/// Used by the legacy list view to set the item pressed
		/// </summary>
		internal void LegacySetPressed(bool isPressed)
			=> _pressedOverride = isPressed;
#endif

		/// <inheritdoc />
		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			base.OnPointerEntered(args);
			UpdateCommonStatesWithoutNeedsLayout(ManipulationUpdateKind.Begin);
		}

		/// <inheritdoc />
		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			if (ShouldHandlePressed
				&& IsItemClickEnabled
				&& args.GetCurrentPoint(this).Properties.IsLeftButtonPressed
				&& CapturePointer(args.Pointer))
			{
				args.Handled = true;
			}

#if !__WASM__
			Focus(FocusState.Pointer);
#endif

			base.OnPointerPressed(args);
			UpdateCommonStatesWithoutNeedsLayout(ManipulationUpdateKind.Begin);
		}

		/// <inheritdoc />
		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			ManipulationUpdateKind update;
			if (IsCaptured(args.Pointer))
			{
				update = ManipulationUpdateKind.Clicked;
				Selector?.OnItemClicked(this);

				args.Handled = true;
			}
			else
			{
				update = ManipulationUpdateKind.End;
			}

			base.OnPointerReleased(args);
			UpdateCommonStatesWithoutNeedsLayout(update);
		}

		/// <inheritdoc />
		protected override void OnPointerExited(PointerRoutedEventArgs args)
		{
			// Not like a Button, if the pointer goes out of this item, we abort the ItemClick
			ReleasePointerCaptures();

			base.OnPointerExited(args);
			UpdateCommonStatesWithoutNeedsLayout(ManipulationUpdateKind.End);
		}

		/// <inheritdoc />
		protected override void OnPointerCanceled(PointerRoutedEventArgs args)
		{
			base.OnPointerCanceled(args);
			UpdateCommonStatesWithoutNeedsLayout(ManipulationUpdateKind.End);
		}

		/// <inheritdoc />
		protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
		{
			base.OnPointerCaptureLost(args);
			UpdateCommonStatesWithoutNeedsLayout(ManipulationUpdateKind.End);
		}

		private IDisposable InterceptSetNeedsLayout()
		{
#if __IOS__
			bool match(UIView view) => view is ListViewBaseInternalContainer || view is Selector;
			var cell = this.FindFirstParent<UIView>(predicate: match);
			return (cell as ListViewBaseInternalContainer)?.InterceptSetNeedsLayout();
#else
			return null;
#endif
		}
	}
}
