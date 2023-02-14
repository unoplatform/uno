#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno;
using System.Threading;
using Uno.Collections;

#if XAMARIN_ANDROID
using _View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using _View = UIKit.UIView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class NameToPropertyDictionary
		{
			private readonly HashtableEx _entries = new HashtableEx(PropertyCacheEntry.DefaultComparer);

			internal bool TryGetValue(PropertyCacheEntry key, out DependencyProperty? result)
			{
				if (_entries.TryGetValue(key, out var value))
				{
					result = (DependencyProperty)value!;

					return true;
				}

				result = null;
				return false;
			}

			internal void Add(PropertyCacheEntry key, DependencyProperty dependencyProperty)
				=> _entries.Add(key, dependencyProperty);

			internal void Remove(PropertyCacheEntry propertyCacheEntry)
				=> _entries.Remove(propertyCacheEntry);

			internal int Count => _entries.Count;

			internal void Clear() => _entries.Clear();

			internal void Dispose()
				=> _entries.Dispose();
		}
	}
}
