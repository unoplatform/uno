#nullable enable

using System;
using Uno.Collections;
using Uno.UI.Helpers;
using Uno.Extensions;
using System.Collections;

namespace Microsoft.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private readonly static TypeNullableDictionary _isTypeNullableDictionary = new TypeNullableDictionary();

		private bool GetIsTypeNullable(Type type)
		{
			if (!_isTypeNullableDictionary.TryGetValue(type, out var isNullable))
			{
				isNullable = type.IsNullable();

				// Never cache collectible types: the app-lifetime dictionary would pin the
				// type's AssemblyLoadContext after unload — and teardown sweeps themselves can
				// re-query such types, racing any clear-on-teardown approach. Type.IsCollectible
				// (not Assembly.IsCollectible) also covers generic instantiations whose
				// DEFINITION lives in a shared assembly (e.g. ObservableCollection<T> from
				// CoreLib) but whose type ARGUMENT is collectible.
				if (!type.IsCollectible)
				{
					_isTypeNullableDictionary.Add(type, isNullable);
				}
			}

			return isNullable;
		}

		private partial class TypeNullableDictionary
		{
			// This dictionary has a single static instance that is kept for the lifetime of the whole app.
			// So we don't use pooling to not cause pool exhaustion by renting without returning.
			private readonly HashtableEx _entries = new HashtableEx(FastTypeComparer.Default, usePooling: false);

			internal bool TryGetValue(Type key, out bool result)
			{
				if (_entries.TryGetValue(key, out var value))
				{
					result = (bool)value!;
					return true;
				}

				result = false;
				return false;
			}

			internal void Add(Type key, bool isNullable)
				=> _entries.Add(key, isNullable);

			internal void Clear()
				=> _entries.Clear();
		}
	}
}
