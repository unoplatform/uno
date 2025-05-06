// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutItem_Partial.h, tag winui3/release/1.7.1, commit 5f27a786ac96c

using System;
using Uno.Client;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using ICommand = System.Windows.Input.ICommand;
using Microsoft.UI.Xaml.Markup;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutItem
{
	// Whether the pointer is currently over the
	private bool m_bIsPointerOver;

	// Whether the pointer is currently pressed over the
	internal bool m_bIsPressed;

	// Whether the pointer's left button is currently down.
	internal bool m_bIsPointerLeftButtonDown;

	// True if the SPACE or ENTER key is currently pressed, false otherwise.
	internal bool m_bIsSpaceOrEnterKeyDown;

	// True if the NAVIGATION_ACCEPT or GAMEPAD_A vkey is currently pressed, false otherwise.
	internal bool m_bIsNavigationAcceptOrGamepadAKeyDown;

	// On pointer released we perform some actions depending on control. We decide to whether to perform them
	// depending on some parameters including but not limited to whether released is followed by a pressed, which
	// mouse button is pressed, what type of pointer is it etc. This bool keeps our decision.
	private bool m_shouldPerformActions;

	// Event pointer for the ICommand.CanExecuteChanged event.
	private readonly SerialDisposable m_epCanExecuteChangedHandler = new(); // TODO:MZ: Should be WeakReference

	// Event pointer for the Loaded event.
	private readonly SerialDisposable m_epLoadedHandler = new();

	private readonly SerialDisposable m_epMenuFlyoutItemClickEventCallback = new();

	private double m_maxKeyboardAcceleratorTextWidth;
	private TextBlock m_tpKeyboardAcceleratorTextBlock;

	private bool m_isTemplateApplied;

	private bool m_isSettingKeyboardAcceleratorTextOverride;
	private bool m_ownsKeyboardAcceleratorTextOverride = true;

	internal virtual bool HasToggle() => false;

#if false // Unused in WinUI
	private bool GetIsPressed() => m_bIsPressed;

	private bool GetIsPointerOver() => m_bIsPointerOver;
#endif
}
