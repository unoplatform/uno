// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlowLayoutState.cpp, commit 4b206bce3

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class FlowLayoutState
{
	internal void InitializeForContext(
		VirtualizingLayoutContext context,
		IFlowLayoutAlgorithmDelegates callbacks)
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
		=> m_flowAlgorithm.UninitializeForContext(context);

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
