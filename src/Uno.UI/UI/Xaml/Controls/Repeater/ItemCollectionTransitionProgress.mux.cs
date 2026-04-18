// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProgress.cpp, commit 5f9e851133b3

#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionProgress
{
	internal ItemCollectionTransitionProgress(ItemCollectionTransition transition)
	{
		_transition = WeakReferencePool.RentWeakReference(this, transition);
	}

	/// <summary>
	/// Gets the UIElement to be animated.
	/// </summary>
	public UIElement? Element
		=> _transition.TryGetTarget<ItemCollectionTransition>(out var transition)
			? transition.ElementInternal
			: null;

	/// <summary>
	/// Raises the TransitionCompleted event on the ItemCollectionTransitionProvider
	/// that owns the associated ItemCollectionTransition.
	/// </summary>
	public void Complete()
	{
		if (_transition.TryGetTarget<ItemCollectionTransition>(out var transition))
		{
			var transitionProvider = transition.OwningProvider();
			transitionProvider?.NotifyTransitionCompleted(transition);
		}
	}
}
