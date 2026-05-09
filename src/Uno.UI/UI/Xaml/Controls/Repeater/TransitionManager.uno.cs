namespace Microsoft.UI.Xaml.Controls;

partial class TransitionManager
{
	// Uno-specific: .NET event delegates are strong references; ItemsRepeater detaches on Unload
	// and reattaches on Load to avoid keeping a shared ItemCollectionTransitionProvider's
	// subscribers alive past the repeater's lifetime. We fully clear m_transitionProvider here so
	// the OnTransitionProviderChanged invariant (provider != null ⇒ revoker != null) holds even
	// if the ItemTransitionProvider DP changes while the repeater is unloaded.
	internal void DetachFromProvider()
	{
		if (m_transitionProvider is null)
		{
			return;
		}

		m_transitionCompletedRevoker.Disposable = null;
		m_transitionProvider = null;
	}

	// Uno-specific: ItemsRepeater computes the effective provider (explicit DP, or layout-default
	// when m_ownsTransitionProvider is true) and passes it in. Passing null is valid and matches the
	// "no provider" state.
	internal void ReattachToProvider(ItemCollectionTransitionProvider provider)
	{
		if (m_transitionProvider is not null
			|| provider is null)
		{
			return;
		}

		OnTransitionProviderChanged(provider);
	}
}
