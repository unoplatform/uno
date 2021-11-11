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
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Uno
{
	internal static partial class Transactional
	{
		/// <summary>
		/// Transactionally updates the <paramref name="original"/> reference using the provided <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="T">The type of the reference to update</typeparam>
		/// <param name="original">A ref variable to the original value</param>
		/// <param name="selector">A selector method that creates an updated version of the original value</param>
		/// <returns>Successful updated version</returns>
		public static T Update<T>(ref T original, Func<T, T> selector)
			 where T : class?
		{
			do
			{
				// Get the original value
				var capture = original;

				// Apply the transformation using the selector
				var updated = selector(capture);
				if (object.ReferenceEquals(capture, updated))
				{
					return capture;
				}

				// Compare and exchange the original with the updated value, if the original value has not changed.
				if (Interlocked.CompareExchange(ref original, updated, capture) == capture)
				{
					return updated;
				}
			}
			while (true);
		}

		/// <summary>
		/// Transactionally updates the <paramref name="original"/> reference using the provided <paramref name="selector"/>.
		/// </summary>
		/// <param name="original">A ref variable to the original value</param>
		/// <param name="selector">A selector method that creates an updated version of the original value</param>
		/// <returns>Successful updated version</returns>
		public static object Update(ref object original, Func<object, object> selector)
		{
			do
			{
				// Get the original value
				var capture = original;

				// Apply the transformation using the selector
				var updated = selector(capture);
				if (object.ReferenceEquals(capture, updated))
				{
					return capture;
				}

				// Compare and exchange the original with the updated value, if the original value has not changed.
				if (Interlocked.CompareExchange(ref original, updated, capture) == capture)
				{
					return updated;
				}
			}
			while (true);
		}

		/// <summary>
		/// Transactionally updates the <paramref name="original"/> reference using the provided <paramref name="selector"/>.
		/// </summary>
		/// <remarks>
		/// This version let you pass a parameter to prevent creation of a display class for capturing data in your lambda
		/// </remarks>
		/// <typeparam name="T">The type of the reference to update</typeparam>
		/// <param name="original">A ref variable to the <paramref name="original"/> value</param>
		/// <param name="selector">A selector method that creates an updated version of the original value</param>
		/// <returns>Successful updated version</returns>
		public static T Update<T, TParam>(ref T original, TParam param, Func<T, TParam, T> selector)
			 where T : class?
		{
			do
			{
				// Get the original value
				var capture = original;

				// Apply the transformation using the selector
				var updated = selector(capture, param);
				if (object.ReferenceEquals(capture, updated))
				{
					return capture;
				}

				// Compare and exchange the original with the updated value, if the original value has not changed.
				if (Interlocked.CompareExchange(ref original, updated, capture) == capture)
				{
					return updated;
				}
			}
			while (true);
		}

		/// <summary>
		/// Transactionally updates the <paramref name="original"/> reference using the provided <paramref name="selector"/>.
		/// </summary>
		/// <remarks>
		/// This version let you pass a parameters to prevent creation of a display class for capturing data in your lambda
		/// </remarks>
		/// <typeparam name="T">The type of the reference to update</typeparam>
		/// <param name="original">A ref variable to the original value</param>
		/// <param name="selector">A selector method that creates an updated version of the original value</param>
		/// <returns>Successful updated version</returns>
		public static T Update<T, TParam1, TParam2>(ref T original, TParam1 param1, TParam2 param2, Func<T, TParam1, TParam2, T> selector)
			 where T : class?
		{
			do
			{
				// Get the original value
				var capture = original;

				// Apply the transformation using the selector
				var updated = selector(capture, param1, param2);
				if (object.ReferenceEquals(capture, updated))
				{
					return capture;
				}

				// Compare and exchange the original with the updated value, if the original value has not changed.
				if (Interlocked.CompareExchange(ref original, updated, capture) == capture)
				{
					return updated;
				}
			}
			while (true);
		}

		/// <summary>
		/// Transactionally updates the <paramref name="original"/> reference using the provided
		/// <paramref name="selector"/>, and returns a selected value from the <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="TSource">The type of the reference to update</typeparam>
		/// <typeparam name="TResult">The inner value from the updated TSource returned by the selector</typeparam>
		/// <param name="original">The original value reference</param>
		/// <param name="selector">The selector returning a Tuple with updated value as Item1 and Update result as Item2.</param>
		/// <returns>The inner value returned by the selector.</returns>
		public static TResult Update<TSource, TResult>(ref TSource original, Func<TSource, System.Tuple<TSource, TResult>> selector)
			 where TSource : class?
		{
			do
			{
				var capture = original;

				var updated = selector(capture);
				if (object.ReferenceEquals(capture, updated.Item1))
				{
					return updated.Item2;
				}

				if (Interlocked.CompareExchange(ref original, updated.Item1, capture) == capture)
				{
					return updated.Item2;
				}
			}
			while (true);
		}

		/// <summary>
		/// Transactionally updates the <paramref name="original"/> reference using the provided
		/// <paramref name="selector"/>, and returns a selected value from the <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="TSource">The type of the reference to update</typeparam>
		/// <typeparam name="TResult">The inner value from the updated TSource returned by the selector</typeparam>
		/// <param name="original">The original value reference</param>
		/// <param name="selector">The selector returning a Tuple with updated value as Item1 and Update result as Item2.</param>
		/// <returns>The inner value returned by the selector.</returns>
		public static TResult Update<TSource, TParam, TResult>(ref TSource original, TParam param, Func<TSource, TParam, System.Tuple<TSource, TResult>> selector)
			 where TSource : class?
		{
			do
			{
				var capture = original;

				var updated = selector(capture, param);
				if (object.ReferenceEquals(capture, updated.Item1))
				{
					return updated.Item2;
				}

				if (Interlocked.CompareExchange(ref original, updated.Item1, capture) == capture)
				{
					return updated.Item2;
				}
			}
			while (true);
		}
	}
}
