// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionCompletedEventArgs.cpp, commit 5f9e851133b3

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionCompletedEventArgs
{
	internal ItemCollectionTransitionCompletedEventArgs(ItemCollectionTransition transition)
	{
		_transition = transition;
	}

	/// <summary>
	/// Gets the UIElement that was transitioned.
	/// </summary>
	public UIElement Element => _transition?.ElementInternal;
}
