// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ItemCollectionTransitionCompletedEventArgs.cpp, tag winui3/release/1.8.4

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemCollectionTransitionCompletedEventArgs
	{
		private readonly ItemCollectionTransition m_transition;

		internal ItemCollectionTransitionCompletedEventArgs()
		{
			// Default parameterless constructor for compatibility
		}

		internal ItemCollectionTransitionCompletedEventArgs(ItemCollectionTransition transition)
		{
			m_transition = transition;
		}

		public UIElement Element => m_transition?.Element;

		public ItemCollectionTransition Transition => m_transition;

		public ItemCollectionTransitionOperation Operation => m_transition?.Operation ?? default;
	}
}
