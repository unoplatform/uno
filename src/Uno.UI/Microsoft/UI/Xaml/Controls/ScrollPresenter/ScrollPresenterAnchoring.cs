// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Private.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class ScrollPresenter : FrameworkElement
{
	// Used when ScrollPresenter.HorizontalAnchorRatio or ScrollPresenter.VerticalAnchorRatio is 0.0 or 1.0 to determine whether the Content is scrolled to an edge.
	// It is declared at an edge if it's within 1/10th of a pixel.
	private const double c_edgeDetectionTolerance = 0.1;

	// private void RaiseConfigurationChanged()
	// {
	// 	if (m_configurationChanged)
	// 	{
	// 		SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

	// 		m_configurationChanged(*this);
	// 	}
	// }

	// private void RaisePostArrange()
	// {
	// 	if (m_postArrange)
	// 	{
	// 		SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

	// 		m_postArrange(*this);
	// 	}
	// }

	// void RaiseViewportChanged(const bool isFinal)
	// {
	// 	if (m_viewportChanged)
	// 	{
	// 		SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

	// 		m_viewportChanged(*this, isFinal);
	// 	}
	// }

	private void RaiseAnchorRequested()
	{
		if (AnchorRequested is not null)
		{
			// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

			if (m_anchorRequestedEventArgs is null)
			{
				m_anchorRequestedEventArgs = new ScrollingAnchorRequestedEventArgs(this);
			}

			ScrollingAnchorRequestedEventArgs anchorRequestedEventArgs = m_anchorRequestedEventArgs;

			anchorRequestedEventArgs.SetAnchorElement(null);
			anchorRequestedEventArgs.SetAnchorCandidates(m_anchorCandidates);
			AnchorRequested.Invoke(this, anchorRequestedEventArgs);
		}
	}

	// Computes the type of anchoring to perform, if any, based on ScrollPresenter.HorizontalAnchorRatio, ScrollPresenter.VerticalAnchorRatio, 
	// the current offsets, zoomFactor, viewport size, content size and state.
	// When all 4 returned booleans are False, no element anchoring is performed, no far edge anchoring is performed. There may still be anchoring at near edges.
	private void IsAnchoring(
		out bool isAnchoringElementHorizontally,
		out bool isAnchoringElementVertically,
		out bool isAnchoringFarEdgeHorizontally,
		out bool isAnchoringFarEdgeVertically)
	{
		isAnchoringElementHorizontally = false;
		isAnchoringElementVertically = false;
		isAnchoringFarEdgeHorizontally = false;
		isAnchoringFarEdgeVertically = false;

		// Mouse wheel comes in as a custom animation, and we are currently not 
		// anchoring because of the check below. Unfortunately, I cannot validate that
		// removing the check is the correct fix due to dcomp bug 17523225. I filed a 
		// tracking bug to follow up once the dcomp bug is fixed.
		// Bug 17523266: ScrollPresenter is not anchoring during mouse wheel
		if (m_interactionTracker is null || m_state == ScrollingInteractionState.Animation)
		{
			// Skip calls to SetContentLayoutOffsetX / SetContentLayoutOffsetY when the InteractionTracker has not been set up yet,
			// or when it is performing a custom animation because if would result in a visual flicker.
			return;
		}

		double horizontalAnchorRatio = HorizontalAnchorRatio;
		double verticalAnchorRatio = VerticalAnchorRatio;

		// For edge anchoring, the near edge is considered when HorizontalAnchorRatio or VerticalAnchorRatio is 0.0. 
		// When the property is 1.0, the far edge is considered.
		if (!double.IsNaN(horizontalAnchorRatio))
		{
#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"HorizontalAnchorRatio", horizontalAnchorRatio);
#endif

			MUX_ASSERT(horizontalAnchorRatio >= 0.0);
			MUX_ASSERT(horizontalAnchorRatio <= 1.0);

			if (horizontalAnchorRatio == 0.0 || horizontalAnchorRatio == 1.0)
			{
				if (horizontalAnchorRatio == 1.0 && m_zoomedHorizontalOffset + m_viewportWidth - m_unzoomedExtentWidth * m_zoomFactor > -c_edgeDetectionTolerance)
				{
					isAnchoringFarEdgeHorizontally = true;
				}
				else if (!(horizontalAnchorRatio == 0.0 && m_zoomedHorizontalOffset < c_edgeDetectionTolerance))
				{
					isAnchoringElementHorizontally = true;
				}
			}
			else
			{
				isAnchoringElementHorizontally = true;
			}
		}

		if (!double.IsNaN(verticalAnchorRatio))
		{
#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"VerticalAnchorRatio", verticalAnchorRatio);
#endif

			MUX_ASSERT(verticalAnchorRatio >= 0.0);
			MUX_ASSERT(verticalAnchorRatio <= 1.0);

			if (verticalAnchorRatio == 0.0 || verticalAnchorRatio == 1.0)
			{
				if (verticalAnchorRatio == 1.0 && m_zoomedVerticalOffset + m_viewportHeight - m_unzoomedExtentHeight * m_zoomFactor > -c_edgeDetectionTolerance)
				{
					isAnchoringFarEdgeVertically = true;
				}
				else if (!(verticalAnchorRatio == 0.0 && m_zoomedVerticalOffset < c_edgeDetectionTolerance))
				{
					isAnchoringElementVertically = true;
				}
			}
			else
			{
				isAnchoringElementVertically = true;
			}
		}

#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"isAnchoringElementHorizontally", *isAnchoringElementHorizontally);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"isAnchoringElementVertically", *isAnchoringElementVertically);

		// if (isAnchoringFarEdgeHorizontally)
		// {
		// 	SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"isAnchoringFarEdgeHorizontally", *isAnchoringFarEdgeHorizontally);
		// }
		// if (isAnchoringFarEdgeVertically)
		// {
		// 	SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"isAnchoringFarEdgeVertically", *isAnchoringFarEdgeVertically);
		// }
#endif
	}

	// Returns:
	// - viewportAnchorPointHorizontalOffset: unzoomed horizontal offset of the anchor point within the ScrollPresenter.Content. NaN if there is no horizontal anchoring.
	// - viewportAnchorPointVerticalOffset: unzoomed vertical offset of the anchor point within the ScrollPresenter.Content. NaN if there is no vertical anchoring.
	private void ComputeViewportAnchorPoint(
		double viewportWidth,
		double viewportHeight,
		out double viewportAnchorPointHorizontalOffset,
		out double viewportAnchorPointVerticalOffset)
	{
		viewportAnchorPointHorizontalOffset = double.NaN;
		viewportAnchorPointVerticalOffset = double.NaN;

		Rect viewportAnchorBounds = new Rect(
			(float)(m_zoomedHorizontalOffset / m_zoomFactor),
			(float)(m_zoomedVerticalOffset / m_zoomFactor),
			(float)(viewportWidth / m_zoomFactor),
			(float)(viewportHeight / m_zoomFactor)
		);

		ComputeAnchorPoint(viewportAnchorBounds, out viewportAnchorPointHorizontalOffset, out viewportAnchorPointVerticalOffset);

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, *viewportAnchorPointHorizontalOffset, *viewportAnchorPointVerticalOffset);
	}

	// Returns:
	// - elementAnchorPointHorizontalOffset: unzoomed horizontal offset of the anchor element's anchor point within the ScrollPresenter.Content. NaN if there is no horizontal anchoring.
	// - elementAnchorPointVerticalOffset: unzoomed vertical offset of the anchor element's point within the ScrollPresenter.Content. NaN if there is no vertical anchoring.
	private void ComputeElementAnchorPoint(
		bool isForPreArrange,
		out double elementAnchorPointHorizontalOffset,
		out double elementAnchorPointVerticalOffset)
	{
		elementAnchorPointHorizontalOffset = double.NaN;
		elementAnchorPointVerticalOffset = double.NaN;

		MUX_ASSERT(!m_isAnchorElementDirty);

		if (m_anchorElement is not null)
		{
			Rect anchorElementBounds = isForPreArrange ? m_anchorElementBounds : GetDescendantBounds(Content, m_anchorElement);

			ComputeAnchorPoint(anchorElementBounds, out elementAnchorPointHorizontalOffset, out elementAnchorPointVerticalOffset);

			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, *elementAnchorPointHorizontalOffset, *elementAnchorPointVerticalOffset);
		}
	}

	private void ComputeAnchorPoint(
		Rect anchorBounds,
		out double anchorPointX,
		out double anchorPointY)
	{
		if (double.IsNaN(HorizontalAnchorRatio))
		{
			anchorPointX = double.NaN;
		}
		else
		{
			MUX_ASSERT(HorizontalAnchorRatio >= 0.0);
			MUX_ASSERT(HorizontalAnchorRatio <= 1.0);

			anchorPointX = anchorBounds.X + HorizontalAnchorRatio * anchorBounds.Width;
		}

		if (double.IsNaN(VerticalAnchorRatio))
		{
			anchorPointY = double.NaN;
		}
		else
		{
			MUX_ASSERT(VerticalAnchorRatio >= 0.0);
			MUX_ASSERT(VerticalAnchorRatio <= 1.0);

			anchorPointY = anchorBounds.Y + VerticalAnchorRatio * anchorBounds.Height;
		}
	}

	// Computes the distance between the viewport's anchor point and the anchor element's anchor point.
	private Size ComputeViewportToElementAnchorPointsDistance(
		double viewportWidth,
		double viewportHeight,
		bool isForPreArrange)
	{
		if (m_anchorElement is not null)
		{
			MUX_ASSERT(!isForPreArrange || IsElementValidAnchor(m_anchorElement));

			if (!isForPreArrange && !IsElementValidAnchor(m_anchorElement))
			{
				return new Size(float.NaN, float.NaN);
			}

			double elementAnchorPointHorizontalOffset = 0.0;
			double elementAnchorPointVerticalOffset = 0.0;
			double viewportAnchorPointHorizontalOffset = 0.0;
			double viewportAnchorPointVerticalOffset = 0.0;

			ComputeElementAnchorPoint(
				isForPreArrange,
				out elementAnchorPointHorizontalOffset,
				out elementAnchorPointVerticalOffset);
			ComputeViewportAnchorPoint(
				viewportWidth,
				viewportHeight,
				out viewportAnchorPointHorizontalOffset,
				out viewportAnchorPointVerticalOffset);

			MUX_ASSERT(!double.IsNaN(viewportAnchorPointHorizontalOffset) || !double.IsNaN(viewportAnchorPointVerticalOffset));
			MUX_ASSERT(double.IsNaN(viewportAnchorPointHorizontalOffset) == double.IsNaN(elementAnchorPointHorizontalOffset));
			MUX_ASSERT(double.IsNaN(viewportAnchorPointVerticalOffset) == double.IsNaN(elementAnchorPointVerticalOffset));

			// Rounding the distance to 6 precision digits to avoid layout cycles due to float/double conversions.
			Size viewportToElementAnchorPointsDistance = new Size(
				double.IsNaN(viewportAnchorPointHorizontalOffset) ?
					float.NaN : (float)(Math.Round((elementAnchorPointHorizontalOffset - viewportAnchorPointHorizontalOffset) * 1000000) / 1000000),
				double.IsNaN(viewportAnchorPointVerticalOffset) ?
					float.NaN : (float)(Math.Round((elementAnchorPointVerticalOffset - viewportAnchorPointVerticalOffset) * 1000000) / 1000000)
			);

			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, viewportToElementAnchorPointsDistance.Width, viewportToElementAnchorPointsDistance.Height);

			return viewportToElementAnchorPointsDistance;
		}
		else
		{
			return new Size(float.NaN, float.NaN);
		}
	}

#if false
	private void ClearAnchorCandidates()
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"m_anchorCandidates cleared. m_isAnchorElementDirty set.");
#endif

		m_anchorCandidates.Clear();
		m_isAnchorElementDirty = true;
	}
#endif

	private void ResetAnchorElement()
	{
		if (m_anchorElement is not null)
		{
#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"m_anchorElement, m_anchorElementBounds, m_isAnchorElementDirty reset.");
#endif

			m_anchorElement = null;
			m_anchorElementBounds = default;
			m_isAnchorElementDirty = false;

			ScrollPresenterTestHooks globalTestHooks = ScrollPresenterTestHooks.GetGlobalTestHooks();

			if (globalTestHooks is not null && ScrollPresenterTestHooks.AreAnchorNotificationsRaised)
			{
				globalTestHooks.NotifyAnchorEvaluated(this, null /*anchorElement*/, double.NaN /*viewportAnchorPointHorizontalOffset*/, double.NaN /*viewportAnchorPointVerticalOffset*/);
			}
		}
	}

	// Raises the ScrollPresenter.AnchorRequested event. If no anchor element was specified, selects an anchor among the candidates vector that may have been altered 
	// in the AnchorRequested event handler.
	private void EnsureAnchorElementSelection()
	{
		if (!m_isAnchorElementDirty)
		{
#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_anchorElement.get(), L"Early exit as m_isAnchorElementDirty==False.");
#endif
			return;
		}

#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, L"m_anchorElement, m_anchorElementBounds, m_isAnchorElementDirty reset.");
#endif

		m_anchorElement = null;
		m_anchorElementBounds = default;
		m_isAnchorElementDirty = false;

		ScrollPresenterTestHooks globalTestHooks = ScrollPresenterTestHooks.GetGlobalTestHooks();
		double viewportAnchorPointHorizontalOffset = 0.0;
		double viewportAnchorPointVerticalOffset = 0.0;

		ComputeViewportAnchorPoint(
			m_viewportWidth,
			m_viewportHeight,
			out viewportAnchorPointHorizontalOffset,
			out viewportAnchorPointVerticalOffset);

		MUX_ASSERT(!double.IsNaN(viewportAnchorPointHorizontalOffset) || !double.IsNaN(viewportAnchorPointVerticalOffset));

		RaiseAnchorRequested();

		var anchorRequestedEventArgs = m_anchorRequestedEventArgs;
		UIElement requestedAnchorElement = null;
		IList<UIElement> anchorCandidates = null;
		UIElement content = Content;

		if (anchorRequestedEventArgs is not null)
		{
			requestedAnchorElement = anchorRequestedEventArgs.GetAnchorElement();
			anchorCandidates = anchorRequestedEventArgs.GetAnchorCandidates();
		}

		if (requestedAnchorElement is not null)
		{
			m_anchorElement = requestedAnchorElement;
			m_anchorElementBounds = GetDescendantBounds(content, requestedAnchorElement);

			if (globalTestHooks is not null && ScrollPresenterTestHooks.AreAnchorNotificationsRaised)
			{
				globalTestHooks.NotifyAnchorEvaluated(this, requestedAnchorElement, viewportAnchorPointHorizontalOffset, viewportAnchorPointVerticalOffset);
			}

#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_anchorElement.get(), L"m_anchorElement set.");
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this, TypeLogging::RectToString(m_anchorElementBounds).c_str(), L"m_anchorElementBounds set.");
#endif

			return;
		}

		Rect bestAnchorCandidateBounds = default;
		UIElement bestAnchorCandidate = null;
		double bestAnchorCandidateDistance = float.MaxValue;
		Rect viewportAnchorBounds = new Rect(
			(float)(m_zoomedHorizontalOffset / m_zoomFactor),
			(float)(m_zoomedVerticalOffset / m_zoomFactor),
			(float)(m_viewportWidth / m_zoomFactor),
			(float)(m_viewportHeight / m_zoomFactor)
		);

		MUX_ASSERT(content is not null);

		if (anchorCandidates is not null)
		{
			foreach (UIElement anchorCandidate in anchorCandidates)
			{
				ProcessAnchorCandidate(
					anchorCandidate,
					content,
					viewportAnchorBounds,
					viewportAnchorPointHorizontalOffset,
					viewportAnchorPointVerticalOffset,
					ref bestAnchorCandidateDistance,
					ref bestAnchorCandidate,
					ref bestAnchorCandidateBounds);
			}
		}
		else
		{
			foreach (UIElement anchorCandidateTracker in m_anchorCandidates)
			{
				UIElement anchorCandidate = anchorCandidateTracker;

				ProcessAnchorCandidate(
					anchorCandidate,
					content,
					viewportAnchorBounds,
					viewportAnchorPointHorizontalOffset,
					viewportAnchorPointVerticalOffset,
					ref bestAnchorCandidateDistance,
					ref bestAnchorCandidate,
					ref bestAnchorCandidateBounds);
			}
		}

		if (bestAnchorCandidate is not null)
		{
			m_anchorElement = bestAnchorCandidate;
			m_anchorElementBounds = bestAnchorCandidateBounds;

#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_anchorElement.get(), L"m_anchorElement set.");
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this, TypeLogging::RectToString(m_anchorElementBounds).c_str(), L"m_anchorElementBounds set.");
#endif
		}

		if (globalTestHooks is not null && ScrollPresenterTestHooks.AreAnchorNotificationsRaised)
		{
			globalTestHooks.NotifyAnchorEvaluated(this, m_anchorElement, viewportAnchorPointHorizontalOffset, viewportAnchorPointVerticalOffset);
		}
	}

	// Checks if the provided anchor candidate is better than the current best, based on its distance to the viewport anchor point,
	// and potentially updates the best candidate and its bounds.
	private void ProcessAnchorCandidate(
		UIElement anchorCandidate,
		UIElement content,
		Rect viewportAnchorBounds,
		double viewportAnchorPointHorizontalOffset,
		double viewportAnchorPointVerticalOffset,
		ref double bestAnchorCandidateDistance,
		ref UIElement bestAnchorCandidate,
		ref Rect bestAnchorCandidateBounds)
	{
		MUX_ASSERT(anchorCandidate is not null);
		MUX_ASSERT(content is not null);

		if (!IsElementValidAnchor(anchorCandidate, content))
		{
			// Ignore candidates that are collapsed or do not belong to the Content element and are not the Content itself. 
			return;
		}

		Rect anchorCandidateBounds = GetDescendantBounds(content, anchorCandidate);

		if (!SharedHelpers.DoRectsIntersect(viewportAnchorBounds, anchorCandidateBounds))
		{
			// Ignore candidates that do not intersect with the viewport in order to favor those that do.
			return;
		}

		// Using the distances from the viewport anchor point to the four corners of the anchor candidate.
		double anchorCandidateDistance = 0.0;

		if (!double.IsNaN(viewportAnchorPointHorizontalOffset))
		{
			anchorCandidateDistance += Math.Pow(viewportAnchorPointHorizontalOffset - anchorCandidateBounds.X, 2);
			anchorCandidateDistance += Math.Pow(viewportAnchorPointHorizontalOffset - (anchorCandidateBounds.X + anchorCandidateBounds.Width), 2);
		}

		if (!double.IsNaN(viewportAnchorPointVerticalOffset))
		{
			anchorCandidateDistance += Math.Pow(viewportAnchorPointVerticalOffset - anchorCandidateBounds.Y, 2);
			anchorCandidateDistance += Math.Pow(viewportAnchorPointVerticalOffset - (anchorCandidateBounds.Y + anchorCandidateBounds.Height), 2);
		}

		if (anchorCandidateDistance <= bestAnchorCandidateDistance)
		{
			bestAnchorCandidate = anchorCandidate;
			bestAnchorCandidateBounds = anchorCandidateBounds;
			bestAnchorCandidateDistance = anchorCandidateDistance;
		}
	}

	// Returns the bounds of a ScrollPresenter.Content descendant in respect to that content.
	private Rect GetDescendantBounds(
		UIElement content,
		UIElement descendant)
	{
		MUX_ASSERT(content is not null);
		MUX_ASSERT(IsElementValidAnchor(descendant, content));

		FrameworkElement descendantAsFE = descendant as FrameworkElement;
		Rect descendantRect = new Rect(
			0.0f,
			0.0f,
			descendantAsFE is not null ? (float)descendantAsFE.ActualWidth : 0.0f,
			descendantAsFE is not null ? (float)descendantAsFE.ActualHeight : 0.0f
		);

		return GetDescendantBounds(content, descendant, descendantRect);
	}

	internal static bool IsElementValidAnchor(UIElement element, UIElement content)
	{
		MUX_ASSERT(element is not null);
		MUX_ASSERT(content is not null);

		return element.Visibility == Visibility.Visible && (element == content || SharedHelpers.IsAncestor(element, content));
	}
}
