// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SplitView_Partial.h, commit 69097129a

#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class SplitView
{
	// Event revokers
	private readonly SerialDisposable m_loadedRevoker = new();
	private readonly SerialDisposable m_unloadedRevoker = new();
	private readonly SerialDisposable m_sizeChangedRevoker = new();
	private readonly SerialDisposable m_xamlRootChangedRevoker = new();
	private readonly SerialDisposable m_displayModeStateChangedRevoker = new();
	private readonly SerialDisposable m_dismissLayerPointerPressedRevoker = new();

	// Template parts (framework layer)
	private FrameworkElement? m_tpPaneRoot;
	private FrameworkElement? m_tpContentRoot;
	private VisualStateGroup? m_tpDisplayModeStates;

	// Outer dismiss layer elements
	private Popup? m_outerDismissLayerPopup;
	private Grid? m_dismissHostElement;
	private Path? m_topDismissElement;
	private Path? m_bottomDismissElement;
	private Path? m_leftDismissElement;
	private Path? m_rightDismissElement;

	// State tracking
	private bool m_isPaneOpeningOrClosing;
}
