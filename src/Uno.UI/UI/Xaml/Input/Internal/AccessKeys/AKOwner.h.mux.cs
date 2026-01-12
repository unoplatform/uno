// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AccessKeysOwner.h, tag winui3/release/1.5.3

#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Represents an object or entity in the AccessKeys Scope tree that has a valid attached AccessKey
/// property and can invoke actions on the object (e.g. through automation providers).
/// </summary>
internal class AKOwner
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

	/// <summary>
	/// Construct an AKOwner from an element and access key string.
	/// </summary>
	internal AKOwner(DependencyObject element, string accessKeyString)
	{
		_owningElement = new WeakReference<DependencyObject>(element);
		_accessKey = new AKAccessKey(accessKeyString);
	}

	/// <summary>
	/// Gets the access key for this owner.
	/// </summary>
	internal AKAccessKey AccessKey => _accessKey;

	/// <summary>
	/// Gets a weak reference to the owning element.
	/// </summary>
	internal WeakReference<DependencyObject> Element => _owningElement;

	/// <summary>
	/// Invokes the access key action on the owning element.
	/// First raises AccessKeyInvoked event. If not handled, falls back to automation patterns.
	/// </summary>
	/// <returns>True if a valid action was found and invoked, false otherwise.</returns>
	internal bool Invoke()
	{
		if (!_owningElement.TryGetTarget(out var element))
		{
			return false;
		}

		// First try to raise the AccessKeyInvoked event
		bool eventHandled = AKOwnerEvents.InvokeEvent(element);

		if (!eventHandled)
		{
			// If the event was not handled, try to invoke via automation patterns
			eventHandled = Microsoft.UI.Xaml.Input.KeyboardAutomationInvoker.InvokeAutomationAction(element);
		}

		return eventHandled;
	}

	/// <summary>
	/// Shows the access key (keytip) for this owner.
	/// </summary>
	/// <param name="pressedKeys">The keys pressed so far (for partial match feedback).</param>
	internal void ShowAccessKey(string pressedKeys)
	{
		if (!_owningElement.TryGetTarget(out var element))
		{
			return;
		}

		// Note: We can run into situations where we try to fire an element before it has been added to the tree.
		// In these scenarios, firing will fail because we have not added the request to the event manager
		// for the event to be fired. When the element enters the tree, another attempt will be made to
		// fire the event successfully.
		AKOwnerEvents.RaiseAccessKeyShown(element, pressedKeys);
	}

	/// <summary>
	/// Hides the access key (keytip) for this owner.
	/// </summary>
	internal void HideAccessKey()
	{
		if (!_owningElement.TryGetTarget(out var element))
		{
			return;
		}

		// We do not check IsActive on AccessKeyHidden because an element may
		// have already been removed from the Visual Tree and we want
		// to remove the associated Keytip.
		AKOwnerEvents.RaiseAccessKeyHidden(element);
	}

	public override bool Equals(object? obj)
	{
		if (obj is not AKOwner other)
		{
			return false;
		}

		// Compare by element reference and access key
		if (!_owningElement.TryGetTarget(out var thisElement) ||
			!other._owningElement.TryGetTarget(out var otherElement))
		{
			return false;
		}

		return ReferenceEquals(thisElement, otherElement) && _accessKey == other._accessKey;
	}

	public override int GetHashCode()
	{
		return _accessKey.GetHashCode();
	}

	public static bool operator ==(AKOwner? left, AKOwner? right)
	{
		if (left is null)
		{
			return right is null;
		}
		return left.Equals(right);
	}

	public static bool operator !=(AKOwner? left, AKOwner? right) => !(left == right);
}
