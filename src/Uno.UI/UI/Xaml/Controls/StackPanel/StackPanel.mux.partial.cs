using System;
using System.Collections.Generic;
using DirectUI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class StackPanel
{
	////  IScrollSnapPointsInfo implementation

	/// <summary>
	/// Checks whether the horizontal snap points are equidistant.
	/// </summary>
	/// <returns>True when the horizontal snap points are equidistant.</returns>
	private bool AreHorizontalSnapPointsRegularImpl()
	{
		var result = false;

		var orientation = Orientation;

		if (orientation == Orientation.Horizontal)
		{
			// We use the StackPanel's AreScrollSnapPointsRegular property to answer the question.
			var areScrollSnapPointsRegular = AreScrollSnapPointsRegular;
			result = areScrollSnapPointsRegular;
		}

		// When the orientation is vertical, there are no horizontal snap points.
		// We simply return false then.
		return result;
	}

	/// <summary>
	/// Checks whether the vertical snap points are equidistant.
	/// </summary>
	/// <returns>True when the vertical snap points are equidistant.</returns>
	private bool AreVerticalSnapPointsRegularImpl()
	{
		var result = false;

		var orientation = Orientation;

		if (orientation == Orientation.Vertical)
		{
			// We use the StackPanel's AreScrollSnapPointsRegular property to answer the question.
			var areScrollSnapPointsRegular = AreScrollSnapPointsRegular;
			result = areScrollSnapPointsRegular;
		}

		// When the orientation is horizontal, there are no vertical snap points.
		// We simply return false then.
		return result;
	}

	/// <summary>
	/// Returns a read-only collection of numbers representing the snap points for
	/// the provided orientation. Returns an empty collection when no snap points are present.
	/// </summary>
	/// <param name="orientation">The direction of the requested snap points.</param>
	/// <param name="alignment">The alignment used by the caller when applying the requested snap points.</param>
	/// <returns>The read-only collection of snap points.</returns>
	public IReadOnlyList<float> GetIrregularSnapPoints(
		Orientation orientation,
		SnapPointsAlignment alignment)
	{
		var cSnapPoints = 0;
		float[] pSnapPoints = null;

		GetIrregularSnapPoints(
			orientation == Orientation.Horizontal,
			alignment == SnapPointsAlignment.Near,
			alignment == SnapPointsAlignment.Far,
			out pSnapPoints,
			out cSnapPoints);

		if (pSnapPoints is null)
		{
			return Array.Empty<float>();
		}

		return pSnapPoints.AsReadOnly();
	}

	/// <summary>
	/// Returns an original offset and interval for equidistant snap points for
	/// the provided orientation. Returns 0 when no snap points are present.
	/// </summary>
	/// <param name="orientation">The direction of the requested snap points.</param>
	/// <param name="alignment">The alignment used by the caller when applying the requested snap points.</param>
	/// <param name="offset">The offset of the first snap point.</param>
	/// <returns>The interval between the regular snap points.</returns>
	public float GetRegularSnapPoints(
		Orientation orientation,
		SnapPointsAlignment alignment,
		out float offset)
	{
		GetRegularSnapPoints(
			orientation == Orientation.Horizontal,
			alignment == SnapPointsAlignment.Near,
			alignment == SnapPointsAlignment.Far,
			out offset,
			out var interval);

		return interval;
	}

	/// <summary>
	/// Adds an event handler for the HorizontalSnapPointsChanged event.
	/// </summary>
	private void AddHorizontalSnapPointsChanged(EventHandler<object> value)
	{
		if (_horizontalSnapPointsChanged is null)
		{
			SetSnapPointsChangeNotificationsRequirement(true, true);
		}

		_horizontalSnapPointsChanged += value;
	}

	/// <summary>
	/// Removes an event handler for the HorizontalSnapPointsChanged event.
	/// </summary>
	private void RemoveHorizontalSnapPointsChanged(EventHandler<object> value)
	{
		_horizontalSnapPointsChanged -= value;

		if (_horizontalSnapPointsChanged is null)
		{
			SetSnapPointsChangeNotificationsRequirement(true, false);
		}
	}

	/// <summary>
	/// Adds an event handler for the VerticalSnapPointsChanged event.
	/// </summary>
	private void AddVerticalSnapPointsChanged(EventHandler<object> value)
	{
		if (_verticalSnapPointsChanged is null)
		{
			SetSnapPointsChangeNotificationsRequirement(false, true);
		}

		_verticalSnapPointsChanged += value;
	}

	/// <summary>
	/// Removes an event handler for the VerticalSnapPointsChanged event.
	/// </summary>
	private void RemoveVerticalSnapPointsChanged(EventHandler<object> value)
	{
		_verticalSnapPointsChanged -= value;

		if (_verticalSnapPointsChanged is null)
		{
			SetSnapPointsChangeNotificationsRequirement(false, false);
		}
	}

	/// <summary>
	/// Raises the HorizontalSnapPointsChanged event.
	/// </summary>
	private void OnHorizontalSnapPointsChanged() =>
		_horizontalSnapPointsChanged?.Invoke(this, EventArgs.Empty);

	/// <summary>
	/// Raises the VerticalSnapPointsChanged event.
	/// </summary>
	private void OnVerticalSnapPointsChanged() =>
		_verticalSnapPointsChanged?.Invoke(this, EventArgs.Empty);

	/// <summary>
	/// Called when a snap points change needs to raise an event.
	/// </summary>
	/// <param name="isForHorizontalSnapPoints">
	/// Value indicating whether notification
	/// is for horizonatal snap points.
	/// </param>
	private void NotifySnapPointsChanged(bool isForHorizontalSnapPoints)
	{
		if (isForHorizontalSnapPoints)
		{
			// Raise HorizontalSnapPointsChanged event.
			OnHorizontalSnapPointsChanged();
		}
		else
		{
			// Raise VerticalSnapPointsChanged event.
			OnVerticalSnapPointsChanged();
		}
	}

	private void GetClosestElementInfo(Point position, out (int, bool) elementInfo)
	{
		var childIndex = GetClosestOrInsertionIndex(position, false);
		elementInfo = (childIndex, false);
	}

	/// <summary>
	/// Get the index where an item should be inserted if it were dropped at
	///	the given position.  This will be used by live reordering.
	///	In case of stackPanel, the position is position from the very first Item
	/// StackPanel goes through all children and finds the insertion position
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	private int GetInsertionIndex(Point position) =>
			GetClosestOrInsertionIndex(position, true);

	//// Gets a series of bool values indicating whether a given index is
	//// positioned on the leftmost, topmost, rightmost, or bottommost
	//// edges of the layout.  This can be useful for both determining whether
	//// to tilt items at the edges of rows or columns as well as providing
	//// data for portal animations.
	private void IsLayoutBoundary(
		int index,
		out bool isLeftBoundary,
		out bool isTopBoundary,
		out bool isRightBoundary,
		out bool isBottomBoundary)
	{
		var children = Children;
		var count = children.Count;

		var orientation = Orientation;
		var isHorizontal = orientation == Orientation.Horizontal;

		// ComputeLayoutBoundary computation is same for VSP and StackPanel
		VirtualizingStackPanel.ComputeLayoutBoundary(
			index,
			count,
			isHorizontal,
			out isLeftBoundary,
			out isTopBoundary,
			out isRightBoundary,
			out isBottomBoundary);
	}

	private Rect GetItemsBounds() => new Rect(0, 0, ActualWidth, ActualHeight);

	//// This method is used for getting closest as well as Insertion Index
	private int GetClosestOrInsertionIndex(Point position, bool isInsertionIndex)
	{
		var totalSize = new Size(0, 0); // itemSize which include Justification Size
		int insertionOrClosestIndex = 0;
		Size childDesiredSize = default;

		var returnValue = -1;

		// Get Orientation
		var orientation = Orientation;
		var isHorizontal = (orientation == Orientation.Horizontal);

		var children = Children;
		var count = children.Count;

		for (var childIndex = 0; childIndex < count; childIndex++)
		{
			var child = children[childIndex];
			if (child is not null)
			{
				childDesiredSize = child.DesiredSize;
			}

			// Calculate ArrangeOffset
			if (isHorizontal)
			{
				totalSize.Width += childDesiredSize.Width;
				if (DoubleUtil.LessThanOrClose(position.X, totalSize.Width))
				{
					insertionOrClosestIndex = childIndex;
					if (isInsertionIndex &&
						position.X > totalSize.Width - childDesiredSize.Width / 2)
					{
						insertionOrClosestIndex += 1;
					}
					break;
				}
			}
			else
			{
				totalSize.Height += childDesiredSize.Height;
				if (DoubleUtil.LessThanOrClose(position.Y, totalSize.Height))
				{
					insertionOrClosestIndex = childIndex;
					if (isInsertionIndex &&
						position.Y > totalSize.Height - childDesiredSize.Height / 2)
					{
						insertionOrClosestIndex += 1;
					}
					break;
				}
			}
		}

		// Special condition for the case when alignment is Left and there is extra space on the right
		if (isHorizontal && DoubleUtil.GreaterThanOrClose(position.X, totalSize.Width))
		{
			insertionOrClosestIndex = (isInsertionIndex) ? count : count - 1;
		}
		else if (!isHorizontal && DoubleUtil.GreaterThanOrClose(position.Y, totalSize.Height))
		{
			insertionOrClosestIndex = (isInsertionIndex) ? count : count - 1;
		}

		returnValue = insertionOrClosestIndex;
		return returnValue;
	}

	/// <summary>
	/// Get the indexes where an item should be inserted if it were dropped at
	/// the given position.
	/// </summary>
	/// <param name="position">Position.</param>
	/// <param name="first">First index.</param>
	/// <param name="second">Second index.</param>
	public void GetInsertionIndexes(
		Point position,
		out int first,
		out int second)
	{
		int insertionIndex = -1;
		bool isLeftBoundary = false;
		bool isRightBoundary = false;
		bool isTopBoundary = false;
		bool isBottomBoundary = false;
		bool firstCheck = false;
		bool secondCheck = false;
		Orientation orientation = Orientation.Horizontal;

		first = -1;
		second = -1;

		insertionIndex = GetInsertionIndex(position);
		IsLayoutBoundary(insertionIndex, out isLeftBoundary, out isTopBoundary, out isRightBoundary, out isBottomBoundary);

		first = insertionIndex - 1;
		second = insertionIndex;

		orientation = Orientation;
		if (orientation == Orientation.Vertical)
		{
			firstCheck = isTopBoundary;
			secondCheck = isBottomBoundary;
		}
		else
		{
			firstCheck = isLeftBoundary;
			secondCheck = isRightBoundary;
		}

		// make sure we're not at the edges of the panel
		if (firstCheck)
		{
			first = -1;
		}
		else if (secondCheck)
		{
			second = -1;
		}
	}

	////------------------------------------------------------------------------
	////
	////  Synopsis:
	////      Gets the index of the last element visible in the viewport
	////
	////------------------------------------------------------------------------
	//HRESULT
	//  StackPanel.GetLastItemIndexInViewport(
	//   IScrollInfo* pScrollInfo,
	//   INT* pResult)
	//{
	//	HRESULT hr = S_OK;
	//	wfc.IVector<xaml.UIElement*> spChildren = NULL;
	//	UIElement spResultChild = NULL;
	//	unsigned itemCount = 0;
	//	Orientation orientation;
	//	DOUBLE viewPortOffset = 0;
	//	UINT low, mid, high = 0;
	//	XFLOAT offsetX = { 0 }, offsetY, orientedOffset = 0;

	//	*pResult = -1;

	//	// Get the offset of the edge of the viewport
	//	(get_Orientation(&orientation));
	//	if (orientation == Orientation_Vertical)
	//	{
	//		DOUBLE viewPortHeight;
	//		DOUBLE verticalOffset;

	//		viewPortHeight = (pScrollInfo.ViewportHeight);
	//		verticalOffset = (pScrollInfo.VerticalOffset);
	//		viewPortOffset = viewPortHeight + verticalOffset;
	//	}
	//	else
	//	{
	//		DOUBLE viewPortWidth;
	//		DOUBLE horizontalOffset;

	//		viewPortWidth = (pScrollInfo.ViewportWidth);
	//		horizontalOffset = (pScrollInfo.HorizontalOffset);
	//		viewPortOffset = viewPortWidth + horizontalOffset;
	//	}

	//   (get_Children(spChildren.GetAddressOf()));
	//	itemCount = (spChildren.Size);

	//	if (itemCount)
	//	{
	//		// Use binary search of iterate over the children to find the
	//		// last visible element in the viewport.
	//		low = 0;
	//		high = itemCount - 1;
	//		while (low < high)
	//		{
	//			UIElement spChild = NULL;

	//			mid = (low + high) / 2;
	//			(spChildren.GetAt(mid, spChild.GetAddressOf()));
	//			(UIElement_GetVisualOffset(
	//				(UIElement*)((UIElement*)(spChild).GetHandle()),
	//				&offsetX,
	//				&offsetY));
	//			orientedOffset = orientation == Orientation_Horizontal ? offsetX : offsetY;

	//			if (orientedOffset > viewPortOffset)
	//			{
	//				high = mid - 1;
	//			}
	//			else
	//			{
	//				low = mid + 1;
	//			}
	//		}

	//		(spChildren.GetAt(low, spResultChild.GetAddressOf()));
	//		(UIElement_GetVisualOffset(
	//			(UIElement*)((UIElement*)(spResultChild).GetHandle()),
	//			&offsetX,
	//			&offsetY));
	//		orientedOffset = orientation == Orientation_Horizontal ? offsetX : offsetY;
	//		*pResult = orientedOffset > viewPortOffset ? low - 1 : low;
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}


	////------------------------------------------------------------------------
	////
	////  Synopsis:
	////      Calculates items per page based on the current viewport size.
	////
	////------------------------------------------------------------------------
	//HRESULT
	//  StackPanel.GetItemsPerPage(
	//	IScrollInfo* pScrollInfo,
	//   out DOUBLE* pItemsPerPage)
	//{
	//	HRESULT hr = S_OK;
	//	wfc.IVector<xaml.UIElement*> spChildren;
	//	UINT itemCount = 0;
	//	UINT itemAverageCount = 0;
	//	Orientation orientation;
	//	DOUBLE viewPortSize = 0;
	//	DOUBLE totalItemSize = 0;

	//	(get_Children(spChildren.GetAddressOf()));
	//	itemCount = (spChildren.Size);
	//	(get_Orientation(&orientation));

	//	if (orientation == Orientation_Vertical)
	//	{
	//		viewPortSize = (pScrollInfo.ViewportHeight);
	//	}
	//	else
	//	{
	//		viewPortSize = (pScrollInfo.ViewportWidth);
	//	}

	//	for (UINT i = 0; i < itemCount; i++)
	//	{
	//		UIElement spChild;

	//		(spChildren.GetAt(i, &spChild));
	//		if (spChild)
	//		{
	//			Size itemSize = { 0, 0 };
	//			itemSize = (spChild.DesiredSize);

	//			if (orientation == Orientation_Vertical)
	//			{
	//				totalItemSize += itemSize.Height;
	//			}
	//			else
	//			{
	//				totalItemSize += itemSize.Width;
	//			}

	//			itemAverageCount++;
	//		}
	//	}

	//	// setup a default
	//	*pItemsPerPage = 0; // I wonder if we should choose an arbritray number here like 5.

	//	// but we can do better
	//	if (itemAverageCount > 0)
	//	{
	//		DOUBLE averageItemSize = totalItemSize / itemAverageCount;
	//		if (averageItemSize > 1)    // Items smaller than 1 pixels are not useful for calculation
	//		{
	//			*pItemsPerPage = viewPortSize / averageItemSize;
	//		}
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}
}
