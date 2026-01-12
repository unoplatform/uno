// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\Export\AccessKeysEvents.Specializations.h, tag winui3/release/1.5.3

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Static methods for raising access key events on elements.
/// </summary>
internal static class AKOwnerEvents
{
	/// <summary>
	/// Raises the AccessKeyInvoked event on the element.
	/// </summary>
	/// <param name="element">The element to raise the event on.</param>
	/// <returns>True if the event was handled, false otherwise.</returns>
	internal static bool InvokeEvent(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			return uiElement.RaiseAccessKeyInvoked();
		}
		else if (element is TextElement textElement)
		{
			return textElement.RaiseAccessKeyInvoked();
		}

		return false;
	}

	/// <summary>
	/// Raises the AccessKeyDisplayRequested event on the element (shows keytip).
	/// </summary>
	/// <param name="element">The element to raise the event on.</param>
	/// <param name="pressedKeys">The keys pressed so far.</param>
	internal static void RaiseAccessKeyShown(DependencyObject element, string pressedKeys)
	{
		if (element is UIElement uiElement)
		{
			uiElement.RaiseAccessKeyShown(pressedKeys);
		}
		else if (element is TextElement textElement)
		{
			textElement.RaiseAccessKeyShown(pressedKeys);
		}
	}

	/// <summary>
	/// Raises the AccessKeyDisplayDismissed event on the element (hides keytip).
	/// </summary>
	/// <param name="element">The element to raise the event on.</param>
	internal static void RaiseAccessKeyHidden(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			uiElement.RaiseAccessKeyHidden();
		}
		else if (element is TextElement textElement)
		{
			textElement.RaiseAccessKeyHidden();
		}
	}
}
