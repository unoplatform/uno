#nullable enable

using System;
using Uno.Collections;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class TypeToPropertiesDictionary
		{
			private readonly HashtableEx _entries = new HashtableEx(FastTypeComparer.Default);

			internal bool TryGetValue(Type key, out DependencyProperty[]? result)
			{
				if (_entries.TryGetValue(key, out var value))
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

			internal void Dispose()
				=> _entries.Dispose();
		}
	}
}
