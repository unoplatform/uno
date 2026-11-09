// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_StickyHeaders.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DirectUI;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
#if HAS_UNO
	// Stands in for the DManip parametric curve that drives the item clip: value = ConstantCoefficient + LinearCoefficient * (offset - BeginOffset).
	private struct ItemClippingCurveSegment
	{
		internal double BeginOffset;
		internal double ConstantCoefficient;
		internal double LinearCoefficient;
	}

	private readonly List<ItemClippingCurveSegment> m_itemClippingCurveSegments = new();
	private bool m_hasItemClippingCurve;
	private RectangleGeometry? m_itemClippingGeometry;
	private List<UIElement> m_stickyClippedContainers = new();
	private List<UIElement> m_previousStickyClippedContainers = new();
#endif

#if HAS_UNO
	// TODO Uno: WinUI's curves follow the content in DManip. Here they follow the offset the presenter last drew: layout reads
	// ScrollViewer.VerticalOffset, which trails an intermediate view until the deferred ViewChanged.
	private double GetStickyCurveOffset()
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		return spScrollViewer?.Presenter?.LastPublishedVerticalOffset ?? m_windowState.m_lastScrollOffset.Y;
	}
#endif

	// Configure or update the sticky behavior of a header
	private void ConfigureStickyHeader(
		UIElement spHeader,
		int groupDataIndex,
		Rect desiredBounds)
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		// If we don't have a ScrollViewer here, we just cannot stick
		if (spScrollViewer is not null)
		{
			DependencyObject? spParentAsIDO = VisualTreeHelper.GetParent(this);
			UIElement? spParentAsUIElement = spParentAsIDO as UIElement;

			if (spParentAsUIElement is not null)
			{
				// Estimate bottom of the group if not given
				double groupBottomY = 0;
				UIElement? spNextHeader = m_containerManager.GetHeaderAtGroupIndex(groupDataIndex + 1);
				if (spNextHeader is not null)
				{
					// Note: if the header has been realized, its VirtualizationInformation is never NULL
					VirtualizationInformation pNextVirtualizationInfo = GetVirtualizationInformationFromElement(spNextHeader)!;
					groupBottomY = pNextVirtualizationInfo.Bounds.Y;
				}
				else
				{
					// If next header is not realized, take the size of the buffer
					// the curves will be updated later with correct values : the bottom only has an impact on the end of the translation
					// which means that next group will always be visible at that time
					groupBottomY = desiredBounds.Y + (1.0 + m_cacheLength) * m_windowState.GetVisibleWindow().Height;
				}

				ConfigureStickyHeader(spHeader, spScrollViewer, spParentAsUIElement, groupBottomY, desiredBounds, false);
			}
		}
	}

	private bool ConfigureStickyHeader(
		UIElement spHeader,
		ScrollViewer pScrollViewer,
		UIElement pLTEParent,
		double groupBottomY,
		Rect desiredBounds,
		bool manuallyUpdatePosition)
	{
		double pannableExtent = 0.0;
		bool pUpdated = false;
		VirtualizationInformation pVirtualizationInfo = GetVirtualizationInformationFromElement(spHeader)!;
		StickyHeaderWrapper? pWrapper = pVirtualizationInfo.m_stickyHeaderWrapper;

#if HAS_UNO
		// No DManip drives the sticky curve, so it is always evaluated by hand.
		manuallyUpdatePosition = true;
#else
		if (m_tpSecondaryContentRelationshipStatics is null)
		{
			IActivationFactory spActivationFactory = SecondaryContentRelationshipFactory.CreateActivationFactory();
			ISecondaryContentRelationshipStatics spSecondaryContentRelationshipStatics = (ISecondaryContentRelationshipStatics)spActivationFactory;

			m_tpSecondaryContentRelationshipStatics = spSecondaryContentRelationshipStatics;
		}
#endif

		if (pWrapper is null)
		{
			pVirtualizationInfo.m_stickyHeaderWrapper = new StickyHeaderWrapper(desiredBounds, groupBottomY, m_currentHeaderHeight, pannableExtent);
			pWrapper = pVirtualizationInfo.m_stickyHeaderWrapper;
#if HAS_UNO
			pWrapper.Initialize(
				spHeader,
				this,
				pScrollViewer,
				pLTEParent);
#else
			pWrapper.Initialize(
				spHeader,
				this,
				pScrollViewer,
				pLTEParent,
				m_tpSecondaryContentRelationshipStatics);
#endif
		}

		pUpdated = pWrapper.SetBounds(desiredBounds, groupBottomY, m_currentHeaderHeight, pannableExtent);

		if (manuallyUpdatePosition)
		{
			// DManip wasn't responsible for the ScrollViewer change, so we need to manually evaluate the secondary content curve and update translate transform on Headers
#if HAS_UNO
			pWrapper.UpdateHeaderPosition(GetStickyCurveOffset());
#else
			pWrapper.UpdateHeaderPosition();
#endif
		}

		return pUpdated;
	}

	private void UpdateStickyHeaders(Size finalSize)
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);
		if (spScrollViewer is not null)
		{
			bool wasStickyCurveUpdated = false;

			DependencyObject? spParentAsIDO = VisualTreeHelper.GetParent(this);
			UIElement? spParentAsUIElement = spParentAsIDO as UIElement;

			if (spParentAsUIElement is not null)
			{
#if HAS_UNO
				// No DManip drives the sticky curve, so it is always evaluated by hand.
				bool manuallyUpdatePosition = true;
#else
				bool manuallyUpdatePosition = !spScrollViewer.IsInDirectManipulation;
#endif

				// Update the size of the header
				m_currentHeaderHeight = spScrollViewer.HeaderHeight;

				// It is better to give a size too large for a group than too small as other groups will be realized once the scroll is stating
				// That's why we put at least one buffer's height even for a group at the very bottom of the buffer
				double groupBottomY = 0.0;
				bool lastGroup = true;
				bool headerCurveUpdated = false;
				for (int validHeaderIndex = m_containerManager.GetValidHeaderCount() - 1;
					validHeaderIndex >= 0;
					--validHeaderIndex)
				{
					int groupIndex = m_containerManager.GetGroupIndexFromValidIndex(validHeaderIndex);
					UIElement? spHeader = m_containerManager.GetHeaderAtGroupIndex(groupIndex);

					if (spHeader is not null)
					{
						VirtualizationInformation pVirtualizationInfo = GetVirtualizationInformationFromElement(spHeader)!;
						Rect bounds = pVirtualizationInfo.Bounds;
						if (lastGroup)
						{
							int itemCount = 0;
							int firstItemDataIndex = 0;
							m_cacheManager.GetGroupInformationFromGroupIndex(groupIndex, out firstItemDataIndex, out itemCount);
							if (itemCount == 0)
							{
								// This is an empty group
								groupBottomY = bounds.Y + bounds.Height;
							}
							else if (m_bLastItemRealized)
							{
								// We have realized the last item of the list,
								// it means that this is the last group and also
								// that we can compute the exact size of the group
								UIElement? spContainer = m_containerManager.GetContainerAtValidIndex(m_containerManager.GetValidContainerCount() - 1);
								global::System.Diagnostics.Debug.Assert(spContainer is not null);
								VirtualizationInformation? pLastVirtualizationInformation = spContainer!.GetVirtualizationInformationOrNull();
								global::System.Diagnostics.Debug.Assert(pLastVirtualizationInformation is not null);
								Rect itemBounds = pLastVirtualizationInformation!.Bounds;
								groupBottomY = itemBounds.Y + itemBounds.Height;
							}
							else
							{
								// we need to get an estimation of the last item's position
								// We will provide the header estimaton reference but not
								// the container estimation reference as EstimateContainerLocation
								// should be able to handle that.
								if (m_containerManager.GetValidContainerCount() > 0)
								{
									EstimationReference headerRef = CreateDefaultEstimationReference(ElementType.GroupHeader);
									EstimationReference containerRef = CreateDefaultEstimationReference(ElementType.ItemContainer);
									Rect lastContainerBounds = default;

									headerRef.ElementIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, groupIndex);
									headerRef.ElementBounds = bounds;

									lastContainerBounds = m_spLayoutStrategy.EstimateElementBounds(
										ElementType.ItemContainer,
										firstItemDataIndex + itemCount - 1,
										headerRef,
										containerRef,
										m_windowState.GetRealizationWindow());
									groupBottomY = lastContainerBounds.Y + lastContainerBounds.Height;
								}
								else
								{
									// No container realized but we know that the group
									// is not empty, let's put an estimation which will be corrected
									// as soon as an item is realized
									groupBottomY = bounds.Y + bounds.Height + (1.0 + m_cacheLength) * m_windowState.GetVisibleWindow().Height;
								}
							}
							lastGroup = false;
						}
						else
						{
							if (groupBottomY < bounds.Y + bounds.Height)
							{
								groupBottomY = bounds.Y + bounds.Height;
							}
						}

						headerCurveUpdated = ConfigureStickyHeader(
							spHeader,
							spScrollViewer,
							spParentAsUIElement,
							groupBottomY,
							bounds,
							manuallyUpdatePosition);
						groupBottomY = bounds.Y;
						wasStickyCurveUpdated |= headerCurveUpdated;
					}
				}
				UpdateItemClippingForStickyHeaders(finalSize, manuallyUpdatePosition, wasStickyCurveUpdated /* updateClip */);
			}
		}
	}

	private void UpdateItemClippingForStickyHeaders(
		Size finalSize,
		bool manuallyUpdatePosition,
		bool updateClip)
	{
		bool updateClipAnimation = updateClip;
		Rect clipRect = new Rect(0, 0, Math.Max(finalSize.Width, m_estimatedSize.Width), Math.Max(finalSize.Height, m_estimatedSize.Height));
		RectangleGeometry? spClip;

		global::System.Diagnostics.Debug.Assert(m_cacheManager.IsGrouping());

#if HAS_UNO
		// TODO Uno: WinUI clips the panel and draws the sticky header outside of that clip through a layout transition element.
		// Uno has no such element, so a panel clip would hide the header too: the geometry only computes the curve, and
		// UpdateItemClippingTransform clips the few containers under the header instead.
		spClip = m_itemClippingGeometry;
#else
		spClip = Clip as RectangleGeometry;
#endif

		// Ensure the clip is created and populated
		if (spClip is null)
		{
			updateClipAnimation = true;

			spClip = new RectangleGeometry();
#if HAS_UNO
			m_itemClippingGeometry = spClip;
#else
			Clip = spClip;
#endif
		}

		spClip.Rect = clipRect;

		// Ensure we have a workable transform
		if (m_tpItemClippingTransform is null)
		{
			updateClipAnimation = true;

			CompositeTransform spTransform = new();
			m_tpItemClippingTransform = spTransform;
		}
		else
		{
			Transform spITransform = spClip.Transform;
			if (!ReferenceEquals(spITransform, m_tpItemClippingTransform))
			{
				updateClipAnimation = true;
			}
		}

		m_newStickyHeaderLocations.Clear();
		m_newStickyHeaderLocations.EnsureCapacity(m_containerManager.GetValidHeaderCount());
		for (int validHeaderIndex = 0; validHeaderIndex < m_containerManager.GetValidHeaderCount(); ++validHeaderIndex)
		{
			UIElement? spHeader = m_containerManager.GetHeaderAtValidIndex(validHeaderIndex);
			if (spHeader is not null)
			{
				var bounds = GetBoundsFromElement(spHeader);
				m_newStickyHeaderLocations.Add(new StickyHeaderInfo(bounds.Y, bounds.Height));
			}
		}

		if (!m_currentStickyHeaderLocations.SequenceEqual(m_newStickyHeaderLocations))
		{
			updateClipAnimation = true;
			(m_currentStickyHeaderLocations, m_newStickyHeaderLocations) = (m_newStickyHeaderLocations, m_currentStickyHeaderLocations);
		}

#if HAS_UNO
		if (!m_hasItemClippingCurve)
		{
			updateClipAnimation = true;

			m_hasItemClippingCurve = true;
		}
#else // TODO Uno: no DManip secondary content, the clip curve is kept in m_itemClippingCurveSegments and evaluated by hand.
		if (m_tpItemClippingRelationship is null)
		{
			updateClipAnimation = true;

			ScrollViewer spScrollViewer = m_wrScrollViewer.As<ScrollViewer>();

			SecondaryContentRelationship spRelationship = new();

			spRelationship.SetPrimaryContent(spScrollViewer);
			global::System.Diagnostics.Debug.Assert(m_tpItemClippingTransform is not null);
			spRelationship.SetSecondaryContent(this, m_tpItemClippingTransform);
			ParametricCurveCollection spParametricCurveCollection = spRelationship.Curves;
			spRelationship.ShouldTargetClip = true;

			// We'll create the curve here, but populate the segments later
			{
				ParametricCurve spParametricCurve = new();
				string strPanningOffsetPropertyName = "VerticalOffset";
				string strPanningTranslatePropertyName = "TranslateY";

				spParametricCurve.SetPrimaryContentProperty(strPanningOffsetPropertyName);
				spParametricCurve.SetSecondaryContentProperty(DirectManipulationProperty.TranslationY, strPanningTranslatePropertyName);

				spParametricCurveCollection.Append(spParametricCurve);
			}

			// Now that we've got the relationship and its curve collection, let's also add a curve to keep
			// the clipping region stationary in the non-panning direction.
			{
				ParametricCurve spParametricCurve = new();
				string strOrthogonalOffsetPropertyName = "HorizontalOffset";
				string strOrthogonalTranslatePropertyName = "TranslateX";

				spParametricCurve.SetPrimaryContentProperty(strOrthogonalOffsetPropertyName);
				spParametricCurve.SetSecondaryContentProperty(DirectManipulationProperty.TranslationX, strOrthogonalTranslatePropertyName);

				ParametricCurveSegmentCollection spCurveSegmentCollection = spParametricCurve.CurveSegments;
				ParametricCurveSegment spCurveSegment = new(); // Default init to all zeroes
				spCurveSegmentCollection.Append(spCurveSegment);

				spParametricCurveCollection.Append(spParametricCurve);
			}

			m_tpItemClippingRelationship = spRelationship;
		}
#endif

		if (updateClipAnimation)
		{
#if HAS_UNO
			List<ItemClippingCurveSegment> spCurveSegmentCollection = m_itemClippingCurveSegments;
#else
			ParametricCurveSegmentCollection spCurveSegmentCollection;
#endif

			// First, ensure we're all referring to the correct transform object
			spClip.Transform = m_tpItemClippingTransform;

#if !HAS_UNO // TODO Uno: the segments live directly in m_itemClippingCurveSegments.
			// Extract the target curve and its collection of curve segments
			{
				ParametricCurveCollection spParametricCurveCollection = m_tpItemClippingRelationship.Curves;
				uint curveCollectionSize = spParametricCurveCollection.Size;

				// We should only have a curve for panning and an orthogonal curve
				// The curve we are modifying is the last one in the collection
				global::System.Diagnostics.Debug.Assert(curveCollectionSize == 2);
				DependencyObject spParametricCurveAsIDO = spParametricCurveCollection.GetAt(0);

				spCurveSegmentCollection = ((ParametricCurve)spParametricCurveAsIDO).CurveSegments;
			}
#endif
			spCurveSegmentCollection.Clear();

			if (m_currentStickyHeaderLocations.Count != 0)
			{
				// Make sure we account for the listview/ScrollViewer header. If it exists
				// we want this function animation to begin with a region that's not animating
				if (m_currentHeaderHeight + m_currentStickyHeaderLocations[0].m_offset > 0)
				{
#if HAS_UNO
					ItemClippingCurveSegment spCurveSegment = default;
					spCurveSegment.BeginOffset = 0;
					spCurveSegment.ConstantCoefficient = 0;
					spCurveSegment.LinearCoefficient = 0;
					spCurveSegmentCollection.Add(spCurveSegment);
#else
					ParametricCurveSegment spCurveSegment = new();
					spCurveSegment.BeginOffset = 0;
					spCurveSegment.ConstantCoefficient = 0;
					spCurveSegment.LinearCoefficient = 0;
					spCurveSegmentCollection.Append(spCurveSegment);
#endif
				}

				// Now we walk through each header, and ensure the clipping region is in the right place for each
				double previousHeaderSize = 0;
				double previousLinearCurveBeginOffset = 0;
				double previousConstantCurveCoefficient = 0;
				foreach (var currentHeaderLocation in m_currentStickyHeaderLocations)
				{
					double constantCurveBeginOffset = currentHeaderLocation.m_offset - previousHeaderSize + m_currentHeaderHeight;
					double constantCurveCoefficient = currentHeaderLocation.m_offset + currentHeaderLocation.m_size;

#if HAS_UNO
					ItemClippingCurveSegment spCurveSegment = default;
					spCurveSegment.BeginOffset = constantCurveBeginOffset;
					spCurveSegment.ConstantCoefficient = -constantCurveCoefficient;
					spCurveSegment.LinearCoefficient = 0;
					spCurveSegmentCollection.Add(spCurveSegment);
#else
					ParametricCurveSegment spCurveSegment = new();
					spCurveSegment.BeginOffset = constantCurveBeginOffset;
					spCurveSegment.ConstantCoefficient = -constantCurveCoefficient;
					spCurveSegment.LinearCoefficient = 0;
					spCurveSegmentCollection.Append(spCurveSegment);
#endif

					double linearCurveBeginOffset = currentHeaderLocation.m_offset + m_currentHeaderHeight;

#if HAS_UNO
					spCurveSegment = default;
					spCurveSegment.BeginOffset = linearCurveBeginOffset;
					spCurveSegment.ConstantCoefficient = -constantCurveCoefficient;
					spCurveSegment.LinearCoefficient = -1;
					spCurveSegmentCollection.Add(spCurveSegment);
#else
					spCurveSegment = new();
					spCurveSegment.BeginOffset = linearCurveBeginOffset;
					spCurveSegment.ConstantCoefficient = -constantCurveCoefficient;
					spCurveSegment.LinearCoefficient = -1;
					spCurveSegmentCollection.Append(spCurveSegment);
#endif

					previousHeaderSize = currentHeaderLocation.m_size;
					previousLinearCurveBeginOffset = linearCurveBeginOffset;
					previousConstantCurveCoefficient = constantCurveCoefficient;
				}
			}

#if !HAS_UNO // TODO Uno: no DManip relationship to synchronize.
			// Synchronize the change of the clip manipulation curve.
			// See extensive banner comments on CInputServices::PrepareSecondaryContentRelationshipForCurveUpdate().
			m_tpItemClippingRelationship.PrepareForCurveUpdate();
			m_tpItemClippingRelationship.Apply();
#endif
		}

		if (manuallyUpdatePosition)
		{
			// DManip wasn't responsible for the ScrollViewer change, so we need to manually evaluate the secondary content curve
#if HAS_UNO
			UpdateItemClippingTransform(GetStickyCurveOffset());
#else
			m_tpItemClippingRelationship.UpdateDependencyProperties();
#endif
		}
	}

#if HAS_UNO
	// Evaluates the item clip curve for the given vertical offset and applies it to the clip transform.
	// The curve is expressed in DManip's sign convention, so the panel-space translation is its negation.
	private void UpdateItemClippingTransform(double verticalOffset)
	{
		double curveValue = 0;
		foreach (var segment in m_itemClippingCurveSegments)
		{
			if (segment.BeginOffset > verticalOffset)
			{
				break;
			}

			curveValue = segment.ConstantCoefficient + segment.LinearCoefficient * (verticalOffset - segment.BeginOffset);
		}

		if (m_tpItemClippingTransform is not null)
		{
			m_tpItemClippingTransform.TranslateX = 0;
			m_tpItemClippingTransform.TranslateY = -curveValue;

			ClipContainersUnderStickyHeader(-curveValue, verticalOffset - m_currentHeaderHeight);
		}
	}

	// Clips what the panel clip would have hidden: the part of a visible container that sits above the clip top, i.e. under the sticky header.
	private void ClipContainersUnderStickyHeader(double clipTop, double visibleTop)
	{
		(m_stickyClippedContainers, m_previousStickyClippedContainers) = (m_previousStickyClippedContainers, m_stickyClippedContainers);
		m_stickyClippedContainers.Clear();

		var validCount = m_containerManager.GetValidContainerCount();
		for (int validIndex = 0; validIndex < validCount; ++validIndex)
		{
			if (m_containerManager.GetContainerAtValidIndex(validIndex) is not { } container)
			{
				continue;
			}

			var bounds = GetBoundsFromElement(container);
			if (bounds.Y >= clipTop)
			{
				break;
			}

			if (bounds.Y + bounds.Height > visibleTop)
			{
				var visual = container.Visual;
				var clip = visual.TransitionClip as Microsoft.UI.Composition.InsetClip ?? visual.Compositor.CreateInsetClip();
				clip.TopInset = (float)(clipTop - bounds.Y);
				visual.TransitionClip = clip;
				m_stickyClippedContainers.Add(container);
			}
		}

		foreach (var container in m_previousStickyClippedContainers)
		{
			if (!m_stickyClippedContainers.Contains(container) && container.TransitionTarget is not { HasClipAnimation: true })
			{
				container.Visual.TransitionClip = null;
			}
		}

		m_previousStickyClippedContainers.Clear();
	}
#endif

	// Remove sticky header transformation
	private void RemoveStickyHeader(UIElement spHeader)
	{
		VirtualizationInformation pVirtualizationInfo = GetVirtualizationInformationFromElement(spHeader)!;
		pVirtualizationInfo.m_stickyHeaderWrapper?.Dispose();
		pVirtualizationInfo.m_stickyHeaderWrapper = null;
	}

	// Remove all sticky headers transformations
	private void RemoveStickyHeaders()
	{
		for (int groupIndex = m_containerManager.EndOfHeaderVisualSection() - 1;
			groupIndex >= m_containerManager.StartOfHeaderVisualSection();
			groupIndex--)
		{
			UIElement? spHeader = m_containerManager.GetHeaderAtValidIndex(groupIndex);

			if (spHeader is not null)
			{
				RemoveStickyHeader(spHeader);
			}
		}

		m_tpItemClippingTransform = null;
#if HAS_UNO
		m_itemClippingCurveSegments.Clear();
		m_hasItemClippingCurve = false;
		m_itemClippingGeometry = null;
		foreach (var container in m_stickyClippedContainers)
		{
			container.Visual.TransitionClip = null;
		}
		m_stickyClippedContainers.Clear();
#else
		if (m_tpItemClippingRelationship is not null)
		{
			m_tpItemClippingRelationship.Remove();
		}
		m_tpItemClippingRelationship = null;
#endif

		ClearValue(ClipProperty);
	}

	// Computes the vertical position of a sticky header
	private static double GetStickyHeaderPositionY(
		double groupTopY,
		double groupInnerHeight,
		double visibleTopY)
	{
		if (groupTopY > visibleTopY)
		{
			// When the group top is below visible top, it does not stick
			return groupTopY;
		}
		else
		{
			// This is the maximum position of the header (i.e. just above next group)
			double maxTop = groupTopY + groupInnerHeight;
			// Therefore the constrained value is either the top of visible window
			// or this one.
			return DoubleUtil.Min(visibleTopY, maxTop);
		}
	}

	private float GetRealHeaderExtentOutsideVisibleWindow()
	{
		global::System.Diagnostics.Debug.Assert(m_bUseStickyHeaders);
		float headerExtentOutsideVisibleWindow = 0;

		if (m_containerManager.IsGroupIndexWithinValidRange(m_firstVisibleGroupIndexBase))
		{
			UIElement? spRealHeader = m_containerManager.GetHeaderAtGroupIndex(m_firstVisibleGroupIndexBase);
			// This method could be invoked between a collection change and the following layout, which
			// means there could be sentinels in the realized range.
			if (spRealHeader is not null)
			{
				Rect realHeaderBounds = GetBoundsFromElement(spRealHeader);
				Rect visibleWindow = m_windowState.GetVisibleWindow();

				// calculate the part of the real (not sticky) header that is outside the visible window.
				// It could be completely above the visible window or a part of it could be within
				// the visible window. We need the part that is outside the visible window
				// since that space will be pushed down to show the full sticky header.
				var realHeaderDistanceFromVisibleWindow = DoubleUtil.Max(visibleWindow.Y - realHeaderBounds.Y, 0.0f);
				headerExtentOutsideVisibleWindow = (float)DoubleUtil.Min(realHeaderDistanceFromVisibleWindow, realHeaderBounds.Height);
			}
		}

		return headerExtentOutsideVisibleWindow;
	}

	internal float GetVerticalViewportPadding()
	{
		float verticalViewportPadding = 0;

		if (m_bUseStickyHeaders)
		{
			verticalViewportPadding = GetRealHeaderExtentOutsideVisibleWindow();
		}

		return verticalViewportPadding;
	}

	// Computes the offsets resulting from Sticky Headers
	// Note: we take as a prerequisite here that all sticky headers are correcly configured and we are just
	// applying the same computation than the Parametric Curve does
	internal void CoerceStickyHeaderOffsets(
		ListViewBaseHeaderItem pHeaderItem,
		int cOffsets,
		double[] pOffsets)
	{
		if (m_bUseStickyHeaders && (cOffsets > 0))
		{
			VirtualizationInformation? pVirtualizationInformation = pHeaderItem.GetVirtualizationInformationOrNull();
			ScrollViewer? spScrollViewer = null;
			m_wrScrollViewer?.TryGetTarget(out spScrollViewer);
			if (spScrollViewer is not null && pVirtualizationInformation is not null && pVirtualizationInformation.IsSticky)
			{
				var pWrapper = pVirtualizationInformation.m_stickyHeaderWrapper!;
				double visibleTopY = 0.0;
				double groupBottomY = pWrapper.GetGroupBottom();
				double groupTopY = pWrapper.GetDesiredBounds().Y;
				double groupHeaderHeight = pWrapper.GetDesiredBounds().Height;
				double groupInnerHeight = groupBottomY - groupTopY - groupHeaderHeight;

				visibleTopY = spScrollViewer.VerticalOffset;

				for (int i = 0; i < cOffsets; i++)
				{
					double newTopY = GetStickyHeaderPositionY(groupTopY + pOffsets[i], groupInnerHeight, visibleTopY);
					pOffsets[i] = newTopY - groupTopY;
				}
			}
		}
	}

	internal void NotifyLayoutTransitionStart(ListViewBaseHeaderItem pHeaderItem)
	{
		if (m_bUseStickyHeaders && pHeaderItem.GetVirtualizationInformationOrNull()!.IsSticky)
		{
			pHeaderItem.GetVirtualizationInformationOrNull()!.m_stickyHeaderWrapper!.NotifyLayoutTransitionStart();
		}
	}

	internal void NotifyLayoutTransitionEnd(ListViewBaseHeaderItem pHeaderItem)
	{
		if (m_bUseStickyHeaders && pHeaderItem.GetVirtualizationInformationOrNull()!.IsSticky)
		{
			pHeaderItem.GetVirtualizationInformationOrNull()!.m_stickyHeaderWrapper!.NotifyLayoutTransitionEnd();
		}
	}
}
