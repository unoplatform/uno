using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class TransitionManager
{
	// Uno-specific: .NET event delegates are strong references; ItemsRepeater detaches on Unload and reattaches on Load to avoid keeping a shared ItemCollectionTransitionProvider's subscribers alive past the repeater's lifetime.
	internal void DetachFromProvider()
	{
		m_transitionCompletedRevoker.Disposable = null;
	}

	internal void ReattachToProvider()
	{
		if (m_transitionProvider is { } provider
			&& m_transitionCompletedRevoker.Disposable is null)
		{
			provider.TransitionCompleted += OnTransitionProviderTransitionCompleted;
			m_transitionCompletedRevoker.Disposable = Disposable.Create(() =>
				provider.TransitionCompleted -= OnTransitionProviderTransitionCompleted);
		}
	}
}
