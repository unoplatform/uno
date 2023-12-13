// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Popup_Partial.h

using Uno.Disposables;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	enum MajorPlacementMode
	{
		Auto,
		Top,
		Bottom,
		Left,
		Right,
	};

	enum PreferredJustification
	{
		Auto,
		HorizontalCenter,
		VerticalCenter,
		Top,
		Bottom,
		Left,
		Right
	};

	enum DismissalTriggerFlags : uint
	{
		None = 0x0,
		CoreLightDismiss = 0x1,
		WindowSizeChange = 0x2,
		WindowDeactivated = 0x4,
		BackPress = 0x8,
		All = 0xFFFFFFFF    // Note: This flag encompases all present and future flags, hence
							// it is not equivilant to the OR of the current set of flags.
	};

	private readonly SerialDisposable m_WindowActivatedToken = new SerialDisposable();

	// Window's SizeChanged event handler.  Used for light-dismiss on screen orientation change.
	//	ctl.EventPtr<WindowSizeChangedEventCallback> m_epWindowSizeChangedHandler;

	//	ctl.WeakRefPtr m_wrOwner;

	// Previous size of the xaml root, used to detect orientation/size changes for light dismiss.
	private double m_previousXamlRootWidth;
	private double m_previousXamlRootHeight;

	// Previous position of the window, used to detect when we move.
	private double m_previousWindowX;
	private double m_previousWindowY;

	// Store the flags that determine what triggers dismissal of this popup
	private uint m_dismissalTriggerFlags;

	private string m_defaultAutomationName;

	private SerialDisposable m_placementTargetLayoutUpdatedToken = new();
	private SerialDisposable m_childSizeChangedToken = new();
	private SerialDisposable m_childLoadedToken = new();
	ctl::WeakRefPtr m_wrOldChild;

	bool m_placementAndJustificationCalculated{ false };
	MajorPlacementMode m_calculatedMajorPlacement;
	PreferredJustification m_calculatedJustification;
}
