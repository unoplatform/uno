#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Uno.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno;
using System.Threading;
using Uno.Collections;

namespace Windows.UI.Xaml
{
	internal class PropertyMetadataDictionary
	{
		/// <summary>
		/// This implementation of PropertyMetadataDictionary uses HashTable because it's generally faster than
		/// Dictionary`ref/ref. 
		/// </summary>
		/// <remarks>
		/// This is created per dependency property instance. So there is a hashtable for each DP, and it's likely that all these instances live for the lifetime of the app.
		/// So we don't use pooling to not cause pool exhaustion by renting without returning.
		/// </remarks>
		private readonly HashtableEx _table = new HashtableEx(usePooling: false);

		internal void Add(Type ownerType, PropertyMetadata ownerTypeMetadata)
			=> _table.Add(ownerType, ownerTypeMetadata);

		internal bool TryGetValue(Type ownerType, out PropertyMetadata? metadata)
		{
			if (_table.TryGetValue(ownerType, out var value))
			{
				metadata = (PropertyMetadata)value!;
				return true;
			}

			metadata = null;
			return false;
		}

		internal bool ContainsKey(Type ownerType)
			=> _table.ContainsKey(ownerType);

		internal bool ContainsValue(PropertyMetadata typeMetadata)
			=> _table.ContainsValue(typeMetadata);

		internal PropertyMetadata FindOrCreate(Type ownerType, Type baseType, DependencyProperty property)
		{
			if (_table.TryGetValue(ownerType, out var value))
			{
				return (PropertyMetadata)value!;
			}

			var metadata = property.GetMetadata(baseType);

			_table[ownerType] = metadata;

			return metadata;
		}
	}
}
