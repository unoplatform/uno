// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsRepeaterScrollHost.h, commit 4b206bce3

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Private.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsRepeaterScrollHost
{
	private bool HasPendingBringIntoView => m_pendingBringIntoView.TargetElement is { };

	private class CandidateInfo
	{
		public static readonly Rect InvalidBounds = new Rect(-1.0f, -1.0f, -1.0f, -1.0f);

		public CandidateInfo(UIElement element)
		{
			Element = element;
			RelativeBounds = InvalidBounds;
		}

		public UIElement Element { get; }

		public Rect RelativeBounds { get; set; }

		public bool IsRelativeBoundsSet => RelativeBounds != InvalidBounds;
	}

	private struct BringIntoViewState
	{
		public BringIntoViewState(
			UIElement targetElement,
			double alignmentX,
			double alignmentY,
			double offsetX,
			double offsetY,
			bool animate)
		{
			TargetElement = targetElement;
			AlignmentX = alignmentX;
			AlignmentY = alignmentY;
			OffsetX = offsetX;
			OffsetY = offsetY;
			Animate = animate;

			ChangeViewCalled = default;
			ChangeViewOffset = default;
		}

		public UIElement TargetElement { get; private set; }
		public double AlignmentX { get; private set; }
		public double AlignmentY { get; private set; }
		public double OffsetX { get; private set; }
		public double OffsetY { get; private set; }
		public bool Animate { get; private set; }
		public bool ChangeViewCalled { get; set; }
		public Point ChangeViewOffset { get; set; }

		public void Reset()
		{
			TargetElement = null;
			AlignmentX = AlignmentY = OffsetX = OffsetY = 0.0;
			Animate = ChangeViewCalled = false;
			ChangeViewOffset = default;
		}
	}

	private List<CandidateInfo> m_candidates = new List<CandidateInfo>();

	private UIElement m_anchorElement;
	private Rect m_anchorElementRelativeBounds;
	// Whenever the m_candidates list changes, we set this to true.
	private bool m_isAnchorElementDirty = true;

#pragma warning disable 169, IDE0051 // Field never used, kept for 1:1 parity with WinUI ItemsRepeaterScrollHost.h:165-166. Uno replaces these with auto-properties in ItemsRepeaterScrollHost.mux.cs.
	private double m_horizontalEdge;
	private double m_verticalEdge;    // Not used in this temporary implementation.
#pragma warning restore 169, IDE0051

	// We can only bring an element into view after it got arranged and
	// we know its bounds as well as the viewport (so that we can account
	// for alignment and offset).
	// The BringIntoView call can however be made at any point, even
	// in the constructor of a page (deserialization scenario) so we
	// need to hold on the parameter that are passed in BringIntoViewOperation.
	private BringIntoViewState m_pendingBringIntoView;

	// A ScrollViewer.ChangeView operation, even if not animated, is not synchronous.
	// In other words, right after the call, ScrollViewer.[Vertical|Horizontal]Offset and
	// TransformToVisual are not going to reflect the new viewport. We need to keep
	// track of the pending viewport shift until the ChangeView operation completes
	// asynchronously.
	private double m_pendingViewportShift;

	private event ViewportChangedEventHandler m_viewportChanged;
	private event PostArrangeEventHandler m_postArrange;
#pragma warning disable 67 // Event never invoked
	private event ConfigurationChangedEventHandler m_configurationChanged;
#pragma warning restore 67

	// Uno specific: WinUI tracks subscriptions via auto_revoker; we use IDisposables.
	private IDisposable m_scrollViewerViewChanging;
	private IDisposable m_scrollViewerViewChanged;
	private IDisposable m_scrollViewerSizeChanged;
}
