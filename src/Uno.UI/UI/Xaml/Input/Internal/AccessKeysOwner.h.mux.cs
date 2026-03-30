// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AccessKeysOwner.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Represents an object in the AccessKeys Scope tree that has a valid attached AccessKey
/// property and can invoke actions on the object (e.g. through automation providers).
/// </summary>
internal sealed class AKOwner
{
	private readonly AKAccessKey _accessKey;
	private readonly WeakReference<DependencyObject> _owningElement;

	/// <summary>
	/// Construct an AKOwner with a valid weak reference to a DependencyObject and a valid AKAccessKey.
	/// </summary>
	internal AKOwner(DependencyObject element, AKAccessKey accessKey)
	{
		_owningElement = new WeakReference<DependencyObject>(element);
		_accessKey = accessKey;
	}

	internal AKAccessKey GetAccessKey() => _accessKey;

	internal WeakReference<DependencyObject> GetElement() => _owningElement;

	internal bool Invoke()
	{
		bool eventHandled = false;

		if (_owningElement.TryGetTarget(out var element))
		{
			// TODO Uno: C++ checks element->IsActive() here. We skip that check for now
			// as it maps to the element being in the live tree, which is generally true
			// when we're processing access keys.
			eventHandled = AKOwnerEvents.InvokeEvent(element);

			if (!eventHandled)
			{
				eventHandled = KeyboardAutomationInvoker.InvokeAutomationAction(element);
			}
		}

		return eventHandled;
	}

	internal void ShowAccessKey(string pressedKeys)
	{
		if (_owningElement.TryGetTarget(out var element))
		{
			// Note: We can run into situations where we try to fire an element before it has been added to the tree.
			// In these scenarios, firing will fail because we have not added the request to the event manager for the
			// event to be fired. When the element enters the tree, another attempt will be made to fire the event successfully.
			AKOwnerEvents.RaiseAccessKeyShown(element, pressedKeys);
		}
	}

	internal void HideAccessKey()
	{
		if (_owningElement.TryGetTarget(out var element))
		{
			// We do not check IsActive on AccessKeyHidden because an element may
			// have already been removed from the Visual Tree and we want
			// to remove the associated KeyTip.
			AKOwnerEvents.RaiseAccessKeyHidden(element);
		}
	}
}
#endif
