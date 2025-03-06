// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBar_Partial.h, tag winui3/release/1.6.5, commit 444ec5

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.DataBinding;
using static Uno.UI.FeatureConfiguration;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar
{
	private double GetCompactHeight() => m_compactHeight;
	private double GetMinimalHeight() => m_minimalHeight;
	private double GetContentHeight() => m_contentHeight;

	// Template parts.
	protected Grid? m_tpLayoutRoot;
	protected FrameworkElement? m_tpContentRoot;
	protected ButtonBase? m_tpExpandButton;
	protected VisualStateGroup? m_tpDisplayModesStateGroup;

	private double GetMinCompactHeight()
	{
		return m_minCompactHeight;
	}

	private void SetMinCompactHeight(double value)
	{
		if (m_minCompactHeight != value)
		{
			m_minCompactHeight = value;
		}
	}

	private void SetCompactHeight(double value)
	{
		if (m_compactHeight != value)
		{
			m_compactHeight = value;
		}
	}

	private void SetMinimalHeight(double value)
	{
		if (m_minimalHeight != value)
		{
			m_minimalHeight = value;
		}
	}

	private void SetContentHeight(double value)
	{
		if (m_contentHeight != value)
		{
			m_contentHeight = value;
		}
	}

	private AppBarMode m_Mode;

	// Focus state to be applied on loaded.
	private FocusState m_onLoadFocusState;

	// Owner, if this AppBar is owned by a Page using TopAppBar/BottomAppBar.
	private ManagedWeakReference? m_wpOwner;

	private SerialDisposable m_loadedEventHandler = new();
	private SerialDisposable m_unloadedEventHandler = new();
	private SerialDisposable m_layoutUpdatedEventHandler = new();
	private SerialDisposable m_sizeChangedEventHandler = new();
	private SerialDisposable m_contentRootSizeChangedEventHandler = new();
	private SerialDisposable m_xamlRootChangedEventHandler = new();
	private SerialDisposable m_expandButtonClickEventHandler = new();
	private SerialDisposable m_displayModeStateChangedEventHandler = new();

	private UIElement? m_layoutTransitionElement;
	private UIElement? m_overlayLayoutTransitionElement;

	private UIElement? m_parentElementForLTEs;
	private FrameworkElement? m_overlayElement;
	private SerialDisposable m_overlayElementPointerPressedEventHandler = new();

	private ManagedWeakReference? m_savedFocusedElementWeakRef;
	private FocusState m_savedFocusState;

	private bool m_isInOverlayState;
	private bool m_isChangingOpenedState;
	private bool m_hasUpdatedTemplateSettings;
	private bool m_hasExpandButtonCustomAutomationName;

	// We refresh this value in the OnSizeChanged() & OnContentSizeChanged() handlers.
	private double m_contentHeight;

	private double m_minCompactHeight;
	private double m_compactHeight;
	private double m_minimalHeight;

	private bool m_isOverlayVisible;
	private Storyboard? m_overlayOpeningStoryboard;
	private Storyboard? m_overlayClosingStoryboard;
}
