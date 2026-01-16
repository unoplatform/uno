// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\ScrollBar_Partial.h, tag winui3/release/1.6.5, commit 444ec52426

using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Uno.Disposables;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using DirectUI;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollBar
{
	// Flag indicating whether the ScrollBar must react to user input or not.
	private bool m_isIgnoringUserInput;

	// Flag indicating whether the mouse is over the
	private bool m_isPointerOver;

	// Used to prevent GoToState(true /*bUseTransitions*/) calls while applying the template.
	// We don't want to show the initial fade-out of the mouse/panning indicators.
	private bool m_suspendVisualStateUpdates = true; // = true: Visual state update are disabled until the template has been applied at least once!

	// Value indicating how far the ScrollBar has beeen dragged.
	private double m_dragValue;

	// Template parts for the horizontal and vertical templates
	// (each including a root, increase small/large repeat buttons, and
	// a thumb).
	private FrameworkElement m_tpElementHorizontalTemplate;
	private RepeatButton m_tpElementHorizontalLargeIncrease;
	private RepeatButton m_tpElementHorizontalLargeDecrease;
	private RepeatButton m_tpElementHorizontalSmallIncrease;
	private RepeatButton m_tpElementHorizontalSmallDecrease;
	private Thumb m_tpElementHorizontalThumb;
	private FrameworkElement m_tpElementVerticalTemplate;
	private RepeatButton m_tpElementVerticalLargeIncrease;
	private RepeatButton m_tpElementVerticalLargeDecrease;
	private RepeatButton m_tpElementVerticalSmallIncrease;
	private RepeatButton m_tpElementVerticalSmallDecrease;
	private Thumb m_tpElementVerticalThumb;

	private FrameworkElement m_tpElementHorizontalPanningRoot;
	private FrameworkElement m_tpElementHorizontalPanningThumb;
	private FrameworkElement m_tpElementVerticalPanningRoot;
	private FrameworkElement m_tpElementVerticalPanningThumb;

	// Event registration tokens for events attached to template parts
	// so the handlers can be removed if we apply a new template.
	private SerialDisposable m_ElementHorizontalThumbDragStartedToken = new SerialDisposable();
	private SerialDisposable m_ElementHorizontalThumbDragDeltaToken = new SerialDisposable();
	private SerialDisposable m_ElementHorizontalThumbDragCompletedToken = new SerialDisposable();
	private SerialDisposable m_ElementHorizontalLargeDecreaseClickToken = new SerialDisposable();
	private SerialDisposable m_ElementHorizontalLargeIncreaseClickToken = new SerialDisposable();
	private SerialDisposable m_ElementHorizontalSmallDecreaseClickToken = new SerialDisposable();
	private SerialDisposable m_ElementHorizontalSmallIncreaseClickToken = new SerialDisposable();
	private SerialDisposable m_ElementVerticalThumbDragStartedToken = new SerialDisposable();
	private SerialDisposable m_ElementVerticalThumbDragDeltaToken = new SerialDisposable();
	private SerialDisposable m_ElementVerticalThumbDragCompletedToken = new SerialDisposable();
	private SerialDisposable m_ElementVerticalLargeDecreaseClickToken = new SerialDisposable();
	private SerialDisposable m_ElementVerticalLargeIncreaseClickToken = new SerialDisposable();
	private SerialDisposable m_ElementVerticalSmallDecreaseClickToken = new SerialDisposable();
	private SerialDisposable m_ElementVerticalSmallIncreaseClickToken = new SerialDisposable();

	// value that indicates that we are currently blocking indicators from showing
	private bool m_blockIndicators;

	// Enters/Leaves the mode where the child's actual size is used
	// for the extent exposed through IScrollInfo
	private bool m_isUsingActualSizeAsExtent;

	private static bool IsConscious() => Uno.UI.Helpers.WinUI.SharedHelpers.ShouldUseDynamicScrollbars();

	// Enters the mode where the child's actual size is used for
	// the extent exposed through IScrollInfo.
	internal void StartUseOfActualSizeAsExtent()
	{
		m_isUsingActualSizeAsExtent = true;
	}

	// Leaves the mode where the child's actual size is used for
	// the extent exposed through IScrollInfo.
	internal void StopUseOfActualSizeAsExtent()
	{
		m_isUsingActualSizeAsExtent = false;
	}
}
