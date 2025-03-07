// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBarToggleButton_Partial.h, tag winui3/release/1.6.4, commit 262a901e09

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

public partial class AppBarToggleButton
{
	internal void SetInputMode(InputDeviceType inputDeviceTypeUsedToOpenOverflow) =>
		m_inputDeviceTypeUsedToOpenOverflow = inputDeviceTypeUsedToOpenOverflow;

	// LabelOnRightStyle doesn't work in AppBarButton/AppBarToggleButton Reveal Style.
	// Animate the width to NaN if width is not overridden and right-aligned labels and no LabelOnRightStyle. 
	private Storyboard m_widthAdjustmentsForLabelOnRightStyleStoryboard;

	private CommandBarDefaultLabelPosition m_defaultLabelPosition;
	private InputDeviceType m_inputDeviceTypeUsedToOpenOverflow;

	private TextBlock m_tpKeyboardAcceleratorTextLabel;

	// We won't actually set the label-on-right style unless we've applied the template,
	// because we won't have the label-on-right style from the template until we do.
	private bool m_isTemplateApplied;


	// We need to adjust our visual state to account for CommandBarElements that use Icons.
	private bool m_isWithIcons;

	// We need to adjust our visual state to account for CommandBarElements that have keyboard accelerator text.
	private bool m_isWithKeyboardAcceleratorText;
	private double m_maxKeyboardAcceleratorTextWidth;

	// If we have a keyboard accelerator attached to us and the app has not set a tool tip on us,
	// then we'll create our own tool tip.  We'll use this flag to indicate that we can unset or
	// overwrite that tool tip as needed if the keyboard accelerator is removed or the button
	// moves into the overflow section of the app bar or command bar.
	private bool m_ownsToolTip;
}
