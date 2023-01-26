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
namespace Uno.Extensions.ValueType
{
	internal interface IValueSupport
	{
		object And(object lhs, object rhs);
		object Or(object lhs, object rhs);
		object Xor(object lhs, object rhs);
		object Add(object lhs, object rhs);
		object Substract(object lhs, object rhs);
		object Multiply(object lhs, object rhs);
		object Divide(object lhs, object rhs);
		object Negate(object instance);
		object Not(object instance);
	}
}
