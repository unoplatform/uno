// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AKCommon.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml.Input;

internal static class AccessKeys
{
	internal const char ALT = (char)18;
	internal const char ESC = (char)27;

	/// <summary>
	/// Return params from an invoke attempt on an AccessKey scope.
	/// </summary>
	internal struct AKInvokeReturnParams
	{
		/// <summary>When we've found an element within the Scope to invoke, we set this to true.</summary>
		internal bool InvokeAttempted;
		/// <summary>When we try to invoke the element, but we were unable to find a pattern, we set this to false.</summary>
		internal bool InvokeFoundValidPattern;
		/// <summary>The element we are trying to invoke.</summary>
		internal WeakReference<DependencyObject>? InvokedElement;
	}

	/// <summary>
	/// Gets the AccessKey string from a UIElement or TextElement.
	/// </summary>
	internal static string GetAccessKey(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			return (string)uiElement.GetValue(UIElement.AccessKeyProperty) ?? string.Empty;
		}
		else if (element is Documents.TextElement textElement)
		{
			return (string)textElement.GetValue(Documents.TextElement.AccessKeyProperty) ?? string.Empty;
		}

		return string.Empty;
	}

	/// <summary>
	/// Returns true if the element type is valid for AccessKey ownership.
	/// </summary>
	internal static bool IsValidAKOwnerType(DependencyObject element)
	{
		return element is Controls.Primitives.FlyoutBase || element is UIElement || element is Documents.TextElement;
	}

	/// <summary>
	/// Returns true if the element has ExitDisplayModeOnAccessKeyInvoked set to true.
	/// </summary>
	internal static bool DismissOnInvoked(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			return uiElement.ExitDisplayModeOnAccessKeyInvoked;
		}
		else if (element is Documents.TextElement textElement)
		{
			return textElement.ExitDisplayModeOnAccessKeyInvoked;
		}

		return false;
	}

	internal static bool IsAccessKeyScope(DependencyObject element)
	{
		if (element is FlyoutBase)
		{
			// Any flyout must always owns its Scope.
			// Besides, moving the property IsAccessKeyScope to CDependencyObject is expensive
			return true;
		}

		DependencyProperty? index = null;

		if (element is UIElement)
		{
			index = UIElement.IsAccessKeyScopeProperty;
		}

		else if (element is TextElement)
		{
			index = TextElement.IsAccessKeyScopeProperty;
		}

		if (index is null)
		{
			return false;
		}

		return (bool)element.GetValue(index);
	}

}
#endif
