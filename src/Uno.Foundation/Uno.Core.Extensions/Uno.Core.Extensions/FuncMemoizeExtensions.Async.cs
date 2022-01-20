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
using System.Threading;
using System.Threading.Tasks;

#if !HAS_NO_CONCURRENT_DICT
using System.Collections.Concurrent;
#endif

namespace Uno.Extensions
{
	internal static partial class FuncMemoizeExtensions
	{
		/// <summary>
		/// Creates a parameter-less memoizer for the the specified task provider. The task provider is guaranteed to be executed only once.
		/// </summary>
		/// <typeparam name="T">The return value type</typeparam>
		/// <param name="func">A function that will call the create the task.</param>
		/// <returns>A function that will return a task </returns>
		[Legacy("NV0115")]
		public static Func<CancellationToken, Task<T>> AsMemoized<T>(this Func<CancellationToken, Task<T>> func)
		{
			Task<T> value = null;
			object gate = new object();

			return ct =>
			{
				if (value == null)
				{
					lock (gate)
					{
						if (value == null)
						{
							value = Funcs.Create(async ct2 =>
							{
								try
								{
									return await func(ct2);
								}
								catch (OperationCanceledException)
								{
									lock (gate)
									{
										value = null;
									}

									throw;
								}
							})(ct);
						}
					}
				}

				return value;
			};
		}

		/// <summary>
		/// Creates a parameter-less memoizer for the the specified task provider. The task provider is guaranteed to be executed only once.
		/// </summary>
		/// <typeparam name="T">The return value type</typeparam>
		/// <param name="func">A function that will call the create the task.</param>
		/// <returns>A function that will return a task </returns>
		public static FuncAsync<T> AsMemoized<T>(this FuncAsync<T> func)
		{
			Task<T> value = null;
			object gate = new object();

			return ct =>
			{
				if (value == null)
				{
					lock (gate)
					{
						if (value == null)
						{
							value = Funcs.Create(async ct2 =>
							{
								try
								{
									return await func(ct2);
								}
								catch (OperationCanceledException)
								{
									lock (gate)
									{
										value = null;
									}

									throw;
								}
							})(ct);
						}
					}
				}

				return value;
			};
		}

		/// <summary>
		/// Creates a memoizer with one parameter for the the specified task provider. The task provider is guaranteed to be executed only once per parameter instance.
		/// </summary>
		/// <typeparam name="TResult">The return value type</typeparam>
		/// <typeparam name="TParam"></typeparam>
		/// <param name="func">A function that will call the create the task.</param>
		/// <returns>A function that will return a task </returns>
		public static FuncAsync<TParam, TResult> AsMemoized<TParam, TResult>(this FuncAsync<TParam, TResult> func)
		{
#if HAS_NO_CONCURRENT_DICT
			var values = new SynchronizedDictionary<TParam, FuncAsync<TResult>>();
#else
			var values = new System.Collections.Concurrent.ConcurrentDictionary<TParam, FuncAsync<TResult>>();
#endif
			// It's safe to use default(TParam) as this won't get called anyway if TParam is a value type.
			var nullValue = Funcs.CreateAsyncMemoized(ct => func(ct, default(TParam)));

			return (ct, param) =>
			{
				if (param == null)
				{
					return nullValue(ct);
				}
				else
				{
					var memoizedFunc = values.GetOrAdd(param, p => Funcs.CreateAsyncMemoized(c => func(c, p)));

					return memoizedFunc(ct);
				}
			};
		}
	}
}
