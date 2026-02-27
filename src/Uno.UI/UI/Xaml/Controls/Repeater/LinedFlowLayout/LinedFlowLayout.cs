// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayout.cpp, tag winui3/release/1.8.4

#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0414 // Field is assigned but never used
#pragma warning disable CS0649 // Field is never assigned

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a layout that arranges items into lines with the items on each line arranged
/// horizontally and stretched to fit the available width. Items flow to a new line when
/// there's no room for more items on the current line.
/// </summary>
public partial class LinedFlowLayout : VirtualizingLayout
{
	// Constants
	private const int MeasureCountdownStart = 5;
	private const int MaxAspectRatioWeight = 16;
	private const int MaxAnchorIndexRetentionCount = 10;
	private const double FrozenLinesRatio = 0.8;
	private const double OffsetEqualityEpsilon = 0.01;

	// Element manager
	private readonly ElementManager _elementManager;

	// Item aspect ratios storage
	private LinedFlowLayoutItemAspectRatios _aspectRatios;

	// Locked items map (itemIndex -> lineIndex)
	private readonly Dictionary<int, int> _lockedItemIndexes = new();

	// Line item counts
	private readonly List<int> _lineItemCounts = new();
	private readonly List<float> _itemsInfoArrangeWidths = new();

	// Items info from ItemsInfoRequested event (fast path)
	private double[] _itemsInfoDesiredAspectRatiosForFastPath;
	private double[] _itemsInfoMinWidthsForFastPath;
	private double[] _itemsInfoMaxWidthsForFastPath;

	// Items info (regular path)
	private readonly List<double> _itemsInfoDesiredAspectRatiosForRegularPath = new();
	private readonly List<double> _itemsInfoMinWidthsForRegularPath = new();
	private readonly List<double> _itemsInfoMaxWidthsForRegularPath = new();
	private int _itemsInfoFirstIndex = -1;
	private double _itemsInfoMinWidth = -1.0;
	private double _itemsInfoMaxWidth = -1.0;

	// State
	private int _measureCountdown = MeasureCountdownStart;
	private int _itemCount;
	private int _unsizedNearLineCount = -1;
	private int _unrealizedNearLineCount = -1;
	private int _firstSizedLineIndex = -1;
	private int _lastSizedLineIndex = -1;
	private int _firstSizedItemIndex = -1;
	private int _lastSizedItemIndex = -1;
	private int _firstFrozenLineIndex = -1;
	private int _lastFrozenLineIndex = -1;
	private int _firstFrozenItemIndex = -1;
	private int _lastFrozenItemIndex = -1;
	private int _anchorIndex = -1;
	private int _anchorIndexRetentionCountdown;

	private bool _isVirtualizingContext;
	private bool _wasInitializedForContext;
	private bool _forceRelayout;
	private bool _isFirstOrLastItemLocked;
	private float _previousAvailableWidth;
	private float _maxLineWidth;
	private double _roundingScaleFactor = 1.0;

	// Average items per line (raw, snapped)
	private (double Raw, double Snapped) _averageItemsPerLine;

	// Requested range for ItemsInfoRequested
	private int _requestedRangeStartIndex = -1;
	private int _requestedRangeLength;

	public LinedFlowLayout()
	{
		_elementManager = new ElementManager();
		LayoutId = "LinedFlowLayout";
		SetIndexBasedLayoutOrientation(IndexBasedLayoutOrientation.LeftToRight);
	}

	public int RequestedRangeStartIndex => _requestedRangeStartIndex;

	public int RequestedRangeLength => _requestedRangeLength;

	public void InvalidateItemsInfo()
	{
		ResetItemsInfo();
		InvalidateLayout();
	}

	public int LockItemToLine(int itemIndex)
	{
		if (itemIndex < 0 || itemIndex >= _itemCount)
		{
			throw new ArgumentOutOfRangeException(nameof(itemIndex));
		}

		if (_averageItemsPerLine.Snapped == 0.0)
		{
			return -1;
		}

		var usesFastPathLayout = UsesFastPathLayout();
		int lockedLineIndex = -1;

		if (itemIndex == 0)
		{
			_isFirstOrLastItemLocked = true;
			lockedLineIndex = 0;
		}
		else if (itemIndex == _itemCount - 1)
		{
			_isFirstOrLastItemLocked = true;
			lockedLineIndex = usesFastPathLayout ?
				GetLineIndex(itemIndex, usesFastPathLayout: true) :
				GetLineIndexFromAverageItemsPerLine(itemIndex, _averageItemsPerLine.Snapped);
		}

		if (_lockedItemIndexes.TryGetValue(itemIndex, out var existingLineIndex))
		{
			return existingLineIndex;
		}

		if (lockedLineIndex == -1)
		{
			if (usesFastPathLayout)
			{
				lockedLineIndex = GetLineIndex(itemIndex, usesFastPathLayout: true);
			}
			else
			{
				if (_firstFrozenItemIndex != -1 &&
					_lastFrozenItemIndex != -1 &&
					itemIndex >= _firstFrozenItemIndex &&
					itemIndex <= _lastFrozenItemIndex)
				{
					lockedLineIndex = GetFrozenLineIndexFromFrozenItemIndex(itemIndex);
				}
				else
				{
					lockedLineIndex = GetLineIndexFromAverageItemsPerLine(itemIndex, _averageItemsPerLine.Snapped);
				}
			}
		}

		if (itemIndex != 0 && itemIndex != _itemCount - 1)
		{
			_lockedItemIndexes[itemIndex] = lockedLineIndex;
		}

		return lockedLineIndex;
	}

	internal void SetDesiredAspectRatios(double[] values)
	{
		_itemsInfoDesiredAspectRatiosForFastPath = values;
	}

	internal void SetMinWidths(double[] values)
	{
		_itemsInfoMinWidthsForFastPath = values;
	}

	internal void SetMaxWidths(double[] values)
	{
		_itemsInfoMaxWidthsForFastPath = values;
	}

	protected override ItemCollectionTransitionProvider CreateDefaultItemTransitionProvider()
	{
		return new LinedFlowLayoutItemCollectionTransitionProvider();
	}

	protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
	{
		if (_wasInitializedForContext)
		{
			throw new InvalidOperationException("LinedFlowLayout cannot be shared.");
		}

		base.InitializeForContextCore(context);

		_wasInitializedForContext = true;
		_isVirtualizingContext = IsVirtualizingContext(context);
		_itemCount = context.ItemCount;
		_elementManager.SetContext(context);
	}

	protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
	{
		base.UninitializeForContextCore(context);

		_wasInitializedForContext = false;
		_itemCount = 0;

		if (_isVirtualizingContext)
		{
			_isVirtualizingContext = false;
			_elementManager.ClearRealizedRange();
		}

		UnlockItems();
	}

	protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
	{
		var availableWidth = availableSize.Width;

		if (!_isVirtualizingContext)
		{
			_forceRelayout = true;
			var newItemCount = context.ItemCount;

			if (!UsesArrangeWidthInfo())
			{
				if (_itemCount < newItemCount)
				{
					// Timer-based measure logic would go here
				}
			}

			_itemCount = newItemCount;
			UnlockItems();
		}

		if (_measureCountdown > 1)
		{
			_measureCountdown--;
		}

		// Update actual line height
		var actualLineHeight = UpdateActualLineHeight(context, availableSize);
		if (actualLineHeight <= 0)
		{
			return new Size(0, 0);
		}

		if (_itemCount == 0)
		{
			return new Size(availableWidth, 0);
		}

		var lineSpacing = LineSpacing;
		var minItemSpacing = MinItemSpacing;

		// Calculate average items per line
		var averageItemsPerLine = GetAverageItemsPerLine((float)availableWidth);
		if (_averageItemsPerLine != averageItemsPerLine)
		{
			SetAverageItemsPerLine(averageItemsPerLine);
		}

		var lineCount = GetLineCount(averageItemsPerLine.Snapped);
		var desiredHeight = lineCount * actualLineHeight + (lineCount - 1) * lineSpacing;

		// Measure items
		if (!_isVirtualizingContext)
		{
			// Non-virtualizing: measure all items
			MeasureAllItems(context, (float)availableWidth, actualLineHeight);
		}
		else
		{
			// Virtualizing: measure visible items
			MeasureVisibleItems(context, (float)availableWidth, actualLineHeight, lineSpacing, lineCount);
		}

		_previousAvailableWidth = (float)availableWidth;
		_forceRelayout = false;

		return new Size(availableWidth, desiredHeight);
	}

	protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
	{
		var actualLineHeight = ActualLineHeight;
		if (actualLineHeight <= 0 || _itemCount == 0)
		{
			return finalSize;
		}

		var lineSpacing = LineSpacing;
		var minItemSpacing = MinItemSpacing;
		var itemsJustification = ItemsJustification;
		var itemsStretch = ItemsStretch;

		var averageItemsPerLine = _averageItemsPerLine.Snapped;
		if (averageItemsPerLine <= 0)
		{
			return finalSize;
		}

		if (!_isVirtualizingContext)
		{
			ArrangeAllItems(context, finalSize, actualLineHeight, lineSpacing, minItemSpacing, itemsJustification, itemsStretch);
		}
		else
		{
			ArrangeVisibleItems(context, finalSize, actualLineHeight, lineSpacing, minItemSpacing, itemsJustification, itemsStretch);
		}

		return finalSize;
	}

	protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		base.OnItemsChangedCore(context, source, args);

		_itemCount = context.ItemCount;

		// Reset items info on collection changes
		if (args.Action == NotifyCollectionChangedAction.Reset ||
			args.Action == NotifyCollectionChangedAction.Add ||
			args.Action == NotifyCollectionChangedAction.Remove)
		{
			UnlockItems();
			ResetItemsInfo();
		}

		InvalidateLayout();
	}

	private void MeasureAllItems(VirtualizingLayoutContext context, float availableWidth, double actualLineHeight)
	{
		for (int i = 0; i < _itemCount; i++)
		{
			var element = context.GetOrCreateElementAt(i, ElementRealizationOptions.None);
			element.Measure(new Size(availableWidth, actualLineHeight));
		}
	}

	private void MeasureVisibleItems(VirtualizingLayoutContext context, float availableWidth, double actualLineHeight, double lineSpacing, int lineCount)
	{
		var realizationRect = context.RealizationRect;
		if (realizationRect.Height == 0)
		{
			return;
		}

		GetFirstAndLastDisplayedLineIndexes(
			realizationRect.Height,
			realizationRect.Y,
			0, // padding
			lineSpacing,
			actualLineHeight,
			lineCount,
			forFullyDisplayedLines: false,
			out var firstDisplayedLineIndex,
			out var lastDisplayedLineIndex);

		if (firstDisplayedLineIndex < 0 || lastDisplayedLineIndex < 0)
		{
			return;
		}

		var averageItemsPerLine = _averageItemsPerLine.Snapped;
		var firstItemIndex = (int)(firstDisplayedLineIndex * averageItemsPerLine);
		var lastItemIndex = Math.Min((int)((lastDisplayedLineIndex + 1) * averageItemsPerLine), _itemCount) - 1;

		for (int i = firstItemIndex; i <= lastItemIndex; i++)
		{
			var element = context.GetOrCreateElementAt(i, ElementRealizationOptions.None);
			element.Measure(new Size(availableWidth, actualLineHeight));
		}
	}

	private void ArrangeAllItems(VirtualizingLayoutContext context, Size finalSize, double actualLineHeight, double lineSpacing, double minItemSpacing, LinedFlowLayoutItemsJustification justification, LinedFlowLayoutItemsStretch stretch)
	{
		var averageItemsPerLine = _averageItemsPerLine.Snapped;
		int itemIndex = 0;
		double y = 0;

		while (itemIndex < _itemCount)
		{
			// Calculate items for this line
			var lineStartIndex = itemIndex;
			var lineEndIndex = Math.Min((int)((GetLineIndexFromAverageItemsPerLine(itemIndex, averageItemsPerLine) + 1) * averageItemsPerLine), _itemCount) - 1;
			var itemsInLine = lineEndIndex - lineStartIndex + 1;

			// Arrange items in this line
			ArrangeLine(context, finalSize.Width, y, actualLineHeight, minItemSpacing, justification, stretch, lineStartIndex, lineEndIndex);

			itemIndex = lineEndIndex + 1;
			y += actualLineHeight + lineSpacing;
		}
	}

	private void ArrangeVisibleItems(VirtualizingLayoutContext context, Size finalSize, double actualLineHeight, double lineSpacing, double minItemSpacing, LinedFlowLayoutItemsJustification justification, LinedFlowLayoutItemsStretch stretch)
	{
		var realizationRect = context.RealizationRect;
		var lineCount = GetLineCount(_averageItemsPerLine.Snapped);

		GetFirstAndLastDisplayedLineIndexes(
			realizationRect.Height,
			realizationRect.Y,
			0,
			lineSpacing,
			actualLineHeight,
			lineCount,
			forFullyDisplayedLines: false,
			out var firstDisplayedLineIndex,
			out var lastDisplayedLineIndex);

		if (firstDisplayedLineIndex < 0 || lastDisplayedLineIndex < 0)
		{
			return;
		}

		var averageItemsPerLine = _averageItemsPerLine.Snapped;

		for (int lineIndex = firstDisplayedLineIndex; lineIndex <= lastDisplayedLineIndex; lineIndex++)
		{
			var lineStartIndex = (int)(lineIndex * averageItemsPerLine);
			var lineEndIndex = Math.Min((int)((lineIndex + 1) * averageItemsPerLine), _itemCount) - 1;
			var y = lineIndex * (actualLineHeight + lineSpacing);

			ArrangeLine(context, finalSize.Width, y, actualLineHeight, minItemSpacing, justification, stretch, lineStartIndex, lineEndIndex);
		}
	}

	private void ArrangeLine(VirtualizingLayoutContext context, double availableWidth, double y, double lineHeight, double minItemSpacing, LinedFlowLayoutItemsJustification justification, LinedFlowLayoutItemsStretch stretch, int startIndex, int endIndex)
	{
		var itemCount = endIndex - startIndex + 1;
		if (itemCount <= 0)
		{
			return;
		}

		// Calculate total desired width
		double totalDesiredWidth = 0;
		for (int i = startIndex; i <= endIndex; i++)
		{
			var element = context.GetOrCreateElementAt(i, ElementRealizationOptions.None);
			totalDesiredWidth += element.DesiredSize.Width;
		}

		// Calculate spacing and starting position based on justification
		double spacing = minItemSpacing;
		double startX = 0;
		double extraSpace = availableWidth - totalDesiredWidth - (itemCount - 1) * minItemSpacing;

		if (stretch == LinedFlowLayoutItemsStretch.Fill && extraSpace > 0)
		{
			spacing = minItemSpacing + extraSpace / (itemCount - 1);
			extraSpace = 0;
		}

		switch (justification)
		{
			case LinedFlowLayoutItemsJustification.Start:
				startX = 0;
				break;
			case LinedFlowLayoutItemsJustification.Center:
				startX = extraSpace / 2;
				break;
			case LinedFlowLayoutItemsJustification.End:
				startX = extraSpace;
				break;
			case LinedFlowLayoutItemsJustification.SpaceAround:
				if (itemCount > 0)
				{
					var spacePerItem = extraSpace / itemCount;
					startX = spacePerItem / 2;
					spacing = minItemSpacing + spacePerItem;
				}
				break;
			case LinedFlowLayoutItemsJustification.SpaceBetween:
				if (itemCount > 1)
				{
					spacing = minItemSpacing + extraSpace / (itemCount - 1);
				}
				break;
			case LinedFlowLayoutItemsJustification.SpaceEvenly:
				if (itemCount > 0)
				{
					var spacePerGap = extraSpace / (itemCount + 1);
					startX = spacePerGap;
					spacing = minItemSpacing + spacePerGap;
				}
				break;
		}

		// Arrange each item
		double x = startX;
		for (int i = startIndex; i <= endIndex; i++)
		{
			var element = context.GetOrCreateElementAt(i, ElementRealizationOptions.None);
			var itemWidth = element.DesiredSize.Width;

			element.Arrange(new Rect(x, y, itemWidth, lineHeight));
			x += itemWidth + spacing;
		}
	}

	private double UpdateActualLineHeight(VirtualizingLayoutContext context, Size availableSize)
	{
		var lineHeight = LineHeight;
		double actualLineHeight;

		if (double.IsNaN(lineHeight) || lineHeight <= 0)
		{
			// Auto line height - determine from first item
			if (_itemCount > 0)
			{
				var firstElement = context.GetOrCreateElementAt(0, ElementRealizationOptions.None);
				firstElement.Measure(availableSize);
				actualLineHeight = firstElement.DesiredSize.Height;
			}
			else
			{
				actualLineHeight = 0;
			}
		}
		else
		{
			actualLineHeight = lineHeight;
		}

		if (ActualLineHeight != actualLineHeight)
		{
			ActualLineHeight = actualLineHeight;
		}

		return actualLineHeight;
	}

	private (double Raw, double Snapped) GetAverageItemsPerLine(float availableWidth)
	{
		if (_itemCount == 0 || availableWidth <= 0)
		{
			return (0, 0);
		}

		// Simple estimate: assume square items
		var actualLineHeight = ActualLineHeight;
		if (actualLineHeight <= 0)
		{
			return (1, 1);
		}

		var estimatedItemWidth = actualLineHeight; // Assume square
		var minItemSpacing = MinItemSpacing;
		var itemsPerLine = Math.Max(1, (availableWidth + minItemSpacing) / (estimatedItemWidth + minItemSpacing));

		return (itemsPerLine, Math.Max(1, Math.Round(itemsPerLine)));
	}

	private int GetLineCount(double averageItemsPerLine)
	{
		if (averageItemsPerLine <= 0 || _itemCount == 0)
		{
			return 0;
		}

		return (int)Math.Ceiling(_itemCount / averageItemsPerLine);
	}

	private int GetLineIndex(int itemIndex, bool usesFastPathLayout)
	{
		return GetLineIndexFromAverageItemsPerLine(itemIndex, _averageItemsPerLine.Snapped);
	}

	private int GetLineIndexFromAverageItemsPerLine(int itemIndex, double averageItemsPerLine)
	{
		if (averageItemsPerLine <= 0)
		{
			return 0;
		}

		return (int)(itemIndex / averageItemsPerLine);
	}

	private int GetFrozenLineIndexFromFrozenItemIndex(int frozenItemIndex)
	{
		if (_firstFrozenItemIndex == -1 || frozenItemIndex < _firstFrozenItemIndex)
		{
			return -1;
		}

		int lineIndex = _firstFrozenLineIndex;
		int itemIndex = _firstFrozenItemIndex;

		foreach (var count in _lineItemCounts)
		{
			if (frozenItemIndex < itemIndex + count)
			{
				return lineIndex;
			}
			itemIndex += count;
			lineIndex++;
		}

		return _lastFrozenLineIndex;
	}

	private void GetFirstAndLastDisplayedLineIndexes(
		double scrollViewport,
		double scrollOffset,
		double padding,
		double lineSpacing,
		double actualLineHeight,
		int lineCount,
		bool forFullyDisplayedLines,
		out int firstDisplayedLineIndex,
		out int lastDisplayedLineIndex)
	{
		firstDisplayedLineIndex = -1;
		lastDisplayedLineIndex = -1;

		if (lineCount == 0 || actualLineHeight <= 0)
		{
			return;
		}

		var lineHeightWithSpacing = actualLineHeight + lineSpacing;
		if (lineHeightWithSpacing <= 0)
		{
			return;
		}

		firstDisplayedLineIndex = Math.Max(0, (int)((scrollOffset - padding) / lineHeightWithSpacing));
		lastDisplayedLineIndex = Math.Min(lineCount - 1, (int)((scrollOffset + scrollViewport - padding) / lineHeightWithSpacing));

		if (forFullyDisplayedLines)
		{
			var firstLineTop = firstDisplayedLineIndex * lineHeightWithSpacing + padding;
			if (firstLineTop < scrollOffset - OffsetEqualityEpsilon)
			{
				firstDisplayedLineIndex++;
			}

			var lastLineBottom = (lastDisplayedLineIndex + 1) * lineHeightWithSpacing - lineSpacing + padding;
			if (lastLineBottom > scrollOffset + scrollViewport + OffsetEqualityEpsilon)
			{
				lastDisplayedLineIndex--;
			}
		}
	}

	private bool IsVirtualizingContext(VirtualizingLayoutContext context)
	{
		var realizationRect = context.RealizationRect;
		return !double.IsInfinity(realizationRect.Width) || !double.IsInfinity(realizationRect.Height);
	}

	private bool UsesFastPathLayout()
	{
		return _itemsInfoDesiredAspectRatiosForFastPath != null &&
			   _itemsInfoDesiredAspectRatiosForFastPath.Length == _itemCount;
	}

	private bool UsesArrangeWidthInfo()
	{
		return _itemsInfoArrangeWidths.Count > 0;
	}

	private void SetAverageItemsPerLine((double Raw, double Snapped) averageItemsPerLine, bool unlockItems = true)
	{
		if (_averageItemsPerLine != averageItemsPerLine)
		{
			_averageItemsPerLine = averageItemsPerLine;

			if (unlockItems)
			{
				UnlockItems();
			}
		}
	}

	private void UnlockItems()
	{
		if (_lockedItemIndexes.Count > 0 || _isFirstOrLastItemLocked)
		{
			_lockedItemIndexes.Clear();
			_isFirstOrLastItemLocked = false;
			_itemsUnlocked?.Invoke(this, null);
		}
	}

	private void ResetItemsInfo()
	{
		_itemsInfoDesiredAspectRatiosForFastPath = null;
		_itemsInfoMinWidthsForFastPath = null;
		_itemsInfoMaxWidthsForFastPath = null;
		_itemsInfoDesiredAspectRatiosForRegularPath.Clear();
		_itemsInfoMinWidthsForRegularPath.Clear();
		_itemsInfoMaxWidthsForRegularPath.Clear();
		_itemsInfoFirstIndex = -1;
		_itemsInfoMinWidth = -1.0;
		_itemsInfoMaxWidth = -1.0;
	}

	private void InvalidateLayout(bool forceRelayout = true, bool resetItemsInfo = false, bool invalidateMeasure = true)
	{
		_forceRelayout |= forceRelayout;

		if (resetItemsInfo)
		{
			ResetItemsInfo();
		}

		if (invalidateMeasure)
		{
			InvalidateMeasure();
		}
	}
}
