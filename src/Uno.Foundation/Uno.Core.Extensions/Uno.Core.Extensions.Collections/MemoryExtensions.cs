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
using System.Threading.Tasks;

namespace Uno.Collections
{
    internal static class MemoryExtensions
    {
        /// <summary>
        /// Selects the values of a <see cref="List{T}"/> to a pre-allocated <see cref="Span{T}"/>.
        /// </summary>
        /// <typeparam name="TIn">The input type</typeparam>
        /// <typeparam name="TOut">The output type</typeparam>
        /// <param name="list">The <see cref="List{T}"/> to be projected</param>
        /// <param name="span">The output span</param>
        /// <param name="selector">A selector method that projects <typeparamref name="TIn"/> to <typeparamref name="TOut"/></param>
        public static void SelectToSpan<TIn, TOut>(
            this List<TIn> list,
            Span<TOut> span,
            Func<TIn, TOut> selector
        )
        {
            for (int i = 0; i < list.Count; i++)
            {
                span[i] = selector(list[i]);
            }
        }

        /// <summary>
        /// Selects the values of a <see cref="Span{T}"/> to a pre-allocated <see cref="Span{T}"/>.
        /// </summary>
        /// <typeparam name="TIn">The input type</typeparam>
        /// <typeparam name="TOut">The output type</typeparam>
        /// <param name="list">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="span">The output span</param>
        /// <param name="selector">A selector method that projects <typeparamref name="TIn"/> to <typeparamref name="TOut"/></param>
        public static void SelectToSpan<TIn, TOut>(
            this Span<TIn> list,
            Span<TOut> span,
            Func<TIn, TOut> selector
        )
        {
            for (int i = 0; i < list.Length; i++)
            {
                span[i] = selector(list[i]);
            }
        }

        /// <summary>
        /// Selects the values of a <see cref="Span{T}"/> to a pre-allocated <see cref="Span{T}"/>.
        /// </summary>
        /// <typeparam name="TIn">The input type</typeparam>
        /// <typeparam name="TOut">The output type</typeparam>
        /// <param name="list">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="span">The output span</param>
        /// <param name="selector">A selector method that projects <typeparamref name="TIn"/> to <typeparamref name="TOut"/> with the index of the value to project</param>
        public static void SelectToSpan<TIn, TOut>(
            this Span<TIn> list,
            Span<TOut> span,
            Func<TIn, int, TOut> selector
        )
        {
            for (int i = 0; i < list.Length; i++)
            {
                span[i] = selector(list[i], i);
            }
        }

        /// <summary>
        /// Selects the values of a <see cref="Array"/> to a pre-allocated <see cref="Span{T}"/>.
        /// </summary>
        /// <typeparam name="TIn">The input type</typeparam>
        /// <typeparam name="TOut">The output type</typeparam>
        /// <param name="list">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="span">The output span</param>
        /// <param name="selector">A selector method that projects <typeparamref name="TIn"/> to <typeparamref name="TOut"/></param>
        public static void SelectToSpan<TIn, TOut>(
            this TIn[] list,
            Span<TOut> span,
            Func<TIn, TOut> selector
        )
        {
            for (int i = 0; i < list.Length; i++)
            {
                span[i] = selector(list[i]);
            }
        }


        /// <summary>
        /// Filters the values of a <see cref="Span{T}"/> to a pre-allocated <see cref="Span{T}"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of values to filter</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="target">The output span</param>
        /// <param name="predicate">A predicate to filter the values</param>
        public static Span<TValue> WhereToSpan<TValue>(
            this Span<TValue> span,
            Span<TValue> target,
            Func<TValue, bool> predicate
        )
        {
            int values = 0;
            for (int i = 0; i < span.Length; i++)
            {
                var value = span[i];

                if (predicate(value))
                {
                    target[values++] = value;
                }
            }

            return target.Slice(0, values);
        }

        /// <summary>
        /// Selects the values of a <see cref="Span{T}"/> to a new <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="TIn">The input type</typeparam>
        /// <typeparam name="TOut">The output type</typeparam>
        /// <param name="list">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="span">The output span</param>
        /// <param name="selector">A selector method that projects <typeparamref name="TIn"/> to <typeparamref name="TOut"/></param>
        public static Memory<TOut> SelectToMemory<TIn, TOut>(
            this Span<TIn> list,
            Func<TIn, TOut> selector
        )
        {
            var output = new Memory<TOut>(new TOut[list.Length]);
            for (int i = 0; i < list.Length; i++)
            {
                output.Span[i] = selector(list[i]);
            }

            return output;
        }

        public static Memory<TOut> SelectToMemory<TIn, TOut>(
            this Span<TIn> list,
            Func<TIn, int, TOut> selector
        )
        {
            var output = new Memory<TOut>(new TOut[list.Length]);
            for (int i = 0; i < list.Length; i++)
            {
                output.Span[i] = selector(list[i], i);
            }

            return output;
        }


        /// <summary>
        /// Selects the values of a <see cref="IList{T}"/> to a new <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="TIn">The input type</typeparam>
        /// <typeparam name="TOut">The output type</typeparam>
        /// <param name="list">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="span">The output span</param>
        /// <param name="selector">A selector method that projects <typeparamref name="TIn"/> to <typeparamref name="TOut"/></param>
        public static Memory<TOut> SelectToMemory<TIn, TOut>(
            this IList<TIn> list,
            Func<TIn, TOut> selector
        )
        {
            var output = new Memory<TOut>(new TOut[list.Count]);
            for (int i = 0; i < list.Count; i++)
            {
                output.Span[i] = selector(list[i]);
            }

            return output;
        }

        /// <summary>
        /// Filters the values of a <see cref="Span{T}"/> to a new <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of values to filter</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="predicate">A predicate to filter the values</param>
        public static Memory<TValue> WhereToMemory<TValue>(
            this Span<TValue> span,
            Func<TValue, bool> predicate
        )
        {
            var output = new Memory<TValue>(new TValue[span.Length]);
            int valuesCount = 0;
            for (int i = 0; i < span.Length; i++)
            {
                var value = span[i];

                if (predicate(value))
                {
                    output.Span[valuesCount++] = value;
                }
            }

            return output.Slice(0, valuesCount);
        }

        /// <summary>
        /// Filters the values of a <see cref="Span{T}"/> to a new <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of values to filter</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="predicate">A predicate to filter the values with the index of the value to filter</param>
        public static Memory<TValue> WhereToMemory<TValue>(
            this Span<TValue> list,
            Func<TValue, int, bool> filter
        )
        {
            var output = new Memory<TValue>(new TValue[list.Length]);
            int values = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var value = list[i];

                if (filter(value, i))
                {
                    output.Span[values++] = value;
                }
            }

            return output.Slice(0, values);
        }

        /// <summary>
        /// Filters the values of a <see cref="Span{T}"/>, then projects the values to a new <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of values to filter</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to be projected</param>
        /// <param name="predicate">A predicate to filter the values with the index of the value to filter</param>
        /// <param name="selector">A selector method that projects <typeparamref name="TValue"/> to <typeparamref name="TResult"/></param>
        public static Memory<TResult> WhereToMemory<TValue, TResult>(
            this Span<TValue> list,
            Func<TValue, bool> filter,
            Func<TValue, TResult> selector
        )
        {
            var output = new Memory<TResult>(new TResult[list.Length]);
            int values = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var value = list[i];

                if (filter(value))
                {
                    output.Span[values++] = selector(value);
                }
            }

            return output.Slice(0, values);
        }

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

        /// <summary>
        /// Creates a slice for which the <paramref name="start"/> and <paramref name="range"/> are clamped to the size of <paramref name="span"/>.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="span">The span to slice</param>
        /// <param name="start">The starting index</param>
        /// <param name="range">The length of the slice</param>
        /// <returns>A slice of the source span</returns>
        public static Span<TValue> SliceClamped<TValue>(this Span<TValue> span, int start, int range)
            => span.Slice(
                start: Math.Min(span.Length - 1, start),
                length: Math.Max(0, Math.Min(range, span.Length - start))
            );
    }
}
