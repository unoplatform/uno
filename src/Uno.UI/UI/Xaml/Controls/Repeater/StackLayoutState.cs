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
			// Uno-specific: grow the estimation buffer so each item index lands in its own slot when
			// possible. WinUI's hard-coded BufferSize=100 means item indices wrap around, so for lists
			// with more than 100 items the average is biased toward whichever 100 items happen to be
			// in the buffer at a given time — that's how high-variance lists end up with an extent
			// estimate that doesn't match real cumulative content size, which makes the trailing items
			// (e.g. items 195-199 of 200) unreachable at the trailing scroll edge or floats blank
			// space below the last item. With a per-item slot, once every index has been measured at
			// least once the running average equals the true average and the extent matches reality.
			if (elementIndex >= m_estimationBuffer.Length)
			{
				// Grow exponentially so we don't resize every measure pass; cap at a sane upper bound
				// so extremely large item counts don't pin the per-item buffer in memory indefinitely.
				const int MaxBufferSize = 100_000;
				var newSize = Math.Min(MaxBufferSize, Math.Max(elementIndex + 1, m_estimationBuffer.Length * 2));
				Array.Resize(ref m_estimationBuffer, newSize);
			}

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
