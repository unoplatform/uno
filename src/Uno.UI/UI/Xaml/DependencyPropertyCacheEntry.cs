#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// An optimized cache entry for the DependencyProperty.GetProperty method.
	/// </summary>
	internal class PropertyCacheEntry
	{
		private Type Type;
		private string Name;
		private int CachedHashCode;

		public PropertyCacheEntry()
			=> Update(typeof(object), "");

		private PropertyCacheEntry(PropertyCacheEntry other)
		{
			CachedHashCode = other.CachedHashCode;
			Name = other.Name;
			Type = other.Type;
		}

		public PropertyCacheEntry Clone()
			=> new(this);

		/// <summary>
		/// Mutates the fields from this instance
		/// </summary>
		[MemberNotNull(nameof(Type), nameof(Name))]
		public void Update(Type type, string name)
		{
			this.Type = type;
			this.Name = name;
			this.CachedHashCode = type.GetHashCode() ^ name.GetHashCode();
		}

		public static readonly Comparer DefaultComparer = new Comparer();

		internal class Comparer : IEqualityComparer<PropertyCacheEntry>
		{
			bool IEqualityComparer<PropertyCacheEntry>.Equals(PropertyCacheEntry? x, PropertyCacheEntry? y)
			{
				// This method assumes that there will never be null parameters, and that the Type and Name fields 
				// are never null.
				return x!.Type == y!.Type && string.CompareOrdinal(x.Name, y.Name) == 0;
			}

			int IEqualityComparer<PropertyCacheEntry>.GetHashCode(PropertyCacheEntry? obj)
				=> obj?.CachedHashCode ?? 0;
		}
	}

}
