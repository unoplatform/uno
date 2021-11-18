#nullable enable

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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.Equality
{
	/// <summary>
	/// An EqualityComparre which compare sequence of items
	/// </summary>
	internal class CollectionEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
	{
		private readonly IEqualityComparer<T>? _comparer;

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="comparer">Comparer to use to compare each item</param>
		public CollectionEqualityComparer(IEqualityComparer<T>? comparer = null)
		{
			_comparer = comparer;
		}

		/// <inheritdoc/>
		public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
		{
			return x == y || (_comparer == null
				? Safe(x).SequenceEqual(Safe(y))
				: Safe(x).SequenceEqual(Safe(y), _comparer));
		}

		/// <inheritdoc/>
		public int GetHashCode(IEnumerable<T> source)
		{
			return Safe(source)
				.Aggregate(0, (hash, item) => hash ^ (item?.GetHashCode() ?? 0));
		}

		private IEnumerable<T> Safe(IEnumerable<T>? source)
			=> source ?? Enumerable.Empty<T>();
	}
}
