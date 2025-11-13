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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Uno.Extensions
{
	internal static class TypeExtensions
	{
		public static bool Is<T>(this Type type)
		{
			return type.Is(typeof(T));
		}

		public static bool Is(this Type instance, Type type)
		{
			return type.IsAssignableFrom(instance);
		}

#if WINAPPSDK || WINPRT || XAMARIN
#pragma warning disable IL2070
		public static IEnumerable<ConstructorInfo> GetConstructors(this TypeInfo type)
		{
			return type.DeclaredConstructors;
		}
#pragma warning restore IL2070
#endif

#if !WINAPPSDK
		/// <summary>
		/// Gets whether null can be assigned to a variable of the given <see cref="type"/>
		/// </summary>
		/// <param name="type">The type on which to test nullability</param>
		/// <returns></returns>
		public static bool IsNullable(this Type type)
		{
			return
				!type.GetTypeInfo().IsValueType                 // is reference type
				|| Nullable.GetUnderlyingType(type) != null;    // is Nullable<T>
		}
#endif

		/// <summary>
		/// Checks whether the type is of a specific generic type regardless of the generic type argument(s): <![CDATA[ 'is List<>' or 'is IList<>' ]]>
		/// </summary>
		/// <param name="type"></param>
		/// <param name="genericTypeDefinition">The generic type without generic type argument(s).</param>
		/// <returns></returns>
		public static bool IsGenericDescentOf(
#if NET9_0_OR_GREATER
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
			this Type type,
			Type genericTypeDefinition)
		{
			if (!genericTypeDefinition.IsGenericTypeDefinition)
			{
				return genericTypeDefinition.IsAssignableFrom(type);
			}

			if (genericTypeDefinition.IsInterface)
			{
				return type.GetInterfaces()
					.Where(x => x.IsGenericType)
					.Any(x => genericTypeDefinition.IsAssignableFrom(x.GetGenericTypeDefinition()));
			}
			else
			{
				return type.EnumerateAncestorTypes(includeSelf: true)
					.Where(x => x.IsGenericType)
					.Select(x => x.GetGenericTypeDefinition())
					.Contains(genericTypeDefinition);
			}
		}

		/// <summary>
		/// Enumerate all the types that this type inherits.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="includeSelf"></param>
		/// <returns></returns>
		public static IEnumerable<Type> EnumerateAncestorTypes(this Type type, bool includeSelf = false)
		{
			if (includeSelf)
			{
				yield return type;
			}

			while (type.BaseType is { } baseType)
			{
				yield return type = baseType;
			}
		}
	}
}
