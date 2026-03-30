// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AccessKeysEvents.h, tag winui3/release/1.4.3, commit 685d2bf
// MUX Reference dxaml\xcp\components\AccessKeys\Export\AccessKeysEvents.Specializations.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Static event dispatch for AccessKey events on UIElement and TextElement.
/// </summary>
internal static class AKOwnerEvents
{
	/// <summary>
	/// Raises the AccessKeyInvoked event on the element and returns whether it was handled.
	/// </summary>
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
	/// Raises the AccessKeyDisplayRequested event on the element.
	/// </summary>
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
	/// Raises the AccessKeyDisplayDismissed event on the element.
	/// </summary>
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
#endif
