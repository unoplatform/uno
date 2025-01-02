using Windows.System;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class CheckBox
	{
		private protected override void Initialize()
		{
			base.Initialize();

			// Ignore the enter key by default.
			SetAcceptsReturn(false);
		}

		/// <summary>
		/// Handles the KeyDown event for CheckBox.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Handled?</returns>
		private protected override bool OnKeyDownInternal(VirtualKey key)
		{
			var handled = base.OnKeyDownInternal(key);

			var isThreeState = IsThreeState;
			var isEnabled = IsEnabled;

			if (!isThreeState && isEnabled)
			{
				if (key == VirtualKey.Add)
				{
					handled = true;
					IsPressed = false;
					IsChecked = true;
				}
				else if (key == VirtualKey.Subtract)
				{
					handled = true;
					IsPressed = false;
					IsChecked = false;
				}
			}

			return handled;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// If HighContrastAdjustment property hasn't been set for CheckGlyph set the initial value to None to avoid BackPlate drawing over CheckBox rectangle.
			var checkGlyphPart = GetTemplateChild("CheckGlyph");

			if (checkGlyphPart is UIElement checkGlyph)
			{
				if (!checkGlyph.IsDependencyPropertySet(HighContrastAdjustmentProperty))
				{
					checkGlyph.SetValue(HighContrastAdjustmentProperty, ElementHighContrastAdjustment.None);
				}
			}
		}

		/// <summary>
		/// Create CheckBoxAutomationPeer to represent the CheckBox.
		/// </summary>
		/// <returns>Automation peer.</returns>
		protected override AutomationPeer OnCreateAutomationPeer() =>
			new CheckBoxAutomationPeer(this);

		/// <summary>
		/// Change to the correct visual state for the CheckBox.
		/// </summary>
		/// <param name="useTransitions">Use transitions.</param>
		private protected override void ChangeVisualState(bool useTransitions)
		{
			var isEnabled = IsEnabled;
			var isPressed = IsPressed;
			var isPointerOver = IsPointerOver;
			var focusState = FocusState;

			var isChecked = false;
			var isCheckedReference = IsChecked;
			if (isCheckedReference != null)
			{
				isChecked = isCheckedReference.Value;
			}

			bool succeeded = false;

			//Update the Combined state group
			if (isCheckedReference == null)
			{
				if (!isEnabled)
				{
					succeeded = GoToState(useTransitions, "IndeterminateDisabled");
				}
				else if (isPressed)
				{
					succeeded = GoToState(useTransitions, "IndeterminatePressed");
				}
				else if (isPointerOver)
				{
					succeeded = GoToState(useTransitions, "IndeterminatePointerOver");
				}
				else
				{
					succeeded = GoToState(useTransitions, "IndeterminateNormal");
				}
			}
			else if (isChecked)
			{
				if (!isEnabled)
				{
					succeeded = GoToState(useTransitions, "CheckedDisabled");
				}
				else if (isPressed)
				{
					succeeded = GoToState(useTransitions, "CheckedPressed");
				}
				else if (isPointerOver)
				{
					succeeded = GoToState(useTransitions, "CheckedPointerOver");
				}
				else
				{
					succeeded = GoToState(useTransitions, "CheckedNormal");
				}
			}
			else
			{
				if (!isEnabled)
				{
					succeeded = GoToState(useTransitions, "UncheckedDisabled");
				}
				else if (isPressed)
				{
					succeeded = GoToState(useTransitions, "UncheckedPressed");
				}
				else if (isPointerOver)
				{
					succeeded = GoToState(useTransitions, "UncheckedPointerOver");
				}
				else
				{
					succeeded = GoToState(useTransitions, "UncheckedNormal");
				}
			}

			// Go to an older state if a combined state isn't available. If a Blue app is upgraded to TH
			// without updating its custom templates, these states will be present in the template instead
			// of the newer ones.
			if (!succeeded)
			{
				// Update the Interaction state group
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

				// Update the Check state group
				if (isCheckedReference == null)
				{
					// Indeterminate
					GoToState(useTransitions, "Indeterminate");
				}
				else if (isChecked)
				{
					// Checked
					GoToState(useTransitions, "Checked");
				}
				else
				{
					// Unchecked
					GoToState(useTransitions, "Unchecked");
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
	}
}
