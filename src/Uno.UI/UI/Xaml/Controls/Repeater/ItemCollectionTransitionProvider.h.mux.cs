// MUX Reference ItemCollectionTransitionProvider.h, tag winui3/release/1.6-stable

using System.Collections.Generic;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

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
