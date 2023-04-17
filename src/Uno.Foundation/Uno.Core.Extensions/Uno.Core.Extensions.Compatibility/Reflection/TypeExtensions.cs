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
using System.Reflection;
using Uno.Reflection;

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

#if WINDOWS_UWP || WINPRT || XAMARIN
		public static IEnumerable<ConstructorInfo> GetConstructors(this TypeInfo type)
		{
			return type.DeclaredConstructors;
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

#if !WINDOWS_UWP
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
