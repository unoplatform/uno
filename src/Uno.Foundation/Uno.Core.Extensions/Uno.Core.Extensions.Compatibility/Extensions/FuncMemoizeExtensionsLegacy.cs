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
using System.Threading;
using System.Threading.Tasks;
using Uno.Collections;

namespace Uno.Extensions
{
	internal static class FuncMemoizeExtensionsLegacy
	{
		/// <summary>
		/// Creates a memoizer with one parameter for the the specified task provider. The task provider is guaranteed to be executed only once per parameter instance.
		/// </summary>
		/// <typeparam name="TResult">The return value type</typeparam>
		/// <typeparam name="TParam"></typeparam>
		/// <param name="func">A function that will call the create the task.</param>
		/// <returns>A function that will return a task </returns>
		[Legacy("NV0115")]
		public static Func<CancellationToken, TParam, Task<TResult>> AsMemoized<TParam, TResult>(this Func<CancellationToken, TParam, Task<TResult>> func)
		{
			var values = new SynchronizedDictionary<TParam, Func<CancellationToken, Task<TResult>>>();
			// It's safe to use default(TParam) as this won't get called anyway if TParam is a vlue type.
			var nullValue = Funcs.CreateAsyncMemoized(ct => func(ct, default(TParam)));

			return (ct, param) =>
			{
				if (param == null)
				{
					return nullValue(ct);
				}
				else
				{
					var memoizedFunc = values.FindOrCreate(param, () => Funcs.CreateMemoized(c => func(c, param)));

					return memoizedFunc(ct);
				}
			};
		}


		/// <summary>
		/// Memoizer with one parameter (kept as weak reference), used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="TParam">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TParam, TResult> AsWeakLockedMemoized<TParam, TResult>(this Func<TParam, TResult> func)
			where TParam : class
		{
			var values = new WeakAttachedDictionary<TParam, string>();

			return v => values.GetValue(v, "value", () => func(v));
		}
	}
}