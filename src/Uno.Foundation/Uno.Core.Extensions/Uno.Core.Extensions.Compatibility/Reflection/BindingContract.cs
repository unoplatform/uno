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
#if !HAS_CRIPPLEDREFLECTION || XAMARIN
using System;
using System.Reflection;
using Uno.Contracts;

namespace Uno.Reflection
{
    internal class BindingContract : IContract
    {
        internal static readonly BindingContract Default;

        //internal static readonly BindingContract DefaultIgnoreCase;

        public static BindingFlags DefaultBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Static |
                                                         BindingFlags.Instance | BindingFlags.Public |
                                                         BindingFlags.NonPublic;

        public static BindingFlags DefaultBindingFlagsIgnoreCase = DefaultBindingFlags | BindingFlags.IgnoreCase;

        static BindingContract()
        {
             Default = new BindingContract(DefaultBindingFlags);
             //DefaultIgnoreCase = new BindingContract(DefaultBindingFlagsIgnoreCase);
        }

        public BindingContract()
            : this(DefaultBindingFlags)
        {
        }

        public BindingContract(BindingFlags bindingFlags)
            : this(bindingFlags, null)
        {
        }

        public BindingContract(BindingFlags bindingFlags, Type[] types)
            : this(MemberTypes.All, bindingFlags, null, types, BindingBehavior.All)
        {
        }

        public BindingContract(MemberTypes memberType, BindingFlags bindingFlags, Type returnType, Type[] types,
                               BindingBehavior behavior)
        {
            MemberType = memberType;
            BindingFlags = bindingFlags;
            ReturnType = returnType;
            Types = types;
            Behavior = behavior;
        }

        public MemberTypes MemberType { get; set; }

        public BindingFlags BindingFlags { get; set; }

        public Type ReturnType { get; set; }

        public Type[] SafeTypes
        {
            get { return Types ?? Array.Empty<Type>(); }
        }

        public Type[] Types { get; set; }

        public BindingBehavior Behavior { get; set; }
    }
}
#endif
