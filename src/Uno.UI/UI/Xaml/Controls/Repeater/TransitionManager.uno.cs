using System;
using Uno.UI.Helpers;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class TransitionManager
{
	// Uno-specific: like WinUI's raw-'this' delegate, a provider that outlives the repeater (e.g. a shared one) must not
	// keep it alive, even if the repeater never loads and so never unloads to detach. The handler holds this manager,
	// and so its repeater, weakly. Only this manager references the returned subscription, whose finalizer removes the
	// handler once the repeater is collected. Static so the handler cannot capture the manager.
	private static IDisposable SubscribeToTransitionCompleted(ItemCollectionTransitionProvider provider, TransitionManager manager)
	{
		var weakManager = new WeakReference<TransitionManager>(manager);
		TypedEventHandler<ItemCollectionTransitionProvider, ItemCollectionTransitionCompletedEventArgs> handler = (sender, args) =>
		{
			if (weakManager.TryGetTarget(out var target))
			{
				target.OnTransitionProviderTransitionCompleted(sender, args);
			}
		};

		provider.TransitionCompleted += handler;
		return new WeakSourceSubscription<ItemCollectionTransitionProvider, TypedEventHandler<ItemCollectionTransitionProvider, ItemCollectionTransitionCompletedEventArgs>>(
			provider,
			handler,
			static (p, h) => p.TransitionCompleted -= h,
			onDisposed: null,
			typeof(TransitionManager),
			"a TransitionCompleted subscription");
	}

	// Uno-specific: ItemsRepeater detaches on Unload and reattaches on Load, which removes the handler of an unloaded
	// repeater right away rather than once it's collected. We fully clear m_transitionProvider here so
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
