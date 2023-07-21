// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
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

namespace Uno.Extensions
{
	internal static class ObjectExtensions
	{
		public static void Maybe<TInstance>(this TInstance instance, Action<TInstance> action)
		{
			if (instance != null)
			{
				action(instance);
			}
		}

		public static void Maybe<TInstance>(this object instance, Action<TInstance> action)
			where TInstance : class
		{
			Maybe<TInstance>(instance as TInstance, action);
		}

		public static TResult SelectOrDefault<TInstance, TResult>(this TInstance instance, Func<TInstance, TResult> selector)
		{
			return SelectOrDefault(instance, selector, default(TResult));
		}

		public static TResult SelectOrDefault<TInstance, TResult>(this TInstance instance, Func<TInstance, TResult> selector, TResult defaultValue)
		{
			return instance == null ? defaultValue : selector(instance);
		}

		public static bool SafeEquals<T>(this T obj, T other)
			where T : class
		{
			if (obj == null)
			{
				return other == null;
			}
			else
			{
				return obj.Equals(other);
			}
		}

		/// <summary>
		/// A helper method that allows the execution of an action in a fluent expression.
		/// </summary>
		/// <param name="action">The action to execute on the source object</param>
		/// <returns>The source instance</returns>
		public static TSource Apply<TSource>(this TSource source, Action<TSource> action)
		{
			action(source);

			return source;
		}
	}
}
