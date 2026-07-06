// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemsInfoRequestedEventArgs.cpp, commit b8cfb8490

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the <see cref="LinedFlowLayout.ItemsInfoRequested"/> event, allowing a
	/// handler to supply the desired aspect ratios and width bounds for a range of items.
	/// </summary>
	public partial class LinedFlowLayoutItemsInfoRequestedEventArgs
	{
		private int m_itemsRangeStartIndex;
		private int m_itemsRangeRequestedStartIndex;
		private int m_itemsRangeEstablishedLength;
		private int m_itemsRangeLength;
		private int m_itemsRangeRequestedLength;
		private double m_minWidth = -1.0;
		private double m_maxWidth = -1.0;
		private LinedFlowLayout? m_linedFlowLayout;

		internal LinedFlowLayoutItemsInfoRequestedEventArgs(
			LinedFlowLayout linedFlowLayout,
			int itemsRangeStartIndex,
			int itemsRangeRequestedLength)
		{
			MUX_ASSERT(itemsRangeStartIndex >= 0);
			MUX_ASSERT(itemsRangeRequestedLength > 0);

			m_itemsRangeStartIndex = itemsRangeStartIndex;
			m_itemsRangeRequestedStartIndex = itemsRangeStartIndex;
			m_itemsRangeRequestedLength = itemsRangeRequestedLength;

			m_linedFlowLayout = linedFlowLayout;
		}

		#region ILinedFlowLayoutItemsInfoRequestedEventArgs

		public int ItemsRangeStartIndex
		{
			get => m_itemsRangeStartIndex;
			set
			{
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
		}

		public int ItemsRangeRequestedLength => m_itemsRangeRequestedLength;

		public double MinWidth
		{
			get => m_minWidth;
			set => m_minWidth = value < 0.0 ? -1.0 : value;
		}

		public double MaxWidth
		{
			get => m_maxWidth;
			set => m_maxWidth = value < 0.0 ? -1.0 : value;
		}

		public void SetDesiredAspectRatios(double[] values)
		{
			if (m_linedFlowLayout is { } linedFlowLayout)
			{
				SetItemsRangeEstablishedLength(values.Length);

				m_itemsRangeLength = m_itemsRangeEstablishedLength;

				linedFlowLayout.SetDesiredAspectRatios(values);
			}
		}

		public void SetMinWidths(double[] values)
		{
			if (m_linedFlowLayout is { } linedFlowLayout)
			{
				SetItemsRangeEstablishedLength(values.Length);

				linedFlowLayout.SetMinWidths(values);
			}
		}

		public void SetMaxWidths(double[] values)
		{
			if (m_linedFlowLayout is { } linedFlowLayout)
			{
				SetItemsRangeEstablishedLength(values.Length);

				linedFlowLayout.SetMaxWidths(values);
			}
		}

		#endregion

		internal int ItemsRangeLength => m_itemsRangeLength;

		internal void ResetLinedFlowLayout() => m_linedFlowLayout = null;

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
	}
}
