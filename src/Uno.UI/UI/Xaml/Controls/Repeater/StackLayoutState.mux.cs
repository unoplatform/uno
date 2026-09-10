// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference StackLayoutState.cpp, commit 4b206bce3

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class StackLayoutState
{
	internal void InitializeForContext(
		VirtualizingLayoutContext context,
		IFlowLayoutAlgorithmDelegates callbacks)
	{
		m_flowAlgorithm.InitializeForContext(context, callbacks);
		if (m_estimationBuffer.Length == 0)
		{
			Array.Resize(ref m_estimationBuffer, BufferSize);
		}

		context.LayoutStateCore = this;
	}

	internal void UninitializeForContext(VirtualizingLayoutContext context)
		=> m_flowAlgorithm.UninitializeForContext(context);

	internal void OnElementMeasured(int elementIndex, double majorSize, double minorSize)
	{
		int estimationBufferIndex = elementIndex % m_estimationBuffer.Length;
		bool alreadyMeasured = m_estimationBuffer[estimationBufferIndex] != 0;
		double lastElementSize = m_lastElementSize;

		if (!alreadyMeasured)
		{
			m_totalElementsMeasured++;
		}

		m_totalElementSize -= m_estimationBuffer[estimationBufferIndex];
		m_totalElementSize += majorSize;
		m_lastElementSize = majorSize;
		m_estimationBuffer[estimationBufferIndex] = majorSize;

		if (m_areElementsMeasuredRegular && lastElementSize != 0.0 && lastElementSize != m_lastElementSize)
		{
			// Elements in the StackLayout are declared irregular as two elements have different desired major sizes.
			m_areElementsMeasuredRegular = false;
		}

		m_maxArrangeBounds = Math.Max(m_maxArrangeBounds, minorSize);
	}

	internal void OnMeasureStart() => m_maxArrangeBounds = 0.0;

	// Invoked when the StackLayout's source is reset with a NotifyCollectionChangedAction::Reset.
	internal void OnElementSizesReset()
	{
		// Assume the new elements are regular again.
		m_areElementsMeasuredRegular = true;
		m_totalElementsMeasured = 0;
		m_totalElementSize = 0.0;
		m_lastElementSize = 0.0;
		Array.Clear(m_estimationBuffer);
	}
}
