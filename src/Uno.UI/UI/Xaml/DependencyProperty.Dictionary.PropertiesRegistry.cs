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
using Uno.UI.Helpers;
using System.Collections;

namespace Windows.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class DependencyPropertyRegistry
		{
			private readonly Hashtable _entries = new Hashtable(FastTypeComparer.Default);

			internal bool TryGetValue(Type type, string name, out DependencyProperty? result)
			{
				if (TryGetTypeTable(type, out var typeTable))
				{
					if (typeTable![name] is { } propertyObject)
					{
						result = (DependencyProperty)propertyObject!;
						return true;
					}
				}

				result = null;
				return false;
			}

			internal void Clear() => _entries.Clear();

			internal void Add(Type type, string name, DependencyProperty property)
			{
				if (!TryGetTypeTable(type, out var typeTable))
				{
					typeTable = new Hashtable();
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
						properties.Add((DependencyProperty)value);
					}
				}
			}

			private bool TryGetTypeTable(Type type, out Hashtable? table)
			{
				if (_entries[type] is { } dictionaryObject)
				{
					table = (Hashtable)dictionaryObject!;
					return true;
				}

				table = null;
				return false;
			}
		}
	}
}
