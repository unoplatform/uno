// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutStorage.h and LayoutStorage.cpp

using System.Diagnostics;
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
	// Uno docs: We use a simple Rect rather than RectangleGeometry
	// Uno docs: This doesn't include clipping from UIElement.Clip dependency property. It's all about the layout clipping calculated by the arrange logic.
#if __ANDROID__ || __IOS__ || __MACOS__
	internal Rect? m_pLayoutClipGeometry;
#endif

	internal bool HasLayoutStorage;

	internal void ResetLayoutInformation()
	{
		m_previousAvailableSize = default;
		m_desiredSize = default;
		m_finalRect = default;
		//m_offset = default;
		m_unclippedDesiredSize = default;
		m_size = default;
#if __ANDROID__ || __IOS__ || __MACOS__
		m_pLayoutClipGeometry = default;
#endif
	}

	internal void EnsureLayoutStorage()
	{
		HasLayoutStorage = true;
	}

	internal void Shutdown()
	{
		// Cancel all active transitions.
		//if (Transition.HasActiveTransition(this) && this.GetContext() is not null)
		//{
		//	try
		//	{
		//		Transition.CancelTransitions(this);
		//	}
		//	catch
		//	{
		//	}
		//}


		// Cancel and clean-up unloading transitions as well.
		//IGNOREHR(m_pChildren->RemoveAllUnloadingChildren(false /* removeFromKeepAliveList */, nullptr /* dcompTreeHost */));
		foreach (var child in VisualTreeHelper.GetManagedVisualChildren(this))
		{
			child.Shutdown();
		}

		ResetLayoutInformation();
		HasLayoutStorage = false;

		//Clearing all layout flags except the expecting events.
		//m_layoutFlags &= LF_EXPECTED_EVENTS;

		// note we do not change the life cycle of an element here. it will get
		// the left tree value from the LeaveImpl call
	}
}
