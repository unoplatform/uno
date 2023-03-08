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
using System.Reflection;
using Uno.Reflection;
using System.Linq.Expressions;

namespace Uno.Extensions
{
	internal static class TypeExtensions
	{
		public static object New(this Type type, params object[] args)
		{
			return Activator.CreateInstance(type, args);
		}

		public static T New<T>(this Type type, params object[] args)
		{
			return (T)New(type, args);
		}

		public static bool Is<T>(this Type type)
		{
			return type.Is(typeof(T));
		}

		public static bool Is(this Type instance, Type type)
		{
			return type.IsAssignableFrom(instance);
		}

		public static MemberInfo GetMemberInfo(this Type type, string memberName)
		{
			return GetMemberInfo(type, memberName, BindingContract.Default);
		}

		public static MemberInfo GetMemberInfo(this Type type, string memberName, BindingContract contract)
		{
			var mi = FindMemberInfo(type, memberName, contract);

			return mi.Validation().Found();
		}

#if HAS_NO_TYPEINFO
		public static Type GetTypeInfo(this Type type)
		{
			return type;
		}
#endif

#if WINDOWS_UWP || WINPRT || XAMARIN
		public static IEnumerable<ConstructorInfo> GetConstructors(this TypeInfo type)
		{
			return type.DeclaredConstructors;
		}
#endif

#if !WINDOWS_UWP
		public static MemberInfo FindMemberInfo(this Type type, string memberName)
		{
			return FindMemberInfo(type, memberName, BindingContract.Default);
		}
#endif

#if WINDOWS_UWP
		public static Assembly GetAssembly(this Type type)
		{
			return type.GetTypeInfo().Assembly;
		}
#else
		public static Assembly GetAssembly(this Type type)
		{
			return type.Assembly;
		}
#endif

		public static MemberInfo FindMemberInfo(this Type type, string memberName, BindingContract contract)
		{
			var mi = FindInheritedMember(type, memberName, contract);

			if (mi == null &&
				(contract.Behavior & BindingBehavior.Interface) == BindingBehavior.Interface)
			{
				foreach (var interfaceType in type.GetInterfaces())
				{
					mi = FindInheritedMember(interfaceType, memberName, contract);

					if (mi != null)
					{
						break;
					}
				}
			}

			return mi;
		}

		public static MemberInfo FindInheritedMember(Type type, string memberName, BindingContract contract)
		{
			MemberInfo mi = null;

			while (type != null)
			{
				mi = FindMember(type, memberName, contract);

				if (mi != null ||
					(contract.Behavior & BindingBehavior.Inherited) != BindingBehavior.Inherited)
				{
					break;
				}

#if !WINDOWS_UWP && !HAS_CRIPPLEDREFLECTION
				type = type.BaseType;
#else
				type = type.GetTypeInfo().BaseType;
#endif
			}

			return mi;
		}

		public static MemberInfo FindMember(Type type, string memberName, BindingContract contract)
		{
			MemberInfo memberInfo;

			if ((contract.MemberType & MemberTypes.Property) == MemberTypes.Property)
			{
#if !WINDOWS_UWP && !HAS_CRIPPLEDREFLECTION
				memberInfo = type.GetProperty(memberName, contract.BindingFlags, null, contract.ReturnType,
											  contract.SafeTypes, null);
#elif HAS_CRIPPLEDREFLECTION && !HAS_TYPEINFO
				memberInfo = type.GetProperty(memberName, contract.ReturnType, contract.SafeTypes);
#else
				memberInfo = type.GetTypeInfo().GetDeclaredProperty(memberName);
#endif

				if (memberInfo != null)
				{
					return memberInfo;
				}
			}

			if ((contract.MemberType & MemberTypes.Event) == MemberTypes.Event)
			{
#if !WINDOWS_UWP && !HAS_CRIPPLEDREFLECTION
				memberInfo = type.GetEvent(memberName, contract.BindingFlags);
#else
				memberInfo = type.GetTypeInfo().GetDeclaredEvent(memberName);
#endif

				if (memberInfo != null)
				{
					return memberInfo;
				}
			}

			if ((contract.MemberType & MemberTypes.Field) == MemberTypes.Field)
			{
#if !WINDOWS_UWP && !HAS_CRIPPLEDREFLECTION
				memberInfo = type.GetField(memberName, contract.BindingFlags);
#else
				memberInfo = type.GetTypeInfo().GetDeclaredField(memberName);
#endif

				if (memberInfo != null)
				{
					return memberInfo;
				}
			}

			if ((contract.MemberType & MemberTypes.Method) == MemberTypes.Method)
			{
#if !WINDOWS_UWP && !HAS_CRIPPLEDREFLECTION
				memberInfo = contract.Types == null
								 ? type.GetMethod(memberName, contract.BindingFlags)
								 : type.GetMethod(memberName, contract.BindingFlags, null, contract.Types, null);
#elif HAS_CRIPPLEDREFLECTION && !HAS_TYPEINFO
				memberInfo = contract.Types == null
								 ? type.GetMethod(memberName, contract.BindingFlags)
								 : type.GetMethod(memberName, contract.Types);

#else
				memberInfo = type.GetTypeInfo()
								 .GetDeclaredMethods(memberName)
								 .FirstOrDefault(
									m => m.GetParameters().Select(p => p.ParameterType).Equality().Equal(contract.Types)
								 );
#endif

				if (memberInfo != null)
				{
					return memberInfo;
				}
			}

			if ((contract.MemberType & MemberTypes.NestedType) == MemberTypes.NestedType)
			{
#if !WINDOWS_UWP
				memberInfo = type.GetNestedType(memberName, contract.BindingFlags);
#else
				memberInfo = type.GetTypeInfo().GetDeclaredNestedType(memberName);
#endif
				if (memberInfo != null)
				{
					return memberInfo;
				}
			}

			return null;
		}

		/// <summary>
		/// Recursively get all interfaces of a type
		/// </summary>
		public static IEnumerable<Type> GetAllInterfaces(this Type type)
		{
			foreach (var iface in type.GetInterfaces())
			{
				yield return iface;

				foreach (var subInterfaces in iface.GetAllInterfaces())
				{
					yield return subInterfaces;
				}
			}
		}

		public static TAttribute FindAttribute<TAttribute>(this Type type)
			where TAttribute : Attribute
		{
			return type.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() as TAttribute;
		}

		/// <summary>
		/// Gets the inheritance hierarchy of supplied type.
		/// </summary>
		public static IEnumerable<Type> GetBaseTypes(this Type type)
		{
			var previousType = type;
			while (true)
			{
#if !WINDOWS_UWP
				var baseType = previousType.BaseType;
#else
				var baseType = previousType.GetTypeInfo().BaseType;
#endif
				if (baseType == null || baseType.FullName == previousType.FullName)
				{
					yield break;
				}
				else
				{
					yield return baseType;
					previousType = baseType;
				}
			}
		}

#if !WINDOWS_UWP
		/// <summary>
		/// Determine if <see cref="type"/> implements IEnumerable&lt;T&gt;.
		/// </summary>
		/// <returns>The type of ITEM or null if <see cref="type"/> is not IEnumerable</returns>
		public static Type EnumerableOf(this Type type)
		{
			if (type.IsArray)
			{
				return type.GetElementType();
			}

			if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				return type.GetGenericArguments().First();
			}

			return type.GetAllInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)).SelectOrDefault(i => i.GetGenericArguments().First());
		}
#endif


#if !HAS_NO_TYPEINFO
		/// <summary>
		/// Gets the declared property by searching the flattened hierarchy.
		/// </summary>
		/// <param name="type">The type to search into</param>
		/// <param name="name">The name of the declared property</param>
		/// <returns>The property info if found, otherwise null.</returns>
		public static PropertyInfo GetFlattenedDeclaredProperty(this Type type, string name)
		{
			return type
				.GetTypeInfo()
				.Flatten(t => t.BaseType != typeof(object) ? t.BaseType.GetTypeInfo() : null)
				.Select(t => t.GetDeclaredProperty(name))
				.FirstOrDefault(t => t != null);
		}

		/// <summary>
		/// Gets the declared field by searching the flattened hierarchy.
		/// </summary>
		/// <param name="type">The type to search into</param>
		/// <param name="name">The name of the declared field</param>
		/// <returns>The field info if found, otherwise null.</returns>
		public static FieldInfo GetFlattenedDeclaredField(this Type type, string name)
		{
			return type
				.GetTypeInfo()
				.Flatten(t => t.BaseType != typeof(object) ? t.BaseType.GetTypeInfo() : null)
				.Select(t => t.GetDeclaredField(name))
				.FirstOrDefault(t => t != null);
		}
#endif

#if !WINDOWS_UWP
		private static Func<Type, bool> _isNullable = Funcs.Create((Type t) => IsNullable(t)).AsLockedMemoized();

		/// <summary>
		/// Returns a cached result of the the IsNullable method, as it works
		/// in O(n) where n is the depth of the hierarchy.
		/// </summary>
		public static bool IsNullableCached(this Type type)
		{
			return _isNullable(type);
		}

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
	}
}
