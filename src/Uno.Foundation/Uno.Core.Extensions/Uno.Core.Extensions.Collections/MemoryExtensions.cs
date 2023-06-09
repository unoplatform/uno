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

namespace Uno.Collections
{
	internal static class MemoryExtensions
	{
		/// <summary>
		/// Provides a Count of values given a predicate
		/// </summary>
		/// <typeparam name="T">The type of the values</typeparam>
		/// <param name="span">The span to count the values in</param>
		/// <param name="predicate">The predicate to filter the values</param>
		/// <returns>The count of values</returns>
		public static int Count<T>(this Span<T> span, Func<T, bool> predicate)
		{
			int result = 0;
			foreach (var value in span)
			{
				if (predicate(value))
				{
					result++;
				}
			}

			return result;
		}

		/// <summary>
		/// Determines if the provided span contains values given a predicate
		/// </summary>
		/// <typeparam name="T">The type of the values</typeparam>
		/// <param name="span">The span to analyze</param>
		/// <param name="predicate">The predicate to filter the values</param>
		/// <returns><c>true</c> if the predicate returned true, otherwise <c>false</c></returns>
		public static bool Any<T>(this Span<T> span, Func<T, bool> predicate)
		{
			foreach (var value in span)
			{
				if (predicate(value))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Creates a new <see cref="Dictionary{TKey, TValue}"/> from the values of a span.
		/// </summary>
		/// <param name="span">The input span</param>
		/// <param name="keySelector">The selector to create a key of the dictionary</param>
		/// <param name="valueSelector">The selector to create the value with the corresponding key</param>
		/// <returns>A new <see cref="Dictionary{TKey, TValue}"/></returns>
		public static Dictionary<TKey, TValue> ToDictionary<TIn, TKey, TValue>(this Span<TIn> span, Func<TIn, TKey> keySelector, Func<TIn, TValue> valueSelector)
		{
			var result = new Dictionary<TKey, TValue>(span.Length);
			foreach (var item in span)
			{
				result.Add(keySelector(item), valueSelector(item));
			}
			return result;
		}

		/// <summary>
		/// Computes the sum of all the values of a <see cref="Span{T}"/> where <c>T</c> is a double
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public static double Sum(this Span<double> span)
		{
			double result = 0;

			foreach (var value in span)
			{
				result += value;
			}

			return result;
		}

		/// <summary>
		/// Computes the sum of all the values of a <see cref="Span{T}"/>, using a predicate to get each value.
		/// </summary>
		/// <typeparam name="TIn"></typeparam>
		/// <param name="span">The span to use</param>
		/// <param name="selector">A selector to get the value</param>
		/// <returns>The sum of all the projected values</returns>
		public static double Sum<TIn>(this Span<TIn> span, Func<TIn, double> selector)
		{
			double result = 0;

			foreach (var value in span)
			{
				result += selector(value);
			}

			return result;
		}
	}
}
