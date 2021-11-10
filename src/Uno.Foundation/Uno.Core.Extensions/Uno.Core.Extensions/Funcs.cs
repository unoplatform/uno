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
using Uno.Extensions;
using System.Threading.Tasks;
using System.Threading;

namespace Uno
{
	public static class Funcs
	{
		/// <summary>
		/// Creates a function, to allow for type inference from the returned value.
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static Func<TResult> Create<TResult>(Func<TResult> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a function, to allow for type inference from the returned value.
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static Func<TParam, TResult> Create<TParam, TResult>(Func<TParam, TResult> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a function, to allow for type inference from the returned value.
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static Func<TParam1, TParam2, TResult> Create<TParam1, TParam2, TResult>(Func<TParam1, TParam2, TResult> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a function, to allow for type inference from the returned value.
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static Func<TParam1, TParam2, TParam3, TResult> Create<TParam1, TParam2, TParam3, TResult>(Func<TParam1, TParam2, TParam3, TResult> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a parameterless cancellable async function.
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static FuncAsync<TResult> CreateAsync<TResult>(FuncAsync<TResult> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a parameterized cancellable async function.
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static FuncAsync<TParam, TResult> CreateAsync<TParam, TResult>(FuncAsync<TParam, TResult> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a parameterized cancellable async function.
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static FuncAsync<TParam1, TParam2, TResult> CreateAsync<TParam1, TParam2, TResult>(FuncAsync<TParam1, TParam2, TResult> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a parameterized cancellable async function.
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static FuncAsync<TParam1, TParam2, TParam3, TResult> CreateAsync<TParam1, TParam2, TParam3, TResult>(FuncAsync<TParam1, TParam2, TParam3, TResult> function)
		{
			return function;
		}


		/// <summary>
		/// Creates a parameterless cancellable async function.
		/// </summary>
		/// <typeparam name="TResult">The return value</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A func of the source</returns>
		[Legacy("NV0115")]
		public static Func<CancellationToken, Task<TResult>> Create<TResult>(Func<CancellationToken, Task<TResult>> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a parameterized cancellable async function.
		/// </summary>
		/// <typeparam name="TResult">The return value</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A func of the source</returns>
		[Legacy("NV0115")]
		public static Func<CancellationToken, TParam, Task<TResult>> Create<TParam, TResult>(Func<CancellationToken, TParam, Task<TResult>> function)
		{
			return function;
		}

		/// <summary>
		/// Creates a parameterless memoized function. <seealso cref="Uno.Extensions.FuncMemoizeExtensions"/>
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static Func<TResult> CreateMemoized<TResult>(Func<TResult> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterless thread-safe memoized function. <seealso cref="Uno.Extensions.FuncMemoizeExtensions"/>
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static Func<TResult> CreateLockedMemoized<TResult>(Func<TResult> function)
		{
			return function.AsLockedMemoized();
		}

		/// <summary>
		/// Creates a parameterized memoized function.
		/// </summary>
		/// <typeparam name="TParam">The parameter</typeparam>
		/// <typeparam name="TResult">The return value</typeparam>
		/// <param name="function">The function to memoize</param>
		/// <returns>The memoized function</returns>
		public static Func<TParam, TResult> CreateMemoized<TParam, TResult>(Func<TParam, TResult> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterized memoized function.
		/// </summary>
		/// <typeparam name="TParam1">The first parameter</typeparam>
		/// <typeparam name="TParam2">The second parameter</typeparam>
		/// <typeparam name="TResult">The return value</typeparam>
		/// <param name="function">The function to memoize</param>
		/// <returns>The memoized function</returns>
		public static Func<TParam1, TParam2, TResult> CreateMemoized<TParam1, TParam2, TResult>(Func<TParam1, TParam2, TResult> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterized memoized function.
		/// </summary>
		/// <typeparam name="TParam1">The first parameter</typeparam>
		/// <typeparam name="TParam2">The second parameter</typeparam>
		/// <typeparam name="TParam3">The third parameter</typeparam>
		/// <typeparam name="TResult">The return value</typeparam>
		/// <param name="function">The function to memoize</param>
		/// <returns>The memoized function</returns>
		public static Func<TParam1, TParam2, TParam3, TResult> CreateMemoized<TParam1, TParam2, TParam3, TResult>(Func<TParam1, TParam2, TParam3, TResult> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterized memoized function.
		/// </summary>
		/// <typeparam name="TParam1">The first parameter</typeparam>
		/// <typeparam name="TParam2">The second parameter</typeparam>
		/// <typeparam name="TParam3">The third parameter</typeparam>
		/// <typeparam name="TParam4">The fourth parameter</typeparam>
		/// <typeparam name="TResult">The return value</typeparam>
		/// <param name="function">The function to memoize</param>
		/// <returns>The memoized function</returns>
		public static Func<TParam1, TParam2, TParam3, TParam4, TResult> CreateMemoized<TParam1, TParam2, TParam3, TParam4, TResult>(Func<TParam1, TParam2, TParam3, TParam4, TResult> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterized memoized function.
		/// </summary>
		/// <typeparam name="TParam1">The first parameter</typeparam>
		/// <typeparam name="TParam2">The second parameter</typeparam>
		/// <typeparam name="TParam3">The third parameter</typeparam>
		/// <typeparam name="TParam4">The fourth parameter</typeparam>
		/// <typeparam name="TParam5">The fifth parameter</typeparam>
		/// <typeparam name="TResult">The return value</typeparam>
		/// <param name="function">The function to memoize</param>
		/// <returns>The memoized function</returns>
		public static Func<TParam1, TParam2, TParam3, TParam4, TParam5, TResult> CreateMemoized<TParam1, TParam2, TParam3, TParam4, TParam5, TResult>(Func<TParam1, TParam2, TParam3, TParam4, TParam5, TResult> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterless memoized task providing function. <seealso cref="Uno.Extensions.FuncMemoizeExtensions"/>
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static FuncAsync<TResult> CreateAsyncMemoized<TResult>(FuncAsync<TResult> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterless memoized task providing function. <seealso cref="Uno.Extensions.FuncMemoizeExtensions"/>
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		public static FuncAsync<TParam, TResult> CreateAsyncMemoized<TParam, TResult>(FuncAsync<TParam, TResult> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterless memoized task providing function. <seealso cref="Uno.Extensions.FuncMemoizeExtensions"/>
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		[Legacy("NV0115")]
		public static Func<CancellationToken, Task<TResult>> CreateMemoized<TResult>(Func<CancellationToken, Task<TResult>> function)
		{
			return function.AsMemoized();
		}

		/// <summary>
		/// Creates a parameterless memoized task providing function. <seealso cref="Uno.Extensions.FuncMemoizeExtensions"/>
		/// </summary>
		/// <typeparam name="TResult">The returned type</typeparam>
		/// <param name="function">The source function</param>
		/// <returns>A function</returns>
		[Legacy("NV0115")]
		public static Func<CancellationToken, TParam, Task<TResult>> CreateMemoized<TParam, TResult>(Func<CancellationToken, TParam, Task<TResult>> function)
		{
			return function.AsMemoized();
		}
	}

	public static class NullableFuncs<T>
		where T : struct
	{
		public static readonly Func<T?, T> FromNullable = item => item.GetValueOrDefault();
		public static readonly Func<T, T?> ToNullable = item => item;
	}
}