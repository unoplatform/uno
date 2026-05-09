// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProvider.h, commit 5f9e851133b3

using System.Collections.Generic;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

// Given some elements and their animation context, ItemCollectionTransitionProvider
// animates them (Add, Delete and Move) and ensures the timing
// is correct (Delete -> Move -> Add).
// It's possible to customize the animations by inheriting from ItemCollectionTransitionProvider
// and overriding virtual/abstract members.
partial class ItemCollectionTransitionProvider
{
	// Current batch identifier; incremented each time CompositionTarget.Rendering starts a new batch.
	private uint _transitionsBatch;

	// Maps each keep-alive DispatcherTimer to the batch it is guarding.
	private readonly Dictionary<DispatcherTimer, uint> _keepAliveTimersMap = new();

	// All transitions queued in a given batch (animated + non-animated).
	private readonly Dictionary<uint, List<ItemCollectionTransition>> _transitionsMap = new();

	// Only the transitions in a given batch that will be animated.
	private readonly Dictionary<uint, List<ItemCollectionTransition>> _transitionsWithAnimationsMap = new();

	// Revoker for CompositionTarget.Rendering subscription.
	private readonly SerialDisposable _renderingRevoker = new();
}
