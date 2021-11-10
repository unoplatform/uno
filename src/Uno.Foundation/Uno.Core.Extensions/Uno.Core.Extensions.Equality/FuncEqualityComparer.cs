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
using System.ComponentModel;

namespace Uno.Equality
{
	/// <summary>
	/// Allows <see cref="FuncEqualityComparer{T}"/> methods to be called without specifying the compared 
	/// type explicitely, which simplifies the syntax and allows for equality comparisons on anonymous types
	/// </summary>
	public class FuncEqualityComparer
	{
		/// <summary>
		/// Creates an <see cref="IEqualityComparer{T}"/> which determine objects equality and hash codes based on a value obtained using a selector.
		/// </summary>
		/// <param name="valueSelector">Selector to get the value used for comparison of objects.</param>
		/// <typeparam name="TValue">Type of the value to compare.</typeparam>
		public static IEqualityComparer<T> Create<T, TValue>(Func<T, TValue> valueSelector)
		{
			return FuncEqualityComparer<T>.Create(valueSelector);
		}

		/// <summary>
		/// Creates an <see cref="IEqualityComparer{T}"/> which determine objects equality and hash codes based on a value obtained using a selector.
		/// </summary>
		/// <param name="valueSelector">Selector to get the value used for comparison of objects.</param>
		/// <param name="valueEqualityComparer">Equlity comparre to use to comprer the selectd values.</param>
		/// <typeparam name="TValue">Type of the value to compare.</typeparam>
		public static IEqualityComparer<T> Create<T, TValue>(Func<T, TValue> valueSelector, IEqualityComparer<TValue> valueEqualityComparer)
		{
			return FuncEqualityComparer<T>.Create(valueSelector, valueEqualityComparer);
		}
	}

	/// <summary>
	/// An EqualityComparer configurable using Funcs.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	public class FuncEqualityComparer<T> : IEqualityComparer<T>
	{
		private readonly Func<T?, T?, bool> _equals;
		private readonly Func<T, int> _getHashCode;

		/// <summary>
		/// DO NOT USE
		/// </summary>
		/// <remarks>This constructor does not provide a way to correctly implements the <see cref="IEqualityComparer{T}.GetHashCode(T)"/>. Prefer use overload with the GetHashCode</remarks>
		/// <param name="predicate"></param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public FuncEqualityComparer(Func<T?, T?, bool> predicate)
			: this(predicate, _ => 0)
		// The expected comportement when using an IEqualityComparer is to first call the GetHasCode() which is really fast, 
		// and ONLY if there is a colision call the Equals() which is slower.
		// Here we don't any solution to determine the right hash code, so we wend back always the same hash code
		// to ensure the call to Equals.
		{
		}

		/// <summary>
		/// .ctor
		/// </summary>
		/// <param name="equals">Predicate to invoke to determine equality between objects.</param>
		/// <param name="getHashCode">Predicate to invoke to get the hash code of an object.</param>
		public FuncEqualityComparer(Func<T?, T?, bool> equals, Func<T, int> getHashCode)
		{
			_equals = equals;
			_getHashCode = getHashCode;
		}

		#region IEqualityComparer<T> Members

		public bool Equals(T? x, T? y)
		{
			return _equals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return _getHashCode(obj);
		}

		#endregion

		/// <summary>
		/// Creates an <see cref="IEqualityComparer{T}"/> which determine objects equality and hash codes based on a value obtained using a selector.
		/// </summary>
		/// <param name="valueSelector">Selector to get the value used for comparison of objects.</param>
		/// <typeparam name="TValue">Type of the value to compare.</typeparam>
		/// <returns></returns>
		public static IEqualityComparer<T> Create<TValue>(Func<T, TValue> valueSelector)
		{
			return new FuncEqualityComparer<T, TValue>(valueSelector);
		}

		/// <summary>
		/// Creates an <see cref="IEqualityComparer{T}"/> which determine objects equality and hash codes based on a value obtained using a selector.
		/// </summary>
		/// <param name="valueSelector">Selector to get the value used for comparison of objects.</param>
		/// <param name="valueEqualityComparer">Equlity comparre to use to comprer the selectd values.</param>
		/// <typeparam name="TValue">Type of the value to compare.</typeparam>
		/// <returns></returns>
		public static IEqualityComparer<T> Create<TValue>(Func<T, TValue> valueSelector, IEqualityComparer<TValue> valueEqualityComparer)
		{
			return new FuncEqualityComparer<T, TValue>(valueSelector);
		}
	}

	/// <summary>
	/// An <see cref="IEqualityComparer{T}"/> which determine objects equality and hash codes based on a value obtained using a selector.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	/// <typeparam name="TValue">The type of the value used for comparison.</typeparam>
	public class FuncEqualityComparer<T, TValue> : IEqualityComparer<T>
	{
		private readonly Func<T, TValue> _valueSelector;
		private readonly IEqualityComparer<TValue> _valueEqualityComparer;

		/// <summary>
		/// .ctor
		/// </summary>
		/// <param name="valueSelector">Selector to get the value used for comparison of objects.</param>
		public FuncEqualityComparer(Func<T, TValue> valueSelector, IEqualityComparer<TValue>? valueEqualityComparer = null)
		{
			_valueSelector = valueSelector;
			_valueEqualityComparer = valueEqualityComparer ?? EqualityComparer<TValue>.Default;
		}

		public bool Equals(T? x, T? y)
		{
			if (x == null)
			{
				return y == null;
			}

			if (y == null)
			{
				return false;
			}

			var xValue = _valueSelector(x);
			var yValue = _valueSelector(y);

			if (xValue == null)
			{
				return yValue == null;
			}

			return _valueEqualityComparer.Equals(xValue, yValue);
		}

		public int GetHashCode(T obj)
		{
			if (obj == null)
			{
				return 0;
			}

			var value = _valueSelector(obj);
			
			if (value == null)
			{
				return 0;
			}

			return _valueEqualityComparer.GetHashCode(value);
		}
	}
}
