// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsViewInteractions.cpp, tag winui3/release/1.5.0

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsView
{
	#region IControlOverrides

	protected override void OnKeyDown(
		KeyRoutedEventArgs args)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::KeyRoutedEventArgsToString(args).c_str());

		base.OnKeyDown(args);

		if (args.Handled)
		{
			return;
		}

		switch (args.Key)
		{
			case VirtualKey.A:
				{
					if (m_selector != null)
					{
						ItemsViewSelectionMode selectionMode = SelectionMode;

						if (selectionMode != ItemsViewSelectionMode.None &&
							selectionMode != ItemsViewSelectionMode.Single &&
							(InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
						{
							m_selector.SelectAll();
							args.Handled = true;
						}
					}
					break;
				}
		}
	}

	#endregion

	// Returns True when the provided virtual key and navigation key are canceling each other.
	bool AreNavigationKeysOpposite(
		VirtualKey key1,
		VirtualKey key2)
	{
		MUX_ASSERT(IsNavigationKey(key1));
		MUX_ASSERT(IsNavigationKey(key2));

		return (key1 == VirtualKey.Left && key2 == VirtualKey.Right) ||
			   (key1 == VirtualKey.Right && key2 == VirtualKey.Left) ||
			   (key1 == VirtualKey.Up && key2 == VirtualKey.Down) ||
			   (key1 == VirtualKey.Down && key2 == VirtualKey.Up);
	}

	// Returns True when ScrollView.ComputedVerticalScrollMode is Enabled.
	bool CanScrollVertically()
	{
		if (m_scrollView is { } scrollView)
		{
			return scrollView.ComputedVerticalScrollMode == ScrollingScrollMode.Enabled;
		}
		return false;
	}

	// Returns the index of the closest focusable element to the current element following the provided direction, or -1 when no element was found.
	// hasIndexBasedLayoutOrientation indicates whether the Layout has one orientation that has an index-based layout.
	int GetAdjacentFocusableElementByDirection(
		FocusNavigationDirection focusNavigationDirection,
		bool hasIndexBasedLayoutOrientation)
	{
		int currentElementIndex = GetCurrentElementIndex();

		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, focusNavigationDirection, currentElementIndex);

		MUX_ASSERT(
			focusNavigationDirection == FocusNavigationDirection.Up ||
			focusNavigationDirection == FocusNavigationDirection.Down ||
			focusNavigationDirection == FocusNavigationDirection.Left ||
			focusNavigationDirection == FocusNavigationDirection.Right);

		if (currentElementIndex == -1)
		{
			return -1;
		}

		var currentElement = TryGetElement(currentElementIndex);

		if (currentElement == null)
		{
			bool startBringItemIntoViewSuccess = StartBringItemIntoViewInternal(true /*throwOutOfBounds*/, false /* throwOnAnyFailure */, currentElementIndex, null /* options */);

			currentElementIndex = GetCurrentElementIndex();

			if (!startBringItemIntoViewSuccess || currentElementIndex == -1)
			{
				return -1;
			}

			currentElement = TryGetElement(currentElementIndex);

			if (currentElement == null)
			{
				return -1;
			}
		}

		var itemsRepeater = m_itemsRepeater;

		MUX_ASSERT(itemsRepeater != null);

		var itemsSourceView = itemsRepeater.ItemsSourceView;

		MUX_ASSERT(itemsSourceView != null);

		int itemsCount = itemsSourceView.Count;

		MUX_ASSERT(itemsCount > 0);

		bool useKeyboardNavigationReferenceHorizontalOffset =
			focusNavigationDirection == FocusNavigationDirection.Up ||
			focusNavigationDirection == FocusNavigationDirection.Down;

		MUX_ASSERT((useKeyboardNavigationReferenceHorizontalOffset && m_keyboardNavigationReferenceRect.X != -1.0f) ||
			(!useKeyboardNavigationReferenceHorizontalOffset && m_keyboardNavigationReferenceRect.Y != -1.0f));

		Rect currentElementRect = GetElementRect(currentElement, itemsRepeater);
		Point keyboardNavigationReferenceOffsetPoint = GetUpdatedKeyboardNavigationReferenceOffset();
		float keyboardNavigationReferenceOffset = useKeyboardNavigationReferenceHorizontalOffset ? (float)keyboardNavigationReferenceOffsetPoint.X : (float)keyboardNavigationReferenceOffsetPoint.Y;

		MUX_ASSERT(keyboardNavigationReferenceOffset != -1.0f);

		bool getPreviousFocusableElement = focusNavigationDirection == FocusNavigationDirection.Up || focusNavigationDirection == FocusNavigationDirection.Left;
		bool traversalDirectionChanged = false;
		int closestElementIndex = -1;
		int itemIndex = getPreviousFocusableElement ? currentElementIndex - 1 : currentElementIndex + 1;
		float smallestDistance = float.MaxValue;
		float smallestNavigationDirectionDistance = float.MaxValue;
		float smallestNoneNavigationDirectionDistance = float.MaxValue;
		double roundingScaleFactor = GetRoundingScaleFactor(itemsRepeater);

		while ((getPreviousFocusableElement && itemIndex >= 0) || (!getPreviousFocusableElement && itemIndex < itemsCount))
		{
			var element = itemsRepeater.TryGetElement(itemIndex);

			if (element == null)
			{
				if (hasIndexBasedLayoutOrientation)
				{
					if (smallestNoneNavigationDirectionDistance == float.MaxValue)
					{
						// Ran out of realized items and smallestNoneNavigationDirectionDistance still has its initial float::max value.
						// Assuming no qualifying item will be found, return -1 instead of realizing more items and bringing them into view.
						return -1;
					}

					// Bring the still unrealized item into view to realize it.
					bool startBringItemIntoViewSuccess = StartBringItemIntoViewInternal(true /* throwOutOfBounds */, false /* throwOnAnyFailure */, itemIndex, null /*options*/);

					if (!startBringItemIntoViewSuccess)
					{
						return -1;
					}

					element = itemsRepeater.TryGetElement(itemIndex);
				}
				else
				{
					// When the Layout has no index-based orientation, all items are traversed to find the closest one.
					// First a traversal from the current item to one end, and then from the current item to the other end.
					if (traversalDirectionChanged)
					{
						// Both traversals were performed. Return the resulting closest index.
						return closestElementIndex;
					}
					else
					{
						// Do the second traversal in the opposite direction.
						traversalDirectionChanged = true;
						getPreviousFocusableElement = !getPreviousFocusableElement;
						itemIndex = getPreviousFocusableElement ? currentElementIndex - 1 : currentElementIndex + 1;
						continue;
					}
				}
			}

			MUX_ASSERT(element != null);

			if (SharedHelpers.IsFocusableElement(element))
			{
				float navigationDirectionDistance = float.MaxValue;
				float noneNavigationDirectionDistance = float.MaxValue;

				GetDistanceToKeyboardNavigationReferenceOffset(
					focusNavigationDirection,
					currentElementRect,
					element,
					itemsRepeater,
					keyboardNavigationReferenceOffset,
					roundingScaleFactor,
					ref navigationDirectionDistance,
					ref noneNavigationDirectionDistance);

				MUX_ASSERT(navigationDirectionDistance >= 0.0f);
				MUX_ASSERT(noneNavigationDirectionDistance >= 0.0f);

				if (navigationDirectionDistance <= 1.0f / (float)roundingScaleFactor && noneNavigationDirectionDistance <= 1.0f / (float)roundingScaleFactor)
				{
					// Stop the search right away since an element at the target point was found.
					return itemIndex;
				}
				else if (hasIndexBasedLayoutOrientation)
				{
					// When the Layout has an index-based orientation, its orthogonal orientation defines the primary (favored) distance. The index-based orientation defines a secondary distance.
					if (noneNavigationDirectionDistance < smallestNoneNavigationDirectionDistance ||
						(noneNavigationDirectionDistance == smallestNoneNavigationDirectionDistance && navigationDirectionDistance < smallestNavigationDirectionDistance))
					{
						smallestNoneNavigationDirectionDistance = noneNavigationDirectionDistance;
						smallestNavigationDirectionDistance = navigationDirectionDistance;
						closestElementIndex = itemIndex;
					}
					else if (noneNavigationDirectionDistance > smallestNoneNavigationDirectionDistance)
					{
						return closestElementIndex;
					}
				}
				else
				{
					// When the Layout has no index-based orientation, the typical Euclidean distance is used.
					float distance = MathF.Pow(navigationDirectionDistance, 2.0f) + MathF.Pow(noneNavigationDirectionDistance, 2.0f);

					if (distance < smallestDistance)
					{
						smallestDistance = distance;
						closestElementIndex = itemIndex;
					}
				}
			}

			itemIndex = getPreviousFocusableElement ? itemIndex - 1 : itemIndex + 1;
		}

		return closestElementIndex;
	}

	// Returns the index of the previous or next focusable element from the current element, or -1 when no element was found.
	int GetAdjacentFocusableElementByIndex(
		bool getPreviousFocusableElement)
	{
		int currentElementIndex = GetCurrentElementIndex();

		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, getPreviousFocusableElement, currentElementIndex);

		if (currentElementIndex == -1)
		{
			return -1;
		}

		var currentElement = TryGetElement(currentElementIndex);

		if (currentElement == null)
		{
			// Realize the current element so its neighbors are available for evaluation.
			bool startBringItemIntoViewSuccess = StartBringItemIntoViewInternal(false /*throwOutOfBounds*/, false /* throwOnAnyFailure */, currentElementIndex, null /*options*/);

			currentElementIndex = GetCurrentElementIndex();

			if (!startBringItemIntoViewSuccess || currentElementIndex == -1 || TryGetElement(currentElementIndex) == null)
			{
				return -1;
			}
		}

		var itemsRepeater = m_itemsRepeater;

		MUX_ASSERT(itemsRepeater != null);

		var itemsSourceView = itemsRepeater.ItemsSourceView;

		MUX_ASSERT(itemsSourceView != null);

		int itemsCount = itemsSourceView.Count;

		MUX_ASSERT(itemsCount > 0);

		// Because we are dealing with an index-based layout, the search is only done in one direction.
		int itemIndex = getPreviousFocusableElement ? currentElementIndex - 1 : currentElementIndex + 1;

		while ((getPreviousFocusableElement && itemIndex >= 0) || (!getPreviousFocusableElement && itemIndex < itemsCount))
		{
			var element = itemsRepeater.TryGetElement(itemIndex);

			if (element == null)
			{
				bool startBringItemIntoViewSuccess = StartBringItemIntoViewInternal(false /*throwOutOfBounds*/, false /* throwOnAnyFailure */, itemIndex, null /*options*/);

				if (!startBringItemIntoViewSuccess)
				{
					return -1;
				}

				element = itemsRepeater.TryGetElement(itemIndex);
			}

			MUX_ASSERT(element != null);

			if (SharedHelpers.IsFocusableElement(element))
			{
				return itemIndex;
			}

			itemIndex = getPreviousFocusableElement ? itemIndex - 1 : itemIndex + 1;
		}

		return -1;
	}

	// When focusNavigationDirection is FocusNavigationDirection.Up or FocusNavigationDirection.Down, keyboardNavigationReferenceOffset indicates a vertical line
	// to get the distance from 'element'. Otherwise keyboardNavigationReferenceOffset indicates a horizontal line.
	void GetDistanceToKeyboardNavigationReferenceOffset(
		FocusNavigationDirection focusNavigationDirection,
		Rect currentElementRect,
		UIElement element,
		ItemsRepeater itemsRepeater,
		float keyboardNavigationReferenceOffset,
		double roundingScaleFactor,
		ref float navigationDirectionDistance,
		ref float noneNavigationDirectionDistance)
	{
		MUX_ASSERT(focusNavigationDirection == FocusNavigationDirection.Up ||
			focusNavigationDirection == FocusNavigationDirection.Down ||
			focusNavigationDirection == FocusNavigationDirection.Left ||
			focusNavigationDirection == FocusNavigationDirection.Right);
		MUX_ASSERT(element != null);
		MUX_ASSERT(itemsRepeater != null);
		//MUX_ASSERT(navigationDirectionDistance != null);
		//MUX_ASSERT(noneNavigationDirectionDistance != null);
		MUX_ASSERT(keyboardNavigationReferenceOffset != -1.0f);
		MUX_ASSERT(roundingScaleFactor > 0.0);

		noneNavigationDirectionDistance = navigationDirectionDistance = float.MaxValue;

		Rect elementRect = GetElementRect(element, itemsRepeater);
		double roundingMargin = roundingScaleFactor <= 1.0 ? 0.5 : 2.0 / roundingScaleFactor;

		if (focusNavigationDirection == FocusNavigationDirection.Up &&
			elementRect.Y + elementRect.Height > currentElementRect.Y + roundingMargin)
		{
			// This element is disqualified because it is not placed at the top of currentElement.
			return;
		}

		if (focusNavigationDirection == FocusNavigationDirection.Down &&
			elementRect.Y + roundingMargin < currentElementRect.Y + currentElementRect.Height)
		{
			// This element is disqualified because it is not placed at the bottom of currentElement.
			return;
		}

		if (focusNavigationDirection == FocusNavigationDirection.Left &&
			elementRect.X + elementRect.Width > currentElementRect.X + roundingMargin)
		{
			// This element is disqualified because it is not placed at the left of currentElement.
			return;
		}

		if (focusNavigationDirection == FocusNavigationDirection.Right &&
			elementRect.X + roundingMargin < currentElementRect.X + currentElementRect.Width)
		{
			// This element is disqualified because it is not placed at the right of currentElement.
			return;
		}

		switch (focusNavigationDirection)
		{
			case FocusNavigationDirection.Up:
				noneNavigationDirectionDistance = (float)(currentElementRect.Y - elementRect.Y - elementRect.Height);
				break;
			case FocusNavigationDirection.Down:
				noneNavigationDirectionDistance = (float)(elementRect.Y - currentElementRect.Y - currentElementRect.Height);
				break;
			case FocusNavigationDirection.Left:
				noneNavigationDirectionDistance = (float)(currentElementRect.X - elementRect.X - elementRect.Width);
				break;
			case FocusNavigationDirection.Right:
				noneNavigationDirectionDistance = (float)(elementRect.X - currentElementRect.X - currentElementRect.Width);
				break;
		}

		MUX_ASSERT(noneNavigationDirectionDistance >= -roundingMargin);

		noneNavigationDirectionDistance = Math.Max(0.0f, noneNavigationDirectionDistance);

		if (focusNavigationDirection == FocusNavigationDirection.Up || focusNavigationDirection == FocusNavigationDirection.Down)
		{
			navigationDirectionDistance = (float)Math.Abs(elementRect.X + elementRect.Width / 2.0f - keyboardNavigationReferenceOffset);
		}
		else
		{
			navigationDirectionDistance = (float)Math.Abs(elementRect.Y + elementRect.Height / 2.0f - keyboardNavigationReferenceOffset);
		}
	}

	// Returns the position within the ItemsRepeater + size of the provided element as a Rect.
	// The potential element.Margin is not included in the returned rectangle.
	Rect GetElementRect(
		UIElement element,
		ItemsRepeater itemsRepeater)
	{
		MUX_ASSERT(element != null);
		MUX_ASSERT(itemsRepeater != null);

		var generalTransform = element.TransformToVisual(itemsRepeater);
		var elementOffset = generalTransform.TransformPoint(new Point(0, 0));

		if (element is FrameworkElement elementAsFE)
		{
			var farSideOffset = generalTransform.TransformPoint(new Point(
				(float)elementAsFE.ActualWidth,
				(float)elementAsFE.ActualHeight));

			return new Rect(
				elementOffset.X,
				elementOffset.Y,
				farSideOffset.X - elementOffset.X,
				farSideOffset.Y - elementOffset.Y);
		}
		else
		{
			return new Rect(
				elementOffset.X,
				elementOffset.Y,
				0.0f,
				0.0f);
		}
	}

	IndexBasedLayoutOrientation GetLayoutIndexBasedLayoutOrientation()
	{
		if (Layout is { } layout)
		{
			return layout.IndexBasedLayoutOrientation;
		}
		return IndexBasedLayoutOrientation.None;
	}

	// Returns the number of most recently queued navigation keys of the same kind.
	int GetTrailingNavigationKeyCount()
	{
		MUX_ASSERT(m_navigationKeysToProcess.Count > 0);

		VirtualKey lastNavigationKey = m_navigationKeysToProcess[m_navigationKeysToProcess.Count - 1];
		int count = 0;
		for (int i = m_navigationKeysToProcess.Count - 1; i >= 0; i--)
		{
			if (m_navigationKeysToProcess[i] == lastNavigationKey)
			{
				count++;
			}
			else
			{
				break;
			}
		}

		MUX_ASSERT(count > 0);

		return count;
	}

	// Returns the Point within the ItemsRepeater representing the reference for non-index-based orientation keyboard navigation.
	// Both X and Y are relevant for Layouts that have no index-based orientation.
	Point GetUpdatedKeyboardNavigationReferenceOffset()
	{
		if (m_keyboardNavigationReferenceIndex != -1)
		{
			MUX_ASSERT(m_keyboardNavigationReferenceRect.X != -1.0f);
			MUX_ASSERT(m_keyboardNavigationReferenceRect.Y != -1.0f);

			if (m_itemsRepeater is { } itemsRepeater)
			{
				if (itemsRepeater.TryGetElement(m_keyboardNavigationReferenceIndex) is { } keyboardNavigationReferenceElement)
				{
					Rect keyboardNavigationReferenceRect = GetElementRect(keyboardNavigationReferenceElement, itemsRepeater);

					if (keyboardNavigationReferenceRect.X + keyboardNavigationReferenceRect.Width >= 0 && keyboardNavigationReferenceRect.Y + keyboardNavigationReferenceRect.Height >= 0)
					{
						if (keyboardNavigationReferenceRect != m_keyboardNavigationReferenceRect)
						{
							UpdateKeyboardNavigationReference();
						}
					}
					// Else the keyboard navigation reference element was pinned and placed out of bounds. Use the cached m_keyboardNavigationReferenceRect.
				}
				// Else the keyboard navigation reference element was unrealized. Use the cached m_keyboardNavigationReferenceRect.
			}
		}

		MUX_ASSERT((m_keyboardNavigationReferenceRect.X == -1.0f && m_keyboardNavigationReferenceIndex == -1) || (m_keyboardNavigationReferenceRect.X != -1.0f && m_keyboardNavigationReferenceIndex != -1));

		return GetKeyboardNavigationReferenceOffset();
	}

	// Returns True when the provided incoming navigation key is canceled
	// - because of input throttling
	// - because it is the opposite of the last queued key.
	// This method also clears all queued up keys when the incoming key is Home or End.
	bool IsCancelingNavigationKey(
		VirtualKey key,
		bool isRepeatKey)
	{
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"key", key);
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"isRepeatKey", isRepeatKey);
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"queued keys size", m_navigationKeysToProcess.size());

		MUX_ASSERT(IsNavigationKey(key));

		if (m_navigationKeysToProcess.Count == 0)
		{
			return false;
		}

		// Maximum number of unprocessed keystrokes of the same kind being queued in m_navigationKeysToProcess.
		const int maxRepeatQueuedNavigationKeys = 3;    // Do not queue more than 3 identical navigation keys with a repeat count > 1.
		const int maxNonRepeatQueuedNavigationKeys = 6; // Do not queue more than 6 identical navigation keys with a repeat count = 1.
														// Keystrokes without holding down the key are more likely to have a repeat count of 1 and are more liberally queued up because more intentional.

		VirtualKey lastNavigationKey = m_navigationKeysToProcess[m_navigationKeysToProcess.Count - 1];

		if (lastNavigationKey == key)
		{
			// Incoming key is identical to the last queued up.
			int trailingNavigationKeyCount = GetTrailingNavigationKeyCount();

			if ((trailingNavigationKeyCount >= maxRepeatQueuedNavigationKeys && isRepeatKey) ||
				(trailingNavigationKeyCount >= maxNonRepeatQueuedNavigationKeys && !isRepeatKey))
			{
				///ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Navigation keys throttling.");

				// Incoming key is canceled to avoid lenghty processing after keyboard input stops.
				return true;
			}
		}

		switch (key)
		{
			case VirtualKey.Home:
			case VirtualKey.End:
				{
					// Any navigation key queued before a Home or End key can be canceled.
					//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Navigation keys list cleared for Home/End.");

					m_navigationKeysToProcess.Clear();
					break;
				}
			default:
				{
					if (AreNavigationKeysOpposite(key, lastNavigationKey))
					{
						//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Incoming key and last queued navigation key canceled.");

						// The incoming key and last queued navigation key are canceling each other and neither need to be processed.
						m_navigationKeysToProcess.RemoveAt(m_navigationKeysToProcess.Count - 1);
						return true;
					}
					break;
				}
		}

		return false;
	}

	bool IsLayoutOrientationIndexBased(bool horizontal)
	{
		IndexBasedLayoutOrientation indexBasedLayoutOrientation = GetLayoutIndexBasedLayoutOrientation();

		return (horizontal && indexBasedLayoutOrientation == IndexBasedLayoutOrientation.LeftToRight) ||
			(!horizontal && indexBasedLayoutOrientation == IndexBasedLayoutOrientation.TopToBottom);
	}

	bool IsNavigationKey(
		VirtualKey key)
	{
		switch (key)
		{
			case VirtualKey.Home:
			case VirtualKey.End:
			case VirtualKey.Left:
			case VirtualKey.Right:
			case VirtualKey.Up:
			case VirtualKey.Down:
			case VirtualKey.PageUp:
			case VirtualKey.PageDown:
				return true;
		}

		return false;
	}

	void OnItemsViewElementGettingFocus(
		UIElement element,
		GettingFocusEventArgs args)
	{
		var focusState = args.FocusState;

		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::FocusStateToString(focusState).c_str());

		if (focusState == FocusState.Keyboard)
		{
			int elementIndex = GetElementIndex(element);
			int currentElementIndex = GetCurrentElementIndex();
			bool focusMovingIntoItemsRepeater = true;

			// Check if the focus comes from an element outside the scope of the inner ItemsRepeater.
			if (m_itemsRepeater is { } itemsRepeater)
			{
				var oldFocusedElement = args.OldFocusedElement;

				if (oldFocusedElement is not null && SharedHelpers.IsAncestor(oldFocusedElement, itemsRepeater, false /*checkVisibility*/))
				{
					focusMovingIntoItemsRepeater = false;
				}
			}

			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"ElementIndex", elementIndex);
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"CurrentElementIndex", currentElementIndex);
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"FocusMovingIntoItemsRepeater", focusMovingIntoItemsRepeater);
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"Direction", static_cast<int>(args.Direction()));

			if (currentElementIndex != elementIndex && focusMovingIntoItemsRepeater)
			{
				if (currentElementIndex == -1)
				{
					var direction = args.Direction;

					if (direction == FocusNavigationDirection.Previous || direction == FocusNavigationDirection.Next)
					{
						// Tabbing (direction == FocusNavigationDirection.Next) or Shift-Tabbing (direction == FocusNavigationDirection.Previous) into
						// the ItemsRepeater while there is no current element.

						int focusableItemIndex = -1;

						// When these conditions are fulfilled, set the selected item as the current one.
						// - TabNavigation is Once
						// - SelectionMode is Single
						// - an item is selected and is focusable
						if (TabNavigation == KeyboardNavigationMode.Once &&
							SelectionMode == ItemsViewSelectionMode.Single)
						{
							var selectedItem = m_selectionModel.SelectedItem as UIElement;

							if (selectedItem is not null && SharedHelpers.IsFocusableElement(selectedItem))
							{
								IndexPath selectedItemIndexPath = m_selectionModel.SelectedIndex;
								MUX_ASSERT(selectedItemIndexPath != null && selectedItemIndexPath.GetSize() == 1);

								focusableItemIndex = selectedItemIndexPath.GetAt(0);
								MUX_ASSERT(focusableItemIndex != -1);
							}
						}

						if (focusableItemIndex == -1)
						{
							// Retrieve the focusable index on the top/left corner for Tabbing, or bottom/right corner for Shift-Tabbing.
							focusableItemIndex = GetCornerFocusableItem(direction == FocusNavigationDirection.Next /*isForTopLeftItem*/);
							MUX_ASSERT(focusableItemIndex != -1);
						}

						// Set that index as the current one.
						SetCurrentElementIndex(focusableItemIndex, FocusState.Unfocused, false /*forceKeyboardNavigationReferenceReset*/);
						MUX_ASSERT(focusableItemIndex == GetCurrentElementIndex());

						// Allow TrySetNewFocusedElement to be called below for that new current element.
						currentElementIndex = focusableItemIndex;
					}
				}

				if (currentElementIndex != -1)
				{
					// The ItemsView has a current element other than the one receiving focus, and focus is moving into the ItemsRepeater.
					var currentElement = TryGetElement(currentElementIndex);

					if (currentElement == null)
					{
						// Realize the current element.
						bool startBringItemIntoViewSuccess = StartBringItemIntoViewInternal(true /* throwOutOfBounds*/, false /* throwOnAnyFailure */, currentElementIndex, null /*options*/);

						currentElementIndex = GetCurrentElementIndex();

						if (startBringItemIntoViewSuccess && currentElementIndex != -1)
						{
							currentElement = TryGetElement(currentElementIndex);
						}
					}

					if (currentElement != null)
					{
						// Redirect focus to the current element.
						//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"Redirecting focus to CurrentElementIndex", currentElementIndex);

						bool success = args.TrySetNewFocusedElement(currentElement);

						//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"TrySetNewFocusedElement result", success);

						return;
					}
				}
			}

			if (currentElementIndex != elementIndex)
			{
				SetCurrentElementIndex(elementIndex, FocusState.Unfocused, false /*forceKeyboardNavigationReferenceReset*/);
			}

			// Selection is not updated when focus moves into the ItemsRepeater.
			if (m_selector != null && !focusMovingIntoItemsRepeater)
			{
				bool isCtrlDown = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
				bool isShiftDown = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

				try
				{
					m_isProcessingInteraction = true;

					bool isIndexPathValid = false;
					IndexPath indexPath = GetElementIndexPath(element, ref isIndexPathValid);

					if (isIndexPathValid)
					{
						m_selector.OnFocusedAction(indexPath, isCtrlDown, isShiftDown);
					}
				}
				finally
				{
					m_isProcessingInteraction = false;
				}
			}
		}
	}

	// Process the Home/End, arrows and page navigation keystrokes before the inner ScrollView gets a chance to do it by simply scrolling.
	void OnItemsViewElementKeyDown(

	object sender,
	KeyRoutedEventArgs args)
	{
		var element = sender as UIElement;

		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"sender element index", element? GetElementIndex(element) : -1);
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"current element index", GetCurrentElementIndex());
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"key", args.Key());
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"key repeat count", args.KeyStatus().RepeatCount);

		MUX_ASSERT(!args.Handled);

		var key = args.Key;

		if (!IsNavigationKey(key))
		{
			return;
		}

		if (IsCancelingNavigationKey(key, args.KeyStatus.RepeatCount > 1 /*isRepeatKey*/))
		{
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Key canceled.");

			// Mark the event as handled to prevent the outer ScrollView from processing it.
			args.Handled = true;
			return;
		}

		QueueNavigationKey(key);

		if (m_navigationKeyBringIntoViewPendingCount == 0 && m_navigationKeyBringIntoViewCorrelationId == -1 && m_navigationKeyProcessingCountdown == 0)
		{
			// No OnScrollViewBringingIntoView call is pending and there is no count down to a stable layout, so process the key right away.
			if (ProcessNavigationKeys())
			{
				args.Handled = true;
			}
		}

		else
		{
#if DEBUG
			if (m_navigationKeyBringIntoViewPendingCount > 0)
			{
				//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Key processing delayed until bring-into-view started & completed.");
			}
			else
			{
				//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Key processing delayed until bring-into-view completed.");
			}
#endif
			// Even though the key will be processed asynchronously, it is marked as handled to prevent the parents, like the ScrollView, from processing it.
			args.Handled = true;
		}
	}

#if DEBUG
	void OnItemsViewElementLosingFocusDbg(
		UIElement element,
		LosingFocusEventArgs args)
	{
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, GetElementIndex(element));
	}
#endif

	void OnItemsViewItemContainerItemInvoked(
		ItemContainer itemContainer,
		ItemContainerInvokedEventArgs args)
	{
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, TypeLogging::ItemContainerInteractionTriggerToString(args.InteractionTrigger()).c_str(), GetElementIndex(itemContainer));

		var interactionTrigger = args.InteractionTrigger;
		bool handled = args.Handled;

		switch (interactionTrigger)
		{
			case ItemContainerInteractionTrigger.PointerReleased:
				{
					handled |= ProcessInteraction(itemContainer, FocusState.Pointer);
					break;
				}

			case ItemContainerInteractionTrigger.EnterKey:
			case ItemContainerInteractionTrigger.SpaceKey:
				{
					handled |= ProcessInteraction(itemContainer, FocusState.Keyboard);
					break;
				}

			case ItemContainerInteractionTrigger.Tap:
			case ItemContainerInteractionTrigger.DoubleTap:
			case ItemContainerInteractionTrigger.AutomationInvoke:
				{
					break;
				}

			default:
				{
					return;
				}
		}

		if (!args.Handled &&
			interactionTrigger != ItemContainerInteractionTrigger.PointerReleased &&
			CanRaiseItemInvoked(interactionTrigger, itemContainer))
		{
			MUX_ASSERT(GetElementIndex(itemContainer) != -1);

			RaiseItemInvoked(itemContainer);
		}

		args.Handled = handled;
	}

#if DEBUG
	void OnItemsViewItemContainerSizeChangedDbg(
		object sender,
		SizeChangedEventArgs args)
	{
		var element = sender as UIElement;

		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"ItemContainer index:", GetElementIndex(element));
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, args.PreviousSize().Width, args.PreviousSize().Height);
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, args.NewSize().Width, args.NewSize().Height);
	}
#endif

	bool ProcessInteraction(
		UIElement element,
		FocusState focusState)
	{
		MUX_ASSERT(element != null);

		int elementIndex = GetElementIndex(element);

		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, TypeLogging::FocusStateToString(focusState).c_str(), elementIndex);

		MUX_ASSERT(elementIndex >= 0);

		// When the focusState is Pointer, the element not only gets focus but is also brought into view by SetFocusElementIndex's StartBringIntoView call.
		bool handled = SetCurrentElementIndex(elementIndex, focusState, true /*forceKeyboardNavigationReferenceReset*/, focusState == FocusState.Pointer /*startBringIntoView*/);

		if (m_selector != null)
		{
			bool isCtrlDown = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
			bool isShiftDown = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			try
			{
				m_isProcessingInteraction = true;

				bool isIndexPathValid = false;
				IndexPath indexPath = GetElementIndexPath(element, ref isIndexPathValid);

				if (isIndexPathValid)
				{
					m_selector.OnInteractedAction(indexPath, isCtrlDown, isShiftDown);
				}
			}
			finally
			{
				m_isProcessingInteraction = false;
			}
		}

		return handled;
	}

	// Processes the queued up navigation keys while there is no pending bring-into-view operation which needs to settle
	// before any subsequent processing, so items are realized, layed out and ready for identification of the target item.
	bool ProcessNavigationKeys()
	{
		MUX_ASSERT(m_navigationKeyBringIntoViewPendingCount == 0);
		MUX_ASSERT(m_navigationKeyBringIntoViewCorrelationId == -1);
		MUX_ASSERT(m_navigationKeyProcessingCountdown == 0);

		while (m_navigationKeysToProcess.Count > 0 &&
			m_navigationKeyBringIntoViewPendingCount == 0 &&
			m_navigationKeyBringIntoViewCorrelationId == -1 &&
			m_navigationKeyProcessingCountdown == 0)
		{
			VirtualKey navigationKey = m_navigationKeysToProcess[0];

			m_lastNavigationKeyProcessed = navigationKey;
			m_navigationKeysToProcess.RemoveAt(0);

#if DEBUG
			switch (navigationKey)
			{
				case VirtualKey.Left:
					{
						//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Left key dequeued.");
						break;
					}
				case VirtualKey.Right:
					{
						//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Right key dequeued.");
						break;
					}
				case VirtualKey.Up:
					{
						//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Up key dequeued.");
						break;
					}
				case VirtualKey.Down:
					{
						//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Down key dequeued.");
						break;
					}
				case VirtualKey.PageUp:
					{
						//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"PageUp key dequeued.");
						break;
					}
				case VirtualKey.PageDown:
					{
						//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"PageDown key dequeued.");
						break;
					}
				case VirtualKey.Home:
					{
						//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Home key dequeued.");
						break;
					}
				case VirtualKey.End:
					{
						//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"End key dequeued.");
						break;
					}
			}
#endif

			bool forceKeyboardNavigationReferenceReset = false;
			int newCurrentElementIndexToFocus = -1;

			switch (navigationKey)
			{
				case VirtualKey.Home:
				case VirtualKey.End:
					{
						if (m_itemsRepeater is { } itemsRepeater)
						{
							var itemsSourceView = itemsRepeater.ItemsSourceView;

							if (itemsSourceView == null)
							{
								return false;
							}

							int itemsCount = itemsSourceView.Count;

							if (itemsCount == 0)
							{
								return false;
							}

							bool targetIsFirstItem = navigationKey == VirtualKey.Home;
							int itemIndex = targetIsFirstItem ? 0 : itemsCount - 1;
							BringIntoViewOptions options = new();

							// When processing the Home key, the top/left corner of the first focusable element is aligned to the top/left corner of the viewport.
							// When processing the End key, the bottom/right corner of the last focusable element is aligned to the bottom/right corner of the viewport.
							options.HorizontalAlignmentRatio = targetIsFirstItem ? 0.0 : 1.0;
							options.VerticalAlignmentRatio = targetIsFirstItem ? 0.0 : 1.0;

							bool startBringItemIntoViewSuccess = StartBringItemIntoViewInternal(false /*throwOutOfBounds*/, false /* throwOnAnyFailure */, itemIndex, options);

							if (!startBringItemIntoViewSuccess)
							{
								return false;
							}

							// Now that the target item is realized and moving into view, check if it needs to take keyboard focus.
							if (Layout is { } layout)
							{
								var focusableObject = targetIsFirstItem ? FocusManager.FindFirstFocusableElement(itemsRepeater) : FocusManager.FindLastFocusableElement(itemsRepeater);

								if (focusableObject is UIElement focusableElement)
								{
									int index = GetElementIndex(focusableElement);

									MUX_ASSERT(index != -1);

									if (index != GetCurrentElementIndex())
									{
										forceKeyboardNavigationReferenceReset = true;
										newCurrentElementIndexToFocus = index;
									}
								}
							}
						}
						break;
					}
				case VirtualKey.Left:
				case VirtualKey.Right:
				case VirtualKey.Up:
				case VirtualKey.Down:
					{
						bool isLayoutOrientationIndexBased = IsLayoutOrientationIndexBased(navigationKey == VirtualKey.Left || navigationKey == VirtualKey.Right /*horizontal*/);
						bool isRightToLeftDirection = FlowDirection == FlowDirection.RightToLeft;

						if (isLayoutOrientationIndexBased)
						{
							bool getPreviousFocusableElement =
								(navigationKey == VirtualKey.Left && !isRightToLeftDirection) ||
								(navigationKey == VirtualKey.Right && isRightToLeftDirection) ||
								navigationKey == VirtualKey.Up;
							int index = GetAdjacentFocusableElementByIndex(getPreviousFocusableElement);

							if (index != -1 && index != GetCurrentElementIndex())
							{
								forceKeyboardNavigationReferenceReset = true;
								newCurrentElementIndexToFocus = index;
							}
						}
						else
						{
							bool hasIndexBasedLayoutOrientation = GetLayoutIndexBasedLayoutOrientation() != IndexBasedLayoutOrientation.None;
							FocusNavigationDirection focusNavigationDirection = default;

							switch (navigationKey)
							{
								case VirtualKey.Left:
									focusNavigationDirection = isRightToLeftDirection ? FocusNavigationDirection.Right : FocusNavigationDirection.Left;
									break;
								case VirtualKey.Right:
									focusNavigationDirection = isRightToLeftDirection ? FocusNavigationDirection.Left : FocusNavigationDirection.Right;
									break;
								case VirtualKey.Up:
									focusNavigationDirection = FocusNavigationDirection.Up;
									break;
								case VirtualKey.Down:
									focusNavigationDirection = FocusNavigationDirection.Down;
									break;
							}

							int index = GetAdjacentFocusableElementByDirection(focusNavigationDirection, hasIndexBasedLayoutOrientation);

							if (index != -1 && index != GetCurrentElementIndex())
							{
								if (!hasIndexBasedLayoutOrientation)
								{
									forceKeyboardNavigationReferenceReset = true;
								}

								newCurrentElementIndexToFocus = index;
							}
						}
						break;
					}
				case VirtualKey.PageUp:
				case VirtualKey.PageDown:
					{
						IndexBasedLayoutOrientation indexBasedLayoutOrientation = GetLayoutIndexBasedLayoutOrientation();
						bool isHorizontalDistanceFavored = indexBasedLayoutOrientation == IndexBasedLayoutOrientation.TopToBottom;
						bool isVerticalDistanceFavored = indexBasedLayoutOrientation == IndexBasedLayoutOrientation.LeftToRight;
						bool isForPageUp = navigationKey == VirtualKey.PageUp;
						bool isPageNavigationRailed = true; // Keeping this variable for now. It could be set to an ItemsView.IsPageNavigationRailed() property to opt out of railing.
						bool useKeyboardNavigationReferenceHorizontalOffset = false;
						bool useKeyboardNavigationReferenceVerticalOffset = false;
						double horizontalViewportRatio = default, verticalViewportRatio = default;

						// First phase: Check if target element is on the current page.
						if (isVerticalDistanceFavored || CanScrollVertically())
						{
							if (isPageNavigationRailed)
							{
								useKeyboardNavigationReferenceHorizontalOffset = true;
							}
							else
							{
								horizontalViewportRatio = isForPageUp ? -double.MaxValue : double.MaxValue;
							}

							verticalViewportRatio = isForPageUp ? 0.0 : 1.0;
						}
						else if (isHorizontalDistanceFavored)
						{
							horizontalViewportRatio = isForPageUp ? 0.0 : 1.0;

							if (isPageNavigationRailed)
							{
								useKeyboardNavigationReferenceVerticalOffset = true;
							}
							else
							{
								verticalViewportRatio = isForPageUp ? -double.MaxValue : double.MaxValue;
							}
						}

						MUX_ASSERT(!isPageNavigationRailed || horizontalViewportRatio == 0.0 || verticalViewportRatio == 0.0);
						MUX_ASSERT(!useKeyboardNavigationReferenceHorizontalOffset || !useKeyboardNavigationReferenceVerticalOffset);

						int index = GetItemInternal(
							horizontalViewportRatio,
							verticalViewportRatio,
							isHorizontalDistanceFavored,
							isVerticalDistanceFavored,
							useKeyboardNavigationReferenceHorizontalOffset,
							useKeyboardNavigationReferenceVerticalOffset,
							true /*capItemEdgesToViewportRatioEdges*/,
							true /*forFocusableItemsOnly*/);

						//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, isForPageUp ? L"PageUp - phase 1" : L"PageDown - phase 1", index);

						if (index != -1)
						{
							if (!isPageNavigationRailed || indexBasedLayoutOrientation == IndexBasedLayoutOrientation.None)
							{
								forceKeyboardNavigationReferenceReset = true;
							}

							if (index != GetCurrentElementIndex())
							{
								// Target element is on the current page.
								newCurrentElementIndexToFocus = index;
							}
							else
							{
								// Find target on the neighboring page.
								if (isVerticalDistanceFavored || CanScrollVertically())
								{
									verticalViewportRatio = isForPageUp ? -1.0 : 2.0;
								}
								else if (isHorizontalDistanceFavored)
								{
									horizontalViewportRatio = isForPageUp ? -1.0 : 2.0;
								}

								MUX_ASSERT(!isPageNavigationRailed || horizontalViewportRatio == 0.0 || verticalViewportRatio == 0.0);

								index = GetItemInternal(
									horizontalViewportRatio,
									verticalViewportRatio,
									isHorizontalDistanceFavored,
									isVerticalDistanceFavored,
									useKeyboardNavigationReferenceHorizontalOffset,
									useKeyboardNavigationReferenceVerticalOffset,
									true /*capItemEdgesToViewportRatioEdges*/,
									true /*forFocusableItemsOnly*/);

								//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, isForPageUp ? L"PageUp - phase 2" : L"PageDown - phase 2", index);

								if (index != -1)
								{
									if (index != GetCurrentElementIndex())
									{
										// Found target element on neighboring page.
										newCurrentElementIndexToFocus = index;
									}
									else if (isPageNavigationRailed)
									{
										MUX_ASSERT(useKeyboardNavigationReferenceHorizontalOffset || useKeyboardNavigationReferenceVerticalOffset);

										// Beginning or end of items reached while railing is turned on. Turn it off and try again.
										if (isVerticalDistanceFavored || CanScrollVertically())
										{
											horizontalViewportRatio = isForPageUp ? -double.MaxValue : double.MaxValue;
											verticalViewportRatio = isForPageUp ? 0.0 : 1.0;
										}
										else if (isHorizontalDistanceFavored)
										{
											horizontalViewportRatio = isForPageUp ? 0.0 : 1.0;
											verticalViewportRatio = isForPageUp ? -double.MaxValue : double.MaxValue;
										}

										index = GetItemInternal(
											horizontalViewportRatio,
											verticalViewportRatio,
											isHorizontalDistanceFavored,
											isVerticalDistanceFavored,
											false /*useKeyboardNavigationReferenceHorizontalOffset*/,
											false /*useKeyboardNavigationReferenceVerticalOffset*/,
											true  /*capItemEdgesToViewportRatioEdges*/,
											true  /*forFocusableItemsOnly*/);

										//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, isForPageUp ? L"PageUp - phase 3" : L"PageDown - phase 3", index);

										if (index != -1 && index != GetCurrentElementIndex())
										{
											// Target element is first or last focusable element.
											forceKeyboardNavigationReferenceReset = true;
											newCurrentElementIndexToFocus = index;
										}
									}
								}
							}
						}
						break;
					}
			}

			if (newCurrentElementIndexToFocus != -1)
			{
				SetCurrentElementIndex(newCurrentElementIndexToFocus, FocusState.Keyboard, forceKeyboardNavigationReferenceReset, false /*startBringIntoView*/, true /*expectBringIntoView*/);

				if (m_navigationKeysToProcess.Count == 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	// Queues the incoming navigation key for future processing in ProcessNavigationKeys.
	void QueueNavigationKey(

		VirtualKey key)
	{
		MUX_ASSERT(IsNavigationKey(key));

#if DEBUG
		switch (key)
		{
			case VirtualKey.Home:
				{
					//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Home key queued.");
					break;
				}
			case VirtualKey.End:
				{
					//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"End key queued.");
					break;
				}
			case VirtualKey.Left:
				{
					//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Left key queued.");
					break;
				}
			case VirtualKey.Right:
				{
					//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Right key queued.");
					break;
				}
			case VirtualKey.Up:
				{
					//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Up key queued.");
					break;
				}
			case VirtualKey.Down:
				{
					//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"Down key queued.");
					break;
				}
			case VirtualKey.PageUp:
				{
					//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"PageUp key queued.");
					break;
				}
			case VirtualKey.PageDown:
				{
					//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"PageDown key queued.");
					break;
				}
		}
#endif

		m_navigationKeysToProcess.Add(key);
	}

	bool SetFocusElementIndex(
		int index,
		FocusState focusState,
		bool startBringIntoView = false,
		bool expectBringIntoView = false)
	{
		MUX_ASSERT(!startBringIntoView || !expectBringIntoView);

		if (index != -1 && focusState != FocusState.Unfocused)
		{
			if (TryGetElement(index) is { } element)
			{
				bool success = CppWinRTHelpers.SetFocus(element, focusState);

				//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT_INT, METH_NAME, this, L"index", index, success);

				if (success)
				{
					if (m_scrollView is { } scrollView)
					{
						if (expectBringIntoView)
						{
							m_navigationKeyBringIntoViewPendingCount++;

							//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"m_navigationKeyBringIntoViewPendingCount incremented", m_navigationKeyBringIntoViewPendingCount);
						}
						else if (startBringIntoView)
						{
							//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_METH, METH_NAME, this, L"UIElement::StartBringIntoView");

							element.StartBringIntoView();
						}
					}
				}

				return success;
			}
		}

		return false;
	}

	// Updates the values of m_keyboardNavigationReferenceIndex and m_keyboardNavigationReferenceRect based on the current element.
	void UpdateKeyboardNavigationReference()
	{
		int currentElementIndex = GetCurrentElementIndex();

		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, currentElementIndex);

		Point oldKeyboardNavigationReferenceOffset = GetKeyboardNavigationReferenceOffset();

		m_keyboardNavigationReferenceIndex = currentElementIndex;

		if (currentElementIndex != -1)
		{
			if (m_itemsRepeater is { } itemsRepeater)
			{
				if (TryGetElement(currentElementIndex) is { } currentElement)
				{
					var generalTransform = currentElement.TransformToVisual(itemsRepeater);
					var currentElementOffset = generalTransform.TransformPoint(new Point(0, 0));

					if (currentElement is FrameworkElement currentElementAsFE)
					{
						m_keyboardNavigationReferenceRect = new Rect(
							currentElementOffset.X,
						currentElementOffset.Y,
						(float)currentElementAsFE.ActualWidth,
						(float)currentElementAsFE.ActualHeight);
					}

					else
					{
						m_keyboardNavigationReferenceRect = new Rect(
							currentElementOffset.X,
							currentElementOffset.Y,
							0.0f,
							0.0f);
					}
				}
				else
				{
					m_keyboardNavigationReferenceIndex = -1;
					m_keyboardNavigationReferenceRect = new Rect(-1.0f, -1.0f, -1.0f, -1.0f);
				}
			}

			else
			{
				m_keyboardNavigationReferenceIndex = -1;
				m_keyboardNavigationReferenceRect = new Rect(-1.0f, -1.0f, -1.0f, -1.0f);
			}
		}
		else
		{
			m_keyboardNavigationReferenceRect = new Rect(-1.0f, -1.0f, -1.0f, -1.0f);
		}

		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, TypeLogging::RectToString(m_keyboardNavigationReferenceRect).c_str(), m_keyboardNavigationReferenceIndex);

		ItemsViewTestHooks globalTestHooks = ItemsViewTestHooks.GetGlobalTestHooks();

		if (globalTestHooks != null)
		{
			Point newKeyboardNavigationReferenceOffset = GetKeyboardNavigationReferenceOffset();
			IndexBasedLayoutOrientation indexBasedLayoutOrientation = GetLayoutIndexBasedLayoutOrientation();

			if ((oldKeyboardNavigationReferenceOffset.X != newKeyboardNavigationReferenceOffset.X && (indexBasedLayoutOrientation == IndexBasedLayoutOrientation.LeftToRight || indexBasedLayoutOrientation == IndexBasedLayoutOrientation.None)) ||
				(oldKeyboardNavigationReferenceOffset.Y != newKeyboardNavigationReferenceOffset.Y && (indexBasedLayoutOrientation == IndexBasedLayoutOrientation.TopToBottom || indexBasedLayoutOrientation == IndexBasedLayoutOrientation.None)))
			{
				ItemsViewTestHooks.NotifyKeyboardNavigationReferenceOffsetChanged(this);
			}
		}
	}
}
