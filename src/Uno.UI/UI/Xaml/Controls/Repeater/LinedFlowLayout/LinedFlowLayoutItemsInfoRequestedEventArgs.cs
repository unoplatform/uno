// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemsInfoRequestedEventArgs.cpp, tag winui3/release/1.8.4

using System;

namespace Microsoft.UI.Xaml.Controls;

public partial class LinedFlowLayoutItemsInfoRequestedEventArgs
{
	private int _itemsRangeStartIndex;
	private readonly int _itemsRangeRequestedStartIndex;
	private int _itemsRangeEstablishedLength;
	private int _itemsRangeLength;
	private readonly int _itemsRangeRequestedLength;
	private double _minWidth = -1.0;
	private double _maxWidth = -1.0;
	private WeakReference<LinedFlowLayout> _linedFlowLayout;

	internal LinedFlowLayoutItemsInfoRequestedEventArgs(
		LinedFlowLayout linedFlowLayout,
		int itemsRangeStartIndex,
		int itemsRangeRequestedLength)
	{
		_itemsRangeStartIndex = itemsRangeStartIndex;
		_itemsRangeRequestedStartIndex = itemsRangeStartIndex;
		_itemsRangeRequestedLength = itemsRangeRequestedLength;
		_linedFlowLayout = new WeakReference<LinedFlowLayout>(linedFlowLayout);
	}

	public int ItemsRangeStartIndex
	{
		get => _itemsRangeStartIndex;
		set
		{
			if (value < 0)
			{
				throw new ArgumentException("'ItemsRangeStartIndex' must be positive.");
			}

			if (value > _itemsRangeStartIndex)
			{
				throw new ArgumentException("'ItemsRangeStartIndex' cannot be increased.");
			}

			if (_itemsRangeEstablishedLength != 0 && value + _itemsRangeEstablishedLength < _itemsRangeRequestedStartIndex + _itemsRangeRequestedLength)
			{
				throw new ArgumentException("Value is too small given the array length already provided.");
			}

			_itemsRangeStartIndex = value;
		}
	}

	public int ItemsRangeRequestedLength => _itemsRangeRequestedLength;

	public double MinWidth
	{
		get => _minWidth;
		set => _minWidth = value < 0.0 ? -1.0 : value;
	}

	public double MaxWidth
	{
		get => _maxWidth;
		set => _maxWidth = value < 0.0 ? -1.0 : value;
	}

	internal int ItemsRangeLength => _itemsRangeLength;

	internal void ResetLinedFlowLayout() => _linedFlowLayout = null;

	public void SetDesiredAspectRatios(double[] values)
	{
		if (_linedFlowLayout?.TryGetTarget(out var linedFlowLayout) == true)
		{
			SetItemsRangeEstablishedLength(values.Length);
			_itemsRangeLength = _itemsRangeEstablishedLength;
			linedFlowLayout.SetDesiredAspectRatios(values);
		}
	}

	public void SetMinWidths(double[] values)
	{
		if (_linedFlowLayout?.TryGetTarget(out var linedFlowLayout) == true)
		{
			SetItemsRangeEstablishedLength(values.Length);
			linedFlowLayout.SetMinWidths(values);
		}
	}

	public void SetMaxWidths(double[] values)
	{
		if (_linedFlowLayout?.TryGetTarget(out var linedFlowLayout) == true)
		{
			SetItemsRangeEstablishedLength(values.Length);
			linedFlowLayout.SetMaxWidths(values);
		}
	}

	private void SetItemsRangeEstablishedLength(int value)
	{
		if (value != _itemsRangeEstablishedLength)
		{
			if (value < _itemsRangeRequestedLength && _itemsRangeStartIndex == _itemsRangeRequestedStartIndex)
			{
				throw new ArgumentException("The provided array length must be greater than or equal to 'ItemsRangeRequestedLength'.");
			}

			if (_itemsRangeStartIndex + value < _itemsRangeRequestedStartIndex + _itemsRangeRequestedLength && _itemsRangeStartIndex < _itemsRangeRequestedStartIndex)
			{
				throw new ArgumentException("The provided array length is too small given the decreased 'ItemsRangeStartIndex' and the 'ItemsRangeRequestedLength' values.");
			}

			if (_itemsRangeEstablishedLength > 0)
			{
				throw new ArgumentException("All provided arrays must have the same length.");
			}

			_itemsRangeEstablishedLength = value;
		}
	}
}
