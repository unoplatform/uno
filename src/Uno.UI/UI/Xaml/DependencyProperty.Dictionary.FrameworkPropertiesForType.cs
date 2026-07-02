#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.UI.Xaml;
using Uno;
using Uno.Collections;
using Uno.Extensions;

namespace Microsoft.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class FrameworkPropertiesForTypeDictionary
		{
			// Single process-lifetime instance, keyed by owner Type through a ConditionalWeakTable (weak
			// keys) so a collectible-ALC control type is not pinned here (Type -> RuntimeType ->
			// LoaderAllocator). Eviction alone loses the race to the unloading app's residual DP activity;
			// weak identity keys remove the pin structurally. Type identity is canonical, so weak-keyed
			// lookup is equivalent to the previous strong table.
			private readonly ConditionalWeakTable<Type, DependencyProperty[]> _entries = new();

			internal bool TryGetValue(Type key, out DependencyProperty[]? result)
			{
				if (_entries.TryGetValue(key, out var value))
				{
					result = value;

					return true;
				}

				result = null;
				return false;
			}

			internal void Add(Type key, DependencyProperty[] value)
				=> _entries.AddOrUpdate(key, value);

			internal void Clear() => _entries.Clear();

			/// <summary>
			/// Eagerly removes entries whose Type key belongs to a non-default ALC (weak keys also let
			/// them be collected once the type is otherwise unreferenced).
			/// </summary>
			internal void RemoveNonDefaultAlcEntries()
			{
				var keysToRemove = new List<Type>();
				foreach (var entry in _entries)
				{
					var alc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(entry.Key.Assembly);
					if (alc is not null && alc != global::System.Runtime.Loader.AssemblyLoadContext.Default)
					{
						keysToRemove.Add(entry.Key);
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
