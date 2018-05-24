using System;
using System.Collections.Generic;
using System.Text;
using Uno.Core.Comparison;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// An optimized cache entry for the DependencyProperty.GetProperty method.
	/// </summary>
	internal class PropertyCacheEntry
	{
		private readonly Type Type;
		private readonly string Name;
		private readonly int CachedHashCode;

		public PropertyCacheEntry(Type type, string name)
		{
			this.Type = type;
			this.Name = name;
			this.CachedHashCode = type.GetHashCode() ^ name.GetHashCode();
		}

		public static readonly Comparer DefaultComparer = new Comparer();

		internal class Comparer : IEqualityComparer<PropertyCacheEntry>
		{
			bool IEqualityComparer<PropertyCacheEntry>.Equals(PropertyCacheEntry left, PropertyCacheEntry right)
			{
				// This method assumes that there will never be null parameters, and that the Type and Name fields 
				// are never null.
				return left.Type == right.Type
				&& (
						object.ReferenceEquals(left.Name, right.Name)
						|| string.CompareOrdinal(left.Name, right.Name) == 0
					);
			}

			int IEqualityComparer<PropertyCacheEntry>.GetHashCode(PropertyCacheEntry obj)
			{
				return obj.CachedHashCode;
			}
		}
	}

}
