using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Globalization;
using System.Reflection.Emit;
using System.Text.Json;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal static class AppBarButtonHelpers
{
	private static void UpdateToolTip<T>(T button)
		where T : FrameworkElement, IAppBarButtonHelpersProvider
	{
		if (button.m_ownsToolTip)
		{
			var useOverflowStyle = button.UseOverflowStyle;
			var keyboardAcceleratorText = button.KeyboardAcceleratorTextOverride;

			if (!useOverflowStyle && keyboardAcceleratorText is null)
			{
				// If we're in the primary section of the app bar or command bar and have accelerator text,
				// then we should give ourselves a tool tip showing the label plus the accelerator text.
				wrl_wrappers::HString labelText;

				button->get_Label(labelText.ReleaseAndGetAddressOf()));

				wrl_wrappers::HString toolTipFormatString;
				IFC_RETURN(DXamlCore::GetCurrent()->GetLocalizedResourceString(KEYBOARD_ACCELERATOR_TEXT_TOOLTIP, toolTipFormatString.ReleaseAndGetAddressOf()));

				WCHAR buffer[1024];
				IFCEXPECT_RETURN(swprintf_s(
					buffer, ARRAYSIZE(buffer),
					toolTipFormatString.GetRawBuffer(nullptr),
					labelText.GetRawBuffer(nullptr),
					keyboardAcceleratorText.GetRawBuffer(nullptr)) >= 0);

				wrl_wrappers::HString toolTipString;
				IFC_RETURN(toolTipString.Set(buffer));

				ctl::ComPtr<IInspectable> boxedToolTipString;
				IFC_RETURN(PropertyValue::CreateFromString(toolTipString.Get(), &boxedToolTipString));

				button.SetValue(ToolTipService.ToolTipProperty, boxedToolTipString);
			}
			else
			{
				button.ClearValue(ToolTipService.ToolTipProperty);
			}

			// Setting the value of ToolTipService.ToolTip causes us to flag us as no longer owning the tool tip,
			// since that's the code path that an app setting the value will also take.
			// In order to ensure that we know that we still own the tool tip, we'll set this value to true here.
			button.m_ownsToolTip = true;
		}
	}

	private static Size GetKeyboardAcceleratorTextDesiredSize<T>(T button)
		where T : FrameworkElement, IAppBarButtonHelpersProvider
	{
		Size desiredSize = default;

		if (button.m_tpKeyboardAcceleratorTextLabel is not null)
		{
			button.m_tpKeyboardAcceleratorTextLabel.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
			desiredSize = button.m_tpKeyboardAcceleratorTextLabel.DesiredSize;
			var margin = button.m_tpKeyboardAcceleratorTextLabel.Margin;

			desiredSize = new Size(
				desiredSize.Width - (margin.Left + margin.Right),
				desiredSize.Height - (margin.Top + margin.Bottom));

		}

		return desiredSize;
	}

	internal static string GetKeyboardAcceleratorText<T>(T button)
		where T : FrameworkElement, IAppBarButtonHelpersProvider
	{
		var keyboardAcceleratorText = button.GetValue(button.GetKeyboardAcceleratorTextDependencyProperty()) as string;

		// If we have no keyboard accelerator text already provided by the app,
		// then we'll see if we can construct it ourselves based on keyboard accelerators
		// set on this item.  For example, if a keyboard accelerator with key "S" and modifier "Control"
		// is set, then we'll convert that into the keyboard accelerator text "Ctrl+S".
		if (keyboardAcceleratorText == null)
		{
			keyboardAcceleratorText = KeyboardAccelerator.GetStringRepresentationForUIElement(button);

			// If we were able to get a string representation from keyboard accelerators,
			// then we should now set that as the value of KeyboardAcceleratorText.
			if (keyboardAcceleratorText != null)
			{
				PutKeyboardAcceleratorText(button, keyboardAcceleratorText);
			}
		}

		return keyboardAcceleratorText;
	}

	internal static void PutKeyboardAcceleratorText<T>(T button, string value)
		where T : FrameworkElement, IAppBarButtonHelpersProvider =>
		button.SetValue(button.GetKeyboardAcceleratorTextDependencyProperty(), value);

	private static bool IsKeyboardFocusable(FrameworkElement button)
	{
		// If an AppBarButton is in a CommandBar, then it's keyboard focusable even if it's not a tab stop,
		// so we want to exclude that from the conditions we check in uielement.cpp.
		FrameworkElement coreButtonType = button;

		return coreButtonType.IsLoaded && // TODO:Uno: Should be "coreButtonType.IsActive() &&"
			coreButtonType.IsVisible() &&
			(coreButtonType.IsEnabledInternal() || coreButtonType.AllowFocusWhenDisabled) &&
			coreButtonType.AreAllAncestorsVisible();
	}


	/* OLD CODE



	#region AppBarButtonHelpers
	private void OnBeforeApplyTemplate()
	{
		if (m_isTemplateApplied)
		{
			StopAnimationForWidthAdjustments();
			m_isTemplateApplied = false;
		}
	}

	private void OnAfterApplyTemplate()
	{
		GetTemplatePart<TextBlock>("KeyboardAcceleratorTextLabel", out var keyboardAcceleratorTextLabel);
		m_tpKeyboardAcceleratorTextLabel = keyboardAcceleratorTextLabel;

		m_isTemplateApplied = true;

		// Set the initial view state
		UpdateInternalStyles();
		UpdateVisualState();
	}

	private void CloseSubMenusOnPointerEntered(ISubMenuOwner? pMenuToLeaveOpen)
	{
		var isInOverflow = IsInOverflow;

		if (isInOverflow)
		{
			// If there are other buttons that have open sub-menus, then we should
			// close those on a delay, since they no longer have mouse-over.

			CommandBar.FindParentCommandBarForElement(this, out var parentCommandBar);

			if (parentCommandBar is { })
			{
				parentCommandBar.CloseSubMenus(pMenuToLeaveOpen, true /* closeOnDelay );
			}
		}
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == IsCompactProperty
			|| args.Property == UseOverflowStyleProperty
			|| args.Property == LabelPositionProperty)
		{
			UpdateInternalStyles();
			UpdateVisualState();
		}

		if (args.Property == ToolTipService.ToolTipProperty)
		{
			var toolTipValue = GetValue(ToolTipService.ToolTipProperty);

			if (toolTipValue is { })
			{
				m_ownsToolTip = false;
			}
			else
			{
				m_ownsToolTip = true;
			}
		}
	}

	private void ChangeCommonVisualStates(bool useTransitions)
	{
		var isCompact = IsCompact;
		var useOverflowStyle = UseOverflowStyle;
		var effectiveLabelPosition = GetEffectiveLabelPosition();
		bool isKeyboardPresent = false;

		// We only care about finding if we have a keyboard if we also have a menu item with keyboard accelerator text,
		// since if we don't have any menu items with keyboard accelerator text, we won't be showing any that text anyway.
		if (m_isWithKeyboardAcceleratorText)
		{
			// UNO TODO
			// isKeyboardPresent = DXamlCore.GetCurrent().GetIsKeyboardPresent();
			isKeyboardPresent = true;
		}

		if (!useOverflowStyle)
		{
			if (effectiveLabelPosition == CommandBarDefaultLabelPosition.Right)
			{
				GoToState(useTransitions, "LabelOnRight");
			}
			else if (effectiveLabelPosition == CommandBarDefaultLabelPosition.Collapsed)
			{
				GoToState(useTransitions, "LabelCollapsed");
			}
			else if (isCompact)
			{
				GoToState(useTransitions, "Compact");
			}
			else
			{
				GoToState(useTransitions, "FullSize");
			}
		}

		GoToState(useTransitions, "InputModeDefault");
		//if (button->m_inputDeviceTypeUsedToOpenOverflow == DirectUI::InputDeviceType::Touch)
		//{
		//	IFC_RETURN(button->GoToState(useTransitions, L"TouchInputMode", &ignored));
		//}
		//else if (button->m_inputDeviceTypeUsedToOpenOverflow == DirectUI::InputDeviceType::GamepadOrRemote)
		//{
		//	IFC_RETURN(button->GoToState(useTransitions, L"GameControllerInputMode", &ignored));
		//}

		// We'll make the keyboard accelerator text visible if any element in the overflow has keyboard accelerator text,
		// as this causes the margin to be applied which reserves space, ensuring that keyboard accelerator text
		// in one button won't be at the same horizontal position as label text in another button.
		if (m_isWithKeyboardAcceleratorText && isKeyboardPresent && useOverflowStyle)
		{
			GoToState(useTransitions, "KeyboardAcceleratorTextVisible");
		}
		else
		{
			GoToState(useTransitions, "KeyboardAcceleratorTextCollapsed");
		}

	}

	private void OnCommandChangedHelper(object pOldValue, object pNewValue)
	{
		if (pOldValue is { })
		{
			if (pOldValue is XamlUICommand oldCommandAsUICommand)
			{
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, LabelProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, IconProperty);
			}
		}

		if (pNewValue is { })
		{
			if (pNewValue is XamlUICommand newCommandAsUICommand)
			{
				// The call to ButtonBase::OnCommandChanged() will have set the Content property, which we don't want -
				// it's not used anywhere in AppBar*Button, and having it be set can cause problems if an AppBarButton
				// has a ContentPresenter with a null Content property in its template, as that will be caused to pick up
				// the parent ContentControl's Content property if one exists.
				CommandingHelpers.ClearBindingIfSet(newCommandAsUICommand, this, ContentControl.ContentProperty);

				CommandingHelpers.BindToLabelPropertyIfUnset(newCommandAsUICommand, this, LabelProperty);
				CommandingHelpers.BindToIconPropertyIfUnset(newCommandAsUICommand, this, IconProperty);
			}
		}
	}

	internal void UpdateTemplateSettings(double maxKeyboardAcceleratorTextWidth)
	{
		if (m_maxKeyboardAcceleratorTextWidth != maxKeyboardAcceleratorTextWidth)
		{
			m_maxKeyboardAcceleratorTextWidth = maxKeyboardAcceleratorTextWidth;

			var templateSettings = TemplateSettings;

			if (templateSettings == null)
			{
				templateSettings = new AppBarToggleButtonTemplateSettings();
				TemplateSettings = templateSettings;
			}

			templateSettings.KeyboardAcceleratorTextMinWidth = m_maxKeyboardAcceleratorTextWidth;
		}
	}

	private void UpdateToolTip()
	{
		if (m_ownsToolTip)
		{
			var useOverflowStyle = UseOverflowStyle;
			var keyboardAcceleratorText = KeyboardAcceleratorTextOverride;

			if (!useOverflowStyle && !string.IsNullOrWhiteSpace(keyboardAcceleratorText))
			{
				// If we're in the primary section of the app bar or command bar and have accelerator text,
				// then we should give ourselves a tool tip showing the label plus the accelerator text.
				var labelText = Label;

				var toolTipFormatString = DXamlCore.Current.GetLocalizedResourceString("KEYBOARD_ACCELERATOR_TEXT_TOOLTIP");

				SetValue(ToolTipService.ToolTipProperty, string.Format(CultureInfo.CurrentCulture, toolTipFormatString, labelText, keyboardAcceleratorText));
			}
			else
			{
				ClearValue(ToolTipService.ToolTipProperty);
			}

			// Setting the value of ToolTipService.ToolTip causes us to flag us as no longer owning the tool tip,
			// since that's the code path that an app setting the value will also take.
			// In order to ensure that we know that we still own the tool tip, we'll set this value to true here.
			m_ownsToolTip = true;
		}
	}

	internal Size GetKeyboardAcceleratorTextDesiredSize()
	{
		var desiredSize = new Size(0, 0);

		if (m_tpKeyboardAcceleratorTextLabel is { })
		{
			m_tpKeyboardAcceleratorTextLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			desiredSize = m_tpKeyboardAcceleratorTextLabel.DesiredSize;
			var margin = m_tpKeyboardAcceleratorTextLabel.Margin;

			desiredSize.Width -= (margin.Left + margin.Right);
			desiredSize.Height -= (margin.Top + margin.Bottom);
		}

		return desiredSize;
	}


	private string GetKeyboardAcceleratorText()
	{
		var keyboardAcceleratorText = GetValue(KeyboardAcceleratorTextOverrideProperty) as string;

		// If we have no keyboard accelerator text already provided by the app,
		// then we'll see if we can construct it ourselves based on keyboard accelerators
		// set on this item.  For example, if a keyboard accelerator with key "S" and modifier "Control"
		// is set, then we'll convert that into the keyboard accelerator text "Ctrl+S".
		if (string.IsNullOrWhiteSpace(keyboardAcceleratorText))
		{
			keyboardAcceleratorText = KeyboardAccelerator.GetStringRepresentationForUIElement(this);

			// If we were able to get a string representation from keyboard accelerators,
			// then we should now set that as the value of KeyboardAcceleratorText.
			if (!string.IsNullOrWhiteSpace(keyboardAcceleratorText))
			{
				PutKeyboardAcceleratorText(keyboardAcceleratorText);
			}
		}

		return keyboardAcceleratorText ?? string.Empty;
	}

	private void PutKeyboardAcceleratorText(string keyboardAcceleratorText)
	{
		SetValue(KeyboardAcceleratorTextOverrideProperty, keyboardAcceleratorText);
	}

	#endregion

	private void GetTemplatePart<T>(string name, out T? element) where T : class
	{
		element = GetTemplateChild(name) as T;
	}

	*/
}
