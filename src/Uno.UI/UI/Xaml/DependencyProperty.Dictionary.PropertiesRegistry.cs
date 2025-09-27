﻿#nullable enable

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

			// This dictionary has a single static instance that is kept for the lifetime of the whole app.
			// So we don't use pooling to not cause pool exhaustion by renting without returning.
			private readonly HashtableEx _entries = new HashtableEx(FastTypeComparer.Default, usePooling: false);

			private DependencyPropertyRegistry()
			{
			}

			internal bool TryGetValueByName(string type, string name, out DependencyProperty? result)
			{
				foreach (Type key in _entries.Keys)
				{
					if (key.Name == type && TryGetTypeTable(key, out var typeTable))
					{
						if (typeTable!.TryGetValue(name, out var propertyObject))
						{
							result = (DependencyProperty)propertyObject!;
							return true;
						}
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
					_entries[type] = typeTable;
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
				if (_entries.TryGetValue(type, out var dictionaryObject))
				{
					table = (HashtableEx)dictionaryObject!;
					return true;
				}

				table = null;
				return false;
			}
		}
	}
}
