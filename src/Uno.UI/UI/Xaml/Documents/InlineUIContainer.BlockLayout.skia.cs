// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InlineUIContainer.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents;

partial class InlineUIContainer
{
	private IEmbeddedElementHost? m_pCachedHost;
	private bool m_isChildAttached;
	private float m_childDesiredWidth;  // Cached child desired width. Only used in RichTextBlock.
	private float m_childDesiredHeight; // Cached child desired height. Only used in RichTextBlock.
	private float m_childBaseline;      // Cached child baseline. Only used in RichTextBlock.

	//------------------------------------------------------------------------
	//  Summary:
	//      Returns the child of the InlineUIContainer. Can be NULL.
	//------------------------------------------------------------------------
	internal UIElement? GetChild() => m_pChild;

	internal void EnsureAttachedToHost(IEmbeddedElementHost? pHost)
	{
		if (pHost is not null && m_pChild is not null && !m_isChildAttached)
		{
			pHost.AddElement(this);

			m_isChildAttached = true;
			m_pCachedHost = pHost;
		}
	}

	internal void EnsureDetachedFromHost()
	{
		if (m_pChild is not null && m_isChildAttached)
		{
			m_pCachedHost!.RemoveElement(this);
			m_isChildAttached = false;
		}
	}

	internal IEmbeddedElementHost? GetCachedHost() => m_pCachedHost;

	internal void ClearCachedHost()
	{
		m_isChildAttached = false;
		m_pCachedHost = null;
	}

	// Stores measured width, height, and baseline.
	internal void SetChildLayoutCache(float width, float height, float baseline)
	{
		m_childDesiredWidth = width;
		m_childDesiredHeight = height;
		m_childBaseline = baseline;
	}

	// Retrieves measured width, height, and baseline.
	internal void GetChildLayoutCache(out float pWidth, out float pHeight, out float pBaseline)
	{
		pWidth = m_childDesiredWidth;
		pHeight = m_childDesiredHeight;
		pBaseline = m_childBaseline;
	}

	// The host lives in the Skia block-layout engine, so the Child setter reaches it through these.
	partial void DetachChildFromHost() => EnsureDetachedFromHost();

	partial void AttachChildToCachedHost() => EnsureAttachedToHost(m_pCachedHost);
}
