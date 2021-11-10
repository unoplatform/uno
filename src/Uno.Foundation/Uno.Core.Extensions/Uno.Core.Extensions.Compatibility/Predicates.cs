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
using Uno.Extensions;

namespace Uno
{
	/// <summary>
	/// A set of common prediactes
	/// </summary>
	public static class Predicates
	{
		/// <summary>
		/// A predicate that checks equality of two objects using the <see cref="EqualityExtensions"/>.
		/// </summary>
		public static readonly Func<object, object, bool> Equal = Predicates<object>.Equal;

		/// <summary>
		/// A predicate that checks if two objects are <see cref="object.ReferenceEquals"/>.
		/// </summary>
		public static readonly Func<object, object, bool> ReferenceEqual = Predicates<object>.ReferenceEqual;

		/// <summary>
		/// A predicate that always returns true.
		/// </summary>
		public static readonly Predicate<object> True = Predicates<object>.True;

		/// <summary>
		/// A predicate that always returns false.
		/// </summary>
		public static readonly Predicate<object> False = Predicates<object>.False;
	}
}