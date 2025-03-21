// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBarButtonHelpers.h, tag winui3/release/1.6.4, commit 262a901e09

//  Abstract:
//      Contains helper methods that centralize functionality common to both
//      AppBarButton and AppBarToggleButton, which we need since
//      the two types are unrelated to each other in terms of class hierarchy.

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Globalization;
using System.Reflection.Emit;
using System.Text.Json;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using static Uno.UI.FeatureConfiguration;
using System.ComponentModel;
using Uno.UI.Xaml.Input;
using System.Windows.Input;
using Uno.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

internal static class AppBarButtonHelpers<TButton>
	where TButton : Control, IAppBarButtonHelpersProvider, ICommandBarElement, ICommandBarOverflowElement
{
	internal static void OnBeforeApplyTemplate(TButton button)
	{
		if (button.m_isTemplateApplied)
		{
			button.StopAnimationForWidthAdjustments();
			button.m_isTemplateApplied = false;
		}
	}

	internal static void OnApplyTemplate(TButton button)
	{
		var keyboardAcceleratorTextLabel = button.GetTemplateChild("KeyboardAcceleratorTextLabel") as TextBlock;
		button.m_tpKeyboardAcceleratorTextLabel = keyboardAcceleratorTextLabel;

		button.m_isTemplateApplied = true;

		// Set the initial view state
		button.UpdateInternalStyles();
		button.UpdateVisualState();
	}

	internal static void CloseSubMenusOnPointerEntered(TButton button, ISubMenuOwner pMenuToLeaveOpen)
	{
		bool isInOverflow = false;
		isInOverflow = button.IsInOverflow;

		if (isInOverflow)
		{
			// If there are other buttons that have open sub-menus, then we should
			// close those on a delay, since they no longer have mouse-over.
			var parentCommandBar = CommandBar.FindParentCommandBarForElement(button);

			if (parentCommandBar is not null)
			{
				parentCommandBar.CloseSubMenus(pMenuToLeaveOpen, true /* closeOnDelay */);
			}
		}
	}

	internal static void OnPropertyChanged(TButton button, DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == button.GetIsCompactDependencyProperty() ||
			args.Property == button.GetUseOverflowStyleDependencyProperty() ||
			args.Property == button.GetLabelPositionDependencyProperty())
		{
			button.UpdateInternalStyles();
			button.UpdateVisualState();
		}

		if (args.Property == ToolTipService.ToolTipProperty)
		{
			var boxedToolTipValue = button.GetValue(ToolTipService.ToolTipProperty);

			if (boxedToolTipValue is not null)
			{
				button.m_ownsToolTip = false;
			}
			else
			{
				button.m_ownsToolTip = true;
			}
		}
	}

	internal static void ChangeCommonVisualStates(TButton button, bool useTransitions)
	{
		bool isCompact = false;
		bool useOverflowStyle = false;
		bool isKeyboardPresent = false;

		useOverflowStyle = button.UseOverflowStyle;
		isCompact = button.IsCompact;
		var effectiveLabelPosition = button.GetEffectiveLabelPosition();

		// We only care about finding if we have a keyboard if we also have a menu item with keyboard accelerator text,
		// since if we don't have any menu items with keyboard accelerator text, we won't be showing any that text anyway.
		if (button.m_isWithKeyboardAcceleratorText)
		{
			isKeyboardPresent = DXamlCore.Current.IsKeyboardPresent;
		}

		if (!useOverflowStyle)
		{
			if (effectiveLabelPosition == CommandBarDefaultLabelPosition.Right)
			{
				button.GoToState(useTransitions, "LabelOnRight");
			}
			else if (effectiveLabelPosition == CommandBarDefaultLabelPosition.Collapsed)
			{
				button.GoToState(useTransitions, "LabelCollapsed");
			}
			else if (isCompact)
			{
				button.GoToState(useTransitions, "Compact");
			}
			else
			{
				button.GoToState(useTransitions, "FullSize");
			}
		}

		button.GoToState(useTransitions, "InputModeDefault");
		if (button.m_inputDeviceTypeUsedToOpenOverflow == InputDeviceType.Touch)
		{
			button.GoToState(useTransitions, "TouchInputMode");
		}
		else if (button.m_inputDeviceTypeUsedToOpenOverflow == InputDeviceType.GamepadOrRemote)
		{
			button.GoToState(useTransitions, "GameControllerInputMode");
		}

		// We'll make the keyboard accelerator text visible if any element in the overflow has keyboard accelerator text,
		// as this causes the margin to be applied which reserves space, ensuring that keyboard accelerator text
		// in one button won't be at the same horizontal position as label text in another button.
		if (button.m_isWithKeyboardAcceleratorText && isKeyboardPresent && useOverflowStyle)
		{
			button.GoToState(useTransitions, "KeyboardAcceleratorTextVisible");
		}
		else
		{
			button.GoToState(useTransitions, "KeyboardAcceleratorTextCollapsed");
		}
	}

	internal static void OnCommandChanged(TButton button, object oldValue, object newValue)
	{
		if (oldValue is not null)
		{
			var oldCommandAsUICommand = oldValue as XamlUICommand;

			if (oldCommandAsUICommand is not null)
			{
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, button, button.GetLabelDependencyProperty());
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, button, button.GetIconDependencyProperty());
			}
		}

		if (newValue is not null)
		{
			var newCommandAsUICommand = newValue as XamlUICommand;

			if (newCommandAsUICommand is not null)
			{
				// The call to ButtonBase.OnCommandChanged() will have set the Content property, which we don't want -
				// it's not used anywhere in AppBar*Button, and having it be set can cause problems if an AppBarButton
				// has a ContentPresenter with a null Content property in its template, as that will be caused to pick up
				// the parent ContentControl's Content property if one exists.
				CommandingHelpers.ClearBindingIfSet(newCommandAsUICommand, button, ContentControl.ContentProperty);

				CommandingHelpers.BindToLabelPropertyIfUnset(newCommandAsUICommand, button, button.GetLabelDependencyProperty());
				CommandingHelpers.BindToIconPropertyIfUnset(newCommandAsUICommand, button, button.GetIconDependencyProperty());
			}
		}
	}

	internal static void UpdateTemplateSettings<TTemplateSettings>(TButton button, double maxKeyboardAcceleratorTextWidth)
		where TTemplateSettings : IAppBarButtonTemplateSettings, new()
	{
		if (button.m_maxKeyboardAcceleratorTextWidth != maxKeyboardAcceleratorTextWidth)
		{
			button.m_maxKeyboardAcceleratorTextWidth = maxKeyboardAcceleratorTextWidth;

			var templateSettings = button.TemplateSettings;

			if (templateSettings is null)
			{
				var templateSettingsImplementation = new TTemplateSettings();
				button.TemplateSettings = templateSettingsImplementation;
				templateSettings = templateSettingsImplementation;
			}

			templateSettings.KeyboardAcceleratorTextMinWidth = button.m_maxKeyboardAcceleratorTextWidth;
		}
	}

	internal static void UpdateToolTip(TButton button)
	{
		if (button.m_ownsToolTip)
		{
			var useOverflowStyle = button.UseOverflowStyle;
			var keyboardAcceleratorText = button.KeyboardAcceleratorTextOverride;

			if (!useOverflowStyle && !string.IsNullOrEmpty(keyboardAcceleratorText))
			{
				// If we're in the primary section of the app bar or command bar and have accelerator text,
				// then we should give ourselves a tool tip showing the label plus the accelerator text.
				string labelText = button.Label;

				string toolTipFormatString = DXamlCore.Current.GetLocalizedResourceString("KEYBOARD_ACCELERATOR_TEXT_TOOLTIP");

				// format is %s (%s)
				var toolTipString = StringUtil.Swprintf_s(toolTipFormatString, labelText, keyboardAcceleratorText);

				button.SetValue(ToolTipService.ToolTipProperty, toolTipString);
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

	internal static Size GetKeyboardAcceleratorTextDesiredSize(TButton button)
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

	internal static string GetKeyboardAcceleratorText(TButton button)
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

	internal static void PutKeyboardAcceleratorText(TButton button, string value) =>
		button.SetValue(button.GetKeyboardAcceleratorTextDependencyProperty(), value);

	internal static bool IsKeyboardFocusable(Control button)
	{
		// If an AppBarButton is in a CommandBar, then it's keyboard focusable even if it's not a tab stop,
		// so we want to exclude that from the conditions we check in uielement.cpp.
		FrameworkElement coreButtonType = button;

		return coreButtonType.IsLoaded && // TODO:Uno: Should be "coreButtonType.IsActive() &&"
			coreButtonType.IsVisible() &&
			(coreButtonType.IsEnabledInternal() || coreButtonType.AllowFocusWhenDisabled) &&
			coreButtonType.AreAllAncestorsVisible();
	}
}
