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
using System.Collections;
using System.Collections.Generic;

namespace Uno
{
	/// <summary>
	/// This is an implementation of <see cref="IEqualityComparer{T}"/> which compare the
	/// <see cref="Option{T}"/> type and uses an optional inner comparer for the value.
	/// </summary>
	public sealed class OptionEqualityComparer<T> : IEqualityComparer<Option<T>>
	{
		private readonly IEqualityComparer<T> _innerComparer;

		public OptionEqualityComparer(IEqualityComparer<T> innerComparer = null)
		{
			_innerComparer = innerComparer ?? EqualityComparer<T>.Default;
		}

		/// <inheritdoc />
		public bool Equals(Option<T> x, Option<T> y)
		{
			// treat any "null" as Option.None (that's not the job of this comparer to crash on this)
			x = x ?? Option.None<T>();
			y = y ?? Option.None<T>();

			if (x.Type != y.Type)
			{
				// One is None and other is Some
				// So definitely not equal!
				return false;
			}

			return x.Type == OptionType.None // both "None"
				|| _innerComparer.Equals((x as Some<T>).Value, (y as Some<T>).Value); // compare the 2 "Some"
		}

		/// <inheritdoc />
		public int GetHashCode(Option<T> obj)
		{
			return (obj as Some<T>)?.Value?.GetHashCode() ?? 0;
		}
	}
}
