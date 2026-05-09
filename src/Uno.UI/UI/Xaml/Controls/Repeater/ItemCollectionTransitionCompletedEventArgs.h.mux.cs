// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionCompletedEventArgs.h, commit 5f9e851133b3

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionCompletedEventArgs
{
	private readonly ItemCollectionTransition _transition;

	/// <summary>
	/// Gets the ItemCollectionTransition whose animations have completed.
	/// </summary>
	public ItemCollectionTransition Transition => _transition;
}
