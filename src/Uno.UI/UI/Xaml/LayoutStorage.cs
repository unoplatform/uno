// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutStorage.h and LayoutStorage.cpp

#if !UNO_REFERENCE_API
// Layout storage causes issues on Android at least. We disable it on layouter-based platforms for now.
#define LAYOUTER_WORKAROUND
#endif

using System.Diagnostics;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace Windows.UI.Xaml;

partial class UIElement
{
	// This is dynamic information that is used by the layout system, and does not need to be saved
	// if the element enters or leaves the tree. It also does not need to be validated.
	// note: keep ordering as is. See methodex FN_GET_LAYOUT_INFORMATION
	internal Size m_previousAvailableSize;
	internal Size m_desiredSize;
	internal Rect m_finalRect;
	//internal Point m_offset;
#if UNO_REFERENCE_API
	// On mobile, stored in Layouter
	internal Size m_unclippedDesiredSize;
#endif
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

#if LAYOUTER_WORKAROUND
	// Causes issues for Layouter-based platforms.
	internal bool HasLayoutStorage => true;
#else
	internal bool HasLayoutStorage;
#endif

	[Conditional("UNO_REFERENCE_API")]
	internal void ResetLayoutInformation()
	{
#if !LAYOUTER_WORKAROUND
		m_previousAvailableSize = default;
		m_desiredSize = default;
		m_finalRect = default;
		//m_offset = default;
#if UNO_REFERENCE_API
		m_unclippedDesiredSize = default;
#endif
		m_size = default;
		//m_pLayoutClipGeometry = default;
#endif
	}

	[Conditional("UNO_REFERENCE_API")]
	internal void EnsureLayoutStorage()
	{
#if !LAYOUTER_WORKAROUND
		HasLayoutStorage = true;
#endif
	}

	[Conditional("UNO_REFERENCE_API")]
	internal void Shutdown()
	{
#if !LAYOUTER_WORKAROUND
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
#endif
	}
}
