// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ItemCollectionTransition.cpp, tag winui3/release/1.8.4

using System;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemCollectionTransition
	{
		private readonly WeakReference<ItemCollectionTransitionProvider> m_owningProvider;
		private readonly WeakReference<UIElement> m_element;
		private readonly ItemCollectionTransitionOperation m_operation;
		private readonly ItemCollectionTransitionTriggers m_triggers;
		private readonly Rect m_oldBounds;
		private readonly Rect m_newBounds;
		private ItemCollectionTransitionProgress m_progress;

		internal ItemCollectionTransition()
		{
			// Default parameterless constructor for compatibility
			m_owningProvider = new WeakReference<ItemCollectionTransitionProvider>(null);
			m_element = new WeakReference<UIElement>(null);
		}

		/// <summary>
		/// Creates an ItemCollectionTransition for Add or Remove operations.
		/// </summary>
		internal ItemCollectionTransition(
			ItemCollectionTransitionProvider owningProvider,
			UIElement element,
			ItemCollectionTransitionOperation operation,
			ItemCollectionTransitionTriggers triggers)
			: this(owningProvider, element, operation, triggers, default, default)
		{
			MUX_ASSERT(operation != ItemCollectionTransitionOperation.Move);
		}

		/// <summary>
		/// Creates an ItemCollectionTransition for Move operations.
		/// </summary>
		internal ItemCollectionTransition(
			ItemCollectionTransitionProvider owningProvider,
			UIElement element,
			ItemCollectionTransitionTriggers triggers,
			Rect oldBounds,
			Rect newBounds)
			: this(owningProvider, element, ItemCollectionTransitionOperation.Move, triggers, oldBounds, newBounds)
		{
		}

		private ItemCollectionTransition(
			ItemCollectionTransitionProvider owningProvider,
			UIElement element,
			ItemCollectionTransitionOperation operation,
			ItemCollectionTransitionTriggers triggers,
			Rect oldBounds,
			Rect newBounds)
		{
			m_owningProvider = new WeakReference<ItemCollectionTransitionProvider>(owningProvider);
			m_element = new WeakReference<UIElement>(element);
			m_operation = operation;
			m_triggers = triggers;
			m_oldBounds = oldBounds;
			m_newBounds = newBounds;
		}

		public bool HasStarted => m_progress != null;

		public Rect NewBounds => m_newBounds;

		public Rect OldBounds => m_oldBounds;

		public ItemCollectionTransitionOperation Operation => m_operation;

		public ItemCollectionTransitionTriggers Triggers => m_triggers;

		/// <summary>
		/// Gets the element associated with this transition.
		/// </summary>
		internal UIElement Element => m_element.TryGetTarget(out var element) ? element : null;

		/// <summary>
		/// Gets the owning transition provider.
		/// </summary>
		internal ItemCollectionTransitionProvider OwningProvider =>
			m_owningProvider.TryGetTarget(out var provider) ? provider : null;

		public ItemCollectionTransitionProgress Start()
		{
			if (m_progress == null)
			{
				m_progress = new ItemCollectionTransitionProgress(this);
			}

			return m_progress;
		}
	}
}
