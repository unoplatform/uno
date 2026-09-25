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

	// Call ElementScrollViewer.ScrollInDirection if possible.
	private protected void ElementScrollViewerScrollInDirection(
		VirtualKey key,
		bool animate = false)
	{
		if (m_tpScrollViewer is null)
		{
			return;
		}

		// TODO Uno: Animated moves (modern panels only in WinUI) and ScrollViewer.ScrollInDirection are not supported.
		var (physicalOrientation, _ /*pLogicalOrientation*/) = GetItemsHostOrientations();
		var isVertical = physicalOrientation == Orientation.Vertical;
		var invert = FlowDirection == FlowDirection.RightToLeft;

		switch (key)
		{
			case VirtualKey.PageUp:
				if (isVertical)
				{
					m_tpScrollViewer.PageUp();
				}
				else
				{
					if (invert)
					{
						m_tpScrollViewer.PageRight();
					}
					else
					{
						m_tpScrollViewer.PageLeft();
					}
				}
				break;
			case VirtualKey.PageDown:
				if (isVertical)
				{
					m_tpScrollViewer.PageDown();
				}
				else
				{
					if (invert)
					{
						m_tpScrollViewer.PageLeft();
					}
					else
					{
						m_tpScrollViewer.PageRight();
					}
				}
				break;
			case VirtualKey.Home:
				if (isVertical)
				{
					m_tpScrollViewer.HandleVerticalScroll(ScrollEventType.First);
				}
				else
				{
					m_tpScrollViewer.HandleHorizontalScroll(ScrollEventType.First);
				}
				break;
			case VirtualKey.End:
				if (isVertical)
				{
					m_tpScrollViewer.HandleVerticalScroll(ScrollEventType.Last);
				}
				else
				{
					m_tpScrollViewer.HandleHorizontalScroll(ScrollEventType.Last);
				}
				break;
		}
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
