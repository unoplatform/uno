#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarButton
{
	internal void SetInputMode(InputDeviceType inputDeviceTypeUsedToOpenOverflow)
	{
		m_inputDeviceTypeUsedToOpenOverflow = inputDeviceTypeUsedToOpenOverflow;
	}

	bool ISubMenuOwner.IsSubMenuPositionedAbsolutely => false;

	// LabelOnRightStyle doesn't work in AppBarButton/AppBarToggleButton Reveal Style.
	// Animate the width to NaN if width is not overridden and right-aligned labels and no LabelOnRightStyle. 
	private Storyboard m_widthAdjustmentsForLabelOnRightStyleStoryboard;

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
	private CascadingMenuHelper? m_menuHelper;

	// Helpers to track the current opened state of the flyout.
	private bool m_isFlyoutClosing;
	private FlyoutBaseOpenedEventCallback m_flyoutOpenedHandler;
	private FlyoutBaseClosedEventCallback m_flyoutClosedHandler;

	// Holds the last position that its flyout was opened at.
	// Used to reposition the flyout on size changed.
	private Point m_lastFlyoutPosition;

	bool IAppBarButtonHelpersProvider.m_isTemplateApplied { get => m_isTemplateApplied; set => m_isTemplateApplied = value; }

	bool IAppBarButtonHelpersProvider.m_ownsToolTip { get => m_ownsToolTip; set => m_ownsToolTip = value; }

	TextBlock? IAppBarButtonHelpersProvider.m_tpKeyboardAcceleratorTextLabel { get => m_tpKeyboardAcceleratorTextLabel; set => m_tpKeyboardAcceleratorTextLabel = value; }

	bool IAppBarButtonHelpersProvider.m_isWithKeyboardAcceleratorText { get => m_isWithKeyboardAcceleratorText; set => m_isWithKeyboardAcceleratorText = value; }

	double IAppBarButtonHelpersProvider.m_maxKeyboardAcceleratorTextWidth { get => m_maxKeyboardAcceleratorTextWidth; set => m_maxKeyboardAcceleratorTextWidth = value; }

	InputDeviceType IAppBarButtonHelpersProvider.m_inputDeviceTypeUsedToOpenOverflow { get => m_inputDeviceTypeUsedToOpenOverflow; set => m_inputDeviceTypeUsedToOpenOverflow = value; }

	string IAppBarButtonHelpersProvider.Label { get => Label; set => Label = value; }

	string IAppBarButtonHelpersProvider.KeyboardAcceleratorTextOverride { get => KeyboardAcceleratorTextOverride; set => KeyboardAcceleratorTextOverride = value; }

	IAppBarButtonTemplateSettings IAppBarButtonHelpersProvider.TemplateSettings { get => TemplateSettings; set => TemplateSettings = (AppBarButtonTemplateSettings)value; }
}
