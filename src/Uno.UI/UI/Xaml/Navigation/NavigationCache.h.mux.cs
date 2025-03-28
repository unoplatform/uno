// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\NavigationCache.h, tag winui3/release/1.5.5, commit fd8e26f1d

//  Abstract:
//      Asynchronously loads frame content and caches it. Uses two caches:
//      one for permanent caching, and one for transient caching. The size
//      of the transient cache is bounded, and the size of the permanent
//      cache is not. Loading can be canceled and performed again
//      synchronously.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace DirectUI;

partial class NavigationCache
{
	private Frame m_pIFrame;

	// Transient cache size (Frame.CacheSize)
	private int m_transientCacheSize;

	// List of items in the transient cache, sorted by Most Recently Used. MRU cached 
	// item is at the end. Items are flushed out when m_transientCacheSize is exceeded.
	private readonly LinkedList<string> m_transientCacheMruList = new();

	// Transient cache. Items are flushed out when m_transientCacheSize is exceeded.
	private readonly PropertySet m_transientMap = new();

	// Permanent cache. Items are not flushed out.
	private readonly PropertySet m_permanentMap = new();
}
