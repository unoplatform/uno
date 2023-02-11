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

namespace Uno.Reflection
{
	internal abstract class ValueMemberDescriptor<TMemberInfo> : MemberDescriptor<TMemberInfo>, IValueMemberDescriptor
		where TMemberInfo : MemberInfo
	{
		public ValueMemberDescriptor(TMemberInfo memberInfo)
			: base(memberInfo)
		{
		}

		#region IValueMemberDescriptor Members

		public virtual object GetValue(object instance)
		{
			throw new NotImplementedException();
		}

		public virtual void SetValue(object instance, object value)
		{
			throw new NotImplementedException();
		}

		public virtual Action<object, object> ToCompiledSetValue()
		{
			throw new NotImplementedException();
		}

		public virtual Action<object, object> ToCompiledSetValue(bool strict)
		{
			throw new NotImplementedException();
		}

		public virtual Func<object, object> ToCompiledGetValue()
		{
			throw new NotImplementedException();
		}

		public virtual Func<object, object> ToCompiledGetValue(bool strict)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
