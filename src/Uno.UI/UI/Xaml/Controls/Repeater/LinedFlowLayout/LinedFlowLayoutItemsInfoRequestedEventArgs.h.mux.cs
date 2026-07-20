// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemsInfoRequestedEventArgs.h, commit b8cfb8490

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class LinedFlowLayoutItemsInfoRequestedEventArgs
{
	// ~LinedFlowLayoutItemsInfoRequestedEventArgs();

	// #pragma region ILinedFlowLayoutItemsInfoRequestedEventArgs

	/// <summary>
	/// Gets or sets the start index of the item range for which information is provided.
	/// </summary>
	public int ItemsRangeStartIndex
	{
		get => m_itemsRangeStartIndex;
		set => SetItemsRangeStartIndex(value);
	}

	/// <summary>
	/// Gets the requested number of items in the range.
	/// </summary>
	public int ItemsRangeRequestedLength => m_itemsRangeRequestedLength;

	/// <summary>
	/// Gets or sets the minimum item width for the requested range.
	/// </summary>
	public double MinWidth
	{
		get => m_minWidth;
		set => SetMinWidth(value);
	}

	/// <summary>
	/// Gets or sets the maximum item width for the requested range.
	/// </summary>
	public double MaxWidth
	{
		get => m_maxWidth;
		set => SetMaxWidth(value);
	}

	// #pragma endregion

	internal int ItemsRangeLength => m_itemsRangeLength;

	internal void ResetLinedFlowLayout() => m_linedFlowLayout = null;

	private int m_itemsRangeStartIndex;
	private int m_itemsRangeRequestedStartIndex;
	private int m_itemsRangeEstablishedLength;
	private int m_itemsRangeLength;
	private int m_itemsRangeRequestedLength;
	private double m_minWidth = -1.0;
	private double m_maxWidth = -1.0;
	private LinedFlowLayout? m_linedFlowLayout;
}
