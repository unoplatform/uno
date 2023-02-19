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
#if !WINDOWS_UWP
using System;

namespace Uno.Reflection
{
	internal class TypeDescriptor : MemberDescriptor<Type>
	{
		public TypeDescriptor(Type type)
			: base(type)
		{
		}

		public override Type Type
		{
			get { return MemberInfo; }
		}

		public override bool IsStatic
		{
			get { return MemberInfo.IsAbstract && MemberInfo.IsSealed; }
		}

		public override bool IsGeneric
		{
			get { return MemberInfo.IsGenericType; }
		}

		public override bool IsOpen
		{
			get { return MemberInfo.IsGenericTypeDefinition; }
		}

		public override IMemberDescriptor Open()
		{
			return IsClosed ? new TypeDescriptor(MemberInfo.GetGenericTypeDefinition()) : base.Open();
		}

		// TODO: params are ignored
		public override IMemberDescriptor Close(params Type[] types)
		{
			if (!IsOpen)
			{
				return base.Close(types);
			}

			var closedType = MemberInfo.MakeGenericType(types);

			return new TypeDescriptor(closedType);
		}
	}
}
#endif
