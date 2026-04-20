#nullable enable
// Ported from microsoft-ui-xaml2/src/dxaml/xcp/dxaml/lib/ScrollViewer_Partial.cpp
// (ScrollViewer::EnsureAnchorElementSelection, ProcessAnchorCandidate,
//  ComputeViewportAnchorPoint, ComputeElementAnchorPointAndBounds,
//  ComputeViewportToElementAnchorPointsDistance, PerformPositionAdjustment,
//  IsAnchoring, IsElementValidAnchor, GetDescendantBounds,
//  RegisterAnchorCandidateImpl, UnregisterAnchorCandidateImpl, RaiseAnchorRequested,
//  ResetAnchorElement, ClearAnchorCandidates, get_CurrentAnchorImpl).
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollViewer
{
	// Used when HorizontalAnchorRatio or VerticalAnchorRatio is 0.0 or 1.0 to determine
	// whether the Content is scrolled to an edge. It is declared at an edge if it's within 1/10th of a pixel.
	private const double c_edgeDetectionTolerance = 0.1;

	private readonly List<UIElement> m_anchorCandidates = new();

	private UIElement? m_anchorElement;
	private Rect m_anchorElementBounds;
	private bool m_isAnchorElementDirty;

	private AnchorRequestedEventArgs? m_anchorRequestedEventArgs;
	// Matches WinUI's m_useCandidatesFromArgs flag (ScrollViewer_Partial.cpp:16169/16173/16520).
	// Without it, Anchor/AnchorCandidates from a prior handler persist after handlers detach.
	private bool m_useCandidatesFromArgs;

	// Cached post-arrange state used for far-edge anchoring comparisons.
	private double m_unzoomedExtentWidth;
	private double m_unzoomedExtentHeight;
	private double m_viewportWidthCache;
	private double m_viewportHeightCache;

	private double m_pendingViewportShiftX;
	private double m_pendingViewportShiftY;

	// Set when AnchoringArrangeOverride eagerly syncs dimension DPs, so AfterArrange can skip the duplicate call.
	private bool m_dimensionsUpdatedInArrange;

	public static DependencyProperty HorizontalAnchorRatioProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalAnchorRatio), typeof(double),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(double.NaN));

	public double HorizontalAnchorRatio
	{
		get => (double)GetValue(HorizontalAnchorRatioProperty);
		set => SetValue(HorizontalAnchorRatioProperty, value);
	}

	public static DependencyProperty VerticalAnchorRatioProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalAnchorRatio), typeof(double),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(double.NaN));

	public double VerticalAnchorRatio
	{
		get => (double)GetValue(VerticalAnchorRatioProperty);
		set => SetValue(VerticalAnchorRatioProperty, value);
	}

	public event global::Windows.Foundation.TypedEventHandler<ScrollViewer, AnchorRequestedEventArgs>? AnchorRequested;

	public UIElement? CurrentAnchor
	{
		get
		{
			IsAnchoring(out var isAnchoringElementHorizontally, out var isAnchoringElementVertically, out _, out _);

			if (!isAnchoringElementHorizontally && !isAnchoringElementVertically)
			{
				return null;
			}

			var content = Content as UIElement;
			if (content is null)
			{
				return null;
			}

			var preArrangeViewport = new Rect(
				HorizontalOffset + m_pendingViewportShiftX,
				VerticalOffset + m_pendingViewportShiftY,
				ViewportWidth,
				ViewportHeight);

			EnsureAnchorElementSelection(preArrangeViewport);
			return m_anchorElement;
		}
	}

	public void RegisterAnchorCandidate(UIElement element)
	{
		if (element is null)
		{
			throw new global::System.ArgumentNullException(nameof(element));
		}

		// Note: WinUI Release builds do not dedup. Preserve that behavior (duplicate registers are allowed).
		m_anchorCandidates.Add(element);
		m_isAnchorElementDirty = true;
	}

	public void UnregisterAnchorCandidate(UIElement element)
	{
		if (element is null)
		{
			throw new global::System.ArgumentNullException(nameof(element));
		}

		// Remove only the first match (parity with WinUI).
		var index = m_anchorCandidates.IndexOf(element);
		if (index >= 0)
		{
			m_anchorCandidates.RemoveAt(index);
			m_isAnchorElementDirty = true;
		}
	}

	private void ClearAnchorCandidates()
	{
		m_anchorCandidates.Clear();
		m_isAnchorElementDirty = true;
	}

	private void ResetAnchorElement()
	{
		if (m_anchorElement is not null)
		{
			m_anchorElement = null;
			m_anchorElementBounds = default;
			m_isAnchorElementDirty = false;
		}
	}

	// Computes whether element / far-edge anchoring is active per dimension.
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

		var horizontalAnchorRatio = HorizontalAnchorRatio;
		var verticalAnchorRatio = VerticalAnchorRatio;

		if (!double.IsNaN(horizontalAnchorRatio) && !double.IsPositiveInfinity(ViewportWidth))
		{
			if (horizontalAnchorRatio == 0.0 || horizontalAnchorRatio == 1.0)
			{
				if (horizontalAnchorRatio == 1.0 &&
					HorizontalOffset + ViewportWidth - ExtentWidth > -c_edgeDetectionTolerance)
				{
					isAnchoringFarEdgeHorizontally = true;
				}
				else if (!(horizontalAnchorRatio == 0.0 && HorizontalOffset < c_edgeDetectionTolerance))
				{
					isAnchoringElementHorizontally = true;
				}
			}
			else
			{
				isAnchoringElementHorizontally = true;
			}
		}

		if (!double.IsNaN(verticalAnchorRatio) && !double.IsPositiveInfinity(ViewportHeight))
		{
			if (verticalAnchorRatio == 0.0 || verticalAnchorRatio == 1.0)
			{
				if (verticalAnchorRatio == 1.0 &&
					VerticalOffset + ViewportHeight - ExtentHeight > -c_edgeDetectionTolerance)
				{
					isAnchoringFarEdgeVertically = true;
				}
				else if (!(verticalAnchorRatio == 0.0 && VerticalOffset < c_edgeDetectionTolerance))
				{
					isAnchoringElementVertically = true;
				}
			}
			else
			{
				isAnchoringElementVertically = true;
			}
		}
	}

	private void ComputeViewportAnchorPoint(
		Rect zoomedViewport,
		out double viewportAnchorPointHorizontalOffset,
		out double viewportAnchorPointVerticalOffset)
	{
		// ScrollViewer's managed pipeline has zoomFactor == 1, so the unzoomed bounds equal the zoomed bounds.
		ComputeAnchorPoint(zoomedViewport, out viewportAnchorPointHorizontalOffset, out viewportAnchorPointVerticalOffset);
	}

	private void ComputeElementAnchorPoint(
		bool isForPreArrange,
		out double elementAnchorPointHorizontalOffset,
		out double elementAnchorPointVerticalOffset)
	{
		elementAnchorPointHorizontalOffset = double.NaN;
		elementAnchorPointVerticalOffset = double.NaN;

		if (m_anchorElement is null)
		{
			return;
		}

		Rect anchorElementBounds;
		if (isForPreArrange)
		{
			anchorElementBounds = m_anchorElementBounds;
		}
		else
		{
			var content = Content as UIElement;
			anchorElementBounds = content is not null
				? GetDescendantBounds(content, m_anchorElement)
				: m_anchorElementBounds;
		}

		ComputeAnchorPoint(anchorElementBounds, out elementAnchorPointHorizontalOffset, out elementAnchorPointVerticalOffset);
	}

	private void ComputeAnchorPoint(Rect bounds, out double anchorPointX, out double anchorPointY)
	{
		var horizontalAnchorRatio = HorizontalAnchorRatio;
		var verticalAnchorRatio = VerticalAnchorRatio;

		anchorPointX = double.IsNaN(horizontalAnchorRatio)
			? double.NaN
			: bounds.X + horizontalAnchorRatio * bounds.Width;

		anchorPointY = double.IsNaN(verticalAnchorRatio)
			? double.NaN
			: bounds.Y + verticalAnchorRatio * bounds.Height;
	}

	private Size ComputeViewportToElementAnchorPointsDistance(Rect zoomedViewport, bool isForPreArrange)
	{
		if (m_anchorElement is null)
		{
			return new Size(float.NaN, float.NaN);
		}

		if (!isForPreArrange && !IsElementValidAnchor(m_anchorElement))
		{
			return new Size(float.NaN, float.NaN);
		}

		ComputeElementAnchorPoint(isForPreArrange,
			out var elementAnchorPointHorizontalOffset,
			out var elementAnchorPointVerticalOffset);
		ComputeViewportAnchorPoint(zoomedViewport,
			out var viewportAnchorPointHorizontalOffset,
			out var viewportAnchorPointVerticalOffset);

		// Round to 6 decimal places to avoid layout cycles due to float/double conversions (parity with WinUI).
		return new Size(
			double.IsNaN(viewportAnchorPointHorizontalOffset)
				? float.NaN
				: (float)(global::System.Math.Round((elementAnchorPointHorizontalOffset - viewportAnchorPointHorizontalOffset) * 1000000) / 1000000),
			double.IsNaN(viewportAnchorPointVerticalOffset)
				? float.NaN
				: (float)(global::System.Math.Round((elementAnchorPointVerticalOffset - viewportAnchorPointVerticalOffset) * 1000000) / 1000000));
	}

	private void RaiseAnchorRequested()
	{
		if (AnchorRequested is null)
		{
			m_useCandidatesFromArgs = false;
			return;
		}

		m_anchorRequestedEventArgs ??= new AnchorRequestedEventArgs();
		m_anchorRequestedEventArgs.Reset(m_anchorCandidates);
		AnchorRequested.Invoke(this, m_anchorRequestedEventArgs);
		m_useCandidatesFromArgs = true;
	}

	// Raises AnchorRequested and selects an anchor based on either the handler's Anchor
	// override, or the closest candidate to the viewport anchor point.
	private void EnsureAnchorElementSelection(Rect zoomedViewport)
	{
		if (!m_isAnchorElementDirty)
		{
			return;
		}

		m_anchorElement = null;
		m_anchorElementBounds = default;
		m_isAnchorElementDirty = false;

		var content = Content as UIElement;
		if (content is null)
		{
			return;
		}

		ComputeViewportAnchorPoint(zoomedViewport,
			out var viewportAnchorPointHorizontalOffset,
			out var viewportAnchorPointVerticalOffset);

		RaiseAnchorRequested();

		UIElement? requestedAnchor = m_useCandidatesFromArgs ? m_anchorRequestedEventArgs?.Anchor : null;
		IList<UIElement>? candidateOverride = m_useCandidatesFromArgs ? m_anchorRequestedEventArgs?.AnchorCandidates : null;

		if (requestedAnchor is not null && IsElementValidAnchor(requestedAnchor, content))
		{
			m_anchorElement = requestedAnchor;
			m_anchorElementBounds = GetDescendantBounds(content, requestedAnchor);
			return;
		}

		var bestAnchorCandidateDistance = double.MaxValue;
		UIElement? bestAnchorCandidate = null;
		Rect bestAnchorCandidateBounds = default;

		var viewportAnchorBounds = zoomedViewport;

		var candidates = candidateOverride ?? (IList<UIElement>)m_anchorCandidates;
		foreach (var candidate in candidates)
		{
			ProcessAnchorCandidate(
				candidate,
				content,
				viewportAnchorBounds,
				viewportAnchorPointHorizontalOffset,
				viewportAnchorPointVerticalOffset,
				ref bestAnchorCandidateDistance,
				ref bestAnchorCandidate,
				ref bestAnchorCandidateBounds);
		}

		if (bestAnchorCandidate is not null)
		{
			m_anchorElement = bestAnchorCandidate;
			m_anchorElementBounds = bestAnchorCandidateBounds;
		}
	}

	private void ProcessAnchorCandidate(
		UIElement anchorCandidate,
		UIElement content,
		Rect viewportAnchorBounds,
		double viewportAnchorPointHorizontalOffset,
		double viewportAnchorPointVerticalOffset,
		ref double bestAnchorCandidateDistance,
		ref UIElement? bestAnchorCandidate,
		ref Rect bestAnchorCandidateBounds)
	{
		if (!IsElementValidAnchor(anchorCandidate, content))
		{
			return;
		}

		var anchorCandidateBounds = GetDescendantBounds(content, anchorCandidate);

		if (!SharedHelpers.DoRectsIntersect(viewportAnchorBounds, anchorCandidateBounds))
		{
			return;
		}

		double distance = 0;
		if (!double.IsNaN(viewportAnchorPointHorizontalOffset))
		{
			distance += global::System.Math.Pow(viewportAnchorPointHorizontalOffset - anchorCandidateBounds.X, 2);
			distance += global::System.Math.Pow(viewportAnchorPointHorizontalOffset - (anchorCandidateBounds.X + anchorCandidateBounds.Width), 2);
		}

		if (!double.IsNaN(viewportAnchorPointVerticalOffset))
		{
			distance += global::System.Math.Pow(viewportAnchorPointVerticalOffset - anchorCandidateBounds.Y, 2);
			distance += global::System.Math.Pow(viewportAnchorPointVerticalOffset - (anchorCandidateBounds.Y + anchorCandidateBounds.Height), 2);
		}

		if (distance <= bestAnchorCandidateDistance)
		{
			bestAnchorCandidate = anchorCandidate;
			bestAnchorCandidateBounds = anchorCandidateBounds;
			bestAnchorCandidateDistance = distance;
		}
	}

	private bool IsElementValidAnchor(UIElement element)
	{
		var content = Content as UIElement;
		return content is not null && IsElementValidAnchor(element, content);
	}

	private static bool IsElementValidAnchor(UIElement element, UIElement content)
	{
		return element.Visibility == Visibility.Visible &&
			(element == content || SharedHelpers.IsAncestor(element, content));
	}

	private Rect GetDescendantBounds(UIElement content, UIElement descendant)
	{
		var descendantAsFE = descendant as FrameworkElement;
		var descendantRect = new Rect(
			0.0,
			0.0,
			descendantAsFE?.ActualWidth ?? 0,
			descendantAsFE?.ActualHeight ?? 0);

		var contentAsFE = content as FrameworkElement;
		Thickness contentMargin = contentAsFE?.Margin ?? default;

		var transform = descendant.TransformToVisual(content);
		return transform.TransformBounds(new Rect(
			contentMargin.Left + descendantRect.X,
			contentMargin.Top + descendantRect.Y,
			descendantRect.Width,
			descendantRect.Height));
	}

	// Applies a flicker-less shift of the ScrollViewer's offset.
	// Clamps negative adjustments to avoid stepping into negative territory.
	private void PerformPositionAdjustment(bool isHorizontalDimension, double unzoomedAdjustment, Rect zoomedViewport)
	{
		// zoomFactor is 1 in Uno's managed ScrollViewer pipeline.
		var zoomedAdjustment = unzoomedAdjustment;

		if (isHorizontalDimension)
		{
			var zoomedHorizontalOffset = zoomedViewport.X;
			if (zoomedAdjustment < 0 && -zoomedAdjustment > zoomedHorizontalOffset)
			{
				zoomedAdjustment = -zoomedHorizontalOffset;
			}
			var newHorizontalOffset = zoomedAdjustment + zoomedHorizontalOffset;
			ChangeView(newHorizontalOffset, null, null, disableAnimation: true);
		}
		else
		{
			var zoomedVerticalOffset = zoomedViewport.Y;
			if (zoomedAdjustment < 0 && -zoomedAdjustment > zoomedVerticalOffset)
			{
				zoomedAdjustment = -zoomedVerticalOffset;
			}
			var newVerticalOffset = zoomedAdjustment + zoomedVerticalOffset;
			ChangeView(null, newVerticalOffset, null, disableAnimation: true);
		}
	}

	// Orchestrates anchoring around base.ArrangeOverride. Mirrors ScrollViewer_Partial.cpp:2112-2320.
	internal Size AnchoringArrangeOverride(Size finalSize, global::System.Func<Size, Size> baseArrange)
	{
		var child = Content as UIElement;
		if (child is null)
		{
			return baseArrange(finalSize);
		}

		IsAnchoring(
			out var isAnchoringElementHorizontally,
			out var isAnchoringElementVertically,
			out var isAnchoringFarEdgeHorizontally,
			out var isAnchoringFarEdgeVertically);

		if (!(isAnchoringElementHorizontally || isAnchoringElementVertically ||
			  isAnchoringFarEdgeHorizontally || isAnchoringFarEdgeVertically))
		{
			ResetAnchorElement();
			var result = baseArrange(finalSize);
			m_isAnchorElementDirty = true;
			return result;
		}

		var preArrangeViewportX = HorizontalOffset + m_pendingViewportShiftX;
		var preArrangeViewportY = VerticalOffset + m_pendingViewportShiftY;
		var preArrangeViewportWidth = ViewportWidth;
		var preArrangeViewportHeight = ViewportHeight;
		var preArrangeViewport = new Rect(preArrangeViewportX, preArrangeViewportY, preArrangeViewportWidth, preArrangeViewportHeight);

		Size preArrangeDistance = new(float.NaN, float.NaN);
		if (isAnchoringElementHorizontally || isAnchoringElementVertically)
		{
			EnsureAnchorElementSelection(preArrangeViewport);
			preArrangeDistance = ComputeViewportToElementAnchorPointsDistance(preArrangeViewport, isForPreArrange: true);
		}
		else
		{
			ResetAnchorElement();
		}

		var arrangeResult = baseArrange(finalSize);

		// WinUI updates its extent/viewport members during the arrange pass; Uno delays the DP
		// update until AfterArrange, which means PerformPositionAdjustment's clamping sees stale
		// values. Eagerly sync now so the anchor offset correction uses post-arrange state.
		UpdateDimensionProperties();
		m_dimensionsUpdatedInArrange = true;

		m_pendingViewportShiftX = 0;
		m_pendingViewportShiftY = 0;

		var childAsFE = child as FrameworkElement;
		Thickness childMargin = childAsFE?.Margin ?? default;
		var childRenderSize = child.RenderSize;
		var finalChildWidth = global::System.Math.Max(
			finalSize.Width,
			global::System.Math.Max(0, childRenderSize.Width + childMargin.Left + childMargin.Right));
		var finalChildHeight = global::System.Math.Max(
			finalSize.Height,
			global::System.Math.Max(0, childRenderSize.Height + childMargin.Top + childMargin.Bottom));

		var postArrangeViewportWidth = ViewportWidth;
		var postArrangeViewportHeight = ViewportHeight;
		var postArrangeViewport = new Rect(preArrangeViewportX, preArrangeViewportY, postArrangeViewportWidth, postArrangeViewportHeight);

		// Snapshot previous extent/viewport before updating the cache, then update immediately so
		// any re-entrant arrange triggered by PerformPositionAdjustment doesn't double-apply.
		var prevUnzoomedExtentWidth = m_unzoomedExtentWidth;
		var prevUnzoomedExtentHeight = m_unzoomedExtentHeight;
		var prevViewportWidthCache = m_viewportWidthCache;
		var prevViewportHeightCache = m_viewportHeightCache;
		m_unzoomedExtentWidth = finalChildWidth;
		m_unzoomedExtentHeight = finalChildHeight;
		m_viewportWidthCache = postArrangeViewport.Width;
		m_viewportHeightCache = postArrangeViewport.Height;

		if (!double.IsNaN(preArrangeDistance.Width) || !double.IsNaN(preArrangeDistance.Height))
		{
			var postArrangeDistance = ComputeViewportToElementAnchorPointsDistance(postArrangeViewport, isForPreArrange: false);

			// Now switch to the actual post-arrange offsets for the adjustment viewport.
			postArrangeViewport = new Rect(HorizontalOffset, VerticalOffset, postArrangeViewportWidth, postArrangeViewportHeight);

			if (isAnchoringElementHorizontally &&
				!double.IsNaN(preArrangeDistance.Width) &&
				!double.IsNaN(postArrangeDistance.Width) &&
				preArrangeDistance.Width != postArrangeDistance.Width)
			{
				var unzoomedAdjustment = postArrangeDistance.Width - preArrangeDistance.Width;
				PerformPositionAdjustment(true, unzoomedAdjustment, postArrangeViewport);
				m_pendingViewportShiftX = unzoomedAdjustment;
			}

			if (isAnchoringElementVertically &&
				!double.IsNaN(preArrangeDistance.Height) &&
				!double.IsNaN(postArrangeDistance.Height) &&
				preArrangeDistance.Height != postArrangeDistance.Height)
			{
				var unzoomedAdjustment = postArrangeDistance.Height - preArrangeDistance.Height;
				PerformPositionAdjustment(false, unzoomedAdjustment, postArrangeViewport);
				m_pendingViewportShiftY = unzoomedAdjustment;
			}
		}

		// Far-edge anchoring: use the previous extent/viewport (snapshot BEFORE the cache update).
		if (isAnchoringFarEdgeHorizontally)
		{
			double unzoomedAdjustment = 0;
			if (finalChildWidth > prevUnzoomedExtentWidth)
			{
				unzoomedAdjustment = finalChildWidth - prevUnzoomedExtentWidth;
			}
			if (prevViewportWidthCache > postArrangeViewport.Width)
			{
				unzoomedAdjustment += prevViewportWidthCache - postArrangeViewport.Width;
			}
			if (unzoomedAdjustment != 0)
			{
				PerformPositionAdjustment(true, unzoomedAdjustment, postArrangeViewport);
			}
		}

		if (isAnchoringFarEdgeVertically)
		{
			double unzoomedAdjustment = 0;
			if (finalChildHeight > prevUnzoomedExtentHeight)
			{
				unzoomedAdjustment = finalChildHeight - prevUnzoomedExtentHeight;
			}
			if (prevViewportHeightCache > postArrangeViewport.Height)
			{
				unzoomedAdjustment += prevViewportHeightCache - postArrangeViewport.Height;
			}
			if (unzoomedAdjustment != 0)
			{
				PerformPositionAdjustment(false, unzoomedAdjustment, postArrangeViewport);
			}
		}

		m_isAnchorElementDirty = true;
		return arrangeResult;
	}

	// Marks the anchor element dirty and triggers a reselection on the next arrange or
	// CurrentAnchor access. Called from property-changed callbacks and other mutators.
	internal void InvalidateAnchorElement()
	{
		m_isAnchorElementDirty = true;
	}

	// Ported from ScrollViewer_Partial.cpp:9440 ScrollViewer::OnScrollContentPresenterMeasured.
	// Called by ScrollContentPresenter after its MeasureOverride completes so that, when anchoring
	// is active, ScrollViewer.ArrangeOverride re-runs (and drives the pre/post anchor adjustment).
	// Without this hook Uno's layout short-circuit skips ArrangeCore when the SV's own finalRect
	// is unchanged, so the anchoring flow would not fire on grandchild extent changes.
	internal void OnScrollContentPresenterMeasured()
	{
		IsAnchoring(
			out var isAnchoringElementHorizontally,
			out var isAnchoringElementVertically,
			out var isAnchoringFarEdgeHorizontally,
			out var isAnchoringFarEdgeVertically);

		if (isAnchoringElementHorizontally || isAnchoringElementVertically ||
			isAnchoringFarEdgeHorizontally || isAnchoringFarEdgeVertically)
		{
			InvalidateArrange();
		}
	}
}
