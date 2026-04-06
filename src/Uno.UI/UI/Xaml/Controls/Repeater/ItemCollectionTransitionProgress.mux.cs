// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProgress.cpp, commit 5f9e851133b3

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionProgress
{
	internal ItemCollectionTransitionProgress(ItemCollectionTransition transition)
	{
		_transition = transition;
	}

	/// <summary>
	/// Gets the UIElement to be animated.
	/// </summary>
	public UIElement Element
	{
		get
		{
			if (_transition is not null)
			{
				return _transition.ElementInternal;
			}

			return null;
		}
	}

	/// <summary>
	/// Raises the TransitionCompleted event on the ItemCollectionTransitionProvider
	/// that owns the associated ItemCollectionTransition.
	/// </summary>
	public void Complete()
	{
		if (_transition is not null)
		{
			var transitionProvider = _transition.OwningProvider();
			transitionProvider?.NotifyTransitionCompleted(_transition);
		}
	}
}
