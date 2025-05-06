// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBar_Partial.h, tag winui3/release/1.7.1, commit 5f27a786ac96c

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar
{
	private AppBarMode Mode
	{
		get => m_Mode;
		set
		{
			m_Mode = value;
		}
	}

	private void SetOnLoadFocusState(FocusState focusState) => m_onLoadFocusState = focusState;

	private double CompactHeight
	{
		get => m_compactHeight;
		set
		{
			if (m_compactHeight != value)
			{
				m_compactHeight = value;
			}
		}
	}

	private double MinimalHeight
	{
		get => m_minimalHeight;
		set
		{
			if (m_minimalHeight != value)
			{
				m_minimalHeight = value;
			}
		}
	}

	private double ContentHeight
	{
		get => m_contentHeight;
		set
		{
			if (m_contentHeight != value)
			{
				m_contentHeight = value;
			}
		}
	}

	// Template parts.
	private Grid? m_tpLayoutRoot;
	private FrameworkElement? m_tpContentRoot;
	private ButtonBase? m_tpExpandButton;
	private VisualStateGroup? m_tpDisplayModesStateGroup;

	private bool m_openedWithExpandButton;

	private double MinCompactHeight
	{
		get => m_minCompactHeight;
		set
		{
			if (m_minCompactHeight != value)
			{
				m_minCompactHeight = value;
			}
		}
	}

	private AppBarMode m_Mode;

	// Focus state to be applied on loaded.
	private FocusState m_onLoadFocusState;

	// Owner, if this AppBar is owned by a Page using TopAppBar/BottomAppBar.
	private ManagedWeakReference? m_wpOwner;

	private readonly SerialDisposable m_loadedEventHandler = new();
	private readonly SerialDisposable m_unloadedEventHandler = new();
	private readonly SerialDisposable m_layoutUpdatedEventHandler = new();
	private readonly SerialDisposable m_sizeChangedEventHandler = new();
	private readonly SerialDisposable m_contentRootSizeChangedEventHandler = new();
	private readonly SerialDisposable m_xamlRootChangedEventHandler = new();
	private readonly SerialDisposable m_expandButtonClickEventHandler = new();
	private readonly SerialDisposable m_displayModeStateChangedEventHandler = new();

	private UIElement? m_layoutTransitionElement;
	private UIElement? m_overlayLayoutTransitionElement;

	private UIElement? m_parentElementForLTEs;
	private UIElement? m_overlayElement;
	private SerialDisposable m_overlayElementPointerPressedEventHandler = new();


	private ManagedWeakReference? m_savedFocusedElementWeakRef;
	private FocusState m_savedFocusState;

	private bool m_isInOverlayState;
	private bool m_isChangingOpenedState;
	private bool m_hasUpdatedTemplateSettings;
	private bool m_hasExpandButtonCustomAutomationName;

	// We refresh this value in the OnSizeChanged() & OnContentSizeChanged() handlers.
	double m_contentHeight;
	private double m_minCompactHeight;
	protected double m_compactHeight;
	protected double m_minimalHeight;

	private bool m_isOverlayVisible;
	private Storyboard? m_overlayOpeningStoryboard;
	private Storyboard? m_overlayClosingStoryboard;
}
