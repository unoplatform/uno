// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\CascadingMenuHelper.h, tag winui3/release/1.5.4, commit 98a60c8

using System;
using Uno.Disposables;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

internal partial class CascadingMenuHelper
{
	// This fallback is used if we fail to retrieve a value from the MenuShowDelay RegKey
	private const int DefaultMenuShowDelay = 400; // in milliseconds

	internal bool IsPressed => m_isPressed;

	internal bool IsPointerOver => m_isPointerOver;

	// The overlapped menu pixels between the main main menu presenter and the sub presenter
	private const int m_subMenuOverlapPixels = 4;

	private int m_subMenuShowDelay;

	// Owner of the cascading menu
	private ManagedWeakReference m_wpOwner;

	// Presenter of the sub-menu
	private ManagedWeakReference m_wpSubMenuPresenter;

	// Event pointer for the Loaded event
	private readonly SerialDisposable m_loadedHandler = new();

	// Dispatcher timer to delay showing the sub menu flyout
	private DispatcherTimer m_delayOpenMenuTimer;

	// Dispatcher timer to delay hiding the sub menu flyout
	private DispatcherTimer m_delayCloseMenuTimer;

	// Indicate the pointer is over the cascading menu owner
	private bool m_isPointerOver;

	// Indicate the pointer is pressed on the cascading menu owner
	private bool m_isPressed;
}
