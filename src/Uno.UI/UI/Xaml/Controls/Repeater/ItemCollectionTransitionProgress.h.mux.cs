// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProgress.h, commit 5f9e851133b3

#nullable enable

using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionProgress
{
	// Port of C++ `winrt::weak_ref<winrt::ItemCollectionTransition>`. ManagedWeakReference is Uno's
	// equivalent of winrt::weak_ref and is used throughout MUX ports for the same weak-reference
	// semantics; rented from WeakReferencePool.RentWeakReference(owner, target) in the constructor.
	private readonly ManagedWeakReference _transition;

	/// <summary>
	/// Gets the ItemCollectionTransition whose animations are in progress.
	/// </summary>
	public ItemCollectionTransition? Transition
		=> _transition.TryGetTarget<ItemCollectionTransition>(out var transition) ? transition : null;
}
