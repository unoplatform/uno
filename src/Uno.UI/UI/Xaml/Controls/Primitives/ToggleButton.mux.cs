// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ToggleButton_Partial.h, ToggleButton_Partial.cpp, tag winui3/release/1.4.2

#nullable enable

using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ToggleButton
	{
#if false // Uno docs: This appears to be unused, even on WinUI.
		private bool _skipCreateAutomationPeer;
#endif

		private protected override void Initialize()
		{
			base.Initialize();

			// Handle the ENTER key by default.
			SetAcceptsReturn(true);
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == IsCheckedProperty)
			{
				OnIsCheckedChanged();

				var hasAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);
				if (hasAutomationListener)
				{
					var automationPeer = GetOrCreateAutomationPeer();
					if (automationPeer != null && !(this is RadioButton))
					{
						(automationPeer as ToggleButtonAutomationPeer)?.RaiseToggleStatePropertyChangedEvent(args.OldValue, args.NewValue);
					}
				}
			}
		}

		/// <summary>
		/// Change to the correct visual state for the ToggleButton.
		/// </summary>
		/// <param name="useTransitions">Use transitions.</param>
		private protected override void ChangeVisualState(bool useTransitions)
		{
			var isEnabled = IsEnabled;
			var isPressed = IsPressed;
			var isPointerOver = IsPointerOver;
			var focusState = FocusState;

			var isCheckedReference = IsChecked;

			// Update the CommonStates group
			if (isCheckedReference == null)
			{
				if (!isEnabled)
				{
					GoToState(useTransitions, "IndeterminateDisabled");
				}
				else if (isPressed)
				{
					GoToState(useTransitions, "IndeterminatePressed");
				}
				else if (isPointerOver)
				{
					GoToState(useTransitions, "IndeterminatePointerOver");
				}
				else
				{
					GoToState(useTransitions, "Indeterminate");
				}
			}
			else if (isCheckedReference == true)
			{
				if (!isEnabled)
				{
					GoToState(useTransitions, "CheckedDisabled");
				}
				else if (isPressed)
				{
					GoToState(useTransitions, "CheckedPressed");
				}
				else if (isPointerOver)
				{
					GoToState(useTransitions, "CheckedPointerOver");
				}
				else
				{
					GoToState(useTransitions, "Checked");
				}
			}
			else
			{
				if (!isEnabled)
				{
					GoToState(useTransitions, "Disabled");
				}
				else if (isPressed)
				{
					GoToState(useTransitions, "Pressed");
				}
				else if (isPointerOver)
				{
					GoToState(useTransitions, "PointerOver");
				}
				else
				{
					GoToState(useTransitions, "Normal");
				}
			}

			// Update the Focus group
			if (focusState != FocusState.Unfocused && isEnabled)
			{
				if (focusState == FocusState.Pointer)
				{
					GoToState(useTransitions, "PointerFocused");
				}
				else
				{
					GoToState(useTransitions, "Focused");
				}
			}
			else
			{
				GoToState(useTransitions, "Unfocused");
			}
		}

		/// <summary>
		/// Toggles the Check state and raises the Click routed event.
		/// </summary>
		private protected override void OnClick()
		{
			OnToggle();

			base.OnClick();
		}

		/// <summary>
		/// Raises the Checked routed event.
		/// </summary>
		private protected virtual void OnChecked()
		{
			UpdateVisualState();

			// Create the args
			var args = new RoutedEventArgs();
			args.OriginalSource = this;

			// Raise the event
			Checked?.Invoke(this, args);
		}

		/// <summary>
		/// Raises the Unchecked routed event.
		/// </summary>
		private protected virtual void OnUnchecked()
		{
			UpdateVisualState();

			// Create the args
			var args = new RoutedEventArgs();
			args.OriginalSource = this;

			// Raise the event
			Unchecked?.Invoke(this, args);
		}

		/// <summary>
		/// Raises the Indeterminate routed event.
		/// </summary>
		private protected virtual void OnIndeterminate()
		{
			UpdateVisualState();

			// Create the args
			var args = new RoutedEventArgs();
			args.OriginalSource = this;

			// Raise the event
			Indeterminate?.Invoke(this, args);
		}

		/// <summary>
		/// Toggles the IsChecked state.
		/// </summary>
		private void OnToggleImpl()
		{
			var isCheckedReference = IsChecked;

			if (isCheckedReference == null)
			{
				// Indeterminate
				// Set to Unchecked
				IsChecked = false;
			}
			else if (isCheckedReference == true)
			{
				// Checked
				var isThreeState = IsThreeState;
				if (isThreeState)
				{
					// Set to Indeterminate
					IsChecked = null;
				}
				else
				{
					// Set to Unchecked
					IsChecked = false;
				}
			}
			else
			{
				// Unchecked
				// Set to Checked
				IsChecked = true;
			}
		}

		/// <summary>
		/// Handle the IsChecked status change, resulting in updated VSM states and raising events
		/// </summary>
		private void OnIsCheckedChanged()
		{
			// This workaround can be removed if pooling is removed. See https://github.com/unoplatform/uno/issues/12189
			if (_suppressCheckedChanged) // Uno workaround
			{
				UpdateVisualState();
				return;
			}

			var isChecked = IsChecked;
			if (isChecked == null)
			{
				// Indeterminate
				OnIndeterminate();
			}
			else if (isChecked == true)
			{
				// Checked
				OnChecked();
			}
			else
			{
				// Unchecked
				OnUnchecked();
			}
		}

		internal void AutomationToggleButtonOnToggle()
		{
			// OnToggle through UIAutomation
			OnClick();
		}

		/// <summary>
		/// Create ToggleButtonAutomationPeer to represent the ToggleButton.
		/// </summary>
		/// <returns>Automation peer.</returns>
		protected override AutomationPeer? OnCreateAutomationPeer()
		{
			//if (!_skipCreateAutomationPeer)
			{
				return new ToggleButtonAutomationPeer(this);
			}

			//return null;
		}

#if false // Uno docs: This appears to be unused, even on WinUI.
		private void SetSkipAutomationPeerCreation() => _skipCreateAutomationPeer = true;
#endif
	}
}
