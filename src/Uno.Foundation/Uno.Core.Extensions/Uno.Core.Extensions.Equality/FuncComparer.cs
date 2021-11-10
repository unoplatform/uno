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

namespace Uno.Comparison
{
	/// <summary>
	/// An <see cref="IComparer{T}"/> configurable using Funcs
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FuncComparer<T> : IComparer<T>
	{
		private readonly Func<T?, T?, int> _compare;

		/// <summary>
		/// Creates an <see cref="IComparer{T}"/> using a value selector.
		/// </summary>
		public static IComparer<T> Create<TValue>(Func<T?, TValue> valueSelector)
			where TValue : IComparable
		{
			return new FuncComparer<T>((v1, v2) => valueSelector(v1).CompareTo(valueSelector(v2)));
		}

		/// <summary>
		/// Ctor
		/// </summary>
		public FuncComparer(Func<T?, T?, int> compare)
		{
			_compare = compare;
		}

		/// <inheritdoc/>
		public int Compare(T? x, T? y)
		{
			return _compare(x, y);
		}
	}
}
