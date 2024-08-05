using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation.Collections;
using Windows.System;
using static Uno.UI.FeatureConfiguration;

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

	internal void HandleNavigationKey(
		VirtualKey key,
		bool scrollViewport,
		ref int newFocusedIndex)
	{
		var spItems = Items;
		var nCount = spItems.Count;

		var direction = FlowDirection;
		var	bInvertForRTL = (direction == FlowDirection.RightToLeft);
		var (physicalOrientation, _) = GetItemsHostOrientations();
		var isVertical = (physicalOrientation == Orientation.Vertical);
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
					SelectPrev(newFocusedIndex);
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
						SelectPrev(newFocusedIndex);
					}
					else
					{
						SelectNext(newFocusedIndex);
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
				NavigateByPage(/*forward*/false, newFocusedIndex);
				break;
			case VirtualKey.GamepadLeftTrigger:
				if (isVertical)
				{
					NavigateByPage(/*forward*/false, newFocusedIndex);
				}
				break;
			case VirtualKey.GamepadLeftShoulder:
				if (!isVertical)
				{
					NavigateByPage(/*forward*/false, newFocusedIndex);
				}
				break;
			case VirtualKey.PageDown:
				NavigateByPage(/*forward*/true, newFocusedIndex);
				break;
			case VirtualKey.GamepadRightTrigger:
				if (isVertical)
				{
					NavigateByPage(/*forward*/true, newFocusedIndex);
				}
				break;
			case VirtualKey.GamepadRightShoulder:
				if (!isVertical)
				{
					NavigateByPage(/*forward*/true, newFocusedIndex);
				}
				break;
		}
		newFocusedIndex = (int)(Math.Min(newFocusedIndex, (int)(nCount) - 1));
		newFocusedIndex = (int)(Math.Max(newFocusedIndex, -1));

	}

	SetFocusedItem(
	 INT index,
	 bool shouldScrollIntoView,
	 bool animateIfBringIntoView,
	 xaml_input.FocusNavigationDirection focusNavigationDirection,
	InputActivationBehavior inputActivationBehavior)
	{

		bool hasFocus = false;
		FocusState focusState = FocusState_Programmatic;

		HasFocus(&hasFocus);
		if (hasFocus)
		{
			DependencyObject spFocused;
			UIElement spFocusedAsElement;

			GetFocusedElement(&spFocused);
			spFocusedAsElement = spFocused.AsOrNull<UIElement>();
			if (spFocusedAsElement)
			{
				focusState = spFocusedAsElement.FocusState;
				MUX_ASSERT(FocusState_Unfocused != focusState, "FocusState_Unfocused unexpected since spFocusedAsElement is focused");
			}
		}

		SetFocusedItem(index, shouldScrollIntoView, false /*forceFocus*/, focusState, animateIfBringIntoView, focusNavigationDirection, inputActivationBehavior);

	Cleanup:
		RRETURN(hr);
	}

	// Set the SelectorItem at index to be focused using an explicit FocusState
	// The focus is only set if forceFocus is true or the Selector already has focus.
	// ScrollIntoView is always called if shouldScrollIntoView is set (regardless of focus).
	private void SetFocusedItem(
		int index,
		bool shouldScrollIntoView,
		bool forceFocus,
		FocusState focusState,
		bool animateIfBringIntoView,
		FocusNavigationDirection focusNavigationDirection,
		InputActivationBehavior inputActivationBehavior)
	{
#if SLTR_DEBUG
    IGNOREHR(gps.DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "SLTR[0x%p]: SetFocusedItem. index=%d, shouldScrollIntoView=%d, forceFocus=%d, focusState=%d, animateIfBringIntoView=%d, focusNavigationDirection=%d",
        this, index, shouldScrollIntoView, forceFocus, focusState, animateIfBringIntoView, focusNavigationDirection));
#endif

		uint nCount = 0;
		bool bFocused = false;
		bool shouldFocus = false;

		var spItems = Items;
		nCount = spItems.Cast<ItemCollection>().Size;
		if (index < 0 || (INT)(nCount) <= index)
		{
			index = -1;
		}

		if (index >= 0)
		{
			SetLastFocusedIndex(index);
		}

		if (!forceFocus)
		{
			shouldFocus = HasFocus();
		}
		else
		{
			shouldFocus = true;
		}

		if (shouldFocus)
		{
			SetFocusedIndex(index);
		}

		if (GetFocusedIndex() == -1)
		{
			if (shouldFocus)
			{
				// Since none of our child items have the focus, put the focus back on the main list box.
				//
				// This will happen e.g. when the focused item is being removed but is still in the visual tree at the time of this call.
				// Note that this call may fail e.g. if IsTabStop is false, which is OK; it will just set focus
				// to the next focusable element (or clear focus if none is found).
				Focus(focusState); // FUTURE: should handle inputActivationBehavior
			}
			return;
		}

		if (shouldScrollIntoView)
		{
			CanScrollIntoView(shouldScrollIntoView);
		}

		if (shouldScrollIntoView)
		{
			ScrollIntoView(
				index,
				false /*isGroupItemIndex*/,
				false /*isHeader*/,
				false /*isFooter*/,
				false /*isFromPublicAPI*/,
				true  /*ensureContainerRealized*/,
				animateIfBringIntoView,
				ScrollIntoViewAlignment.ScrollIntoViewAlignment_Default);
		}

		if (shouldFocus)
		{
			ISelectorItem spSelectorItem;
			IDependencyObject spContainer;

			ContainerFromIndex(index, &spContainer);
			spSelectorItem = spContainer.AsOrNull<ISelectorItem>();
			if (spSelectorItem)
			{
				spSelectorItem.Cast<SelectorItem>().FocusSelfOrChild(focusState, animateIfBringIntoView, &bFocused, focusNavigationDirection, inputActivationBehavior);
			}
		}
	}
}
