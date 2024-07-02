#nullable enable

using System;
using System.Windows.Input;
using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class ToggleMenuFlyoutItem : MenuFlyoutItem
	{
		public ToggleMenuFlyoutItem()
		{
			DefaultStyleKey = typeof(ToggleMenuFlyoutItem);
		}

		#region IsChecked

		public bool IsChecked
		{
			get => (bool)GetValue(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, value);
		}

		public static DependencyProperty IsCheckedProperty { get; } =
			DependencyProperty.Register(
				name: nameof(IsChecked),
				propertyType: typeof(bool),
				ownerType: typeof(ToggleMenuFlyoutItem),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: false,
					propertyChangedCallback: (s, e) => (s as ToggleMenuFlyoutItem)?.OnIsCheckedChanged((bool)e.OldValue, (bool)e.NewValue)));

		#endregion

		// Change to the correct visual state for the MenuFlyoutItem.
		private protected override void ChangeVisualState(
			// true to use transitions when updating the visual state, false
			// to snap directly to the new visual state.
			bool bUseTransitions)
		{
			var bIsPressed = IsPointerPressed;
			var bIsPointerOver = IsPointerOver;
			var bIsEnabled = IsEnabled;
			var bIsChecked = IsChecked;
			var hasIconMenuItem = false;
			var hasMenuItemWithKeyboardAcceleratorText = false;
			var shouldBeNarrow = GetShouldBeNarrow();
			var focusState = FocusState;
			var spPresenter = GetParentMenuFlyoutPresenter();
			var isKeyboardPresent = false;

			if (spPresenter != null)
			{
				hasIconMenuItem = spPresenter.GetContainsIconItems();
				hasMenuItemWithKeyboardAcceleratorText = spPresenter.GetContainsItemsWithKeyboardAcceleratorText();
			}

			// We only care about finding if we have a keyboard if we also have a menu item with accelerator text,
			// since if we don't have any menu items with accelerator text, we won't be showing any accelerator text anyway.
			if (hasMenuItemWithKeyboardAcceleratorText)
			{
				isKeyboardPresent = new KeyboardCapabilities().KeyboardPresent != 0;
			}

			// CommonStates
			if (!bIsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", bUseTransitions);
			}
			else if (bIsPressed)
			{
				VisualStateManager.GoToState(this, "Pressed", bUseTransitions);
			}
			else if (bIsPointerOver)
			{
				VisualStateManager.GoToState(this, "PointerOver", bUseTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", bUseTransitions);
			}

			// FocusStates
			if (FocusState.Unfocused != focusState && bIsEnabled)
			{
				if (FocusState.Pointer == focusState)
				{
					VisualStateManager.GoToState(this, "PointerFocused", bUseTransitions);
				}
				else
				{
					VisualStateManager.GoToState(this, "Focused", bUseTransitions);
				}
			}
			else
			{
				VisualStateManager.GoToState(this, "Unfocused", bUseTransitions);
			}

			// CheckStates
			if (bIsChecked && hasIconMenuItem)
			{
				VisualStateManager.GoToState(this, "CheckedWithIcon", bUseTransitions);
			}
			else if (hasIconMenuItem)
			{
				VisualStateManager.GoToState(this, "UncheckedWithIcon", bUseTransitions);
			}
			else if (bIsChecked)
			{
				VisualStateManager.GoToState(this, "Checked", bUseTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Unchecked", bUseTransitions);
			}

			// PaddingSizeStates
			if (shouldBeNarrow)
			{
				VisualStateManager.GoToState(this, "NarrowPadding", bUseTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "DefaultPadding", bUseTransitions);
			}

			// We'll make the accelerator text visible if any item has accelerator text,
			// as this causes the margin to be applied which reserves space, ensuring that accelerator text
			// in one item won't be at the same horizontal position as label text in another item.
			if (hasMenuItemWithKeyboardAcceleratorText && isKeyboardPresent)
			{
				VisualStateManager.GoToState(this, "KeyboardAcceleratorTextVisible", bUseTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "KeyboardAcceleratorTextCollapsed", bUseTransitions);
			}
		}

		// Performs appropriate actions upon a mouse/keyboard invocation of a MenuFlyoutItem.
		internal override void Invoke()
		{
			IsChecked = !IsChecked;

			base.Invoke();
		}

		private void OnIsCheckedChanged(bool oldValue, bool newValue)
		{
			UpdateVisualState();

			var bAutomationListener = AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged);
			if (bAutomationListener)
			{
				var spAutomationPeer = GetAutomationPeer();

				if (spAutomationPeer is ToggleMenuFlyoutItemAutomationPeer spToggleButtonAutomationPeer)
				{
					spToggleButtonAutomationPeer.Toggle();
				}
			}
		}

		// Create ToggleMenuFlyoutItemAutomationPeer to represent the 
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new ToggleMenuFlyoutItemAutomationPeer(this);

		internal override bool HasToggle() => true;
	}
}
