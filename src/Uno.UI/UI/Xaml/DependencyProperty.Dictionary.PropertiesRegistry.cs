#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Helpers;

namespace Windows.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class DependencyPropertyRegistry
		{
			private readonly Dictionary<Type, Dictionary<string, DependencyProperty>> _entries = new(FastTypeComparer.Default);

			internal bool TryGetValue(Type type, string name, out DependencyProperty? result)
			{
				if (TryGetTypeTable(type, out var typeTable))
				{
					return typeTable!.TryGetValue(name, out result);
				}

				result = null;
				return false;
			}

			internal void Clear() => _entries.Clear();

			internal void Add(Type type, string name, DependencyProperty property)
			{
				if (!TryGetTypeTable(type, out var typeTable))
				{
					typeTable = new();
					_entries[type] = typeTable;
				}

				typeTable!.Add(name, property);
			}

			internal void AppendPropertiesForType(Type type, List<DependencyProperty> properties)
			{
				if (TryGetTypeTable(type, out var typeTable))
				{
					foreach (var value in typeTable!.Values)
					{
						properties.Add(value);
					}
				}
			}

			private bool TryGetTypeTable(Type type, out Dictionary<string, DependencyProperty>? table)
			{
				return _entries.TryGetValue(type, out table);
			}
		}
	}
}
