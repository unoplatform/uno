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
			// This dictionary has a single static instance that is kept for the lifetime of the whole app.
			// So we don't use pooling to not cause pool exhaustion by renting without returning.
			private readonly HashtableEx _entries = new HashtableEx(FastTypeComparer.Default, usePooling: false);

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
		}
	}
}
