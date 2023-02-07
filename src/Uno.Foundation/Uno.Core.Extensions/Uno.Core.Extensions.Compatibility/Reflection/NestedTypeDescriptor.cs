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
	internal class NestedTypeDescriptor : TypeDescriptor, IValueMemberDescriptor
	{
		public NestedTypeDescriptor(Type type)
			: base(type)
		{
		}

		#region IValueMemberDescriptor Members

		public override Type Type
		{
			get { return MemberInfo; }
		}

		public override bool IsStatic
		{
			get { return true; }
		}

		public override IMemberDescriptor Open()
		{
			return IsClosed ? new NestedTypeDescriptor(MemberInfo.GetGenericTypeDefinition()) : base.Open();
		}

		// TODO: params are ignored
		public override IMemberDescriptor Close(params Type[] types)
		{
			if (!IsOpen)
			{
				return base.Close(types);
			}
			var closedType = MemberInfo.MakeGenericType(types);

			return new NestedTypeDescriptor(closedType);
		}

		public object GetValue(object instance)
		{
			return instance;
		}

		public void SetValue(object instance, object value)
		{
			throw new NotImplementedException();
		}

		public Action<object, object> ToCompiledSetValue()
		{
			return SetValue;
		}

		public Action<object, object> ToCompiledSetValue(bool strict)
		{
			return SetValue;
		}

		public Func<object, object> ToCompiledGetValue()
		{
			return GetValue;
		}

		public Func<object, object> ToCompiledGetValue(bool strict)
		{
			return GetValue;
		}

		#endregion
	}
}
#endif
