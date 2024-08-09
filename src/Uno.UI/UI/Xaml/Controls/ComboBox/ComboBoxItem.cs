using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class ComboBoxItem : SelectorItem
	{
		private bool? m_appearSelected;

		public ComboBoxItem()
		{
			DefaultStyleKey = typeof(ComboBoxItem);
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			if (ItemsControl.ItemsControlFromItemContainer(this) is ComboBox comboBox)
			{
				if (args.Key == VirtualKey.Enter && comboBox.IsDropDownOpen)
				{
					var item = comboBox.ItemFromContainer(this);
					if (item != null)
					{
						comboBox.SelectedItem = item;
						comboBox.IsDropDownOpen = false;
						args.Handled = true;
					}
				}

				if (!args.Handled)
				{
					// Fallback to combobox keydown handling
					args.Handled = comboBox.TryHandleKeyDown(args, this);
				}
			}

			base.OnKeyDown(args);
		}

		private protected override void ChangeVisualState(bool useTransitions)
		{
			bool bIsEnabled = false;
			bool bIsSelected = false;
			bool bIsPointerOver = false;
			bool bIsParentSelectionActive = false;
			object spContent;
			Selector spParentSelector;
			ComboBox spCbNoRef;
			FocusState focusState = FocusState.Unfocused;
			bool isDropDownOpen = false;
			//bool isInlineMode = false;
			InputDeviceType inputDeviceTypeUsedToOpenComboBox = InputDeviceType.None;

			spParentSelector = ItemsControl.ItemsControlFromItemContainer(this) as Selector; // GetParentSelector();
			if (spParentSelector is null)
			{
				return;
			}

			bIsEnabled = IsEnabled;
			bIsSelected = IsSelected;
			bIsPointerOver = IsPointerOver;
			focusState = FocusState;

			spCbNoRef = spParentSelector as ComboBox;
			if (spCbNoRef is not null)
			{
				ComboBox cbNoRef = spCbNoRef;
				isDropDownOpen = cbNoRef.IsDropDownOpen;
				//isInlineMode = cbNoRef.IsInlineMode;
				inputDeviceTypeUsedToOpenComboBox = cbNoRef.GetInputDeviceTypeUsedToOpen();

				// IsSelected override for searched items when ComboBox is Editable.
				if (cbNoRef.IsSearchResultIndexSet())
				{
					int resultIndex = cbNoRef.GetSearchResultIndex();

					int index = cbNoRef.IndexFromContainer(this);

					if (index > -1 && index == resultIndex)
					{
						bIsSelected = true;
					}
					else
					{
						bIsSelected = false;
					}
				}
			}

			if (m_appearSelected is not null)
			{
				bIsSelected = m_appearSelected.Value;
			}

			if (!bIsEnabled)
			{
				spContent = Content;
				// If our child is a control then we depend on it displaying a proper "disabled" state.  If it is not a control
				// (ie TextBlock, Border, etc) then we will use our visuals to show a disabled state.
				_ = GoToState(useTransitions, spContent is Control ? "Normal" : "Disabled");
			}
			else
			{
				// Selected Item States
				if (bIsSelected && isDropDownOpen)
				{
					if (IsPointerPressed)
					{
						_ = GoToState(useTransitions, "SelectedPressed");
					}
					else if (bIsPointerOver)
					{
						_ = GoToState(useTransitions, "SelectedPointerOver");
					}
					else
					{
						bIsParentSelectionActive = spParentSelector.IsSelectionActive;
						if (bIsParentSelectionActive)
						{
							GoToState(useTransitions, "Selected");
						}
						else
						{
							GoToState(useTransitions, "SelectedUnfocused");
						}
					}
				}
				else if (IsPointerPressed)
				{
					GoToState(useTransitions, "Pressed");
				}
				else if (bIsPointerOver)
				{
					GoToState(useTransitions, "PointerOver");
				}
				else
				{
					GoToState(useTransitions, "Normal");
				}
			}

			// Apply the proper padding on the ContentPresenter according to the input mode.
			if (inputDeviceTypeUsedToOpenComboBox == InputDeviceType.Touch)
			{
				bool usedTouchInputModeState = GoToState(useTransitions, "TouchInputMode");

				// Ensure the input mode default state if the touch input mode state isn't applied.
				if (!usedTouchInputModeState)
				{
					GoToState(useTransitions, "InputModeDefault");
				}
			}
			else if (inputDeviceTypeUsedToOpenComboBox == InputDeviceType.GamepadOrRemote)
			{
				bool usedGameControllerInputModeState = GoToState(useTransitions, "GameControllerInputMode");

				// Ensure the input mode default state if the game controller input mode state isn't applied.
				if (!usedGameControllerInputModeState)
				{
					GoToState(useTransitions, "InputModeDefault");
				}
			}
			else
			{
				GoToState(useTransitions, "InputModeDefault");
			}
		}

		internal void OverrideSelectedVisualState(bool appearSelected)
		{
			m_appearSelected = appearSelected;

			ChangeVisualState(true);
		}

		internal void ClearSelectedVisualState()
		{
			m_appearSelected = null;

			ChangeVisualState(true);
		}
	}
}
