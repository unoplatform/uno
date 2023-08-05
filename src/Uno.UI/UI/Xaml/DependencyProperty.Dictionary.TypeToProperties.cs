#nullable enable

using System;
using System.Collections;
using Uno.Collections;
using Uno.UI.Helpers;

namespace Windows.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class TypeToPropertiesDictionary
		{
			private readonly Hashtable _entries = new Hashtable(FastTypeComparer.Default);

			internal bool TryGetValue(Type key, out DependencyProperty[]? result)
			{
				if (_entries[key] is { } value)
				{
					result = (DependencyProperty[])value!;

					return true;
				}

				result = null;
				return false;
			}

			internal void Add(Type key, DependencyProperty[] dependencyProperty)
				=> _entries.Add(key, dependencyProperty);

			internal void Clear()
				=> _entries.Clear();
		}
	}
}
