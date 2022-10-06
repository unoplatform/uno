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
#if !SILVERLIGHT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions
{
	internal static partial class FuncExtensions
	{
		private static ConditionalWeakTable<object, ConcurrentDictionary<object, object>> _weakResults =
			new ConditionalWeakTable<object, ConcurrentDictionary<object, object>>();

		/// <summary>
		/// Invokes the specified selector on the specified source. The result of the computation will be memoized for the specified source.
		/// </summary>
		/// <remarks>
		/// This method uses the selector instance to associate the results to the source instance. 
		/// Make sure to use a stable instance, e.g. not a lambda with closures over local or instance variables.
		/// </remarks>
		/// <typeparam name="TSource">The type of the parameter</typeparam>
		/// <typeparam name="TResult">The type of the computation result</typeparam>
		/// <param name="source">The source instance.</param>
		/// <param name="selector">The method group to use.</param>
		/// <returns>The memoized result</returns>
		public static TResult ApplyMemoized<TSource, TResult>(this TSource source, Func<TSource, TResult> selector)
		{
			return selector.AsWeakMemoized(source)();
		}

		/// <summary>
		/// Invokes the specified selector on the specified source, with the specified parameter. The result of the computation will be memoized for the specified source and parameter.
		/// </summary>
		/// <remarks>
		/// This method uses the selector instance to associate the results to the source instance. 
		/// Make sure to use a stable instance, e.g. not a lambda with closures over local or instance variables.
		/// </remarks>
		/// <typeparam name="TSource">The type of the parameter</typeparam>
		/// <typeparam name="TResult">The type of the computation result</typeparam>
		/// <typeparam name="TParam">A parameter to pass the selector call</typeparam>
		/// <param name="source">The source instance.</param>
		/// <param name="selector">The method group to use.</param>
		/// <returns>The memoized result</returns>
		public static TResult ApplyMemoized<TSource, TResult, TParam>(this TSource source, Func<TSource, TParam, TResult> selector, TParam param)
		{
			return selector.AsWeakMemoized(source)(param);
		}

		/// <summary>
		/// Invokes the specified selector on the specified source, with the specified parameters. The result of the computation will be memoized for the specified source and parameters.
		/// </summary>
		/// <remarks>
		/// This method uses the selector instance to associate the results to the source instance. 
		/// Make sure to use a stable instance, e.g. not a lambda with closures over local or instance variables.
		/// </remarks>
		/// <typeparam name="TSource">The type of the parameter</typeparam>
		/// <typeparam name="TResult">The type of the computation result</typeparam>
		/// <typeparam name="TParam1">A parameter to pass the selector call</typeparam>
		/// <typeparam name="TParam2">A parameter to pass the selector call</typeparam>
		/// <param name="source">The source instance.</param>
		/// <param name="selector">The method group to use.</param>
		/// <returns>The memoized result</returns>
		public static TResult ApplyMemoized<TSource, TResult, TParam1, TParam2>(this TSource source, Func<TSource, TParam1, TParam2, TResult> selector, TParam1 param1, TParam2 param2)
		{
			return selector.AsWeakMemoized(source)(param1, param2);
		}

		/// <summary>
		/// Creates a func that invokes the specified selector on the specified source. The result of the computation will be memoized for the specified source.
		/// </summary>
		/// <typeparam name="TSource">The type of the parameter</typeparam>
		/// <typeparam name="TResult">The type of the computation result</typeparam>
		/// <param name="source">The source instance.</param>
		/// <param name="selector">The method group to use.</param>
		/// <returns>A function that will return the memoized result</returns>
		public static Func<TResult> AsWeakMemoized<TSource, TResult>(this Func<TSource, TResult> selector, TSource source)
		{
			return () =>
			{
				var values = _weakResults.GetOrCreateValue(source);

				object res;

				bool cached = values.TryGetValue(selector, out res);

				if (!cached)
				{
					res = selector(source);

					values[selector] = res;
				}

				return (TResult)res;
			};
		}

		/// <summary>
		/// Creates a func that invokes the specified selector on the specified source, with the specified parameter. The result of the computation will be memoized for the specified source and parameter.
		/// </summary>
		/// <typeparam name="TSource">The type of the parameter</typeparam>
		/// <typeparam name="TResult">The type of the computation result</typeparam>
		/// <typeparam name="TParam">A parameter to pass the selector call</typeparam>
		/// <param name="source">The source instance.</param>
		/// <param name="selector">The method group to use.</param>
		/// <returns>A function that will return the memoized result</returns>
		public static Func<TParam, TResult> AsWeakMemoized<TSource, TResult, TParam>(this Func<TSource, TParam, TResult> selector, TSource source)
		{
			return param =>
			{
				var values = _weakResults.GetOrCreateValue(source);

				object res;

				var key = new { selector, param };

				bool cached = values.TryGetValue(key, out res);

				if (!cached)
				{
					res = selector(source, param);

					values[key] = res;
				}

				return (TResult)res;
			};
		}

		/// <summary>
		/// Creates a func that invokes the specified selector on the specified source, with the specified parameters. The result of the computation will be memoized for the specified source and parameters.
		/// </summary>
		/// <typeparam name="TSource">The type of the parameter</typeparam>
		/// <typeparam name="TResult">The type of the computation result</typeparam>
		/// <typeparam name="TParam1">A parameter to pass the selector call</typeparam>
		/// <typeparam name="TParam2">A parameter to pass the selector call</typeparam>
		/// <param name="selector">The method group to use.</param>
		/// <param name="source">The source instance.</param>
		/// <returns>A function that will return the memoized result</returns>
		public static Func<TParam1, TParam2, TResult> AsWeakMemoized<TSource, TResult, TParam1, TParam2>(this Func<TSource, TParam1, TParam2, TResult> selector, TSource source)
		{
			return (param1, param2) =>
			{
				var values = _weakResults.GetOrCreateValue(source);

				object res;

				var key = new { selector, param1, param2 };

				bool cached = values.TryGetValue(key, out res);

				if (!cached)
				{
					res = selector(source, param1, param2);

					values[key] = res;
				}

				return (TResult)res;
			};
		}
	}
}
#endif
