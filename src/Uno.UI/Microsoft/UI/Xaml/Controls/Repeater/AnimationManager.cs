// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal partial class AnimationManager
	{
		ItemsRepeater m_owner;
		ElementAnimator m_animator;

		// We infer the animation context
		// from heuristics like whether or not
		// we observed a collection change or a
		// layout transition during the current
		// tick.
		private bool m_hasRecordedAdds;
		private bool m_hasRecordedRemoves;
		private bool m_hasRecordedResets;
		private bool m_hasRecordedLayoutTransitions;

		public AnimationManager(ItemsRepeater owner)
		{
			// ItemsRepeater is not fully constructed yet. Don't interact with it.
			m_owner = owner;
		}

		public void OnAnimatorChanged(ElementAnimator newAnimator)
		{
			// While an element is hiding, we have ownership of it. We need
			// to know when its animation completes so that we give it back
			// to the view generator.
			if (m_animator != null)
			{
				m_animator.HideAnimationCompleted -= OnHideAnimationCompleted;
			}

			m_animator = newAnimator;

			if (newAnimator != null)
			{
				newAnimator.HideAnimationCompleted += OnHideAnimationCompleted;
			}
		}

		public void OnLayoutChanging()
		{
			m_hasRecordedLayoutTransitions = true;
		}

		public void OnItemsSourceChanged(object snd, NotifyCollectionChangedEventArgs args)
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
			if (m_animator != null)
			{
				var context = AnimationContext.None;
				if (m_hasRecordedAdds) context |= AnimationContext.CollectionChangeAdd;
				if (m_hasRecordedResets) context |= AnimationContext.CollectionChangeReset;
				if (m_hasRecordedLayoutTransitions) context |= AnimationContext.LayoutTransition;

				if (context != AnimationContext.None)
				{
					m_animator.OnElementShown(element, context);
				}
			}
		}

		public bool ClearElement(UIElement element)
		{
			bool canClear = false;

			if (m_animator != null)
			{
				var context = AnimationContext.None;
				if (m_hasRecordedRemoves) context |= AnimationContext.CollectionChangeRemove;
				if (m_hasRecordedResets) context |= AnimationContext.CollectionChangeReset;

				canClear =
					context != AnimationContext.None &&
					m_animator.HasHideAnimation(element, context);

				if (canClear)
				{
					m_animator.OnElementHidden(element, context);
				}
			}

			return canClear;
		}

		public void OnElementBoundsChanged(UIElement element, Rect oldBounds, Rect newBounds)
		{
			if (m_animator != null)
			{
				var context = AnimationContext.None;
				if (m_hasRecordedAdds) context |= AnimationContext.CollectionChangeAdd;
				if (m_hasRecordedRemoves) context |= AnimationContext.CollectionChangeRemove;
				if (m_hasRecordedResets) context |= AnimationContext.CollectionChangeReset;
				if (m_hasRecordedLayoutTransitions) context |= AnimationContext.LayoutTransition;

				m_animator.OnElementBoundsChanged(element, context, oldBounds, newBounds);
			}
		}

		public void OnOwnerArranged()
		{
			m_hasRecordedAdds = false;
			m_hasRecordedRemoves = false;
			m_hasRecordedResets = false;
			m_hasRecordedLayoutTransitions = false;
		}

		private void OnHideAnimationCompleted(ElementAnimator sender, UIElement element)
		{
			if (CachedVisualTreeHelpers.GetParent(element) == (DependencyObject)(m_owner))
			{
				m_owner.ViewManager.ClearElementToElementFactory(element);

				// Invalidate arrange so that repeater can arrange this element off-screen.
				m_owner.InvalidateArrange();
			}
		}
	}
}
