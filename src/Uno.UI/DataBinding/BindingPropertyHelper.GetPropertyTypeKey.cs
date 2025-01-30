#nullable enable

#if !NETFX_CORE
using System;
using System.Collections;

namespace Uno.UI.DataBinding
{
	internal static partial class BindingPropertyHelper
	{
		private class GetPropertyTypeKey
		{
			private Type? _type;
			private string? _property;
			private bool _allowPrivateMembers;

			private int _cachedHashcode;

			internal GetPropertyTypeKey()
			{
			}

			private GetPropertyTypeKey(Type type, string property, bool allowPrivateMembers, int cachedHashcode)
			{
				_type = type;
				_property = property;
				_allowPrivateMembers = allowPrivateMembers;

				_cachedHashcode = cachedHashcode;
			}

			internal GetPropertyTypeKey Clone() => new(_type!, _property!, _allowPrivateMembers, _cachedHashcode);

			internal void Update(Type type, string property, bool allowPrivateMembers)
			{
				_type = type;
				_property = property;
				_allowPrivateMembers = allowPrivateMembers;

				_cachedHashcode =
					type.GetHashCode() ^
					property.GetHashCode() ^
					allowPrivateMembers.GetHashCode();
			}

			internal static readonly IEqualityComparer Comparer = new EqualityComparer();

			internal class EqualityComparer : IEqualityComparer
			{
				bool IEqualityComparer.Equals(object? x, object? y)
					=>
						x is GetPropertyTypeKey k1 &&
						y is GetPropertyTypeKey k2 &&
						k1._allowPrivateMembers == k2._allowPrivateMembers &&
						k1._type == k2._type &&
						k1._property == k2._property;

				int IEqualityComparer.GetHashCode(object obj) => obj is GetPropertyTypeKey key ? key._cachedHashcode : 0;
			}
		}
	}
}
#endif
