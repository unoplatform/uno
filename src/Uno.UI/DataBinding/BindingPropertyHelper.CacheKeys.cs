#nullable enable

#if !NETFX_CORE
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Windows.UI.Xaml;

namespace Uno.UI.DataBinding;

internal static partial class BindingPropertyHelper
{

	private sealed class GetValueGetterCacheKey(Type type, string name, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
	{
		private readonly int _hashCode =
				type.GetHashCode()
				^ name.GetHashCode()
				^ (precedence is not null ? (int)precedence : -1)
				^ (allowPrivateMembers ? 1 : 0);

		public Type Type { get; } = type;
		public string Name { get; } = name;
		public DependencyPropertyValuePrecedences? Precedence { get; } = precedence;
		public bool PrivateMembers { get; } = allowPrivateMembers;

		internal static IEqualityComparer<GetValueGetterCacheKey> Comparer { get; } = new EqualityComparer();

		public override int GetHashCode() => _hashCode;

		private sealed class EqualityComparer : IEqualityComparer<GetValueGetterCacheKey>
		{
			public bool Equals(GetValueGetterCacheKey? x, GetValueGetterCacheKey? y)
			{
				if (ReferenceEquals(x, y))
				{
					return true;
				}

				if (x is not null && y is not null)
				{
					return x.Type.GetType().Equals(y.Type.GetType())
						&& x.Name.Equals(y.Name, StringComparison.Ordinal)
						&& x.Precedence == y.Precedence
						&& x.PrivateMembers == y.PrivateMembers;
				}

				return false;
			}

			public int GetHashCode([DisallowNull] GetValueGetterCacheKey obj)
				=> obj._hashCode;
		}
	}

	private sealed class GetValueSetterCacheKey(Type type, string name, DependencyPropertyValuePrecedences precedence, bool convert)
	{
		private readonly int _hashCode =
				type.GetHashCode()
				^ name.GetHashCode()
				^ (int)precedence
				^ (convert ? 1 : 0);

		public Type Type { get; } = type;
		public string Name { get; } = name;
		public DependencyPropertyValuePrecedences Precedence { get; } = precedence;
		public bool Convert { get; } = convert;

		internal static IEqualityComparer<GetValueSetterCacheKey> Comparer { get; } = new EqualityComparer();

		public override int GetHashCode() => _hashCode;

		private sealed class EqualityComparer : IEqualityComparer<GetValueSetterCacheKey>
		{
			public bool Equals(GetValueSetterCacheKey? x, GetValueSetterCacheKey? y)
			{
				if (ReferenceEquals(x, y))
				{
					return true;
				}

				if (x is not null && y is not null)
				{
					return x.Type.GetType().Equals(y.Type.GetType())
						&& x.Name.Equals(y.Name, StringComparison.Ordinal)
						&& x.Precedence == y.Precedence
						&& x.Convert == y.Convert;
				}

				return false;
			}

			public int GetHashCode([DisallowNull] GetValueSetterCacheKey obj)
				=> obj._hashCode;
		}
	}

	private sealed class GenericPropertyCacheKey(Type type, string name, DependencyPropertyValuePrecedences precedence)
	{
		private readonly int _hashCode =
				type.GetHashCode()
				^ name.GetHashCode()
				^ (int)precedence;

		public Type Type { get; } = type;
		public string Name { get; } = name;
		public DependencyPropertyValuePrecedences Precedence { get; } = precedence;

		internal static IEqualityComparer<GenericPropertyCacheKey> Comparer { get; } = new EqualityComparer();

		public override int GetHashCode() => _hashCode;

		private sealed class EqualityComparer : IEqualityComparer<GenericPropertyCacheKey>
		{
			public bool Equals(GenericPropertyCacheKey? x, GenericPropertyCacheKey? y)
			{
				if (ReferenceEquals(x, y))
				{
					return true;
				}

				if (x is not null && y is not null)
				{
					return x.Type.GetType().Equals(y.Type.GetType())
						&& x.Name.Equals(y.Name, StringComparison.Ordinal)
						&& x.Precedence == y.Precedence;
				}

				return false;
			}

			public int GetHashCode([DisallowNull] GenericPropertyCacheKey obj)
				=> obj._hashCode;
		}
	}

	private sealed class EventCacheKey(Type type, string name)
	{
		private readonly int _hashCode =
				type.GetHashCode()
				^ name.GetHashCode();

		public Type Type { get; } = type;
		public string Name { get; } = name;

		internal static IEqualityComparer<EventCacheKey> Comparer { get; } = new EqualityComparer();

		public override int GetHashCode() => _hashCode;

		private sealed class EqualityComparer : IEqualityComparer<EventCacheKey>
		{
			public bool Equals(EventCacheKey? x, EventCacheKey? y)
			{
				if (ReferenceEquals(x, y))
				{
					return true;
				}

				if (x is not null && y is not null)
				{
					return x.Type.GetType().Equals(y.Type.GetType())
						&& x.Name.Equals(y.Name, StringComparison.Ordinal);
				}

				return false;
			}

			public int GetHashCode([DisallowNull] EventCacheKey obj)
				=> obj._hashCode;
		}
	}
}
#endif
