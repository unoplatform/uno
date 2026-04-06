// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProgress.h, commit 5f9e851133b3

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionProgress
{
	// Original C++ uses weak_ref<ItemCollectionTransition>.
	// In .NET the GC handles circular references, so a plain reference is used here.
	// ItemCollectionTransition holds a strong ref to this; using strong ref back does not leak in .NET.
	private readonly ItemCollectionTransition _transition;

	/// <summary>
	/// Gets the ItemCollectionTransition whose animations are in progress.
	/// </summary>
	public ItemCollectionTransition Transition => _transition;
}
