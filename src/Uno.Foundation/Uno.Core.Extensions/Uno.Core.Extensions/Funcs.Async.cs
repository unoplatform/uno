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
	static partial class Funcs
	{
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
		}       /// <summary>
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
	}
}
