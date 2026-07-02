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
using Uno.UI.Helpers;
using System.Collections;

namespace Microsoft.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		internal class DependencyPropertyRegistry
		{
			public static DependencyPropertyRegistry Instance { get; } = new DependencyPropertyRegistry();

			// Single process-lifetime instance. The OUTER map is keyed by owner Type through a
			// ConditionalWeakTable (weak keys) rather than a strong HashtableEx: the owner type of a
			// control loaded into a collectible AssemblyLoadContext (plugin / preview hosting) would
			// otherwise be retained here (Type -> RuntimeType -> LoaderAllocator), pinning the whole ALC.
			// The per-type eviction sweep (RemoveNonDefaultAlcEntries) cannot win the race against the
			// unloading app's residual DP activity re-populating the entry; weak identity keys remove the
			// pin structurally. Type identity is canonical, so weak-keyed lookup is equivalent to the
			// previous FastTypeComparer-keyed table. Inner per-type tables (keyed by property name) stay
			// strong HashtableEx and are collected together with their weakly-held type key.
			private readonly ConditionalWeakTable<Type, HashtableEx> _entries = new();

			private DependencyPropertyRegistry()
			{
			}

			internal bool TryGetValueByName(string type, string name, out DependencyProperty? result)
			{
				foreach (var entry in _entries)
				{
					if (entry.Key.Name == type && entry.Value.TryGetValue(name, out var propertyObject))
					{
						result = (DependencyProperty)propertyObject!;
						return true;
					}
				}

				result = null;
				return false;
			}

			internal bool TryGetValue(Type type, string name, out DependencyProperty? result)
			{
				if (TryGetTypeTable(type, out var typeTable))
				{
					if (typeTable!.TryGetValue(name, out var propertyObject))
					{
						result = (DependencyProperty)propertyObject!;
						return true;
					}
				}

				result = null;
				return false;
			}

			internal void Add(Type type, string name, DependencyProperty property)
			{
				if (!TryGetTypeTable(type, out var typeTable))
				{
					typeTable = new HashtableEx(usePooling: false);
					_entries.AddOrUpdate(type, typeTable);
				}

				typeTable!.Add(name, property);
			}

			internal void AppendInheritedPropertiesForType(Type type, List<DependencyProperty> properties)
			{
				if (TryGetTypeTable(type, out var typeTable))
				{
					foreach (var value in typeTable!.Values)
					{
						var dp = (DependencyProperty)value;
						if (dp.IsInherited)
						{
							properties.Add(dp);
						}
					}
				}
			}

			internal bool TryGetTypeTable(Type type, out HashtableEx? table)
			{
				if (_entries.TryGetValue(type, out var typeTable))
				{
					table = typeTable;
					return true;
				}

				table = null;
				return false;
			}

			/// <summary>
			/// Eagerly removes entries whose Type key belongs to a non-default (collectible) ALC. The
			/// weak keys already let such entries be collected once the type is otherwise unreferenced;
			/// this drops them promptly on ALC teardown rather than waiting for the next GC.
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
