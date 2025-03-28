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
			// This dictionary has a single static instance that is kept for the lifetime of the whole app.
			// So we don't use pooling to not cause pool exhaustion by renting without returning.
			private readonly HashtableEx _entries = new HashtableEx(PropertyCacheEntry.DefaultComparer, usePooling: false);

			internal bool TryGetValue(PropertyCacheEntry key, out DependencyProperty? result)
			{
				if (_entries.TryGetValue(key, out var value))
				{
					result = (DependencyProperty?)value;

					return true;
				}

				result = null;
				return false;
			}

			internal void Add(PropertyCacheEntry key, DependencyProperty? dependencyProperty)
				=> _entries.Add(key, dependencyProperty);

			internal void Remove(PropertyCacheEntry propertyCacheEntry)
				=> _entries.Remove(propertyCacheEntry);

			internal int Count => _entries.Count;

			internal void Clear() => _entries.Clear();
		}
	}
}
