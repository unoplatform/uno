// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsRepeaterScrollHost.cpp, commit 4b206bce3

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Helpers.WinUI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsRepeaterScrollHost
{
	public ItemsRepeaterScrollHost()
	{
		// __RP_Marker_ClassById(RuntimeProfiler.ProfId_ItemsRepeaterScrollHost);
	}

	// #pragma region IFrameworkElementOverrides

	protected override Size MeasureOverride(Size availableSize)
	{
		Size desiredSize = default;
		if (ScrollViewer is { } scrollViewer)
		{
			scrollViewer.Measure(availableSize);
			desiredSize = scrollViewer.DesiredSize;
		}

		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Size result = finalSize;
		if (ScrollViewer is { } scrollViewer)
		{
#if SCROLLVIEWER_SUPPORTS_ANCHORING
			if (SharedHelpers.IsRS5OrHigher())
			{
				// No-op when running on RS5 and above. ScrollViewer can do anchoring on its own.
				scrollViewer.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
			}
			else
#endif
			{
				var shouldApplyPendingChangeView = scrollViewer != default && HasPendingBringIntoView && !m_pendingBringIntoView.ChangeViewCalled;

				Rect anchorElementRelativeBounds = default;
				var anchorElement =
					// BringIntoView takes precedence over tracking.
					shouldApplyPendingChangeView
						? null
						:
						// Pick the best candidate depending on HorizontalAnchorRatio and VerticalAnchorRatio.
						// The best candidate is the element that's the closest to the edge of interest.
						GetAnchorElement(out anchorElementRelativeBounds);

				scrollViewer.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

				m_pendingViewportShift = 0.0;

				if (shouldApplyPendingChangeView)
				{
					ApplyPendingChangeView(scrollViewer);
				}
				else if (anchorElement != default)
				{
					// The anchor element might have changed its position relative to us.
					// If that's the case, we should shift the viewport to follow it as much as possible.
					m_pendingViewportShift = TrackElement(anchorElement, anchorElementRelativeBounds, scrollViewer);
				}
				else if (scrollViewer != default)
				{
					m_pendingBringIntoView.Reset();
				}

				m_candidates.Clear();
				m_isAnchorElementDirty = true;

				m_postArrange?.Invoke(this);
			}
		}

		return result;
	}

	// #pragma endregion

	// #pragma region IScrollAnchorProvider

	internal double HorizontalAnchorRatio { get; set; }

	internal double VerticalAnchorRatio { get; set; }

	public UIElement CurrentAnchor => GetAnchorElement(out _);

	public ScrollViewer ScrollViewer
	{
		// Uno specific: WinUI stores ScrollViewer in a DP; Uno reads/writes through VisualTreeHelper
		// so that the existing ScrollViewer-as-child contract is preserved while no DP exists yet.
		get
		{
			ScrollViewer value = null;
			var children = VisualTreeHelper.GetChildren(this).ToList();
			if (children.Count > 0)
			{
				value = children[0] as ScrollViewer;
			}

			return value;
		}
		set
		{
			m_scrollViewerViewChanging?.Dispose();
			m_scrollViewerViewChanged?.Dispose();
			m_scrollViewerSizeChanged?.Dispose();

			VisualTreeHelper.ClearChildren(this);
			VisualTreeHelper.AddChild(this, value);

			// We don't want to listen to events in RS5+ since this guy is a no-op.
#if SCROLLVIEWER_SUPPORTS_ANCHORING
			if (!SharedHelpers.IsRS5OrHigher())
#endif
			{
				value.ViewChanging += OnScrollViewerViewChanging;
				m_scrollViewerViewChanging = Disposable.Create(() => value.ViewChanging -= OnScrollViewerViewChanging);

				value.ViewChanged += OnScrollViewerViewChanged;
				m_scrollViewerViewChanged = Disposable.Create(() => value.ViewChanged -= OnScrollViewerViewChanged);

				value.SizeChanged += OnScrollViewerSizeChanged;
				m_scrollViewerSizeChanged = Disposable.Create(() => value.SizeChanged -= OnScrollViewerSizeChanged);
			}
		}
	}

	// #pragma endregion

	// #pragma region IRepeaterScrollingSurface

	bool IRepeaterScrollingSurface.IsHorizontallyScrollable => true;
	bool IRepeaterScrollingSurface.IsVerticallyScrollable => true;

	UIElement IRepeaterScrollingSurface.AnchorElement => GetAnchorElement(out _);

	event ViewportChangedEventHandler IRepeaterScrollingSurface.ViewportChanged
	{
		add => m_viewportChanged += value;
		remove => m_viewportChanged -= value;
	}

	event PostArrangeEventHandler IRepeaterScrollingSurface.PostArrange
	{
		add => m_postArrange += value;
		remove => m_postArrange -= value;
	}

	event ConfigurationChangedEventHandler IRepeaterScrollingSurface.ConfigurationChanged
	{
		add => m_configurationChanged += value;
		remove => m_configurationChanged -= value;
	}

	void IScrollAnchorProvider.RegisterAnchorCandidate(UIElement element) => RegisterAnchorCandidate(element);

	void IRepeaterScrollingSurface.RegisterAnchorCandidate(UIElement element) => RegisterAnchorCandidate(element);

	internal void RegisterAnchorCandidate(UIElement element)
	{
		if (!double.IsNaN(HorizontalAnchorRatio) || !double.IsNaN(VerticalAnchorRatio))
		{
			if (ScrollViewer is { } scrollViewer)
			{
#if DEBUG
				// We should not be registring the same element twice. Even through it is functionally ok,
				// we will end up spending more time during arrange than we must.
				// However checking if an element is already in the list every time a new element is registered is worse for perf.
				// So, I'm leaving an assert here to catch regression in our code but in release builds we run without the check.
				var elem = element;
				var it = m_candidates.Any(c => c.Element == elem);
				if (it)
				{
					MUX_ASSERT(false);
				}

#endif // _DEBUG

				m_candidates.Add(new CandidateInfo(element));
				m_isAnchorElementDirty = true;
			}
		}
	}

	void IScrollAnchorProvider.UnregisterAnchorCandidate(UIElement element) => UnregisterAnchorCandidate(element);

	void IRepeaterScrollingSurface.UnregisterAnchorCandidate(UIElement element) => UnregisterAnchorCandidate(element);

	internal void UnregisterAnchorCandidate(UIElement element)
	{
		var elem = element;
		var it = m_candidates.FindIndex(c => c.Element == elem);
		if (it != -1)
		{
			REPEATER_TRACE_INFO("Unregistered candidate %d\n", it);
			m_candidates.RemoveAt(it);
			m_isAnchorElementDirty = true;
		}
	}

	Rect IRepeaterScrollingSurface.GetRelativeViewport(UIElement element)
	{
		if (ScrollViewer is { } scrollViewer)
		{
			var elem = element;
			bool hasLockedViewport = HasPendingBringIntoView;
			var transformer = elem.TransformToVisual(hasLockedViewport ? (scrollViewer.ContentTemplateRoot as UIElement) : scrollViewer);
			var zoomFactor = (double)(scrollViewer.ZoomFactor);
			double viewportWidth = scrollViewer.ViewportWidth / zoomFactor;
			double viewportHeight = scrollViewer.ViewportHeight / zoomFactor;

			var elementOffset = transformer.TransformPoint(new Point());

			elementOffset.X = -elementOffset.X;
			elementOffset.Y = -elementOffset.Y + (float)(m_pendingViewportShift);

			REPEATER_TRACE_INFO("Pending Shift - %lf\n", m_pendingViewportShift);

			if (hasLockedViewport)
			{
				elementOffset.X += m_pendingBringIntoView.ChangeViewOffset.X;
				elementOffset.Y += m_pendingBringIntoView.ChangeViewOffset.Y;
			}

			return new Rect(elementOffset.X, elementOffset.Y, (float)(viewportWidth), (float)(viewportHeight));
		}

		return default;
	}

	// #pragma endregion

	// TODO: this API should go on UIElement.
	internal void StartBringIntoView(UIElement element, double alignmentX, double alignmentY, double offsetX, double offsetY, bool animate)
	{
		m_pendingBringIntoView = new BringIntoViewState(
			element,
			alignmentX,
			alignmentY,
			offsetX,
			offsetY,
			animate);
	}

	private void ApplyPendingChangeView(ScrollViewer scrollViewer)
	{
		var bringIntoView = m_pendingBringIntoView;
		MUX_ASSERT(!bringIntoView.ChangeViewCalled);

		bringIntoView.ChangeViewCalled = true;

		var layoutSlot = CachedVisualTreeHelpers.GetLayoutSlot(bringIntoView.TargetElement as FrameworkElement);

		// Arrange bounds are absolute.
		var arrangeBounds = bringIntoView
			.TargetElement
			.TransformToVisual(scrollViewer.ContentTemplateRoot as UIElement)
			.TransformBounds(new Rect(0, 0, layoutSlot.Width, layoutSlot.Height));

		var scrollableArea = new Point(
			(float)(scrollViewer.ViewportWidth - arrangeBounds.Width),
			(float)(scrollViewer.ViewportHeight - arrangeBounds.Height));

		// Calculate the target offset based on the alignment and offset parameters.
		// Make sure that we are constrained to the ScrollViewer's extent.
		var changeViewOffset = new Point(
			Math.Max(0.0f, (float)(Math.Min(
				arrangeBounds.X + bringIntoView.OffsetX - scrollableArea.X * bringIntoView.AlignmentX,
				scrollViewer.ExtentWidth - scrollViewer.ViewportWidth))),
			Math.Max(0.0f, (float)(Math.Min(
				arrangeBounds.Y + bringIntoView.OffsetY - scrollableArea.Y * bringIntoView.AlignmentY,
				scrollViewer.ExtentHeight - scrollViewer.ViewportHeight))));

		bringIntoView.ChangeViewOffset = changeViewOffset;

		REPEATER_TRACE_INFO("ItemsRepeaterScrollHost scroll to absolute offset (%.0f, %.0f), animate=%d \n", changeViewOffset.X, changeViewOffset.Y, bringIntoView.Animate);
		scrollViewer.ChangeView(
			changeViewOffset.X,
			changeViewOffset.Y,
			null,
			!bringIntoView.Animate);

		m_pendingBringIntoView = bringIntoView;
	}

	private double TrackElement(UIElement element, Rect previousBounds, ScrollViewer scrollViewer)
	{
		var bounds = LayoutInformation.GetLayoutSlot(element as FrameworkElement);
		var transformer = element.TransformToVisual(scrollViewer.ContentTemplateRoot as UIElement);
		var newBounds = transformer.TransformBounds(new Rect(
			0.0f,
			0.0f,
			bounds.Width,
			bounds.Height
		));

		var oldEdgeOffset = previousBounds.Y + HorizontalAnchorRatio * previousBounds.Height;
		var newEdgeOffset = newBounds.Y + HorizontalAnchorRatio * newBounds.Height;

		var unconstrainedPendingViewportShift = newEdgeOffset - oldEdgeOffset;
		var pendingViewportShift = unconstrainedPendingViewportShift;

		// ScrollViewer.ChangeView is not synchronous, so we need to account for the pending ChangeView call
		// and make sure we are locked on the target viewport.
		var verticalOffset =
			HasPendingBringIntoView && !m_pendingBringIntoView.Animate ? m_pendingBringIntoView.ChangeViewOffset.Y : scrollViewer.VerticalOffset;

		// Constrain the viewport shift to the extent
		if (verticalOffset + pendingViewportShift < 0)
		{
			pendingViewportShift = -verticalOffset;
		}
		else if (verticalOffset + scrollViewer.ViewportHeight + pendingViewportShift > scrollViewer.ExtentHeight)
		{
			pendingViewportShift = scrollViewer.ExtentHeight - scrollViewer.ViewportHeight - verticalOffset;
		}

		if (Math.Abs(pendingViewportShift) > 1)
		{
			// TODO: do we need to account for the zoom factor?
			// BUG:
			//  Unfortunately, if we have to correct while animating, we almost never
			//  update the ongoing animation correctly and we end up missing our target
			//  viewport. We should address that when building element tracking as part
			//  of the framework.
			REPEATER_TRACE_INFO("Viewport shift:%.0f. \n", pendingViewportShift);
			scrollViewer.ChangeView(
				null,
				verticalOffset + pendingViewportShift,
				null,
				true /* disableAnimation */);
		}
		else
		{
			pendingViewportShift = 0.0;

			// We can't shift the viewport to follow the tracked element. The viewport relative
			// to the tracked element will have changed. We need to raise ViewportChanged to make
			// sure the repeaters will get a second layout pass to fill any empty space they have.
			if (Math.Abs(unconstrainedPendingViewportShift) > 1)
			{
				m_viewportChanged?.Invoke(this, true /* isFinal */);
			}
		}

		return pendingViewportShift;
	}

	private UIElement GetAnchorElement(out Rect relativeBounds)
	{
		if (m_isAnchorElementDirty)
		{
			if (ScrollViewer is { } scrollViewer)
			{
				// ScrollViewer.ChangeView is not synchronous, so we need to account for the pending ChangeView call
				// and make sure we are locked on the target viewport.
				var verticalOffset =
					HasPendingBringIntoView && !m_pendingBringIntoView.Animate ? m_pendingBringIntoView.ChangeViewOffset.Y : scrollViewer.VerticalOffset;
				double viewportEdgeOffset = verticalOffset + HorizontalAnchorRatio * scrollViewer.ViewportHeight + m_pendingViewportShift;

				CandidateInfo bestCandidate = default;
				double bestCandidateDistance = float.MaxValue;

				foreach (var candidate in m_candidates)
				{
					var element = candidate.Element;

					if (!candidate.IsRelativeBoundsSet)
					{
						var bounds = LayoutInformation.GetLayoutSlot(element as FrameworkElement);
						var transformer = element.TransformToVisual(scrollViewer.ContentTemplateRoot as UIElement);
						candidate.RelativeBounds = transformer.TransformBounds(new Rect(
							0.0f,
							0.0f,
							bounds.Width,
							bounds.Height
						));
					}

					double elementEdgeOffset = candidate.RelativeBounds.Y + HorizontalAnchorRatio * candidate.RelativeBounds.Height;
					double candidateDistance = Math.Abs(elementEdgeOffset - viewportEdgeOffset);
					if (candidateDistance < bestCandidateDistance)
					{
						bestCandidate = candidate;
						bestCandidateDistance = candidateDistance;
					}
				}

				if (bestCandidate is { })
				{
					m_anchorElement = bestCandidate.Element;
					m_anchorElementRelativeBounds = bestCandidate.RelativeBounds;
				}
				else
				{
					m_anchorElement = null;
					m_anchorElementRelativeBounds = CandidateInfo.InvalidBounds;
				}
			}

			m_isAnchorElementDirty = false;
		}

		relativeBounds = m_anchorElementRelativeBounds;

		return m_anchorElement;
	}

	private void OnScrollViewerViewChanging(object sender, ScrollViewerViewChangingEventArgs args)
	{
		m_viewportChanged?.Invoke(this, false /* isFinal */);
	}

	private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs args)
	{
		bool isFinal = !args.IsIntermediate;

		if (isFinal)
		{
			m_pendingViewportShift = 0.0;

			if (HasPendingBringIntoView &&
				m_pendingBringIntoView.ChangeViewCalled == true)
			{
				m_pendingBringIntoView.Reset();
			}
		}

		m_viewportChanged?.Invoke(this, isFinal);
	}

	private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs args)
	{
		m_viewportChanged?.Invoke(this, true /* isFinal */);
	}
}
