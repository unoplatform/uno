#nullable enable

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Uno.Core.Comparison;

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

		internal class Comparer : IEqualityComparer
		{
			bool IEqualityComparer.Equals(object? x, object? y)
			{
				if (x is PropertyCacheEntry left && y is PropertyCacheEntry right)
				{
					// This method assumes that there will never be null parameters, and that the Type and Name fields 
					// are never null.
					return left.Type == right.Type
					&& (
							object.ReferenceEquals(left.Name, right.Name)
							|| string.CompareOrdinal(left.Name, right.Name) == 0
						);
				}
				else
				{
					return false;
				}
			}

			int IEqualityComparer.GetHashCode(object? obj)
				=> obj is PropertyCacheEntry entry ? entry.CachedHashCode : 0;
		}
	}

}
