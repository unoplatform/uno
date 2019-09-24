// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewList.cpp file from WinUI controls.
//

using System;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewList : ListView
	{
		NavigationViewListPosition m_navigationViewListPosition = NavigationViewListPosition.LeftNav;
		bool m_showFocusVisual = true;
		WeakReference<NavigationView> m_navigationView = null;

		// For topnav, like alarm application, we may only need icon and no content for NavigationViewItem. 
		// ListView raise ItemClicked event, but it only provides the content and not the container.
		// It's impossible for customer to identify which NavigationViewItem is associated with the clicked event.
		// So we need to provide a container to help with this. Below solution is fragile. We expect Task 17546992 will finally resolved from os
		// Before ListView raises OnItemClick, it checks if IsItemItsOwnContainerOverride in ListViewBase::OnItemClick
		// We assume this is the container of the clicked item.
		NavigationViewItemBase m_lastItemCalledInIsItemItsOwnContainerOverride;

		public NavigationViewList() : base()
		{

		}

		protected override DependencyObject GetContainerForItemOverride()
			=> IsTemplateOwnContainer ? CreateOwnContainer() : new NavigationViewItem() { IsGeneratedContainer = true };

		// IItemsControlOverrides

		protected override bool IsItemItsOwnContainerOverride(object args)
		{
			bool isItemItsOwnContainerOverride = false;
			if (args != null)
			{
				var nvib = args as NavigationViewItemBase;
				if (nvib != null && nvib != m_lastItemCalledInIsItemItsOwnContainerOverride)
				{
					m_lastItemCalledInIsItemItsOwnContainerOverride = nvib;
				}
				isItemItsOwnContainerOverride = nvib != null;
			}
			return isItemItsOwnContainerOverride;
		}

		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			var itemContainer = element as NavigationViewItem;
			if (itemContainer != null)
			{
				itemContainer.ClearIsContentChangeHandlingDelayedForTopNavFlag();
			}
			base.PrepareContainerForItemOverride(element, item);
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			if (element is NavigationViewItemBase itemContainer)
			{
				itemContainer.Position(m_navigationViewListPosition);
			}
			if (element is NavigationViewItem itemContainer2)
			{
				itemContainer2.UseSystemFocusVisuals = m_showFocusVisual;
				itemContainer2.ClearIsContentChangeHandlingDelayedForTopNavFlag();
			}
			base.PrepareContainerForItemOverride(element, item);
		}

		internal void SetNavigationViewListPosition(NavigationViewListPosition navigationViewListPosition)
		{
			m_navigationViewListPosition = navigationViewListPosition;
			PropagateChangeToAllContainers<NavigationViewItemBase>(
				(NavigationViewItemBase container) =>
					{
						container.Position(navigationViewListPosition);
					});
		}

		internal void SetShowFocusVisual(bool showFocus)
		{
			m_showFocusVisual = showFocus;
			PropagateChangeToAllContainers<NavigationViewItem>(
				(NavigationViewItem container) =>
			{
				container.UseSystemFocusVisuals = showFocus;
			});
		}

		internal void SetNavigationViewParent(NavigationView navigationView)
		{
			m_navigationView = new WeakReference<NavigationView>(navigationView);
		}

		// IControlOverrides
		protected override void OnKeyDown(KeyRoutedEventArgs eventArgs)
		{
			var key = eventArgs.Key;

			if (key == VirtualKey.GamepadLeftShoulder
				|| key == VirtualKey.GamepadRightShoulder
				|| key == VirtualKey.GamepadLeftTrigger
				|| key == VirtualKey.GamepadRightTrigger)
			{
				// We need to return at this point to prevent ListView from handling page up / page down
				// when any of these four keys get triggered. The reason is that it would navigate to the
				// first or last item in the list and handle the event, preventing NavigationView
				// to do its KeyDown handling afterwards.
				return;
			}

			base.OnKeyDown(eventArgs);
		}

		internal NavigationView GetNavigationViewParent()
		{
			var result = default(NavigationView);
			if (m_navigationView?.TryGetTarget(out result) ?? false)
			{
				return result;
			}

			return null;
		}

		internal NavigationViewItemBase GetLastItemCalledInIsItemItsOwnContainerOverride()
		{
			return m_lastItemCalledInIsItemItsOwnContainerOverride;
		}

		void PropagateChangeToAllContainers<T>(Action<T> function) where T:class
		{
			var items = Items;
			if (items != null)
			{
				var size = items.Count;
				for (int i = 0; i < size; i++)
				{
					var container = ContainerFromIndex(i);
					if (container != null)
					{
						var itemContainer = container as T;
						if (itemContainer != null)
						{
							function(itemContainer);
						}
					}
				}
			}
		}

	}
}
