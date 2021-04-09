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

using _Key = Uno.CachedTuple<System.Type, Windows.UI.Xaml.FrameworkPropertyMetadataOptions>;

namespace Windows.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class FrameworkPropertiesForTypeDictionary
		{
			private readonly HashtableEx _entries = new HashtableEx();

			internal bool TryGetValue(_Key key, out DependencyProperty[]? result)
			{
				if (_entries.TryGetValue(key, out var value))
				{
					result = (DependencyProperty[])value!;

					return true;
				}

				result = null;
				return false;
			}

			internal void Add(_Key key, DependencyProperty[] value)
				=> _entries.Add(key, value);

			internal void Clear() => _entries.Clear();
		}
	}
}
