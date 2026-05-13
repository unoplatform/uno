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
		// Snapshot of the previous GetExtent result, used to keep extent.MajorStart (layout origin)
		// stable across measure passes where the running-average element size shifted (typical when
		// a tall item enters or leaves the 100-slot estimation buffer during wheel scroll). Without
		// this, avg fluctuations translate directly into items repositioning in IR-local space
		// because each item's IR-local Y = algorithm Y - layout origin Y. Reset on anchor-jump
		// (FlowLayoutAlgorithm.Uno_LastMeasureDidAnchorJump) so MakeAnchor / disconnected window
		// rebuild re-bases the layout origin, and on back-to-top (firstRealizedItemIndex == 0) so
		// the natural extent.MajorStart=0 is re-established at offset 0.
		internal float Uno_LastReportedExtentMajorStart = float.NaN;
		// Counter that opts subsequent measures out of stable-MajorStart caching so the layout
		// cascade following a MakeAnchor (scroll-to-bottom / BringIntoView landing on a
		// disconnected window) lets each measure track the formula-based origin until items
		// settle. Decremented on every measure until 0; reset to a small N every time
		// FlowLayoutAlgorithm.Uno_LastMeasureDidAnchorJump is observed. Wheel-scroll measures
		// never trigger this so the stable-MajorStart fix continues to suppress flicker there.
		internal int Uno_PostAnchorJumpRefreshCountdown;
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
