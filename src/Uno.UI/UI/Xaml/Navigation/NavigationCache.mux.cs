// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\NavigationCache.cpp, tag winui3/release/1.5.5, commit fd8e26f1d

//  Abstract:
//      Asynchronously loads frame content and caches it. Uses two caches:
//      one for permanent caching, and one for transient caching. The size
//      of the transient cache is bounded, and the size of the permanent
//      cache is not. Loading can be canceled and performed again
//      synchronously.
//  Notes:
//      Transient caching uses a least-recently-used algorithm to
//      determine which content to replace.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DirectUI;

internal partial class NavigationCache
{
	private NavigationCache()
	{
	}

	public static NavigationCache Create(Frame frame, int transientCacheSize)
	{
		var pNavigationCache = new NavigationCache();

		pNavigationCache.m_transientCacheSize = transientCacheSize;
		pNavigationCache.m_pIFrame = frame;

		return pNavigationCache;
	}

	internal object GetContent(PageStackEntry pPageStackEntry)
	{
		bool shouldCache = false;
		bool found = false;
		object pobject = null;
		NavigationCacheMode navigationCacheMode = NavigationCacheMode.Disabled;

		if (pPageStackEntry is null)
		{
			throw new ArgumentNullException(nameof(pPageStackEntry));
		}

		var strDescriptor = pPageStackEntry.GetDescriptor();
		GetCachedContent(strDescriptor, out pobject, out found);

		if (!found)
		{
			pobject = LoadContent(strDescriptor);
			if (pobject is null)
			{
				throw new InvalidOperationException("Content not found");
			}

			if (pobject is Page pIPage)
			{
				navigationCacheMode = pIPage.NavigationCacheMode;
			}

			shouldCache = m_transientCacheSize >= 1 && navigationCacheMode != NavigationCacheMode.Disabled;

			if (shouldCache)
			{
				CacheContent(navigationCacheMode, strDescriptor, pobject);
			}
		}

		pPageStackEntry.PrepareContent(pobject);

		return pobject;

		//TraceNavigationCacheGetContentInfo(WindowsGetStringRawBuffer(strDescriptor, null), found);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "The provided type has been marked before getting at that location")]
	private object LoadContent(string descriptor)
	{
		var type = PageStackEntry.ResolveDescriptor(descriptor);
		return Frame.CreatePageInstance(type);
	}

	private void GetCachedContent(
		string descriptor,
		out object ppobject,
		out bool pFound)
	{
		if (descriptor is null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		var found = m_permanentMap.ContainsKey(descriptor);
		object pobject = null;

		if (found)
		{
			pobject = m_permanentMap[descriptor];
		}
		else
		{
			found = m_transientMap.ContainsKey(descriptor);

			if (found)
			{
				pobject = m_transientMap[descriptor];

				// Make the descriptor the most recently used.
				m_transientCacheMruList.Remove(descriptor);
				m_transientCacheMruList.AddLast(descriptor);
			}
		}

		ppobject = pobject;
		pFound = found;
	}

	private void CacheContent(
		 NavigationCacheMode navigationCacheMode,
		 string descriptor,
		 object pobject)
	{
		if (navigationCacheMode == NavigationCacheMode.Disabled)
		{
			throw new InvalidOperationException("Cache is disabled");
		}

		if (navigationCacheMode == NavigationCacheMode.Enabled)
		{
			if (m_transientCacheSize < 1)
			{
				throw new InvalidOperationException("Cache is empty");
			}
			if (m_transientCacheMruList.Count > m_transientCacheSize)
			{
				throw new InvalidOperationException("Unexpected cache size");
			}

			if (m_transientCacheMruList.Count == m_transientCacheSize)
			{
				UncacheContent();
			}

			var strDescriptor = descriptor;
			m_transientCacheMruList.AddLast(strDescriptor);

			m_transientMap[descriptor] = pobject;
		}
		else if (navigationCacheMode == NavigationCacheMode.Required)
		{
			m_permanentMap[descriptor] = pobject;
		}
	}

	private void UncacheContent()
	{
		if (m_transientCacheMruList.Count < 1)
		{
			throw new InvalidOperationException("Cache is empty");
		}

		var size = m_transientMap.Size;
		if (size < 1)
		{
			throw new InvalidOperationException("Cache is empty");
		}

		// Remove LRU item
		var strDescriptor = m_transientCacheMruList.First.Value;
		m_transientCacheMruList.RemoveFirst();

		m_transientMap.Remove(strDescriptor);
	}

	internal void UncachePageContent(string descriptor)
	{
		var found = m_permanentMap.ContainsKey(descriptor);
		if (found)
		{
			m_permanentMap.Remove(descriptor);
		}
		else
		{
			m_transientCacheMruList.Remove(descriptor);
			m_transientMap.Remove(descriptor);
		}
	}

	internal void ChangeTransientCacheSize(int transientCacheSize)
	{
		int itemsToUncache = 0;

		// If the transient cache size has been reduced,
		// uncache if needed
		if (transientCacheSize < m_transientCacheSize)
		{
			var size = m_transientCacheMruList.Count;

			if (size > transientCacheSize)
			{
				itemsToUncache = size - transientCacheSize;
			}

			for (int i = 0; i < itemsToUncache; ++i)
			{
				UncacheContent();
			}
		}

		m_transientCacheSize = transientCacheSize;
	}

	//void ReferenceTrackerWalk(EReferenceTrackerWalkType walkType)
	//{
	//	m_transientMap.ReferenceTrackerWalk(walkType);
	//	m_permanentMap.ReferenceTrackerWalk(walkType);
	//}

}
