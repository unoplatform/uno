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
using System.Text;
using Uno.Core.Equality;

namespace Uno.Equality
{
	/// <summary>
	/// An <see cref="IEqualityComparer{T}"/> which compares the key of the objects.
	/// </summary>
	/// <remarks>
	/// For this comparer, the compared objects MUST IMPLEMENT <see cref="IKeyEquatable"/> (the non-generic version)
	/// i.e. **not <see cref="IKeyEquatable{T}"/>**
	/// </remarks>
	internal class KeyEqualityComparer : IEqualityComparer
	{
		private readonly IEqualityComparer _fallbackComparer;

		/// <summary>
		/// Gets the default instance of the <see cref="KeyEqualityComparer"/>.
		/// </summary>
		public static IEqualityComparer Default { get; } = new KeyEqualityComparer();

		public KeyEqualityComparer(IEqualityComparer fallbackComparer = null)
		{
			_fallbackComparer = fallbackComparer ?? EqualityComparer<object>.Default;
		}

		bool IEqualityComparer.Equals(object x, object y)
		{
			// Check for reference equality first (fast discriminator)
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			// Check for Key Equality
			var xKey = GetKeyEquatable(x);
			var yKey = GetKeyEquatable(y);

			if (xKey != null)
			{
				return xKey.KeyEquals(y);
			}

			if (yKey != null)
			{
				return yKey.KeyEquals(x);
			}

			// Fallback to default equality
			return _fallbackComparer.Equals(x, y);
		}

		public int GetHashCode(object obj)
		{
			return GetKeyEquatable(obj)?.GetKeyHashCode() ?? 0;
		}

		private IKeyEquatable GetKeyEquatable(object o)
		{
			if (o is IKeyEquatable ike)
			{
				return ike;
			}

			// TODO: check for generic version here

			return null;
		}
	}

	/// <summary>
	/// An <see cref="IEqualityComparer{T}"/> which compares the key of the objects.
	/// </summary>
	/// <typeparam name="T">Type of the object to compare</typeparam>
	internal class KeyEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
		where T : IKeyEquatable<T>
	{
		/// <summary>
		/// Gets the default instance of the <see cref="KeyEqualityComparer{T}"/>.
		/// </summary>
		public static IEqualityComparer<T> Default { get; } = new KeyEqualityComparer<T>();

		private readonly IEqualityComparer<T> _fallbackComparer;
		private IEqualityComparer _innerNonGeneric;

		public KeyEqualityComparer(IEqualityComparer<T> fallbackComparer = null)
		{
			_fallbackComparer = fallbackComparer ?? EqualityComparer<T>.Default;
		}

		/// <inheritdoc />
		public bool Equals(T left, T right)
		{
			if (ReferenceEquals(left, right))
			{
				return true;
			}
			if (left == null || right == null)
			{
				return false;
			}

			if (left.KeyEquals(right))
			{
				return true;
			}
			if (left is IKeyEquatable lke)
			{
				return lke.KeyEquals(right);
			}
			if (right is IKeyEquatable rke)
			{
				return rke.KeyEquals(left);
			}

			return _fallbackComparer.Equals(left, right);
		}

		/// <inheritdoc />
		public int GetHashCode(T obj) => obj?.GetKeyHashCode() ?? 0;

		bool IEqualityComparer.Equals(object x, object y) => EnsureNonGenericComparer().Equals(x, y);

		int IEqualityComparer.GetHashCode(object obj) => EnsureNonGenericComparer().GetHashCode(obj);

		private IEqualityComparer EnsureNonGenericComparer()
		{
			return _innerNonGeneric = _innerNonGeneric
				?? (_innerNonGeneric = new KeyEqualityComparer(_fallbackComparer.ToEqualityComparer()));
		}
	}
}
