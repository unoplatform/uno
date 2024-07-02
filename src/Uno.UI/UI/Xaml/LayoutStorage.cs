// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutStorage.h and LayoutStorage.cpp
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

partial class UIElement
{
	// This is dynamic information that is used by the layout system, and does not need to be saved
	// if the element enters or leaves the tree. It also does not need to be validated.
	// note: keep ordering as is. See methodex FN_GET_LAYOUT_INFORMATION
	internal Size m_previousAvailableSize;
	internal Size m_desiredSize;
	internal Rect m_finalRect;
	//internal Point m_offset;
	internal Size m_unclippedDesiredSize;
	internal Size m_size;

	// Stores the layout clip, which is always an axis-aligned rect.
	// The coordinate space of this clip varies, depending on whether or not we're applying the layout
	// clip as a self clip, or as an ancestor clip.
	// When acting as a self clip, the rect is in the local coordinate space of the element.
	// When acting as an ancestor clip, the rect is in a special coordinate space, defined as follows:
	// [Parent local coordinate space] + [Child Offset]
	// What this does is puts the layout clip above the Child's render transforms, but below its offset.
	// Note that this definition requires that the layout clip not be applied when animating the Child Offset,
	// since this is only possible with children of a Canvas, which does not apply any layout clip, this works out fine.
	//private RectangleGeometry m_pLayoutClipGeometry;

	internal bool HasLayoutStorage;

	internal void ResetLayoutInformation()
	{
		m_previousAvailableSize = default;
		m_desiredSize = default;
		m_finalRect = default;
		//m_offset = default;
		m_unclippedDesiredSize = default;
		m_size = default;
		//m_pLayoutClipGeometry = default;
	}
}
