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
			// This dictionary has a single static instance that is kept for the lifetime of the whole app.
			// So we don't use pooling to not cause pool exhaustion by renting without returning.
			private readonly HashtableEx _entries = new HashtableEx(usePooling: false);

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

			internal void Add(Type key, DependencyProperty[] value)
				=> _entries.Add(key, value);

			internal void Clear() => _entries.Clear();

			/// <summary>
			/// Removes entries whose Type key belongs to a non-default ALC.
			/// </summary>
			internal void RemoveNonDefaultAlcEntries()
			{
				var keysToRemove = new List<Type>();
				foreach (var key in _entries.Keys)
				{
					if (key is Type t)
					{
						var alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(t.Assembly);
						if (alc is not null && alc != System.Runtime.Loader.AssemblyLoadContext.Default)
						{
							keysToRemove.Add(t);
						}
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
