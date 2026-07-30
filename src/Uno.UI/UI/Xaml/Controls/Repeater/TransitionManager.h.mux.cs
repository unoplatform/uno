// MUX Reference TransitionManager.h, tag winui3/release/1.8.1

using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

// Internal component that contains all
// the animation related logic for ItemsRepeater.
partial class TransitionManager
{
	private readonly ItemsRepeater m_owner;
	private ItemCollectionTransitionProvider m_transitionProvider;

	// We infer the animation context
	// from heuristics like whether or not
	// we observed a collection change or a
	// layout transition during the current
	// tick.
	private bool m_hasRecordedAdds;
	private bool m_hasRecordedRemoves;
	private bool m_hasRecordedResets;
	private bool m_hasRecordedLayoutTransitions;

	// Event revoker
	private readonly SerialDisposable m_transitionCompletedRevoker = new SerialDisposable();
}
