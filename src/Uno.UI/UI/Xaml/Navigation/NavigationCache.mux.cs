// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
		object* pobject = null;
		NavigationCache* pNavigationCache = null;

		IFCPTR(ppNavigationCache);
		*ppNavigationCache = null;

		IFCPTR(pIFrame);

		pNavigationCache = new public NavigationCache();

		pNavigationCache.m_transientCacheSize = transientCacheSize;
		pNavigationCache.m_pIFrame = pIFrame;

		*ppNavigationCache = pNavigationCache;

	Cleanup:
		ReleaseInterface(pobject);


		pNavigationCache = null;

		RRETURN(hr);
	}

	private object GetContent(PageStackEntry pPageStackEntry)
	{
		Page pIPage = null;
		bool shouldCache = false;
		bool found = false;
		string strDescriptor;
		object* pobject = null;
		N.NavigationCacheMode navigationCacheMode = N.NavigationCacheMode_Disabled;

		IFCPTR(ppobject);
		*ppobject = null;

		IFCPTR(pPageStackEntry);

		var strDescriptor = pPageStackEntry.GetDescriptor();
		GetCachedContent(strDescriptor, &pobject, &found);

		if (!found)
		{
			LoadContent(strDescriptor, &pobject);
			IFCPTR(pobject);

			pIPage = ctl.query_interface<IPage>(pobject);

			if (pIPage)
			{
				navigationCacheMode = pIPage.NavigationCacheMode;
			}

			shouldCache = m_transientCacheSize >= 1 && navigationCacheMode != N.NavigationCacheMode_Disabled;

			if (shouldCache)
			{
				CacheContent(navigationCacheMode, strDescriptor, pobject);
			}
		}

		pPageStackEntry.PrepareContent(pobject);

		*ppobject = pobject;
		pobject = null;

		TraceNavigationCacheGetContentInfo(WindowsGetStringRawBuffer(strDescriptor, null), found);
	}

	private object LoadContent(string descriptor)
	{
		var type = Type.GetType(descriptor);
		return Activator.CreateInstance(type);
	}

	private void GetCachedContent(
		 string descriptor,
		out object ppobject,
		out bool pFound)
	{

		bool found = false;
		object* pobject = null;
		StringListType.iterator listIterator;

		*ppobject = null;
		*pFound = false;

		IFCPTR(descriptor);

		m_permanentMap.HasKey(descriptor, &found);

		if (found)
		{
			m_permanentMap.Lookup(descriptor, &pobject);
		}
		else
		{
			m_transientMap.HasKey(descriptor, &found);

			if (found)
			{
				m_transientMap.Lookup(descriptor, &pobject);

				// Make the descriptor the most recently used.
				for (listIterator = m_transientCacheMruList.begin(); listIterator != m_transientCacheMruList.end(); ++listIterator)
				{
					if (descriptor == *listIterator)
					{
						m_transientCacheMruList.splice(m_transientCacheMruList.end(), m_transientCacheMruList, listIterator);
						break;
					}
				}
			}
		}

		*ppobject = pobject;
		pobject = null;

		*pFound = found;
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

	private void UncachePageContent(string descriptor)
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
