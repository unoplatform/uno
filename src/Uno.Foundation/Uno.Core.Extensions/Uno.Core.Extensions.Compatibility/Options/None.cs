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
using System.CodeDom;
using System.Diagnostics;

namespace Uno
{
	/// <summary>
	/// Special Option representing an absence of value.
	/// </summary>
	/// <remarks>
	/// This is the implementation of a functional "Option Type" using F# semantic
	/// https://en.wikipedia.org/wiki/Option_type
	/// </remarks>
	[DebuggerDisplay("None()")]
	public sealed class None<T> : Option<T>
	{
		/// <summary>
		/// Singleton instance of this
		/// </summary>
		public static None<T> Instance { get; } = new None<T>();

		private None() : base(OptionType.None) { }

		protected override object GetValue() => throw new NotSupportedException("Cannot get value on a None");

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is None<T>;

		/// <inheritdoc/>
		public override int GetHashCode() => typeof(T).GetHashCode();

		/// <inheritdoc />
		public override string ToString() => $"None<{typeof(T).Name}>()";
	}
}