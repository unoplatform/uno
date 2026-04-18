// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProvider.properties.h + ItemCollectionTransitionProvider.properties.cpp, commit 5f9e851133b3

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionProvider
{
	/// <summary>
	/// Occurs when the ItemCollectionTransitionProgress.Complete method is called to indicate
	/// that a transition on a specific UIElement has completed.
	/// </summary>
	public event TypedEventHandler<ItemCollectionTransitionProvider, ItemCollectionTransitionCompletedEventArgs> TransitionCompleted;
}
