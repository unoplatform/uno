#nullable enable

using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml
{
	internal class PropertyMetadataDictionary
	{
		private readonly Dictionary<Type, PropertyMetadata> _table = new();

		internal void Add(Type ownerType, PropertyMetadata ownerTypeMetadata)
			=> _table.Add(ownerType, ownerTypeMetadata);

		internal bool TryGetValue(Type ownerType, out PropertyMetadata? metadata)
		{
			return _table.TryGetValue(ownerType, out metadata);
		}

		internal bool ContainsKey(Type ownerType)
			=> _table.ContainsKey(ownerType);

		internal bool ContainsValue(PropertyMetadata typeMetadata)
			=> _table.ContainsValue(typeMetadata);

		internal PropertyMetadata FindOrCreate(Type ownerType, Type baseType, DependencyProperty property)
		{
			if (_table.TryGetValue(ownerType, out var value))
			{
				return value;
			}

			var metadata = property.GetMetadata(baseType);

			_table[ownerType] = metadata;

			return metadata;
		}
	}
}
