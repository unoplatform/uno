// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\VisualTree.h, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Islands;
using static Microsoft.UI.Xaml.Controls._Tracing;

#if __IOS__
using UIKit;
#endif

#if __MACOS__
using AppKit;
#endif

namespace Uno.UI.Xaml.Core;

partial class VisualTree
{
	private const int VisualDiagnosticsRootZIndex = UnoTopZIndex - 1;
	private const int ConnectedAnimationRootZIndex = VisualDiagnosticsRootZIndex - 1;
	private const int PopupZIndex = ConnectedAnimationRootZIndex - 1;
	private const int FullWindowMediaRootZIndex = PopupZIndex - 1;

	internal enum LookupOptions
	{
		/// <summary>
		/// Normal lookup.
		/// </summary>
		NoFallback = 0,

		/// <summary>
		/// Provides warning if not found.
		/// </summary>
		WarningIfNotFound = 1
	}

	internal RootVisual? RootVisual => _rootVisual;

	/// <summary>
	/// RootElement is the parent of the roots.  For XAML app window content, this is the _rootVisual.
	// For XamlIsland content, it's the XamlIslandRoot.
	/// </summary>
	internal UIElement? RootElement => _rootElement;

	internal UIElement? PublicRootVisual => _publicRootVisual;

	public ScrollViewer? RootScrollViewer => _rootScrollViewer;

	private readonly CoreServices _core;
	private readonly ContentRoot _coreContentRoot;

	private readonly UIElement _rootElement;
	private RootVisual? _rootVisual;
	private PopupRoot? _popupRoot;
	private Grid? _visualDiagnosticsRoot;
	private UIElement? _publicRootVisual;
	private ScrollViewer? _rootScrollViewer;
	private ContentPresenter? _rootContentPresenter;
	//PrintRoot m_printRoot;
	//TransitionRoot m_transitionRoot;
	private FullWindowMediaRoot? _fullWindowMediaRoot;
	//RenderTargetBitmapRoot m_renderTargetBitmapRoot;
	//ConnectedAnimationRoot m_connectedAnimationRoot;
	private XamlIslandRootCollection? _xamlIslandRootCollection;
	//std::shared_ptr<CLayoutManager> m_layoutManager;
	//std::shared_ptr<RootScale> m_rootScale;

	// This is effectively the public API wrapper for this type
	private XamlRoot? _xamlRoot;

	//std::shared_ptr<QualifierContext> m_pQualifierContext { nullptr };

	bool IsMainVisualTree => _rootVisual is not null;

	bool _isRootScrollViewerAddedToRoot;
	bool _shutdownInProgress;
	bool _isShutdown;
}
