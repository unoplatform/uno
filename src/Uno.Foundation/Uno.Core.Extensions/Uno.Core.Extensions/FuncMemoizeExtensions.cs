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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if IS_UNO_FOUNDATION_RUNTIME_WEBASSEMBLY_PROJECT
namespace Uno.Foundation.Runtime.WebAssembly.Helpers
#else
namespace Uno.Extensions
#endif
{
	internal static partial class FuncMemoizeExtensions
	{
		/// <summary>
		/// Parameter less memoizer, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="T">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<T> AsMemoized<T>(this Func<T> func)
		{
			bool isSet = false;
			T value = default(T);

			return () =>
			{
				if (!isSet)
				{
					value = func();
					isSet = true;
				}

				return value;
			};
		}

		/// <summary>
		/// Memoizer with one parameter, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="TParam">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TParam, TResult> AsMemoized<TParam, TResult>(this Func<TParam, TResult> func)
		{
			Dictionary<TParam, TResult> values = new Dictionary<TParam, TResult>();
			// It's safe to use default(TParam) as this won't get called anyway if TParam is a value type.
			var nullValue = Funcs.CreateMemoized(() => func(default(TParam)));

			return (v) =>
			{
				TResult value;

				if (v == null)
				{
					value = nullValue();
				}
				else if (!values.TryGetValue(v, out value))
				{
					value = values[v] = func(v);
				}

				return value;
			};
		}

		/// <summary>
		/// Memoizer with two parameters, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="TParam1">The first parameter type to memoize</typeparam>
		/// <typeparam name="TParam2">The second parameter type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TParam1, TParam2, TResult> AsMemoized<TParam1, TParam2, TResult>(this Func<TParam1, TParam2, TResult> func)
		{
			Dictionary<CachedTuple<TParam1, TParam2>, TResult> values = new Dictionary<CachedTuple<TParam1, TParam2>, TResult>(CachedTuple<TParam1, TParam2>.Comparer);

			return (arg1, arg2) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2);
				TResult value;

				if (!values.TryGetValue(tuple, out value))
				{
					value = values[tuple] = func(arg1, arg2);
				}

				return value;
			};
		}

		/// <summary>
		/// Memoizer with three parameters, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="TParam1">The first parameter type to memoize</typeparam>
		/// <typeparam name="TParam2">The second parameter type to memoize</typeparam>
		/// <typeparam name="TParam3">The third parameter type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TParam1, TParam2, TParam3, TResult> AsMemoized<TParam1, TParam2, TParam3, TResult>(this Func<TParam1, TParam2, TParam3, TResult> func)
		{
			Dictionary<CachedTuple<TParam1, TParam2, TParam3>, TResult> values = new Dictionary<CachedTuple<TParam1, TParam2, TParam3>, TResult>(CachedTuple<TParam1, TParam2, TParam3>.Comparer);

			return (arg1, arg2, arg3) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2, arg3);
				TResult value;

				if (!values.TryGetValue(tuple, out value))
				{
					value = values[tuple] = func(arg1, arg2, arg3);
				}

				return value;
			};
		}

		/// <summary>
		/// Memoizer with four parameters, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="TParam1">The first parameter type to memoize</typeparam>
		/// <typeparam name="TParam2">The second parameter type to memoize</typeparam>
		/// <typeparam name="TParam3">The third parameter type to memoize</typeparam>
		/// <typeparam name="TParam4">The fourth parameter type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TParam1, TParam2, TParam3, TParam4, TResult> AsMemoized<TParam1, TParam2, TParam3, TParam4, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TResult> func)
		{
			Dictionary<CachedTuple<TParam1, TParam2, TParam3, TParam4>, TResult> values = new Dictionary<CachedTuple<TParam1, TParam2, TParam3, TParam4>, TResult>(CachedTuple<TParam1, TParam2, TParam3, TParam4>.Comparer);

			return (arg1, arg2, arg3, arg4) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2, arg3, arg4);
				TResult value;

				if (!values.TryGetValue(tuple, out value))
				{
					value = values[tuple] = func(arg1, arg2, arg3, arg4);
				}

				return value;
			};
		}

		/// <summary>
		/// Memoizer with five parameters, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="TParam1">The first parameter type to memoize</typeparam>
		/// <typeparam name="TParam2">The second parameter type to memoize</typeparam>
		/// <typeparam name="TParam3">The third parameter type to memoize</typeparam>
		/// <typeparam name="TParam4">The fourth parameter type to memoize</typeparam>
		/// <typeparam name="TParam5">The fifth parameter type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TParam1, TParam2, TParam3, TParam4, TParam5, TResult> AsMemoized<TParam1, TParam2, TParam3, TParam4, TParam5, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TResult> func)
		{
			Dictionary<System.Tuple<TParam1, TParam2, TParam3, TParam4, TParam5>, TResult> values = new Dictionary<System.Tuple<TParam1, TParam2, TParam3, TParam4, TParam5>, TResult>();

			return (arg1, arg2, arg3, arg4, arg5) =>
			{
				var tuple = System.Tuple.Create(arg1, arg2, arg3, arg4, arg5);
				TResult value;

				if (!values.TryGetValue(tuple, out value))
				{
					value = values[tuple] = func(arg1, arg2, arg3, arg4, arg5);
				}

				return value;
			};
		}

		/// <summary>
		/// Parameter less thread-safe memoizer, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="T">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<T> AsLockedMemoized<T>(this Func<T> func)
		{
			object gate = new object();
			bool isSet = false;
			T value = default(T);

			return () =>
			{
				if (!isSet)
				{
					lock (gate)
					{
						if (!isSet)
						{
							value = func();
							isSet = true;
						}
					}
				}

				return value;
			};
		}


		/// <summary>
		/// Memoizer with one parameter, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="T">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TKey, TResult> AsLockedMemoized<TKey, TResult>(this Func<TKey, TResult> func)
		{
			var values = new ConcurrentDictionary<TKey, TResult>();

			return (key) => values.GetOrAdd(key, func);
		}

		/// <summary>
		/// Memoizer with two parameters, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="T">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TArg1, TArg2, TResult> AsLockedMemoized<TArg1, TArg2, TResult>(this Func<TArg1, TArg2, TResult> func)
		{
			var values = new ConcurrentDictionary<CachedTuple<TArg1, TArg2>, TResult>(CachedTuple<TArg1, TArg2>.Comparer);

			return (arg1, arg2) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2);

				return values.GetOrAdd(
					tuple,

					// Use the parameter to avoid closure heap allocation
					k => func(k.Item1, k.Item2)
				);
			};
		}

		/// <summary>
		/// Memoizer with three parameters, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="T">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TArg1, TArg2, TArg3, TResult> AsLockedMemoized<TArg1, TArg2, TArg3, TResult>(this Func<TArg1, TArg2, TArg3, TResult> func)
		{
#if XAMARIN
			var values = new Dictionary<CachedTuple<TArg1, TArg2, TArg3>, TResult>(CachedTuple<TArg1, TArg2, TArg3>.Comparer);

			return (arg1, arg2, arg3) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2, arg3);

				lock (values)
				{
					return values.FindOrCreate(
						tuple,

						// Use the parameter to avoid closure heap allocation
						() => func(tuple.Item1, tuple.Item2, tuple.Item3)
					);
				}
			};
#else
			var values = new ConcurrentDictionary<CachedTuple<TArg1, TArg2, TArg3>, TResult>(CachedTuple<TArg1, TArg2, TArg3>.Comparer);

			return (arg1, arg2, arg3) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2, arg3);

				return values.GetOrAdd(
					tuple,

					// Use the parameter to avoid closure heap allocation
					k => func(k.Item1, k.Item2, k.Item3)
				);
			};
#endif
		}

		/// <summary>
		/// Memoizer with four parameters, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="T">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TArg1, TArg2, TArg3, TArg4, TResult> AsLockedMemoized<TArg1, TArg2, TArg3, TArg4, TResult>(this Func<TArg1, TArg2, TArg3, TArg4, TResult> func)
		{
#if XAMARIN
			var values = new Dictionary<CachedTuple<TArg1, TArg2, TArg3, TArg4>, TResult>(CachedTuple<TArg1, TArg2, TArg3, TArg4>.Comparer);

			return (arg1, arg2, arg3, arg4) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2, arg3, arg4);

				lock (values)
				{
					return values.FindOrCreate(
						tuple,

						// Use the parameter to avoid closure heap allocation
						() => func(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4)
					);
				}
			};
#else
			var values = new ConcurrentDictionary<CachedTuple<TArg1, TArg2, TArg3, TArg4>, TResult>(CachedTuple<TArg1, TArg2, TArg3, TArg4>.Comparer);

			return (arg1, arg2, arg3, arg4) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2, arg3, arg4);

				return values.GetOrAdd(
					tuple,

					// Use the parameter to avoid closure heap allocation
					k => func(k.Item1, k.Item2, k.Item3, k.Item4)
				);
			};
#endif
		}
	}
}
