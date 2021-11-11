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
using System.Text;

namespace Uno.Equality
{
	/// <summary>
	/// Defines a generalized method that a value type or class implements to create a type-specific method for determining equality of version of instances.
	/// </summary>
	internal interface IKeyEquatable
	{
		/// <summary>
		/// Gets the hash code of the key of this object.
		/// </summary>
		/// <returns>A hash code for the current object's key.</returns>
		int GetKeyHashCode();

		/// <summary>
		/// Indicates whether the key of current object is equal to another object's key of the same type.
		/// </summary>
		/// <param name="other">The object to compare with this object.</param>
		/// <returns>True is the current object is another version of the <paramref name="other"/> parameter; otherwise false</returns>
		bool KeyEquals(object other);
	}

	/// <summary>
	/// Defines a generalized method that a value type or class implements to create a type-specific method for determining equality of version of instances.
	/// </summary>
	internal interface IKeyEquatable<T>
	{
		/// <summary>
		/// Gets the hash code of the key of this object.
		/// </summary>
		/// <returns>A hash code for the current object's key.</returns>
		int GetKeyHashCode();

		/// <summary>
		/// Indicates whether the key of current object is equal to another object's key of the same type.
		/// </summary>
		/// <param name="other">The object to compare with this object.</param>
		/// <returns>True is the current object is another version of the <paramref name="other"/> parameter; otherwise false</returns>
		bool KeyEquals(T other);
	}
}
