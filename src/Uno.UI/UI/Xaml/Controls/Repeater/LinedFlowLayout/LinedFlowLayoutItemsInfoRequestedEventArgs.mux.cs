// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemsInfoRequestedEventArgs.cpp, commit b8cfb8490

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class LinedFlowLayoutItemsInfoRequestedEventArgs
{
	internal LinedFlowLayoutItemsInfoRequestedEventArgs(
		LinedFlowLayout linedFlowLayout,
		int itemsRangeStartIndex,
		int itemsRangeRequestedLength)
	{
		m_itemsRangeStartIndex = itemsRangeStartIndex;
		m_itemsRangeRequestedStartIndex = itemsRangeStartIndex;
		m_itemsRangeRequestedLength = itemsRangeRequestedLength;

		MUX_ASSERT(itemsRangeStartIndex >= 0);
		MUX_ASSERT(itemsRangeRequestedLength > 0);

		// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT_INT, METH_NAME, this, itemsRangeStartIndex, itemsRangeRequestedLength);
		m_linedFlowLayout = linedFlowLayout;
	}

	// LinedFlowLayoutItemsInfoRequestedEventArgs::~LinedFlowLayoutItemsInfoRequestedEventArgs()
	// {
	//     LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	// #pragma region ILinedFlowLayoutItemsInfoRequestedEventArgs

	private void SetItemsRangeStartIndex(int value)
	{
		// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, value);
		if (value < 0)
		{
			throw new ArgumentException("'ItemsRangeStartIndex' must be positive.");
		}

		if (value > m_itemsRangeStartIndex)
		{
			throw new ArgumentException("'ItemsRangeStartIndex' cannot be increased.");
		}

		if (m_itemsRangeEstablishedLength != 0 && value + m_itemsRangeEstablishedLength < m_itemsRangeRequestedStartIndex + m_itemsRangeRequestedLength)
		{
			throw new ArgumentException("Value is too small given the array length already provided.");
		}

		m_itemsRangeStartIndex = value;
	}

	private void SetMinWidth(double value)
	{
		if (value < 0.0)
		{
			m_minWidth = -1.0;
		}
		else
		{
			m_minWidth = value;
		}
	}

	private void SetMaxWidth(double value)
	{
		if (value < 0.0)
		{
			m_maxWidth = -1.0;
		}
		else
		{
			m_maxWidth = value;
		}
	}

	/// <summary>
	/// Sets the desired aspect ratios for the item range.
	/// </summary>
	/// <param name="values">The desired aspect ratios.</param>
	public void SetDesiredAspectRatios(double[] values)
	{
		if (m_linedFlowLayout is { } linedFlowLayout)
		{
			SetItemsRangeEstablishedLength(values.Length);

			m_itemsRangeLength = m_itemsRangeEstablishedLength;

			linedFlowLayout.SetDesiredAspectRatios(values);
		}
	}

	/// <summary>
	/// Sets the minimum widths for the item range.
	/// </summary>
	/// <param name="values">The minimum widths.</param>
	public void SetMinWidths(double[] values)
	{
		if (m_linedFlowLayout is { } linedFlowLayout)
		{
			SetItemsRangeEstablishedLength(values.Length);

			linedFlowLayout.SetMinWidths(values);
		}
	}

	/// <summary>
	/// Sets the maximum widths for the item range.
	/// </summary>
	/// <param name="values">The maximum widths.</param>
	public void SetMaxWidths(double[] values)
	{
		if (m_linedFlowLayout is { } linedFlowLayout)
		{
			SetItemsRangeEstablishedLength(values.Length);

			linedFlowLayout.SetMaxWidths(values);
		}
	}

	private void SetItemsRangeEstablishedLength(int value)
	{
		if (value != m_itemsRangeEstablishedLength)
		{
			if (value < m_itemsRangeRequestedLength && m_itemsRangeStartIndex == m_itemsRangeRequestedStartIndex)
			{
				throw new ArgumentException("The provided array length must be greater than or equal to 'ItemsRangeRequestedLength'.");
			}

			if (m_itemsRangeStartIndex + value < m_itemsRangeRequestedStartIndex + m_itemsRangeRequestedLength && m_itemsRangeStartIndex < m_itemsRangeRequestedStartIndex)
			{
				throw new ArgumentException("The provided array length is too small given the decreased 'ItemsRangeStartIndex' and the 'ItemsRangeRequestedLength' values.");
			}

			if (m_itemsRangeEstablishedLength > 0)
			{
				throw new ArgumentException("All provided arrays must have the same length.");
			}

			m_itemsRangeEstablishedLength = value;
		}
	}

	// #pragma endregion
}
