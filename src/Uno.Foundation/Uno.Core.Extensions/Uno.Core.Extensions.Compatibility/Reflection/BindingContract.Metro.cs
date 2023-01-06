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
#if HAS_CRIPPLEDREFLECTION && !XAMARIN
using System;
using System.Reflection;
using Uno.Contracts;

namespace Uno.Reflection
{
    internal class BindingContract : IContract
    {
        internal static readonly BindingContract Default;

        internal static readonly BindingContract DefaultIgnoreCase;

        static BindingContract()
        {
             Default = new BindingContract();
             DefaultIgnoreCase = new BindingContract();
        }

        public BindingContract()
        {
        }

        public MemberTypes MemberType { get; set; }

        public Type ReturnType { get; set; }

        public Type[] SafeTypes
        {
            get { return Types ?? new Type[0]; }
        }

        public Type[] Types { get; set; }

        public BindingBehavior Behavior { get; set; }
    }

    // Summary:
    //     Marks each type of member that is defined as a derived class of MemberInfo.
    [Flags]
    internal enum MemberTypes
    {
        Constructor = 1,
        Event = 2,
        Field = 4,
        Method = 8,
        Property = 16,
        TypeInfo = 32,
        Custom = 64,
        NestedType = 128,
        All = 191,
    }   
}
#endif
