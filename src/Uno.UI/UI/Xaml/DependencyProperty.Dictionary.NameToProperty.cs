#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Uno.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno;
using System.Threading;
using Uno.Collections;
using System.Collections;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#else
using _View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class NameToPropertyDictionary
		{
			private readonly Hashtable _entries = new Hashtable(PropertyCacheEntry.DefaultComparer);
			private static readonly object _nullSentinel = new();

			internal bool TryGetValue(PropertyCacheEntry key, out DependencyProperty? result)
			{
				if (_entries[key] is { } value)
				{
					if (object.ReferenceEquals(value, _nullSentinel))
					{
						result = null;
					}
					else
					{
						result = (DependencyProperty?)value;
					}

					return true;
				}

				result = null;
				return false;
			}

			internal void Add(PropertyCacheEntry key, DependencyProperty? dependencyProperty)
				=> _entries.Add(key, dependencyProperty ?? _nullSentinel);

			internal void Remove(PropertyCacheEntry propertyCacheEntry)
				=> _entries.Remove(propertyCacheEntry);

			internal int Count => _entries.Count;

			internal void Clear() => _entries.Clear();
		}
	}
}
