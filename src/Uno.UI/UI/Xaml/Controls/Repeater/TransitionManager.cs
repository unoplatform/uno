// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// TransitionManager.cpp/.h, tag winui3/release/1.8.4

using System;
using System.Collections.Specialized;
using Uno.Disposables;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Internal class that manages transitions for ItemsRepeater.
	/// Replaces the old AnimationManager.
	/// </summary>
	internal partial class TransitionManager
	{
		private readonly ItemsRepeater m_owner;
		private ItemCollectionTransitionProvider m_transitionProvider;
		private IDisposable m_transitionProviderTransitionCompleted;

		// We infer the animation context
		// from heuristics like whether or not
		// we observed a collection change or a
		// layout transition during the current tick.
		private bool m_hasRecordedAdds;
		private bool m_hasRecordedRemoves;
		private bool m_hasRecordedResets;
		private bool m_hasRecordedLayoutTransitions;

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
			m_transitionProviderTransitionCompleted?.Dispose();
			m_transitionProviderTransitionCompleted = null;

			m_transitionProvider = newTransitionProvider;

			if (newTransitionProvider != null)
			{
				newTransitionProvider.TransitionCompleted += OnTransitionProviderTransitionCompleted;
				m_transitionProviderTransitionCompleted = Disposable.Create(() =>
				{
					newTransitionProvider.TransitionCompleted -= OnTransitionProviderTransitionCompleted;
				});
			}
		}

		public void OnLayoutChanging()
		{
			m_hasRecordedLayoutTransitions = true;
		}

		public void OnItemsSourceChanged(object sender, NotifyCollectionChangedEventArgs args)
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
				var triggers = GetTriggers();

				if (triggers != 0)
				{
					var transition = new ItemCollectionTransition(
						m_transitionProvider,
						element,
						ItemCollectionTransitionOperation.Add,
						triggers);

					m_transitionProvider.QueueTransition(transition);
				}
			}
		}

		public bool ClearElement(UIElement element)
		{
			bool canClear = false;

			if (m_transitionProvider != null)
			{
				var triggers = GetRemoveTriggers();

				if (triggers != 0)
				{
					var transition = new ItemCollectionTransition(
						m_transitionProvider,
						element,
						ItemCollectionTransitionOperation.Remove,
						triggers);

					// Ask provider if it wants to animate this
					if (m_transitionProvider.ShouldAnimate(transition))
					{
						m_transitionProvider.QueueTransition(transition);
						canClear = true;
					}
				}
			}

			return canClear;
		}

		public void OnElementBoundsChanged(UIElement element, Rect oldBounds, Rect newBounds)
		{
			if (m_transitionProvider != null)
			{
				var triggers = GetTriggers();

				if (triggers != 0)
				{
					var transition = new ItemCollectionTransition(
						m_transitionProvider,
						element,
						triggers,
						oldBounds,
						newBounds);

					m_transitionProvider.QueueTransition(transition);
				}
			}
		}

		public void OnOwnerArranged()
		{
			m_hasRecordedAdds = false;
			m_hasRecordedRemoves = false;
			m_hasRecordedResets = false;
			m_hasRecordedLayoutTransitions = false;
		}

		private ItemCollectionTransitionTriggers GetTriggers()
		{
			ItemCollectionTransitionTriggers triggers = 0;

			if (m_hasRecordedAdds)
			{
				triggers |= ItemCollectionTransitionTriggers.CollectionChangeAdd;
			}

			if (m_hasRecordedRemoves)
			{
				triggers |= ItemCollectionTransitionTriggers.CollectionChangeRemove;
			}

			if (m_hasRecordedResets)
			{
				triggers |= ItemCollectionTransitionTriggers.CollectionChangeReset;
			}

			if (m_hasRecordedLayoutTransitions)
			{
				triggers |= ItemCollectionTransitionTriggers.LayoutTransition;
			}

			return triggers;
		}

		private ItemCollectionTransitionTriggers GetRemoveTriggers()
		{
			ItemCollectionTransitionTriggers triggers = 0;

			if (m_hasRecordedRemoves)
			{
				triggers |= ItemCollectionTransitionTriggers.CollectionChangeRemove;
			}

			if (m_hasRecordedResets)
			{
				triggers |= ItemCollectionTransitionTriggers.CollectionChangeReset;
			}

			return triggers;
		}

		private void OnTransitionProviderTransitionCompleted(ItemCollectionTransitionProvider sender, ItemCollectionTransitionCompletedEventArgs args)
		{
			var element = args.Element;

			if (element != null && args.Operation == ItemCollectionTransitionOperation.Remove)
			{
				if (CachedVisualTreeHelpers.GetParent(element) == m_owner)
				{
					m_owner.ViewManager.ClearElementToElementFactory(element);

					// Invalidate arrange so that repeater can arrange this element off-screen.
					m_owner.InvalidateArrange();
				}
			}
		}
	}
}
