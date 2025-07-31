// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class StackLayoutState
	{
		private const int BufferSize = 100;

		private FlowLayoutAlgorithm m_flowAlgorithm;
		private double[] m_estimationBuffer = new double[BufferSize];
		private double m_totalElementSize;
		// During the measure pass, as we measure the elements, we will keep track
		// of the largest arrange bounds in the non-virtualizing direction. This value
		// is going to be used in the calculation of the extent.
		private double m_maxArrangeBounds;
		private int m_totalElementsMeasured;

		internal FlowLayoutAlgorithm FlowAlgorithm => m_flowAlgorithm;
		internal double TotalElementSize => m_totalElementSize;
		internal double MaxArrangeBounds => m_maxArrangeBounds;
		internal int TotalElementsMeasured => m_totalElementsMeasured;

		// Uno workaround [BEGIN]: Backing field for uno workarounds
		internal double Uno_LastKnownAverageElementSize;
		internal int Uno_LastKnownRealizedElementsCount;
		internal int Uno_LastKnownItemsCount;
		internal Size Uno_LastKnownDesiredSize;
		// Uno workaround [END]

		public StackLayoutState()
		{
			m_flowAlgorithm = new FlowLayoutAlgorithm();
		}

		internal void InitializeForContext(VirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
		{
			m_flowAlgorithm.InitializeForContext(context, callbacks);
			if (m_estimationBuffer.Length == 0)
			{
				Array.Resize(ref m_estimationBuffer, BufferSize);
			}

			context.LayoutStateCore = this;
		}

		internal void UninitializeForContext(VirtualizingLayoutContext context)
		{
			m_flowAlgorithm.UninitializeForContext(context);
		}

		internal void OnElementMeasured(int elementIndex, double majorSize, double minorSize)
		{
			int estimationBufferIndex = elementIndex % m_estimationBuffer.Length;
			bool alreadyMeasured = m_estimationBuffer[estimationBufferIndex] != 0;
			if (!alreadyMeasured)
			{
				m_totalElementsMeasured++;
			}

			m_totalElementSize -= m_estimationBuffer[estimationBufferIndex];
			m_totalElementSize += majorSize;
			m_estimationBuffer[estimationBufferIndex] = majorSize;

			m_maxArrangeBounds = Math.Max(m_maxArrangeBounds, minorSize);
		}

		internal void OnMeasureStart()
		{
			m_maxArrangeBounds = 0.0;
		}
	}
}
