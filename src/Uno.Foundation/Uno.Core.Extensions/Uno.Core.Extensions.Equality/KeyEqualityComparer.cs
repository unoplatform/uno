// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
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
		/// <summary>
		/// Gets the default instance of the <see cref="KeyEqualityComparer"/>.
		/// </summary>
		public static IEqualityComparer Default { get; } = new KeyEqualityComparer();

		private KeyEqualityComparer()
		{
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
			return EqualityComparer<object>.Default.Equals(x, y);
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
}
