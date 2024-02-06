// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\ContentRoot\ContentRootCoordinator.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Core;

internal class ContentRootCoordinator
{
	private readonly List<ContentRoot> _contentRoots = new();
	private readonly CoreServices _coreServices;

	// Idealy m_unsafe_XamlIslandsIncompatible_CoreWindowContentRoot must be null in Win32/Desktop/islands, but it is not. This has been tracked by Task# 30029924
	internal ContentRoot? _unsafe_XamlIslandsIncompatible_CoreWindowContentRoot;

	public ContentRootCoordinator(CoreServices coreServices)
	{
		_coreServices = coreServices ?? throw new ArgumentNullException(nameof(coreServices));
	}

	public IReadOnlyList<ContentRoot> ContentRoots => _contentRoots;

	public ContentRoot? Unsafe_XamlIslandsIncompatible_CoreWindowContentRoot => _unsafe_XamlIslandsIncompatible_CoreWindowContentRoot;

	public ContentRoot CreateContentRoot(ContentRootType type, Color backgroundColor, UIElement? rootElement)
	{
		var contentRoot = new ContentRoot(type, backgroundColor, rootElement, _coreServices);

		_contentRoots.Add(contentRoot);

		return contentRoot;
	}

	public void RemoveContentRoot(ContentRoot contentRoot) => _contentRoots.Remove(contentRoot);
}
