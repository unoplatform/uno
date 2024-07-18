using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace DirectUI;

internal partial class NavigationCache
{
	typedef std::list<wrl_wrappers::HString> StringListType;

	private Frame m_pIFrame;

	// Transient cache size (Frame.CacheSize)
	private int m_transientCacheSize;

	// List of items in the transient cache, sorted by Most Recently Used. MRU cached 
	// item is at the end. Items are flushed out when m_transientCacheSize is exceeded.
	StringListType m_transientCacheMruList;

	// Transient cache. Items are flushed out when m_transientCacheSize is exceeded.
	object m_transientMap;

	// Permanent cache. Items are not flushed out.
	object m_permanentMap;
}
