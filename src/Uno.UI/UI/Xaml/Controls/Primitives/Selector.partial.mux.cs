using System;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Input;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Selector
{
	// Selects the next item in the list.
	private protected void SelectNext(ref int index)
	{
		var count = Items.Count;
		if (count > 0)
		{
			int internalSelectedIndex = index + 1;
			if (internalSelectedIndex <= count - 1)
			{
				SelectItemHelper(ref internalSelectedIndex, 1);
				if (internalSelectedIndex != -1)
				{
					index = internalSelectedIndex;
				}
			}
		}
	}

	// Selects the previous item in the list.
	private protected void SelectPrev(ref int index)
	{
		var count = Items.Count;
		if (count > 0)
		{
			int internalSelectedIndex = index - 1;
			if (internalSelectedIndex >= 0)
			{
				SelectItemHelper(ref internalSelectedIndex, -1);
				if (internalSelectedIndex != -1)
				{
					index = internalSelectedIndex;
				}
			}
		}
	}

	// Given a direction, searches through list for next available item to select.
	private void SelectItemHelper(ref int index, int increment)
	{
		var items = Items;
		var count = items.Count;
		bool isSelectable = false;

		for (; index > -1 && index < count; index += increment)
		{
			var item = items[index];
			isSelectable = IsSelectableHelper(item);
			if (isSelectable)
			{
				var container = ContainerFromIndex(index);
				isSelectable = IsSelectableHelper(container);
				if (isSelectable)
				{
					break;
				}
			}
		}

		if (!isSelectable)
		{
			// If no selectable item was found, set index to -1 so selection will not be updated.
			index = -1;
		}
	}

	internal void SetAllowCustomValues(bool allow)
	{
		m_customValuesAllowed = allow;
	}

	private void ElementScrollViewerScrollInDirection(
		VirtualKey key,
		bool animate = false)
	{
		//// When moving to C++20 this probably needs to change to the following:
		//// if (nullptr != m_tpScrollViewer)
		//// There is ambiguity as to which comparison operator should be used and that warning is treated as a break.
		//if (null != m_tpScrollViewer)
		//{
		//	if (animate)
		//	{
		//		// This is a move request within a header or footer. Only perform an animated move when the hosting panel is a modern panel. Moves from item to item
		//		// are only animated for modern panels. So for consistency, moves within headers are only animated for modern panels as well.

		//		ctl::ComPtr<IPanel> spPanel;
		//		ctl::ComPtr<IModernCollectionBasePanel> spModernPanel;

		//		IFC(get_ItemsHost(&spPanel));
		//		spModernPanel = spPanel.AsOrNull<IModernCollectionBasePanel>();

		//		animate = spModernPanel != nullptr;
		//	}

		//	if (animate)
		//	{
		//		IFC(m_tpScrollViewer.Cast<ScrollViewer>()->ScrollInDirection(key, true /*animate*/));
		//	}

		//	else
		//	{
		//		xaml_controls::Orientation physicalOrientation = xaml_controls::Orientation_Vertical;
		//		xaml::FlowDirection direction = xaml::FlowDirection_LeftToRight;
		//		BOOLEAN isVertical = FALSE;
		//		BOOLEAN invert = FALSE;

		//		IFC(GetItemsHostOrientations(&physicalOrientation, NULL /*pLogicalOrientation*/));
		//		isVertical = (physicalOrientation == xaml_controls::Orientation_Vertical);

		//		IFC(get_FlowDirection(&direction));
		//		invert = direction == xaml::FlowDirection_RightToLeft;

		//		switch (key)
		//		{
		//			case wsy::VirtualKey_PageUp:
		//				if (isVertical)
		//				{
		//					IFC(m_tpScrollViewer.Cast<ScrollViewer>()->PageUp());
		//				}
		//				else
		//				{
		//					if (invert)
		//					{
		//						IFC(m_tpScrollViewer.Cast<ScrollViewer>()->PageRight());
		//					}
		//					else
		//					{
		//						IFC(m_tpScrollViewer.Cast<ScrollViewer>()->PageLeft());
		//					}
		//				}
		//				break;
		//			case wsy::VirtualKey_PageDown:
		//				if (isVertical)
		//				{
		//					IFC(m_tpScrollViewer.Cast<ScrollViewer>()->PageDown());
		//				}
		//				else
		//				{
		//					if (invert)
		//					{
		//						IFC(m_tpScrollViewer.Cast<ScrollViewer>()->PageLeft());
		//					}
		//					else
		//					{
		//						IFC(m_tpScrollViewer.Cast<ScrollViewer>()->PageRight());
		//					}
		//				}
		//				break;
		//			case wsy::VirtualKey_Home:
		//				if (isVertical)
		//				{
		//					IFC(m_tpScrollViewer.Cast<ScrollViewer>()->HandleVerticalScroll(xaml_primitives::ScrollEventType_First));
		//				}
		//				else
		//				{
		//					IFC(m_tpScrollViewer.Cast<ScrollViewer>()->HandleHorizontalScroll(xaml_primitives::ScrollEventType_First));
		//				}
		//				break;
		//			case wsy::VirtualKey_End:
		//				if (isVertical)
		//				{
		//					IFC(m_tpScrollViewer.Cast<ScrollViewer>()->HandleVerticalScroll(xaml_primitives::ScrollEventType_Last));
		//				}
		//				else
		//				{
		//					IFC(m_tpScrollViewer.Cast<ScrollViewer>()->HandleHorizontalScroll(xaml_primitives::ScrollEventType_Last));
		//				}
		//				break;
		//			default:
		//				IFC(m_tpScrollViewer.Cast<ScrollViewer>()->ScrollInDirection(key, false /*animate*/));
		//				break;
		//		}
		//	}
		//}
	}

	internal void HandleNavigationKey(
		VirtualKey key,
		bool scrollViewport,
		ref int newFocusedIndex)
	{
		bool bInvertForRTL = false;
		bool isVertical = false;
		FlowDirection direction = FlowDirection.LeftToRight;
		Orientation physicalOrientation = Orientation.Vertical;
		int nCount = 0;

		var spItems = Items;
		nCount = spItems.Count;

		direction = FlowDirection;
		bInvertForRTL = (direction == FlowDirection.RightToLeft);
		(physicalOrientation, _ /*pLogicalOrientation*/) = GetItemsHostOrientations();
		isVertical = (physicalOrientation == Orientation.Vertical);
		switch (key)
		{
			case VirtualKey.Left:
				if (isVertical && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Left);
				}
				else
				{
					if (bInvertForRTL)
					{
						SelectNext(ref newFocusedIndex);
					}
					else
					{
						SelectPrev(ref newFocusedIndex);
					}

					if (GetFocusedIndex() == newFocusedIndex && scrollViewport)
					{
						ElementScrollViewerScrollInDirection(VirtualKey.Left);
					}
				}
				break;
			case VirtualKey.Up:
				if (!isVertical && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Up);
				}
				else
				{
					SelectPrev(ref newFocusedIndex);
				}

				if (GetFocusedIndex() == newFocusedIndex && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Up);
				}
				break;
			case VirtualKey.Right:
				if (isVertical && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Right);
				}
				else
				{
					if (bInvertForRTL)
					{
						SelectPrev(ref newFocusedIndex);
					}
					else
					{
						SelectNext(ref newFocusedIndex);
					}

					if (GetFocusedIndex() == newFocusedIndex && scrollViewport)
					{
						ElementScrollViewerScrollInDirection(VirtualKey.Right);
					}
				}
				break;
			case VirtualKey.Down:
				if (!isVertical && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Down);
				}
				else
				{
					SelectNext(ref newFocusedIndex);
				}

				if (GetFocusedIndex() == newFocusedIndex && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Down);
				}
				break;
			case VirtualKey.Home:
				newFocusedIndex = 0;
				break;
			case VirtualKey.End:
				newFocusedIndex = nCount - 1;
				break;
			case VirtualKey.PageUp:
				NavigateByPage(/*forward*/false, ref newFocusedIndex);
				break;
			case VirtualKey.GamepadLeftTrigger:
				if (isVertical)
				{
					NavigateByPage(/*forward*/false, ref newFocusedIndex);
				}
				break;
			case VirtualKey.GamepadLeftShoulder:
				if (!isVertical)
				{
					NavigateByPage(/*forward*/false, ref newFocusedIndex);
				}
				break;
			case VirtualKey.PageDown:
				NavigateByPage(/*forward*/true, ref newFocusedIndex);
				break;
			case VirtualKey.GamepadRightTrigger:
				if (isVertical)
				{
					NavigateByPage(/*forward*/true, ref newFocusedIndex);
				}
				break;
			case VirtualKey.GamepadRightShoulder:
				if (!isVertical)
				{
					NavigateByPage(/*forward*/true, ref newFocusedIndex);
				}
				break;
		}
		newFocusedIndex = Math.Min(newFocusedIndex, nCount - 1);
		newFocusedIndex = Math.Max(newFocusedIndex, -1);
	}

	private void NavigateByPage(bool forward, ref int newFocusedIndex)
	{
		// TODO: Uno
	}

	private protected virtual void OnSelectionChanged(
		int oldSelectedIndex,
		int newSelectedIndex,
		object pOldSelectedItem,
		object pNewSelectedItem,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		if (newSelectedIndex != -1)
		{
			// Only change the focus if there is a selected item.
			// Use InputActivationBehavior.NoActivate because just changing selected item by default shouldn't steal activation from another window/island.
			SetFocusedItem(newSelectedIndex, true /*shouldScrollIntoView*/, animateIfBringIntoView, focusNavigationDirection, Uno.UI.Xaml.Input.InputActivationBehavior.NoActivate);
		}
	}
}
