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
    internal abstract class MemberDescriptor<TMemberInfo> : IMemberDescriptor
        where TMemberInfo : MemberInfo
    {
        private readonly TMemberInfo memberInfo;

        // TODO: protected constructor?
        public MemberDescriptor(TMemberInfo memberInfo)
        {
            this.memberInfo = memberInfo.Validation().NotNull("memberInfo");
        }

        public TMemberInfo MemberInfo
        {
            get { return memberInfo; }
        }

        #region IMemberDescriptor Members

        public abstract Type Type { get; }

        MemberInfo IMemberDescriptor.MemberInfo
        {
            get { return memberInfo; }
        }

        public virtual bool IsStatic
        {
            get { return false; }
        }

        public virtual bool IsInstance
        {
            get { return !IsStatic; }
        }

        public virtual bool IsGeneric
        {
            get { return false; }
        }

        public virtual bool IsOpen
        {
            get { return false; }
        }

        public virtual bool IsClosed
        {
            get { return !IsOpen; }
        }

        public virtual IMemberDescriptor Open()
        {
            throw new InvalidOperationException();
        }

        public virtual IMemberDescriptor Close(params Type[] types)
        {
            throw new InvalidOperationException();
        }

        #endregion
    }
}
