// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class FlowLayoutState
	{
		private const int BufferSize = 100;

		private readonly FlowLayoutAlgorithm m_flowAlgorithm = new FlowLayoutAlgorithm();
		private double[] m_lineSizeEstimationBuffer = new double[BufferSize];
		private double[] m_itemsPerLineEstimationBuffer = new double[BufferSize];
		private double m_totalLineSize;
		private int m_totalLinesMeasured;
		private double m_totalItemsPerLine;
		private Size m_specialElementDesiredSize;

		internal FlowLayoutAlgorithm FlowAlgorithm => m_flowAlgorithm;
		internal double TotalLineSize => m_totalLineSize;
		internal int TotalLinesMeasured => m_totalLinesMeasured;
		internal double TotalItemsPerLine => m_totalItemsPerLine;
		internal Size SpecialElementDesiredSize
		{
			get => m_specialElementDesiredSize;
			set => m_specialElementDesiredSize = value;
		}

		internal double Uno_LastKnownAverageLineSize;

		internal void InitializeForContext(VirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
		{
			m_flowAlgorithm.InitializeForContext(context, callbacks);

			if (m_lineSizeEstimationBuffer.Length == 0)
			{
				Array.Resize(ref m_lineSizeEstimationBuffer, BufferSize);
				Array.Resize(ref m_itemsPerLineEstimationBuffer, BufferSize);
			}

			context.LayoutStateCore = this;
		}

		internal void UninitializeForContext(VirtualizingLayoutContext context)
		{
			m_flowAlgorithm.UninitializeForContext(context);
		}

		internal void OnLineArranged(int startIndex, int countInLine, double lineSize, VirtualizingLayoutContext context)
		{
			// If we do not have any estimation information, use the line for estimation. 
			// If we do have some estimation information, don't account for the last line which is quite likely
			// different from the rest of the lines and can throw off estimation.
			if (m_totalLinesMeasured == 0 || startIndex + countInLine != context.ItemCount)
			{
				int estimationBufferIndex = startIndex % m_lineSizeEstimationBuffer.Length;
				bool alreadyMeasured = m_lineSizeEstimationBuffer[estimationBufferIndex] != 0;

				if (!alreadyMeasured)
				{
					++m_totalLinesMeasured;
				}

				m_totalLineSize -= m_lineSizeEstimationBuffer[estimationBufferIndex];
				m_totalLineSize += lineSize;
				m_lineSizeEstimationBuffer[estimationBufferIndex] = lineSize;

				m_totalItemsPerLine -= m_itemsPerLineEstimationBuffer[estimationBufferIndex];
				m_totalItemsPerLine += countInLine;
				m_itemsPerLineEstimationBuffer[estimationBufferIndex] = countInLine;
			}
		}
	}
}
