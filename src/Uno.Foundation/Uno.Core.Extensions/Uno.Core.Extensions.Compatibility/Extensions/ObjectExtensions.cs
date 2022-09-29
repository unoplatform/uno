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
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions
{
    internal static class ObjectExtensions
    {
        public static ExtensionPoint<T> Extensions<T>(this T value)
        {
            return new ExtensionPoint<T>(value);
        }

        public static bool IsDefault<T>(this ExtensionPoint<T> extensionPoint)
        {
            return Equals(extensionPoint.ExtendedValue, default(T));
        }

        public static void Dispose<T>(this ExtensionPoint<T> extensionPoint)
        {
            var disposable = extensionPoint.ExtendedValue as IDisposable;

            disposable.Maybe(d => d.Dispose());
        }

        public static IDisposable Using<T>(this ExtensionPoint<T> extensionPoint)
        {
            var disposable = extensionPoint.ExtendedValue as IDisposable;

            return disposable ?? NullDisposable.Instance;
        }

        public static void Maybe<TInstance>(this TInstance instance, Action action)
        {
            if (instance != null)
            {
                action();
            }
        }

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
        /// A helper method to allow for locally defined extension-method like methods. Avoids the creation of an external static class
        /// in the context of fluent expressions.
        /// </summary>
        public static TResult Apply<TSource, TResult>(this TSource source, Func<TSource, TResult> selector)
        {
            return selector(source);
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

        /// <summary>
        /// A helper method that allows the execution of an action in a fluent expression. The action will be executed if the condition is true.
        /// </summary>
        /// <param name="condition">A boolean value that indicates if the action should be executed.</param>
        /// <param name="action">The action to execute, the parameter will contain source</param>
        /// <returns>Returns the source instance</returns>
        public static TSource Apply<TSource>(this TSource source, bool condition, Action<TSource> action)
        {
            if (condition)
            {
                action(source);
            }

            return source;
        }

        /// <summary>
        /// Gets a boolean value that determines if a specific value is within a list of accepted values.
        /// Use this when it's not necessary or when it's overkill to declare the list of accepted values
        /// as a readonly field. For example, can be used to check if one enum value is within a set without that
        /// enum being marked as [Flags].
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="acceptedValues">The list of accepted values.</param>
        /// <returns></returns>
        public static bool IsOneOf<TSource>(this TSource source, params TSource[] acceptedValues)
        {
            return acceptedValues?.Contains(source) ?? false;
        }

        /// <summary>
        /// Gets a boolean value that determines if a specific value is within a list of accepted values.
        /// Use this when it's not necessary or when it's overkill to declare the list of accepted values
        /// as a readonly field. For example, can be used to check if one enum value is within a set without that
        /// enum being marked as [Flags].
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="comparer">The comparer to use to determine equality.</param>
        /// <param name="acceptedValues">The list of accepted values.</param>
        /// <returns></returns>
        public static bool IsOneOf<TSource>(this TSource source, IEqualityComparer<TSource> comparer, params TSource[] acceptedValues)
        {
            return acceptedValues?.Contains(source, comparer) ?? false;
        }
    }
}
