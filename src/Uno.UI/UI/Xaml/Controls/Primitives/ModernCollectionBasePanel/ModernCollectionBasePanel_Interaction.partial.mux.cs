// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_Interaction_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Internal;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	internal bool SupportsKeyNavigationAction(KeyNavigationAction action)
	{
		// We support everything!
		return true;
	}

	internal void GetTargetIndexFromNavigationAction(
		uint sourceIndex,
		ElementType sourceType,
		KeyNavigationAction action,
		bool allowWrap,
		uint itemIndexHintForHeaderNavigation,
		out uint computedTargetIndex,
		out ElementType computedTargetElementType,
		out bool actionValidForSourceIndex)
	{
		uint targetIndex = sourceIndex;
		var targetType = ElementType.ItemContainer;


		computedTargetIndex = 0;
		actionValidForSourceIndex = false;

		if (action == KeyNavigationAction.First)
		{
			targetIndex = 0;
		}
		else if (action == KeyNavigationAction.Last)
		{
			targetIndex = unchecked((uint)(m_cacheManager.GetTotalItemCount() - 1));
		}
		else if (action == KeyNavigationAction.Next)
		{
			targetIndex = GetTargetIndexFromNavigationActionByPage(sourceIndex, true /*isPageDown*/, allowWrap);
		}
		else if (action == KeyNavigationAction.Previous)
		{
			targetIndex = GetTargetIndexFromNavigationActionByPage(sourceIndex, false /*isPageDown*/, allowWrap);
		}
		else if (action == KeyNavigationAction.Left ||
			action == KeyNavigationAction.Right ||
			action == KeyNavigationAction.Up ||
			action == KeyNavigationAction.Down)
		{
			int sourceIndexSigned = unchecked((int)sourceIndex);
			int targetIndexSigned = 0;

			// The layout strategies deal with layout indices, so if this
			// is a group header, translate the given index to a
			// layout index index.
			if (sourceType == ElementType.GroupHeader)
			{
				sourceIndexSigned = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, sourceIndexSigned);
			}

			m_spLayoutStrategy.GetTargetIndexFromNavigationAction(
				sourceType,
				sourceIndexSigned,
				action,
				m_windowState.GetRealizationWindow(),
				unchecked((int)itemIndexHintForHeaderNavigation),
				out targetType,
				out targetIndexSigned);

			// The layout strategies deal with layout indices, so if this
			// is a group header, translate the resulting index to a
			// data index index.
			if (targetType == ElementType.GroupHeader)
			{
				targetIndexSigned = m_cacheManager.LayoutIndexToDataIndex(ElementType.GroupHeader, targetIndexSigned);
			}

			targetIndex = unchecked((uint)targetIndexSigned);

			if (!allowWrap && sourceType == targetType && sourceIndex != targetIndex)
			{
				DependencyObject? oldContainer;
				UIElement? oldContainerAsUIE = null;
				DependencyObject? newContainer;
				UIElement? newContainerAsUIE = null;

				if (sourceType == ElementType.ItemContainer)
				{
					oldContainer = ContainerFromIndex((int)sourceIndex);
					newContainer = ContainerFromIndex((int)targetIndex);
				}
				else
				{
					oldContainer = HeaderFromIndex((int)sourceIndex);
					newContainer = HeaderFromIndex((int)targetIndex);
				}

				if (oldContainer is not null && newContainer is not null)
				{
					oldContainerAsUIE = oldContainer as UIElement;
					newContainerAsUIE = newContainer as UIElement;
				}

				if (oldContainerAsUIE is not null && newContainerAsUIE is not null)
				{
					VirtualizationInformation? pOldContainerVirtualizationInformation =
						oldContainerAsUIE.GetVirtualizationInformationOrNull();

					VirtualizationInformation? pNewContainerVirtualizationInformation =
						newContainerAsUIE.GetVirtualizationInformationOrNull();

					global::System.Diagnostics.Debug.Assert(pOldContainerVirtualizationInformation is not null && pNewContainerVirtualizationInformation is not null);

					// If we aren't allowing wrapping and we're about to wrap, then we'll set the target index
					// equal to the source index to indicate that this operation is not allowed.
					if (((action == KeyNavigationAction.Left &&
							pOldContainerVirtualizationInformation!.Bounds.X <= pNewContainerVirtualizationInformation!.Bounds.X) ||
						(action == KeyNavigationAction.Up &&
							pOldContainerVirtualizationInformation!.Bounds.Y <= pNewContainerVirtualizationInformation!.Bounds.Y) ||
						(action == KeyNavigationAction.Right &&
							pOldContainerVirtualizationInformation!.Bounds.X >= pNewContainerVirtualizationInformation!.Bounds.X) ||
						(action == KeyNavigationAction.Down &&
							pOldContainerVirtualizationInformation!.Bounds.Y >= pNewContainerVirtualizationInformation!.Bounds.Y)))
					{
						targetIndex = sourceIndex;
					}
				}
			}
		}

		computedTargetIndex = targetIndex;
		computedTargetElementType = targetType;
		actionValidForSourceIndex = sourceIndex != targetIndex || sourceType != targetType;
	}

	internal void GetTargetHeaderIndexFromNavigationAction(
		uint sourceIndex,
		KeyNavigationAction action,
		out uint computedTargetIndex,
		out bool actionHandled)
	{
		actionHandled = false;
		computedTargetIndex = sourceIndex;

		if (action == KeyNavigationAction.Next ||
			action == KeyNavigationAction.Previous)
		{
			// Nothing to do for PgUp/PgDown
		}
		else
		{
			if (action == KeyNavigationAction.First)
			{
				// Search for the first non-hidden header
				for (int candidateGroupIndex = 0; candidateGroupIndex < m_cacheManager.GetTotalGroupCount(); ++candidateGroupIndex)
				{
					if (ShouldElementBeVisible(ElementType.GroupHeader, candidateGroupIndex))
					{
						computedTargetIndex = (uint)candidateGroupIndex;
						break;
					}
				}
			}
			else if (action == KeyNavigationAction.Last)
			{
				// Search for the last non-hidden header
				for (int candidateGroupIndex = m_cacheManager.GetTotalGroupCount() - 1; candidateGroupIndex >= 0; --candidateGroupIndex)
				{
					if (ShouldElementBeVisible(ElementType.GroupHeader, candidateGroupIndex))
					{
						computedTargetIndex = (uint)candidateGroupIndex;
						break;
					}
				}
			}
			else if (action == KeyNavigationAction.Left ||
				action == KeyNavigationAction.Right ||
				action == KeyNavigationAction.Up ||
				action == KeyNavigationAction.Down)
			{
				computedTargetIndex = GetTargetHeaderIndexFromNavigationActionByItem(
					sourceIndex,
					action);
			}

			actionHandled = (sourceIndex != computedTargetIndex);
		}
	}

	// Given an index, calculate the index of the item one page from it.
	// WARNING: This method will potentially realize containers to figure
	// things out. Call with caution.
	// Note: computedTargetIndex will be set to sourceIndex if we are unable to
	// find a target index.
	private uint GetTargetIndexFromNavigationActionByPage(
		uint sourceIndex,
		bool isPageDown,
		bool allowWrap)
	{
		uint computedTargetIndex;

		// The usual behavior for PgDn is to jump to the last visible item.
		// If we're already on the last fully visible item (or after it), move
		// one page over then jump to the last visible item.
		// Sometimes, however, the last visible item is irrelevant to
		// the process and should never be jumped to. This is the case
		// if the source index isn't in the visible window or if the source
		// index container isn't realized.
		// In this case, things get complicated - we must bring the source
		// container into view on the appropriate view window side, realize one
		// more page in the appropriate direction, then find the last/first
		// visible container. The latter is the target index.
		DependencyObject? spSourceObject;
		bool viewIsRelevantForCalculation = false;

		// We're going to mess with the visible window quite a bit trying to figure out
		// what a "page" is. Save off the visible window.
		Rect originalVisibleWindow = m_windowState.GetVisibleWindow();
		bool modifiedVisibleWindow = false;
		computedTargetIndex = sourceIndex;

		spSourceObject = ContainerFromIndex((int)sourceIndex);

		// Make sure the source container is in the visible window.
		if (spSourceObject is not null)
		{
			UIElement spSourceObjectAsUI = (UIElement)spSourceObject;
			Rect sourceObjectBounds = GetBoundsFromElement(spSourceObjectAsUI);

			viewIsRelevantForCalculation = !RectUtil.AreDisjoint(sourceObjectBounds, m_windowState.GetVisibleWindow());
		}

		// See if we're the last visible container in the view.
		if (viewIsRelevantForCalculation)
		{
			uint lastVisibleItemIndex = 0;

			GetLastVisibleContainer(isPageDown, true, out lastVisibleItemIndex, out _);

			// We must scroll the page if sourceIndex is at or after the last fully visible item index.
			bool isAtOrAfterLastFullyVisibleItem = isPageDown ? (sourceIndex >= lastVisibleItemIndex) : (sourceIndex <= lastVisibleItemIndex);
			bool isAtLastItem = isPageDown ? (sourceIndex == m_cacheManager.GetTotalItemCount() - 1) : (sourceIndex == 0);

			// get last input device type. If it is gamepad, we do not want
			// to jump to the last visible index in the same page.
			var contentRoot = VisualTree.GetContentRootForElement(this);
			bool isGamepadInput = contentRoot!.InputManager.LastInputDeviceType == InputDeviceType.GamepadOrRemote;

			if ((isAtOrAfterLastFullyVisibleItem || isGamepadInput) && !isAtLastItem)
			{
				// We must scroll the view, run virtualization, and see what the last visible item is.
				// Then return that item's index.
				SynchronousScrollByPage(isPageDown);
				GetLastVisibleContainer(isPageDown, true, out lastVisibleItemIndex, out _);
				if (sourceIndex == lastVisibleItemIndex)
				{
					// In this case, we've entered a case where items are big enough to remain
					// the only fully visible item despite scrolling by a page. Manually add or
					// subtract one to the index depending on the direction.
					if (isPageDown)
					{
						if ((lastVisibleItemIndex + 1) < (uint)m_cacheManager.GetTotalItemCount())
						{
							++lastVisibleItemIndex;
						}
					}
					else if (lastVisibleItemIndex > 0)
					{
						--lastVisibleItemIndex;
					}
				}
				modifiedVisibleWindow = true;
				computedTargetIndex = lastVisibleItemIndex;
			}
			else
			{
				// We're not on the last visible container.
				// Just jump to the last visible container.
				computedTargetIndex = lastVisibleItemIndex;
			}
		}
		else if (sourceIndex < (uint)m_cacheManager.GetTotalItemCount())
		{
			// We're not in view.
			// 1- Bring the source index into view, aligned to the edge of the window we're paging towards.
			// 2- Scroll one page in the appropriate direction.
			// Find the last visible container in the direction appropriate.
			ScrollItemIntoView(sourceIndex, ScrollIntoViewAlignment.Leading, 0.0, true /* forceSynchronous */);
			SynchronousScrollByPage(isPageDown);
			GetLastVisibleContainer(isPageDown, true, out computedTargetIndex, out _);
			modifiedVisibleWindow = true;
		}

		// If we messed with the visible window, restore it here.
		// NOTE: This isn't just to preserve state. Over the course of
		// the above code, our window may have been set out of the real
		// panel extent. It's difficult to detect this. So, we let the
		// offset be corrected by the next measure pass.
		if (modifiedVisibleWindow)
		{
			ScrollRectIntoView(originalVisibleWindow, false /* forceSynchronous */);
		}

		return computedTargetIndex;
	}

	// Calculates the next group header index for a given selection index and item navigation action.
	private uint GetTargetHeaderIndexFromNavigationActionByItem(
		uint sourceIndex,
		KeyNavigationAction action)
	{
		uint computedTargetIndex = sourceIndex;
		Orientation orientation;

		orientation = m_spLayoutStrategy.VirtualizationDirection;

		if ((orientation == Orientation.Horizontal && (action == KeyNavigationAction.Left || action == KeyNavigationAction.Right)) ||
			(orientation == Orientation.Vertical && (action == KeyNavigationAction.Up || action == KeyNavigationAction.Down)))
		{
			// Handle only actions along axis of virtualization.
			int step = (action == KeyNavigationAction.Left || action == KeyNavigationAction.Up) ? -1 : 1;

			// Search for the next non-hidden header
			for (int tempTargetIndex = unchecked((int)sourceIndex) + step; 0 <= tempTargetIndex && tempTargetIndex < m_cacheManager.GetTotalGroupCount(); tempTargetIndex += step)
			{
				if (ShouldElementBeVisible(ElementType.GroupHeader, tempTargetIndex))
				{
					computedTargetIndex = (uint)tempTargetIndex;
					break;
				}
			}
		}

		return computedTargetIndex;
	}

	// Calculates the prior visible group header index from the provided index.
	// Returns the provided index if no visible smaller index is found.
	private uint GetPreviousHeaderIndex(uint sourceIndex)
	{
		uint computedPreviousIndex = sourceIndex;

		// Search for the prior non-hidden header
		for (int tempPreviousIndex = unchecked((int)sourceIndex) - 1; 0 <= tempPreviousIndex && tempPreviousIndex < m_cacheManager.GetTotalGroupCount(); tempPreviousIndex--)
		{
			if (ShouldElementBeVisible(ElementType.GroupHeader, tempPreviousIndex))
			{
				computedPreviousIndex = (uint)tempPreviousIndex;
				break;
			}
		}

		return computedPreviousIndex;
	}

	// Called when attempting to animate from the current group header (currentGroupIndex)
	// to another group header (newGroupIndex).
	// When animating down/right to a larger index, returns the last item's index in the visible
	// group prior to the target group.
	// When animating up/left to a smaller index, returns the first item's index in the target group.
	internal uint GetNeighboringItemIndex(uint currentGroupIndex, uint newGroupIndex)
	{
		uint neighboringItemIndex = 0;

		int startItemIndex = 0;
		int itemCountInGroup = 0;

		if (newGroupIndex < currentGroupIndex)
		{
			// Attempting to animate up to a smaller group index.
			// Identify the first item index of that target group.
			m_cacheManager.GetGroupInformationFromGroupIndex((int)newGroupIndex, out startItemIndex, out itemCountInGroup);
			neighboringItemIndex = (uint)startItemIndex;
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(newGroupIndex > currentGroupIndex);
			// Attempting to animate down to a larger group index.
			// Identify the last item index of the visible group just above that target group.
			uint previousGroupIndex = 0;
			previousGroupIndex = GetPreviousHeaderIndex(newGroupIndex);
			m_cacheManager.GetGroupInformationFromGroupIndex((int)previousGroupIndex, out startItemIndex, out itemCountInGroup);
			neighboringItemIndex = (uint)(startItemIndex + itemCountInGroup - 1);
		}

		return neighboringItemIndex;
	}
}
