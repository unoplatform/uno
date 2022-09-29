#nullable disable

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

namespace Uno.Extensions
{
	internal static class CurryExtensions
	{
		#region Func currying extenions

		public static Func<TResult> 
			Curry<T, TResult>(this Func<T, TResult> func, T value)
		{
			return () => func(value);
		}

		public static Func<T2, TResult> 
			CurryFirst<T1, T2, TResult>(this Func<T1, T2, TResult> func, T1 first)
		{
			return (T2 last) => func(first, last);
		}

		public static Func<T1, TResult> 
			CurryLast<T1, T2, TResult>(this Func<T1, T2, TResult> func, T2 last)
		{
			return (T1 first) => func(first, last);
		}

		public static Func<T2, T3, TResult>
			CurryFirst<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> func, T1 first)
		{
			return (T2 second, T3 last) => func(first, second, last);
		}

		public static Func<T1, T2, TResult> 
			CurryLast<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> func, T3 last)
		{
			return (T1 first, T2 second) => func(first, second, last);
		}

		public static Func<T2, T3, T4, TResult>
			CurryFirst<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> func, T1 first)
		{
			return (T2 second, T3 third, T4 last) => func(first, second, third, last);
		}

		public static Func<T1, T2, T3, TResult> 
			CurryLast<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> func, T4 last)
		{
			return (T1 first, T2 second, T3 third) => func(first, second, third, last);
		}

		#endregion

		#region FuncAsync currying extenions

		public static FuncAsync<TResult>
			Curry<T, TResult>(this FuncAsync<T, TResult> func, T value)
		{
			return ct => func(ct, value);
		}

		public static FuncAsync<T2, TResult>
			CurryFirst<T1, T2, TResult>(this FuncAsync<T1, T2, TResult> func, T1 first)
		{
			return (CancellationToken ct, T2 last) => func(ct, first, last);
		}

		public static FuncAsync<T1, TResult>
			CurryLast<T1, T2, TResult>(this FuncAsync<T1, T2, TResult> func, T2 last)
		{
			return (CancellationToken ct, T1 first) => func(ct, first, last);
		}

		public static FuncAsync<T2, T3, TResult>
			CurryFirst<T1, T2, T3, TResult>(this FuncAsync<T1, T2, T3, TResult> func, T1 first)
		{
			return (CancellationToken ct, T2 second, T3 last) => func(ct, first, second, last);
		}

		public static FuncAsync<T1, T2, TResult>
			CurryLast<T1, T2, T3, TResult>(this FuncAsync<T1, T2, T3, TResult> func, T3 last)
		{
			return (CancellationToken ct, T1 first, T2 second) => func(ct, first, second, last);
		}

		public static FuncAsync<T2, T3, T4, TResult>
			CurryFirst<T1, T2, T3, T4, TResult>(this FuncAsync<T1, T2, T3, T4, TResult> func, T1 first)
		{
			return (CancellationToken ct, T2 second, T3 third, T4 last) => func(ct, first, second, third, last);
		}

		public static FuncAsync<T1, T2, T3, TResult>
			CurryLast<T1, T2, T3, T4, TResult>(this FuncAsync<T1, T2, T3, T4, TResult> func, T4 last)
		{
			return (CancellationToken ct, T1 first, T2 second, T3 third) => func(ct, first, second, third, last);
		}

		#endregion

		#region Action currying extensions

		public static Action
			Curry<T>(this Action<T> action, T value)
		{
			return () => action(value);
		}

		public static Action<T2>
			CurryFirst<T1, T2>(this Action<T1, T2> action, T1 first)
		{
			return (T2 last) => action(first, last);
		}

		public static Action<T1>
			CurryLast<T1, T2>(this Action<T1, T2> action, T2 last)
		{
			return (T1 first) => action(first, last);
		}

		public static Action<T2, T3>
			CurryFirst<T1, T2, T3>(this Action<T1, T2, T3> action, T1 first)
		{
			return (T2 second, T3 last) => action(first, second, last);
		}

		public static Action<T1, T2>
			CurryLast<T1, T2, T3>(this Action<T1, T2, T3> action, T3 last)
		{
			return (T1 first, T2 second) => action(first, second, last);
		}

		public static Action<T2, T3, T4>
			CurryFirst<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action, T1 first)
		{
			return (T2 second, T3 third, T4 last) => action(first, second, third, last);
		}

		public static Action<T1, T2, T3>
			CurryLast<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action, T4 last)
		{
			return (T1 first, T2 second, T3 third) => action(first, second, third, last);
		}

		#endregion

		#region ActionAsync currying extenions

		public static ActionAsync
			Curry<T>(this ActionAsync<T> action, T value)
		{
			return (CancellationToken ct) => action(ct, value);
		}

		public static ActionAsync<T2>
			CurryFirst<T1, T2>(this ActionAsync<T1, T2> action, T1 first)
		{
			return (CancellationToken ct, T2 last) => action(ct, first, last);
		}

		public static ActionAsync<T1>
			CurryLast<T1, T2>(this ActionAsync<T1, T2> action, T2 last)
		{
			return (CancellationToken ct, T1 first) => action(ct, first, last);
		}

		public static ActionAsync<T2, T3>
			CurryFirst<T1, T2, T3>(this ActionAsync<T1, T2, T3> action, T1 first)
		{
			return (CancellationToken ct, T2 second, T3 last) => action(ct, first, second, last);
		}

		public static ActionAsync<T1, T2>
			CurryLast<T1, T2, T3>(this ActionAsync<T1, T2, T3> action, T3 last)
		{
			return (CancellationToken ct, T1 first, T2 second) => action(ct, first, second, last);
		}

		public static ActionAsync<T2, T3, T4>
			CurryFirst<T1, T2, T3, T4>(this ActionAsync<T1, T2, T3, T4> action, T1 first)
		{
			return (CancellationToken ct, T2 second, T3 third, T4 last) => action(ct, first, second, third, last);
		}

		public static ActionAsync<T1, T2, T3>
			CurryLast<T1, T2, T3, T4>(this ActionAsync<T1, T2, T3, T4> action, T4 last)
		{
			return (CancellationToken ct, T1 first, T2 second, T3 third) => action(ct, first, second, third, last);
		}

		#endregion
	}
}
