// MUX Reference TransitionManager.cpp, tag winui3/release/1.8.1

using System.Collections.Specialized;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class TransitionManager
{
	public TransitionManager(ItemsRepeater owner)
	{
		// ItemsRepeater is not fully constructed yet. Don't interact with it.
		m_owner = owner;
	}

	public void OnTransitionProviderChanged(ItemCollectionTransitionProvider newTransitionProvider)
	{
		// While an element is hiding, we have ownership of it. We need
		// to know when its animation completes so that we give it back
		// to the view generator.
		if (m_transitionProvider != null)
		{
			MUX_ASSERT(m_transitionCompletedRevoker.Disposable != null);
		}

		m_transitionCompletedRevoker.Disposable = null;

		m_transitionProvider = newTransitionProvider;

		if (newTransitionProvider != null)
		{
			newTransitionProvider.TransitionCompleted += OnTransitionProviderTransitionCompleted;
			m_transitionCompletedRevoker.Disposable = Disposable.Create(() =>
				newTransitionProvider.TransitionCompleted -= OnTransitionProviderTransitionCompleted);
		}
	}

	public void OnLayoutChanging()
	{
		m_hasRecordedLayoutTransitions = true;
	}

	public void OnItemsSourceChanged(object source, NotifyCollectionChangedEventArgs args)
	{
		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add:
				m_hasRecordedAdds = true;
				break;
			case NotifyCollectionChangedAction.Remove:
				m_hasRecordedRemoves = true;
				break;
			case NotifyCollectionChangedAction.Replace:
				m_hasRecordedAdds = true;
				m_hasRecordedRemoves = true;
				break;
			case NotifyCollectionChangedAction.Reset:
				m_hasRecordedResets = true;
				break;
		}
	}

	public void OnElementPrepared(UIElement element)
	{
		if (m_transitionProvider != null)
		{
			var triggers = (ItemCollectionTransitionTriggers)0;
			if (m_hasRecordedAdds) triggers |= ItemCollectionTransitionTriggers.CollectionChangeAdd;
			if (m_hasRecordedResets) triggers |= ItemCollectionTransitionTriggers.CollectionChangeReset;
			if (m_hasRecordedLayoutTransitions) triggers |= ItemCollectionTransitionTriggers.LayoutTransition;

			if (triggers != (ItemCollectionTransitionTriggers)0)
			{
				m_transitionProvider.QueueTransition(
					new ItemCollectionTransition(
						m_transitionProvider,
						element,
						ItemCollectionTransitionOperation.Add,
						triggers));
			}
		}
	}

	public bool ClearElement(UIElement element)
	{
		bool canClear = false;

		if (m_transitionProvider != null)
		{
			var triggers = (ItemCollectionTransitionTriggers)0;
			if (m_hasRecordedRemoves) triggers |= ItemCollectionTransitionTriggers.CollectionChangeRemove;
			if (m_hasRecordedResets) triggers |= ItemCollectionTransitionTriggers.CollectionChangeReset;

			var transition = new ItemCollectionTransition(
				m_transitionProvider,
				element,
				ItemCollectionTransitionOperation.Remove,
				triggers);

			canClear =
				triggers != (ItemCollectionTransitionTriggers)0 &&
				m_transitionProvider.ShouldAnimate(transition);

			if (canClear)
			{
				m_transitionProvider.QueueTransition(transition);
			}
		}

		return canClear;
	}

	public void OnElementBoundsChanged(UIElement element, Rect oldBounds, Rect newBounds)
	{
		if (m_transitionProvider != null)
		{
			var triggers = (ItemCollectionTransitionTriggers)0;
			if (m_hasRecordedAdds) triggers |= ItemCollectionTransitionTriggers.CollectionChangeAdd;
			if (m_hasRecordedRemoves) triggers |= ItemCollectionTransitionTriggers.CollectionChangeRemove;
			if (m_hasRecordedResets) triggers |= ItemCollectionTransitionTriggers.CollectionChangeReset;
			if (m_hasRecordedLayoutTransitions) triggers |= ItemCollectionTransitionTriggers.LayoutTransition;

			// A bounds change can occur during initial layout or when resizing the owning control,
			// which won't trigger an explicit layout transition but should still be treated as one.
			if (triggers == (ItemCollectionTransitionTriggers)0)
			{
				triggers = ItemCollectionTransitionTriggers.LayoutTransition;
			}

			m_transitionProvider.QueueTransition(
				new ItemCollectionTransition(
					m_transitionProvider,
					element,
					triggers,
					oldBounds,
					newBounds));
		}
	}

	public void OnOwnerArranged()
	{
		m_hasRecordedAdds = false;
		m_hasRecordedRemoves = false;
		m_hasRecordedResets = false;
		m_hasRecordedLayoutTransitions = false;
	}

	private void OnTransitionProviderTransitionCompleted(ItemCollectionTransitionProvider sender, ItemCollectionTransitionCompletedEventArgs args)
	{
		if (args.Transition.Operation == ItemCollectionTransitionOperation.Remove)
		{
			var element = args.Element;

			if (CachedVisualTreeHelpers.GetParent(element) == (DependencyObject)m_owner)
			{
				m_owner.ViewManager.ClearElementToElementFactory(element);

				// Invalidate arrange so that repeater can arrange this element off-screen.
				m_owner.InvalidateArrange();
			}
		}
	}
}
