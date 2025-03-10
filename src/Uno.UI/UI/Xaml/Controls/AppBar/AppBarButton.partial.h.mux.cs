// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBarButton_Partial.h, tag winui3/release/1.6.4, commit 262a901e09

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarButton
{
	internal void SetInputMode(InputDeviceType inputDeviceTypeUsedToOpenOverflow) =>
		m_inputDeviceTypeUsedToOpenOverflow = inputDeviceTypeUsedToOpenOverflow;

	bool ISubMenuOwner.IsSubMenuPositionedAbsolutely => false;

	// LabelOnRightStyle doesn't work in AppBarButton/AppBarToggleButton Reveal Style.
	// Animate the width to NaN if width is not overridden and right-aligned labels and no LabelOnRightStyle. 
	private Storyboard? m_widthAdjustmentsForLabelOnRightStyleStoryboard;

	private bool m_isWithToggleButtons;
	private bool m_isWithIcons;
	private CommandBarDefaultLabelPosition m_defaultLabelPosition;
	private InputDeviceType m_inputDeviceTypeUsedToOpenOverflow;

	private TextBlock? m_tpKeyboardAcceleratorTextLabel;

	// We won't actually set the label-on-right style unless we've applied the template,
	// because we won't have the label-on-right style from the template until we do.
	private bool m_isTemplateApplied;

	// We need to adjust our visual state to account for CommandBarElements that have keyboard accelerator text.
	private bool m_isWithKeyboardAcceleratorText;
	private double m_maxKeyboardAcceleratorTextWidth;

	// If we have a keyboard accelerator attached to us and the app has not set a tool tip on us,
	// then we'll create our own tool tip.  We'll use this flag to indicate that we can unset or
	// overwrite that tool tip as needed if the keyboard accelerator is removed or the button
	// moves into the overflow section of the app bar or command bar.
	private bool m_ownsToolTip;

	// Helper to which to delegate cascading menu functionality.
	internal CascadingMenuHelper? m_menuHelper;

	// Helpers to track the current opened state of the flyout.
	private bool m_isFlyoutClosing;
	private SerialDisposable m_flyoutOpenedHandler = new();
	private SerialDisposable m_flyoutClosedHandler = new();

	// Holds the last position that its flyout was opened at.
	// Used to reposition the flyout on size changed.
	private Point m_lastFlyoutPosition;
}
