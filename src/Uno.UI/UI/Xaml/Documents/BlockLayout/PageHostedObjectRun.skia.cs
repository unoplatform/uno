// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PageHostedObjectRun.h, PageHostedObjectRun.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable
#pragma warning disable CS8600, CS8602, CS8604, CS8618, CS0219, CS0414 // TODO Uno (Stage 5): WIP drafts not yet fully nullable-annotated

using Microsoft.UI.Xaml.Documents.RichTextServices;
using Uno.UI.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

//------------------------------------------------------------------------
//  Summary:
//      An implementation of ObjectRun that contains an embedded UIElement.
//
//------------------------------------------------------------------------
internal class PageHostedObjectRun : ObjectRun
{
	private InlineUIContainer m_pContainer;

	//---------------------------------------------------------------------------
	//
	//  Member:
	//      PageHostedObjectRun::PageHostedObjectRun
	//
	//  Synopsis:
	//      Creates an instance of PageHostedObjectRun class.
	//
	//---------------------------------------------------------------------------
	public PageHostedObjectRun(
		InlineUIContainer pContainer,
		uint characterIndex,
		TextRunProperties pProperties)
		: base(characterIndex, pProperties)
	{
		m_pContainer = pContainer;
	}

	// ~PageHostedObjectRun: empty in C++.

	public override bool HasFixedSize() => true;

	//------------------------------------------------------------------------
	//  Summary:
	//      Measures the embedded element.
	//
	//  Remarks:
	//      The position passed in to this function does not represent the final embedded
	//      element position, so we do not send it to the host (i.e. no calls to
	//      <IEmbeddedElementHost::UpdatePosition> here).
	//------------------------------------------------------------------------
	public override Result Format(
		TextSource pTextSource,
		float remainingParagraphWidth,
		Point currentPosition,
		out ObjectRunMetrics pMetrics)
	{
		IEmbeddedElementHost? pHost = null;
		DependencyObject? pParent = null;
		bool measureElement = true;
		UIElement? pElement = null;

		// TODO Uno (integrate): CInlineUIContainer::GetChild
		pElement = m_pContainer.GetChild();
		pHost = pTextSource.GetEmbeddedElementHost();

		// If the host is not the same as the cached host, element needs to be removed from the
		// cached host and added to the current host. However, in certain cases the host
		// may not support reparenting, e.g. if formatting already took place and the inline object is only
		// being reformatted during arrange/rendering.
		// TODO Uno (integrate): CInlineUIContainer::GetCachedHost / IEmbeddedElementHost::CanAddElement
		if (pHost != m_pContainer.GetCachedHost() &&
			pHost!.CanAddElement())
		{
			measureElement = true;
			// TODO Uno (integrate): CInlineUIContainer::EnsureDetachedFromHost / EnsureAttachedToHost
			m_pContainer.EnsureDetachedFromHost();
			m_pContainer.EnsureAttachedToHost(pHost);
		}
		else
		{
			// If we are called with a different host that cannot support adding elements, we shouldn't
			// remeasure the element at its current host - it's unnecessary and may actually invalidate the host needlessly.
			// In that case we leave the host as is and skip measuring. If the host is the same as the
			// element's current host, then we can measure without reparenting.
			// Essentially we will not perform Measure unless the calling host can (or already does) parent the element.
			if (pHost != m_pContainer.GetCachedHost())
			{
				measureElement = false;
			}
			else
			{
				// In CRichTextBox embedded element hosting, embedded elements may be removed from their
				// host temporarily during formatting due to undo and certain backing store behavior.
				// So even if the host is the same as cached host on InlineUIContainer, it may not
				// actually be attached to the host. In CRichTextBlock/PageHostedObjectNode code path,
				// this can't happen. The host will set the inline container's cached host to NULL
				// when it's detached, so if it has a cached host, it's parented.
				pParent = GetEmbeddedElementParent();
				MUX_ASSERT(pParent is not null);
			}
		}

		// Host can be NULL if we're being measured outside the tree.
		if (pElement is not null && pHost is not null)
		{
			if (measureElement)
			{
				float baseline;
				// TODO Uno (integrate): CUIElement::Measure / IEmbeddedElementHost::GetAvailableMeasureSize
				pElement.Measure(pHost.GetAvailableMeasureSize());

				// If the child doesn't fit, it will be removed from the parent RichTextBlock after Measure completes.
				// At that point, layout storage is cleared, so cache desired size to ensure a consistent
				// desired size when called outside of measure (e.g., hit testing) when we cannot
				// re-parent the child just to re-measure it.
				// TODO Uno (integrate): BlockLayoutHelpers::GetElementBaseline
				BlockLayoutHelpers.GetElementBaseline(pElement, out baseline);
				// TODO Uno (integrate): CInlineUIContainer::SetChildLayoutCache(width, height, baseline)
				m_pContainer.SetChildLayoutCache(pElement.DesiredSize.Width, pElement.DesiredSize.Height, baseline);
			}

			// TODO Uno (integrate): CInlineUIContainer::GetChildLayoutCache(out width, out height, out baseline)
			m_pContainer.GetChildLayoutCache(out var width, out var height, out var cachedBaseline);
			pMetrics = new ObjectRunMetrics((float)width, (float)height, (float)cachedBaseline);
		}
		else
		{
			pMetrics = default; // memset(pMetrics, 0, sizeof(ObjectRunMetrics))
		}

		// TODO Uno (integrate): TxerrFromXResult(hr) - map success/throw to RichTextServices::Result.
		return Result.Success;
	}

	public override Result ComputeBoundingBox(
		bool rightToLeft,
		bool sideways,
		out Rect pBounds)
	{
		pBounds = default;
		return Result.NotImplemented;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the embedded element's parent. Used to verify
	//      that the element is correctly parented.
	//------------------------------------------------------------------------
	public override Result Arrange(
		Point position)
	{
		// TODO Uno (integrate): CInlineUIContainer::GetCachedHost
		IEmbeddedElementHost? pHost = m_pContainer.GetCachedHost();

		if (pHost is not null)
		{
			// TODO Uno (integrate): IEmbeddedElementHost::UpdateElementPosition(container, position)
			pHost.UpdateElementPosition(m_pContainer, position);
		}

		// TODO Uno (integrate): TxerrFromXResult(hr) - map success/throw to RichTextServices::Result.
		return Result.Success;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the embedded element's parent. Used to verify
	//      that the element is correctly parented.
	//------------------------------------------------------------------------
	private DependencyObject? GetEmbeddedElementParent()
	{
		DependencyObject? ppParent = null;
		UIElement? pElement = null;

		// TODO Uno (integrate): CInlineUIContainer::GetChild
		pElement = m_pContainer.GetChild();

		if (pElement is not null)
		{
			// TODO Uno (integrate): do_pointer_cast<CRichTextBlock>(pElement->GetParentInternal())
			var pRichTextBlock = pElement.GetParentInternal() as RichTextBlock;
			if (pRichTextBlock is not null)
			{
				ppParent = pRichTextBlock;
			}
			else
			{
				// TODO Uno (integrate): do_pointer_cast<CRichTextBlockOverflow>(pElement->GetParentInternal())
				var pRichTextBlockOverflow = pElement.GetParentInternal() as RichTextBlockOverflow;
				if (pRichTextBlockOverflow is not null)
				{
					ppParent = pRichTextBlockOverflow;
				}
			}
		}

		return ppParent;
	}
}
