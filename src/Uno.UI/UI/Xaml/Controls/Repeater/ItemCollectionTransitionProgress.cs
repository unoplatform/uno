// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ItemCollectionTransitionProgress.cpp, tag winui3/release/1.8.4

using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemCollectionTransitionProgress
	{
		private readonly WeakReference<ItemCollectionTransition> m_transition;

		internal ItemCollectionTransitionProgress()
		{
			// Default parameterless constructor for compatibility
			m_transition = new WeakReference<ItemCollectionTransition>(null);
		}

		internal ItemCollectionTransitionProgress(ItemCollectionTransition transition)
		{
			m_transition = new WeakReference<ItemCollectionTransition>(transition);
		}

		public UIElement Element
		{
			get
			{
				if (m_transition.TryGetTarget(out var transition))
				{
					return transition.Element;
				}
				return null;
			}
		}

		public ItemCollectionTransition Transition =>
			m_transition.TryGetTarget(out var transition) ? transition : null;

		public void Complete()
		{
			if (m_transition.TryGetTarget(out var transition))
			{
				var transitionProvider = transition.OwningProvider;
				transitionProvider?.NotifyTransitionCompleted(transition);
			}
		}
	}
}
