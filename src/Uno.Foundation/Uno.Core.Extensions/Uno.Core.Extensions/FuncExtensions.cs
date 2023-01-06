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

namespace Uno.Extensions
{
	internal static partial class FuncExtensions
	{
		public static Func<T, bool> Not<T>(this Func<T, bool> func)
		{
			return item => !func(item);
		}

		public static Func<T> ToFunc<T>(this Func<Null, T> func)
		{
			return () => func(null);
		}

		public static Func<Null, T> ToFunc<T>(this Func<T> func)
		{
			return notUsed => func();
		}

		public static Action<TRequest> ToAction<TRequest, TResponse>(this Func<TRequest, TResponse> func)
		{
			// TODO: conver to method group?
			return item => func(item);
		}

		public static Action ToAction<TResponse>(this Func<Null, TResponse> func)
		{
			return () => func(null);
		}

		public static Action<TKey, TValue> ToAction<TKey, TValue>(this Action<KeyValuePair<TKey, TValue>> action)
		{
			return (i, item) => action(new KeyValuePair<TKey, TValue>(i, item));
		}

		public static Func<T, Null> ToFunc<T>(this Action<T> action)
		{
			return item =>
					   {
						   action(item);
						   return null;
					   };
		}

		public static Func<T, T> ToInterceptor<T>(this Action<T> action)
		{
			return item =>
					   {
						   action(item);
						   return item;
					   };
		}

		public static Func<Null, Null> ToFunc(this Action action)
		{
			return ToFunc<Null>(action);
		}

		public static Func<T, T> ToFunc<T>(this Action action)
		{
			return item =>
					   {
						   action();
						   return item;
					   };
		}

		public static Func<U> ToFunc<T, U>(this Func<T> func)
		{
			return () => (U)(object)func();
		}
	}
}
