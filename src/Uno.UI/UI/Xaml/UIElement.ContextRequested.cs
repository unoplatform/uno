// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: uielement.cpp, lines 12515-12623

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
#if __SKIA__
using Microsoft.UI.Xaml.Internal;
#endif
using Uno.UI.Text;

namespace Microsoft.UI.Xaml;

partial class UIElement
{
	/// <summary>
	/// If the UIElement has the ContextFlyout property set, show the flyout and set the event to be "Handled". Otherwise, no-op.
	/// </summary>
	/// <remarks>
	/// Ported from WinUI uielement.cpp:12516-12522
	/// </remarks>
	internal static void OnContextRequested(DependencyObject sender, ContextRequestedEventArgs args)
	{
		OnContextRequestedCore(sender, sender, args);
	}

	/// <summary>
	/// In list-based circumstances, we want to display a list control's ContextFlyout on a list item.
	/// Having a separate contextFlyoutObject allows us to separate the object that owns the ContextFlyout
	/// from the object that we want to display it on.
	/// </summary>
	/// <remarks>
	/// Ported from WinUI uielement.cpp:12527-12613
	/// </remarks>
	internal static void OnContextRequestedCore(
		DependencyObject sender,
		DependencyObject contextFlyoutObject,
		ContextRequestedEventArgs args)
	{
		if (sender is not UIElement uiElement)
		{
			return;
		}

		if (contextFlyoutObject is not UIElement contextFlyoutElement)
		{
			return;
		}

		if (args.Handled)
		{
			return;
		}

		var flyout = contextFlyoutElement.ContextFlyout;
		if (flyout == null)
		{
			return;
		}

		var frameworkElement = uiElement as FrameworkElement;
		if (frameworkElement == null)
		{
			return;
		}

		var gotPoint = args.TryGetPosition(uiElement, out var point);

		// Check for text controls
		var isTextControl = TextCore.IsTextControl(sender);
		var isTextEditControl =
			sender is TextBox ||
			sender is RichEditBox ||
			sender is PasswordBox;

		if (gotPoint || isTextEditControl)
		{
			// If we're here but don't yet have a point, then we're a text edit control.
			// We'll set the point at which to open the flyout to be the point retrieved
			// from the text box based on the selection start or end position (depending on
			// LTR vs. RTL).
			if (!gotPoint)
			{
#if __SKIA__
				if (sender is TextBox textBox)
				{
					var textPosition = textBox.GetContextMenuShowPosition();
					if (textPosition.HasValue)
					{
						point = textPosition.Value;
						gotPoint = true;
					}
				}
#endif
				// TODO: Support RichEditBox
			}

			if (isTextControl)
			{
				// If we are using the default text control ContextFlyout and TextSelection is not enabled, don't show the flyout
				if (IsUsingDefaultContextFlyout(sender))
				{
					if (!TextCore.IsTextSelectionEnabled(sender))
					{
						return;
					}
				}
			}

			if (gotPoint)
			{
#if __SKIA__
				if (isTextControl || isTextEditControl)
				{
					TextControlFlyoutHelper.ShowAt(flyout, frameworkElement, point, FlyoutShowMode.Standard);
				}
				else
#endif
				{
					flyout.ShowAt(frameworkElement, new FlyoutShowOptions
					{
						Position = point,
						ShowMode = FlyoutShowMode.Standard
					});
				}
			}
			else
			{
#if __SKIA__
				if (isTextControl || isTextEditControl)
				{
					TextControlFlyoutHelper.ShowAt(flyout, frameworkElement, default, FlyoutShowMode.Standard);
				}
				else
#endif
				{
					flyout.ShowAt(frameworkElement);
				}
			}
		}
		else
		{
			if (isTextControl)
			{
				if (IsUsingDefaultContextFlyout(sender))
				{
					if (!TextCore.IsTextSelectionEnabled(sender))
					{
						return;
					}
				}
			}

#if __SKIA__
			if (isTextControl || isTextEditControl)
			{
				TextControlFlyoutHelper.ShowAt(flyout, frameworkElement, default, FlyoutShowMode.Standard);
			}
			else
#endif
			{
				flyout.ShowAt(frameworkElement);
			}
		}

		args.Handled = true;
	}

	private static bool IsUsingDefaultContextFlyout(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			return uiElement.ReadLocalValue(ContextFlyoutProperty) == DependencyProperty.UnsetValue;
		}
		return true;
	}
}
