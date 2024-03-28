// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Numerics;
using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class ScrollPresenter
{
#if false
	private bool IsHorizontallyScrollable()
	{
		return m_contentOrientation != ScrollingContentOrientation.Vertical;
	}

	private bool IsVerticallyScrollable()
	{
		return m_contentOrientation != ScrollingContentOrientation.Horizontal;
	}
#endif

	//private event_token ViewportChanged(ViewportChangedEventHandler value)
	//{
	//	return m_viewportChanged.add(value);
	//}

	//private void ViewportChanged(event_token token)
	//{
	//	m_viewportChanged.remove(token);
	//}

	//private event_token PostArrange(PostArrangeEventHandler value)
	//{
	//	return m_postArrange.add(value);
	//}

	//private void PostArrange(event_token token)
	//{
	//	m_postArrange.remove(token);
	//}

	//private event_token ConfigurationChanged(ConfigurationChangedEventHandler value)
	//{
	//	return m_configurationChanged.add(value);
	//}

	//private void ConfigurationChanged(event_token token)
	//{
	//	m_configurationChanged.remove(token);
	//}

#if false
	private Rect GetRelativeViewport(
		UIElement child)
	{
		// The commented out code is expected to work but somehow the child.TransformToVisual(*this)
		// transform returns unexpected values shortly after a ScrollPresenter.Content layout offset change.
		// Bug 14999031 is tracking this issue. For now the m_contentLayoutOffsetX/Y, m_zoomedHorizontalOffset,
		// m_zoomedVerticalOffset usage below mitigates the problem.

		//const GeneralTransform transform = child.TransformToVisual(*this);
		GeneralTransform transform = child.TransformToVisual(Content);
		Point elementOffset = transform.TransformPoint(default(Point));
		float viewportWidth = (float)(m_viewportWidth / m_zoomFactor);
		float viewportHeight = (float)(m_viewportHeight / m_zoomFactor);

		//Rect result = { -elementOffset.X / m_zoomFactor,
		//                       -elementOffset.Y / m_zoomFactor,
		//                       viewportWidth, viewportHeight };

		Vector2 minPosition;

		ComputeMinMaxPositions(m_zoomFactor, out minPosition, out _);

		Rect result = new Rect((minPosition.X - m_contentLayoutOffsetX + (float)m_zoomedHorizontalOffset - elementOffset.X) / m_zoomFactor,
			(minPosition.Y - m_contentLayoutOffsetY + (float)m_zoomedVerticalOffset - elementOffset.Y) / m_zoomFactor,
			viewportWidth, viewportHeight);

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, child, TypeLogging::RectToString(result).c_str());

		return result;
	}
#endif

	public UIElement CurrentAnchor => AnchorElement();

	private UIElement AnchorElement()
	{
		bool isAnchoringElementHorizontally = false;
		bool isAnchoringElementVertically = false;

		IsAnchoring(out isAnchoringElementHorizontally, out isAnchoringElementVertically, out _, out _);

		if (isAnchoringElementHorizontally || isAnchoringElementVertically)
		{
			EnsureAnchorElementSelection();
		}

		var value = m_anchorElement;
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, value);

		return value;
	}

	public void RegisterAnchorCandidate(UIElement element)
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR_DBL, METH_NAME, this, element, VerticalAnchorRatio());
#endif

		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		if (!double.IsNaN(HorizontalAnchorRatio) || !double.IsNaN(VerticalAnchorRatio))
		{
#if DEBUG
			// We should not be registring the same element twice. Even through it is functionally ok,
			// we will end up spending more time during arrange than we must.
			// However checking if an element is already in the list every time a new element is registered is worse for perf.
			// So, I'm leaving an assert here to catch regression in our code but in release builds we run without the check.
			UIElement anchorCandidate = element;

			if (m_anchorCandidates.Any(a => a == anchorCandidate))
			{
				MUX_ASSERT(false);
			}
#endif // DBG

			m_anchorCandidates.Add(element);
			m_isAnchorElementDirty = true;
		}
	}

	public void UnregisterAnchorCandidate(UIElement element)
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, element);
#endif

		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		if (m_anchorCandidates.Remove(element))
		{
			m_isAnchorElementDirty = true;
		}
	}
}
