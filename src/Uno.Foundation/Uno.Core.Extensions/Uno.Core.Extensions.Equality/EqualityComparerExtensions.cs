#nullable disable

// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Uno.Core.Equality
{
	internal static class EqualityComparerExtensions
	{
		/// <summary>
		/// Create a non-generic <see cref="IEqualityComparer"/> from a generic version.
		/// </summary>
		public static IEqualityComparer ToEqualityComparer<T>(this IEqualityComparer<T> genericComparer)
		{
			if (genericComparer is IEqualityComparer comparer)
			{
				return comparer;
			}
			return new GenericToNonGenericComparerAdapter<T>(genericComparer);
		}

		#region GenericToNonGenericComparerAdapter<T> helper class
		private class GenericToNonGenericComparerAdapter<T> : IEqualityComparer, IEqualityComparer<T>
		{
			private readonly IEqualityComparer<T> _genericComparer;

			private static readonly bool IsValueType = typeof(ValueType).IsAssignableFrom(typeof(T));

			public GenericToNonGenericComparerAdapter(IEqualityComparer<T> genericComparer) => _genericComparer = genericComparer;

			bool IEqualityComparer.Equals(object x, object y)
			{
				if (IsValueType)
				{
					return x is T left && y is T right && _genericComparer.Equals(left, right);
				}
				if ((x == null || x is T) && (y == null || y is T))
				{
					return _genericComparer.Equals((T)x!, (T)y!);
				}
				return false;
			}

			int IEqualityComparer.GetHashCode(object obj)
			{
				if (IsValueType)
				{
					return obj is T o ? _genericComparer.GetHashCode(o) : -1;
				}

				return obj == null || obj is T ? _genericComparer.GetHashCode((T)obj!) : -1;
			}

			bool IEqualityComparer<T>.Equals(T x, T y) => _genericComparer.Equals(x!, y!);

			int IEqualityComparer<T>.GetHashCode(T obj) => _genericComparer.GetHashCode(obj!);
		}
		#endregion

		public static IEqualityComparer<T> ToEqualityComparer<T>(this IEqualityComparer comparer)
		{
			if (comparer is IEqualityComparer<T> genericComparer)
			{
				return genericComparer;
			}
			return new NonGenericToGenericComparerAdapter<T>(comparer);
		}

		#region NonGenericToGenericComparerAdapter<T> helper class
		private class NonGenericToGenericComparerAdapter<T> : IEqualityComparer<T>, IEqualityComparer
		{
			private readonly IEqualityComparer _inner;

			public NonGenericToGenericComparerAdapter(IEqualityComparer inner) => _inner = inner;

			bool IEqualityComparer<T>.Equals(T x, T y) => _inner.Equals(x, y);

			int IEqualityComparer<T>.GetHashCode(T obj) => _inner.GetHashCode(obj!);

			bool IEqualityComparer.Equals(object x, object y) => _inner.Equals(x, y);

			int IEqualityComparer.GetHashCode(object obj) => _inner.GetHashCode(obj);
		}
		#endregion
	}
}
