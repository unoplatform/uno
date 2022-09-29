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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;

namespace Uno.Extensions
{
#if HAS_TYPEINFO || WINDOWS_UWP
    internal static class TypeInfoExtensions
    {
#if !HAS_TYPEINFO_EXTENSIONS
        public static MemberInfo FindMemberInfo(this Type type, string memberName)
        {
			return type.GetTypeInfo().DeclaredMembers.FirstOrDefault(m => m.Name == memberName);
        }

		public static bool IsAssignableFrom(this Type type, Type memberName)
        {
            return type.GetTypeInfo().IsAssignableFrom(memberName.GetTypeInfo());
        }
#endif

        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, Type attributeType, bool includeInherited)
        {
            return CustomAttributeExtensions.GetCustomAttributes(type.GetTypeInfo(), attributeType, includeInherited);
        }

#if !HAS_TYPEINFO_EXTENSIONS
		public static IEnumerable<Type> GetInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces;
        }

        public static PropertyInfo GetProperty(this Type type, string name)
        {
            return type.GetTypeInfo().GetDeclaredProperty(name);
        }

        public static FieldInfo GetField(this Type type, string name)
        {
            return type.GetTypeInfo().GetDeclaredField(name);
        }
 
        public static IEnumerable<FieldInfo> GetFields(this Type type)
        {
            return type.GetTypeInfo().DeclaredFields;
        }

        public static ConstructorInfo GetConstructor(this Type t, Type[] parameters)
        {
            return t.GetTypeInfo()
                       .DeclaredConstructors
                       .FirstOrDefault(
                            c => c.GetParameters()
                                  .Select(p => p.ParameterType)
                                  .Equality()
                                  .Equal(parameters)
                       );
        }

        public static MethodInfo GetAddMethod(this EventInfo info, bool throws = true)
        {
            return info.AddMethod;
        }

        public static MethodInfo GetRemoveMethod(this EventInfo info, bool throws = true)
        {
            return info.RemoveMethod;
        }

        public static MethodInfo GetGetMethod(this PropertyInfo info, bool throws = true)
        {
            return info.GetMethod;
        }

        public static MethodInfo GetSetMethod(this PropertyInfo info, bool throws = true)
        {
            return info.SetMethod;
        }
#endif

		public static CustomAttributeData[] GetCustomAttributesData(this FieldInfo info)
        {
            return info.CustomAttributes.ToArray();
        }

#if !HAS_TYPEINFO_EXTENSIONS
        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static MethodInfo GetMethod(this Type type, string name)
        {
            return type.GetTypeInfo().GetDeclaredMethod(name);
        }
#endif
    }
#endif
	}
