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
using System.Reflection;
using Uno.Reflection;
using System.Linq.Expressions;

namespace Uno.Extensions
{
    internal static class ReflectionExtensions
    {
        public static IReflectionExtensions Extensions { get; } = new DefaultReflectionExtensions();

        public static object Get(this IReflectionExtensionPoint extensionPoint, string memberName)
        {
            return Extensions.Get(extensionPoint, memberName);
        }

        public static T Get<T>(this IReflectionExtensionPoint extensionPoint, string memberName)
        {
            return Extensions.Get<T>(extensionPoint, memberName);
        }

        public static T Get<T,U>(this IReflectionExtensionPoint<U> extensionPoint, string memberName)
        {
            return Extensions.Get<T>(extensionPoint, memberName);
        }

        public static object Get(this IReflectionExtensionPoint extensionPoint, IEnumerable<string> memberNames)
        {
            return Extensions.Get(extensionPoint, memberNames);
        }
        
        public static T Get<T>(this IReflectionExtensionPoint extensionPoint, IEnumerable<string> memberNames)
        {
            return Extensions.Get<T>(extensionPoint, memberNames);
        }

        public static T Get<T,U>(this IReflectionExtensionPoint<U> extensionPoint, IEnumerable<string> memberNames)
        {
            return Extensions.Get<T>(extensionPoint, memberNames);
        }

        public static object Get(this IReflectionExtensionPoint extensionPoint,
                                 IEnumerable<IValueMemberDescriptor> descriptors)
        {
            return Extensions.Get(extensionPoint, descriptors);
        }

        public static T Get<T>(this IReflectionExtensionPoint extensionPoint,
                               IEnumerable<IValueMemberDescriptor> descriptors)
        {
            return Extensions.Get<T>(extensionPoint, descriptors);
        }

        public static T Get<T,U>(this IReflectionExtensionPoint<U> extensionPoint,
                               IEnumerable<IValueMemberDescriptor> descriptors)
        {
            return Extensions.Get<T>(extensionPoint, descriptors);
        }

        public static void Set<T>(this IReflectionExtensionPoint extensionPoint, string memberName, T value)
        {
            Extensions.Set(extensionPoint, memberName, value);
        }

        public static void Set<T>(this IReflectionExtensionPoint extensionPoint, IEnumerable<string> memberNames,
                                  T value)
        {
            Extensions.Set(extensionPoint, memberNames, value);
        }

        public static void Set<T,U>(this IReflectionExtensionPoint<U> extensionPoint, IEnumerable<string> memberNames,
                                  T value)
        {
            Extensions.Set(extensionPoint, memberNames, value);
        }

        public static void Set<T>(this IReflectionExtensionPoint extensionPoint,
                                  IEnumerable<IValueMemberDescriptor> descriptors, T value)
        {
            Extensions.Set(extensionPoint, descriptors, value);
        }

        public static void Set<T,U>(this IReflectionExtensionPoint<U> extensionPoint,
                                  IEnumerable<IValueMemberDescriptor> descriptors, T value)
        {
            Extensions.Set(extensionPoint, descriptors, value);
        }

        public static IEnumerable<IValueMemberDescriptor> GetValueDescriptors(
            this IReflectionExtensionPoint extensionPoint, IEnumerable<string> memberNames)
        {
            return Extensions.GetValueDescriptors(extensionPoint, memberNames);
        }

        public static IEnumerable<IMemberDescriptor> GetDescriptors(this IReflectionExtensionPoint extensionPoint,
                                                                    IEnumerable<string> memberNames)
        {
            return Extensions.GetDescriptors(extensionPoint, memberNames);
        }

        public static IMemberDescriptor GetDescriptor(this IReflectionExtensionPoint extensionPoint)
        {
            return Extensions.GetDescriptor(extensionPoint);
        }

        public static IMemberDescriptor GetDescriptor(this IMemberDescriptor descriptor, string memberName)
        {
            return Extensions.GetDescriptor(descriptor, memberName);
        }

        public static IMemberDescriptor FindDescriptor(this IMemberDescriptor descriptor, string memberName)
        {
            return Extensions.FindDescriptor(descriptor, memberName);
        }

        public static IValueMemberDescriptor GetValueDescriptor(this IReflectionExtensionPoint extensionPoint,
                                                                string memberName)
        {
            return Extensions.GetValueDescriptor(extensionPoint, memberName);
        }

        public static IValueMemberDescriptor FindValueDescriptor(this IReflectionExtensionPoint extensionPoint,
                                                                 string memberName)
        {
            return Extensions.FindValueDescriptor(extensionPoint, memberName);
        }

		public static IMemberDescriptor GetDescriptor(this IReflectionExtensionPoint extensionPoint, string memberName)
        {
            return Extensions.GetDescriptor(extensionPoint, memberName);
        }

        public static IMemberDescriptor GetDescriptor<TArg1>(this IReflectionExtensionPoint<TArg1> extensionPoint, Expression<Action<TArg1>> func)
        {
            return Extensions.GetDescriptor(extensionPoint, func);
        }

        public static IMemberDescriptor GetDescriptor<TArg1, TResult>(this IReflectionExtensionPoint<TArg1> extensionPoint, Expression<Func<TArg1, TResult>> func)
        {
            return Extensions.GetDescriptor(extensionPoint, func);
        }

        public static IMemberDescriptor FindDescriptor(this IReflectionExtensionPoint extensionPoint, string memberName)
        {
            return Extensions.FindDescriptor(extensionPoint, memberName);
        }

        public static IMemberDescriptor FindDescriptor(this IReflectionExtensionPoint extensionPoint, string memberName,
                                                       BindingContract contract)
        {
            return Extensions.FindDescriptor(extensionPoint, memberName, contract);
        }

        public static IReflectionExtensionPoint Reflection(this Type type)
        {
            return Extensions.Reflection(type);
        }

        public static IReflectionExtensionPoint<T> Reflection<T>(this T instance)
        {
            return Extensions.Reflection(instance);
        }

        public static IDisposable Observe(this IEventDescriptor descriptor, object publisher, object observer,
                                          string methodName)
        {
            return Extensions.Observe(descriptor, publisher, observer, methodName);
        }

        public static IMemberDescriptor GetDescriptor(this MemberInfo mi)
        {
            return Extensions.GetDescriptor(mi);
        }
    }
}
