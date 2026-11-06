// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference StickyHeaderWrapper.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace DirectUI;

internal sealed partial class StickyHeaderWrapper
{
	internal StickyHeaderWrapper(Rect desiredBounds, double groupBottom, double panelOffset, double pannableExtent)
	{
#if !HAS_UNO // TODO Uno: no layout transition element parent.
		m_pLTEParentNoRef = null;
#endif
		m_desiredBounds = desiredBounds;
		m_groupBottom = groupBottom;
		m_panelOffset = panelOffset;
		m_pannableExtent = pannableExtent;
	}

	// ~StickyHeaderWrapper. C# has no deterministic destructor, so the owner calls Dispose where C++ resets the shared_ptr.
	public void Dispose()
	{
		Clear();
#if !HAS_UNO // TODO Uno: no storyboard completion handler.
		if (m_epStoryboardCompleted)
		{
			m_epStoryboardCompleted.DetachEventHandler(m_spStoryboard);
		}
#endif
	}

	internal void Clear()
	{
		try
		{
			UIElement? spTargetElement = null;
			m_wrTarget?.TryGetTarget(out spTargetElement);
#if !HAS_UNO // TODO Uno: no layout transition element.
			CUIElement pNativeTarget = null;
#endif
			if (spTargetElement is not null)
			{
				spTargetElement.RenderTransform = null;
#if !HAS_UNO // TODO Uno: no layout transition element.
				pNativeTarget = spTargetElement.GetHandle();
#endif
			}
#if !HAS_UNO // TODO Uno: no layout transition element or DManip relationship.
			CoreImports.LayoutTransitionElement_Destroy(
				DXamlCore.GetCurrent().GetHandle(),
				pNativeTarget,
				m_pLTEParentNoRef,
				m_spLayoutTransitionElement.GetHandle());

			m_spRelationship.Remove();
#endif
		}
		finally
		{
#if !HAS_UNO // TODO Uno: no layout transition element or DManip relationship.
			m_spRelationship = null;
			m_pLTEParentNoRef = null;
			m_spTransform = null;
			m_spRelationship = null;
#endif
			m_wrTarget = null;
			m_wrTargetHost = null;
#if HAS_UNO
			m_spHeaderTransform = null;
			m_wrScrollViewer = null;
#endif
		}
	}

#if HAS_UNO
	internal void Initialize(
		UIElement pHeader,
		Panel pPanel,
		ScrollViewer pScrollViewer,
		UIElement pLTEParent)
#else
	internal void Initialize(
		UIElement pHeader,
		Panel pPanel,
		ScrollViewer pScrollViewer,
		UIElement pLTEParent,
		ISecondaryContentRelationshipStatics pSecondaryContentRelationshipStatics)
#endif
	{
		double effectiveTop = 0;
		double effectiveBottom = 0;
		CompositeTransform spHeaderTransform;

#if !HAS_UNO // TODO Uno: no layout transition element or DManip relationship.
		CUIElement pNativeLTE = null;

		global::System.Diagnostics.Debug.Assert(m_spLayoutTransitionElement is null);
		global::System.Diagnostics.Debug.Assert(m_pLTEParentNoRef is null);
#endif
		global::System.Diagnostics.Debug.Assert(m_wrTarget is null);
		global::System.Diagnostics.Debug.Assert(m_wrTargetHost is null);

		m_wrTarget = new WeakReference<UIElement>(pHeader);
		m_wrTargetHost = new WeakReference<Panel>(pPanel);

#if !HAS_UNO // TODO Uno: no layout transition element or DManip relationship.
		m_pLTEParentNoRef = pLTEParent.GetHandle();

		CoreImports.LayoutTransitionElement_Create(
			DXamlCore.GetCurrent().GetHandle(),
			pHeader.GetHandle(),
			m_pLTEParentNoRef,
			false,
			out pNativeLTE);
		{
			DependencyObject spLTEAsDO = DXamlCore.GetCurrent().GetPeer(pNativeLTE, KnownTypeIndex.UIElement);
			m_spLayoutTransitionElement = (UIElement)spLTEAsDO;
		}

		UpdateLTEPosition();

		// Now that we've created the persistent LTE, let's do some of the other work of setting this up
		global::System.Diagnostics.Debug.Assert(m_spTransform is null);
		m_spTransform = new CompositeTransform();
		m_spLayoutTransitionElement.RenderTransform = m_spTransform;

		m_spLayoutTransitionElement.Opacity = 1;
#endif

		// Also put a render transform on the header itself as this also needs to reflect the sticky header curve
		// See notes in ParametricCurve.cpp - CSecondaryContentRelationship::UpdateDependencyProperties().
		spHeaderTransform = new CompositeTransform();
		pHeader.RenderTransform = spHeaderTransform;

		GetEffectiveDimensions(out effectiveTop, out effectiveBottom);

#if HAS_UNO
		m_spHeaderTransform = spHeaderTransform;
		m_wrScrollViewer = new WeakReference<ScrollViewer>(pScrollViewer);
#else
		global::System.Diagnostics.Debug.Assert(m_spRelationship is null);
		m_spRelationship = pSecondaryContentRelationshipStatics.CreateStickyHeaderRelationship(
			pScrollViewer,
			m_spLayoutTransitionElement,
			m_spTransform,
			spHeaderTransform,
			effectiveTop,
			effectiveBottom,
			m_desiredBounds.Height);
		m_spRelationship.Apply();
#endif
	}

	private void GetEffectiveDimensions(out double pEffectiveTop, out double pEffectiveBottom)
	{
		pEffectiveTop = m_desiredBounds.Y + m_panelOffset;
		pEffectiveBottom = m_groupBottom + m_panelOffset;
	}

	internal bool SetBounds(Rect desiredBounds, double groupBottom, double panelOffset, double pannableExtent)
	{
		bool updated = false;

		if (!DoubleUtil.AreClose(desiredBounds.Y, m_desiredBounds.Y)
			|| !DoubleUtil.AreClose(desiredBounds.Height, m_desiredBounds.Height)
			|| !DoubleUtil.AreClose(groupBottom, m_groupBottom)
			|| !DoubleUtil.AreClose(panelOffset, m_panelOffset)
			|| !DoubleUtil.AreClose(pannableExtent, m_pannableExtent))
		{
			double effectiveTop = 0;
			double effectiveBottom = 0;

			m_desiredBounds = desiredBounds;
			m_groupBottom = groupBottom;
			m_panelOffset = panelOffset;
			m_pannableExtent = pannableExtent;

			GetEffectiveDimensions(out effectiveTop, out effectiveBottom);

#if !HAS_UNO // TODO Uno: no DManip relationship, UpdateHeaderPosition evaluates the curve from the bounds.
			// Update the curve
			UpdateStickyHeaderCurve((SecondaryContentRelationship)m_spRelationship, effectiveTop, effectiveBottom, m_desiredBounds.Height);

			// Synchronize the change of this sticky header's curve
			// See extensive banner comments on CInputServices::PrepareSecondaryContentRelationshipForCurveUpdate().
			m_spRelationship.PrepareForCurveUpdate();

			m_spRelationship.Apply();
#endif

			UpdateLTEPosition();
			updated = true;
		}

		if (!updated)
		{
			if (!DoubleUtil.AreClose(desiredBounds.X, m_desiredBounds.X)
				|| !DoubleUtil.AreClose(desiredBounds.Width, m_desiredBounds.Width))
			{
				UpdateLTEPosition();
			}
		}

		return updated;
	}

	internal void SetDesiredBounds(Rect desiredBounds)
		=> SetBounds(desiredBounds, m_groupBottom, m_panelOffset, m_pannableExtent);

	private void UpdateLTEPosition()
	{
#if !HAS_UNO // TODO Uno: no layout transition element, the header is moved by its own layout.
		var pCoreLTE = (CLayoutTransitionElement)m_spLayoutTransitionElement.GetHandle();
		float xOffset = (float)m_desiredBounds.X;

		if (m_wrTarget is not null && m_wrTarget.TryGetTarget(out var spTargetElement))
		{
			var pNativeTarget = spTargetElement.GetHandle();
			xOffset = pNativeTarget.GetActualOffsetX();

			// Update the stored X offset so that the cached value
			// matches for the next check to see if we need to update.
			m_desiredBounds.X = xOffset;
		}

		Point position = new Point(xOffset, m_desiredBounds.Y);
		pCoreLTE.SetDestinationOffset(position);
		CUIElement.NWSetTransformDirty(pCoreLTE, DirtyFlags.Render | DirtyFlags.Bounds);
#endif
	}

	internal void NotifyLayoutTransitionStart()
	{
#if !HAS_UNO // TODO Uno: no layout transition element.
		m_spLayoutTransitionElement.Opacity = 0;
#endif
	}

	internal void NotifyLayoutTransitionEnd()
	{
#if !HAS_UNO // TODO Uno: no layout transition element.
		m_spLayoutTransitionElement.Opacity = 1;
#endif
	}

#if HAS_UNO
	// Evaluates the sticky header curve for the given vertical offset by hand, as no DManip secondary content drives it.
	internal void UpdateHeaderPosition(double verticalOffset)
	{
		GetEffectiveDimensions(out double effectiveTop, out double effectiveBottom);

		double innerHeight = effectiveBottom - m_desiredBounds.Height - effectiveTop;
		double translateY = verticalOffset < effectiveTop ? 0.0 : Math.Min(verticalOffset - effectiveTop, innerHeight);

		if (m_spHeaderTransform is not null)
		{
			m_spHeaderTransform.TranslateY = translateY;
		}
	}
#else
	internal void UpdateHeaderPosition()
	{
		m_spRelationship.UpdateDependencyProperties();
	}
#endif
}
