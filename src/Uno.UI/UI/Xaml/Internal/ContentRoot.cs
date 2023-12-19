// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ContentRoot.h, ContentRoot.cpp

#nullable enable

using System;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Islands;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

/*
    +----------------------------------------------------------------------------------+
    |                                      +---------------+                           |
    |                                      | CoreServices  |                           |
    |                                      +-------+-------+                           |
    |                                              |                                   |
    |                                              |                                   |
    |                                        +-----v----------------+                  |
    |                              +---------+ContentRootCoordinator|                  |
    |                              |         +-----------------+----+                  |
    |                              |                           |                       |
    |                              |                           |                       |
    |                       +------v-----+            +--------v---+                   |
    |                       |ContentRoot |            |ContentRoot |"Main Content Root"|
    |                       +--+---------+            +------+-----+                   |
    |                          |                             |                         |
    |      +--------------+    |                             |      +---------------+  |
    |      | FocusManager<----+                             +------> FocusManager |    |
    |      +--------------+    |                             |      +---------------+  |
    |      +--------------+    |                             |      +--------------+   |
    |      | InputManager<----+                             +------> InputManager|     |
    |      +--------------+    |                             |      +--------------+   |
    |      +------------+      |                             |      +-----------+      |
    |      | VisualTree <------+                             +------> VisualTree|      |
    |      +------------+                                           +-----------+      |
    |                                                                                  |
    |                                                                                  |
    +----------------------------------------------------------------------------------+
*/

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Represents the content root of an application window.
/// </summary>
internal partial class ContentRoot
{
	private readonly CoreServices _coreServices;
	private readonly ContentRootEventListener _contentRootEventListener;

	/// <summary>
	/// Initializes a content root.
	/// </summary>
	/// <param name="rootElement">Root element.</param>
	public ContentRoot(ContentRootType type, Color backgroundColor, UIElement? rootElement, CoreServices coreServices)
	{
		_coreServices = coreServices ?? throw new ArgumentNullException(nameof(coreServices));
		Type = type;
		//TODO Uno: Does not match WinUI exactly, additional logic can be ported later.
		VisualTree = new VisualTree(coreServices, backgroundColor, rootElement, this);
		InputManager = new InputManager(this);
		m_contentRootEventListener = new ContentRootEventListener(this);
		FocusManager = new FocusManager(this);

		//TODO Uno: We may want to create a custom version of adapter and observer for Island vs CoreWindow.
		FocusAdapter = new FocusAdapter(this);
		FocusManager.SetFocusObserver(new FocusObserver(this));

		switch (type)
		{
			case ContentRootType.CoreWindow:
				MUX_ASSERT(coreServices.ContentRootCoordinator.CoreWindowContentRoot == null);
				coreServices.ContentRootCoordinator.CoreWindowContentRoot = this;
				break;
			case ContentRootType.XamlIsland:
				break;
		}
	}

	internal ContentRootType Type { get; }

	/// <summary>
	/// Represents the visual tree associated with this content root.
	/// </summary>
	internal VisualTree VisualTree { get; }

	/// <summary>
	/// Represents the focus manager associated with this content root.
	/// </summary>
	internal FocusManager FocusManager { get; }

	/// <summary>
	/// Represents the input manager associated with this content root.
	/// </summary>
	internal InputManager InputManager { get; }

	/// <summary>
	/// Represents focus adapter.
	/// </summary>
	internal FocusAdapter FocusAdapter { get; }

	//TODO Uno: Initialize properly when Access Keys are supported (see #3219)
	/// <summary>
	/// Access key export.
	/// </summary>
	internal AccessKeyExport AccessKeyExport { get; } = new AccessKeyExport();

	internal XamlRoot? XamlRoot => VisualTree.XamlRoot;

	internal XamlIsland? XamlIslandRoot { get; set; }

	//TODO Uno: This might need to be adjusted when we have proper lifetime handling
	internal bool IsShuttingDown() => false;

	internal XamlRoot GetOrCreateXamlRoot() => VisualTree.GetOrCreateXamlRoot();

	internal Window? GetOwnerWindow()
	{
		return Type switch
		{
			ContentRootType.CoreWindow => Window.CurrentSafe,
			ContentRootType.XamlIsland when XamlIslandRoot is not null => XamlIslandRoot.OwnerWindow,
			_ => null
		};
	}
}
