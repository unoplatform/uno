// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\uielement.cpp, tag winui3/release/1.5.3

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml;

partial class UIElement
{
	// Private event backing fields for access key events
	private TypedEventHandler<UIElement, AccessKeyInvokedEventArgs>? _accessKeyInvoked;
	private TypedEventHandler<UIElement, AccessKeyDisplayRequestedEventArgs>? _accessKeyDisplayRequested;
	private TypedEventHandler<UIElement, AccessKeyDisplayDismissedEventArgs>? _accessKeyDisplayDismissed;

	/// <summary>
	/// Raises the AccessKeyInvoked event.
	/// </summary>
	/// <returns>True if the event was handled, false otherwise.</returns>
	internal bool RaiseAccessKeyInvoked()
	{
		if (_accessKeyInvoked is null)
		{
			return false;
		}

		var args = new AccessKeyInvokedEventArgs();
		_accessKeyInvoked.Invoke(this, args);
		return args.Handled;
	}

	/// <summary>
	/// Raises the AccessKeyDisplayRequested event (shows keytip).
	/// </summary>
	/// <param name="pressedKeys">The keys pressed so far.</param>
	internal void RaiseAccessKeyShown(string pressedKeys)
	{
		// TODO UNO: KeyTipManager.ShowAutoKeyTipForElement(this, pressedKeys);
		// KeyTip visual display is deferred to a future implementation.

		if (_accessKeyDisplayRequested is null)
		{
			return;
		}

		var args = new AccessKeyDisplayRequestedEventArgs();
		args.PressedKeys = pressedKeys;
		_accessKeyDisplayRequested.Invoke(this, args);
	}

	/// <summary>
	/// Raises the AccessKeyDisplayDismissed event (hides keytip).
	/// </summary>
	internal void RaiseAccessKeyHidden()
	{
		// TODO UNO: KeyTipManager.HideAutoKeyTipForElement(this);
		// KeyTip visual display is deferred to a future implementation.

		if (_accessKeyDisplayDismissed is null)
		{
			return;
		}

		var args = new AccessKeyDisplayDismissedEventArgs();
		_accessKeyDisplayDismissed.Invoke(this, args);
	}

	// The public events are declared in UIElement.cs (generated), but we need to provide
	// actual implementation. Since the generated code has NotImplemented attributes,
	// we implement the events with a new keyword to shadow them.
	// This is handled by making UIElement partial and providing the proper implementation.

	/// <summary>
	/// Adds a handler for the AccessKeyInvoked event.
	/// </summary>
	internal void AddAccessKeyInvokedHandler(TypedEventHandler<UIElement, AccessKeyInvokedEventArgs> handler)
	{
		_accessKeyInvoked += handler;
	}

	/// <summary>
	/// Removes a handler for the AccessKeyInvoked event.
	/// </summary>
	internal void RemoveAccessKeyInvokedHandler(TypedEventHandler<UIElement, AccessKeyInvokedEventArgs> handler)
	{
		_accessKeyInvoked -= handler;
	}

	/// <summary>
	/// Adds a handler for the AccessKeyDisplayRequested event.
	/// </summary>
	internal void AddAccessKeyDisplayRequestedHandler(TypedEventHandler<UIElement, AccessKeyDisplayRequestedEventArgs> handler)
	{
		_accessKeyDisplayRequested += handler;
	}

	/// <summary>
	/// Removes a handler for the AccessKeyDisplayRequested event.
	/// </summary>
	internal void RemoveAccessKeyDisplayRequestedHandler(TypedEventHandler<UIElement, AccessKeyDisplayRequestedEventArgs> handler)
	{
		_accessKeyDisplayRequested -= handler;
	}

	/// <summary>
	/// Adds a handler for the AccessKeyDisplayDismissed event.
	/// </summary>
	internal void AddAccessKeyDisplayDismissedHandler(TypedEventHandler<UIElement, AccessKeyDisplayDismissedEventArgs> handler)
	{
		_accessKeyDisplayDismissed += handler;
	}

	/// <summary>
	/// Removes a handler for the AccessKeyDisplayDismissed event.
	/// </summary>
	internal void RemoveAccessKeyDisplayDismissedHandler(TypedEventHandler<UIElement, AccessKeyDisplayDismissedEventArgs> handler)
	{
		_accessKeyDisplayDismissed -= handler;
	}
}
