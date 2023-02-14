#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno;
using System.Threading;
using Uno.Collections;

namespace Microsoft.UI.Xaml
{
	internal class PropertyMetadataDictionary
	{
		/// <summary>
		/// This implementation of PropertyMetadataDictionary uses HashTable because it's generally faster than
		/// Dictionary`ref/ref. 
		/// </summary>
		private readonly HashtableEx _table = new HashtableEx();

		internal delegate PropertyMetadata CreationHandler();

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

		internal PropertyMetadata FindOrCreate(Type ownerType, CreationHandler createHandler)
		{
			if (_table.TryGetValue(ownerType, out var value))
			{
				return (PropertyMetadata)value!;
			}

			var metadata = createHandler();

			_table[ownerType] = metadata;

			return metadata;
		}

		internal void Dispose()
			=> _table.Dispose();
	}
}
