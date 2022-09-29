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
using System.Collections.Generic;

namespace Uno.Reflection
{
    internal class EventDescriptor : MemberDescriptor<EventInfo>, IEventDescriptor
    {
        private Dictionary<string, IMethodDescriptor> descriptors = new Dictionary<string, IMethodDescriptor>();

        public EventDescriptor(EventInfo eventInfo)
            : base(eventInfo)
        {
        }

        #region IEventDescriptor Members

        public override Type Type
        {
            get { return MemberInfo.EventHandlerType; }
        }

        public override bool IsStatic
        {
            get { return Add.IsStatic; }
        }

        public IMethodDescriptor Add
        {
            get { return descriptors.FindOrCreate("Add", () => (IMethodDescriptor)MemberInfo.GetAddMethod(true).GetDescriptor()); }
        }

        public IMethodDescriptor Remove
        {
            get { return descriptors.FindOrCreate("Remove", () => (IMethodDescriptor)MemberInfo.GetRemoveMethod(true).GetDescriptor()); }
        }

        #endregion
    }
}