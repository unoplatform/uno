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
using System.Reflection;
using Uno.Extensions;

namespace Uno.Reflection
{
    internal class PropertyDescriptor : ValueMemberDescriptor<PropertyInfo>
    {
        public PropertyDescriptor(PropertyInfo pi)
            : base(pi)
        {
        }

        public override Type Type
        {
            get { return MemberInfo.PropertyType; }
        }

        public override bool IsStatic
        {
            get { return MemberInfo.GetGetMethod(true).IsStatic; }
        }

        public override object GetValue(object instance)
        {
            return MemberInfo.GetValue(instance, null);
        }

        public override void SetValue(object instance, object value)
        {
            MemberInfo.SetValue(instance, value, null);
        }

        public override Func<object, object> ToCompiledGetValue()
        {
            return instance => MemberInfo.GetValue(instance, null);
        }

        public override Func<object, object> ToCompiledGetValue(bool strict)
        {
            return instance => MemberInfo.GetValue(instance, null);
        }

        public override Action<object, object> ToCompiledSetValue()
        {
            return (instance, value) => MemberInfo.SetValue(instance, value, null);
        }

        public override Action<object, object> ToCompiledSetValue(bool strict)
        {
            return (instance, value) => MemberInfo.SetValue(instance, value, null);
        }
    }
}