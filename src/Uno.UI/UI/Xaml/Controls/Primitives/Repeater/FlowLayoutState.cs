// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public class FlowLayoutState
	{
		private const int BufferSize = 100;

		readonly FlowLayoutAlgorithm m_flowAlgorithm = new FlowLayoutAlgorithm();
		readonly List<double> m_lineSizeEstimationBuffer = new List<double>(BufferSize);
		readonly List<double> m_itemsPerLineEstimationBuffer = new List<double>(BufferSize);
		double m_totalLineSize;
		int m_totalLinesMeasured;
		double m_totalItemsPerLine;
		Size m_specialElementDesiredSize;

		internal FlowLayoutAlgorithm FlowAlgorithm => m_flowAlgorithm;
		internal double TotalLineSize => m_totalLineSize;
		internal int TotalLinesMeasured => m_totalLinesMeasured;
		internal double TotalItemsPerLine => m_totalItemsPerLine;
		internal Size SpecialElementDesiredSize
		{
			get => m_specialElementDesiredSize;
			set => m_specialElementDesiredSize = value;
		}

		internal void InitializeForContext(VirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
		{
			m_flowAlgorithm.InitializeForContext(context, callbacks);

			//if (m_lineSizeEstimationBuffer.Count == 0)
			//{
			//	m_lineSizeEstimationBuffer.resize(BufferSize, 0.0f);
			//	m_itemsPerLineEstimationBuffer.resize(BufferSize, 0.0f);
			//}

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
				int estimationBufferIndex = startIndex % m_lineSizeEstimationBuffer.Count;
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
