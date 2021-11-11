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

namespace Uno
{
	/// <summary>
	/// Container for stock actions.
	/// </summary>
	/// <typeparam name="T">The type of the argument for the actions.</typeparam>
	internal static class Actions<T>
	{
		/// <summary>
		/// A Null action, that performs nothing.
		/// </summary>
		public static readonly Action<T> Null = item => { };

		/// <summary>
		/// A Null action, that performs nothing.
		/// </summary>
		public static readonly ActionAsync<T> NullAsync = (_, __) => Actions.NullTask;
	}

	internal static class Actions<T1, T2>
	{
		/// <summary>
		/// A Null action, that performs nothing.
		/// </summary>
		public static readonly Action<T1, T2> Null = (_1, _2) => { };
		
		/// <summary>
		/// A Null action, that performs nothing.
		/// </summary>
		public static readonly ActionAsync<T1, T2> NullAsync = (_, __, ___) => Actions.NullTask;
	}
}