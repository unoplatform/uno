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

				// Never cache collectible-assembly types: the app-lifetime dictionary would pin
				// the type's AssemblyLoadContext after unload — and teardown sweeps themselves
				// can re-query such types, racing any clear-on-teardown approach.
				if (!type.Assembly.IsCollectible)
				{
					_isTypeNullableDictionary.Add(type, isNullable);
				}
			}

			return isNullable;
		}

		private class TypeNullableDictionary
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

			/// <summary>
			/// Removes entries whose key <see cref="Type"/> belongs to a non-default
			/// (collectible) <see cref="System.Runtime.Loader.AssemblyLoadContext"/>, so the
			/// app-lifetime dictionary does not pin an unloaded secondary app's types.
			/// </summary>
			internal void RemoveNonDefaultAlcEntries()
			{
				var defaultAlc = global::System.Runtime.Loader.AssemblyLoadContext.Default;
				var keysToRemove = new global::System.Collections.Generic.List<Type>();

				foreach (Type key in _entries.Keys)
				{
					var alc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(key.Assembly);
					if (alc is not null && alc != defaultAlc)
					{
						keysToRemove.Add(key);
					}
				}

				foreach (var key in keysToRemove)
				{
					_entries.Remove(key);
				}
			}
		}
	}
}
