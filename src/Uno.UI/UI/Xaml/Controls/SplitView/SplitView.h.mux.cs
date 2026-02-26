// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SplitView.h, commit 67aeb8f23

#nullable enable

using System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class SplitView
{
	// Template part names
	private const string c_paneClipRectangle = "PaneClipRectangle";
	private const string c_contentRoot = "ContentRoot";
	private const string c_paneRoot = "PaneRoot";
	private const string c_lightDismissLayer = "LightDismissLayer";

	// Template parts (core layer)
	private RectangleGeometry? m_paneClipRectangle;
	private UIElement? m_coreContentRoot;
	private UIElement? m_corePaneRoot;
	private UIElement? m_coreLightDismissLayer;

	// Focus tracking
	private WeakReference<DependencyObject>? m_prevFocusedElementWeakRef;
	private FocusState m_prevFocusState = FocusState.Unfocused;

	// Pane state
	private bool m_isPaneClosingByLightDismiss;
	private double m_paneMeasuredLength;
}
