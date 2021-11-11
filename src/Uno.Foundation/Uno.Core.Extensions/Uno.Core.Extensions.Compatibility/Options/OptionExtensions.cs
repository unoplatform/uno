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
	/// Extension methods over <see cref="Option{T}"/>.
	/// </summary>
	internal static class OptionExtensions
	{
		/// <summary>
		/// Creates an <see cref="Option{T2}"/> using an <see cref="Option{T1}"/>.
		/// </summary>
		/// <typeparam name="T1">Type of the source option</typeparam>
		/// <typeparam name="T2">Type of the target option</typeparam>
		/// <param name="option">The source option</param>
		/// <param name="func">Method to create an <see cref="Option{T1}"/> for a given <typeparamref name="T1"/>.</param>
		/// <returns>The resulting option</returns>
		public static Option<T2> Bind<T1, T2>(this Option<T1> option, Func<T1, Option<T2>> func)
		{
			T1 value1;
			return option.MatchSome(out value1)
				? func(value1)
				: Option.None<T2>();
		}

		/// <summary>
		/// Convert an <see cref="Option{T1}"/> to an <see cref="Option{T2}"/>
		/// </summary>
		/// <typeparam name="T1">Type of the source option</typeparam>
		/// <typeparam name="T2">Type of the target option</typeparam>
		/// <param name="option">The source option to convert</param>
		/// <param name="func">Method to convert the value of the source to the value the target</param>
		/// <returns>The converted option</returns>
		public static Option<T2> Map<T1, T2>(this Option<T1> option, Func<T1, T2> func)
		{
			T1 value1;
			return option.MatchSome(out value1)
				? Option.Some(func(value1))
				: Option.None<T2>();
		}

		/// <summary>
		/// Gets the value of the option or default(<typeparamref name="T"/>) if none.
		/// </summary>
		/// <typeparam name="T">Type of the option</typeparam>
		/// <param name="option">The source option from which the value have to be extracted</param>
		/// <returns>The value of the option or default(<typeparamref name="T"/>) if none.</returns>
		public static T SomeOrDefault<T>(this Option<T> option, T defaultValue = default(T))
			=> option.MatchSome(out var value)
				? value
				: defaultValue;

		/// <summary>
		/// Gets the value of the option or default(object) if none.
		/// </summary>
		/// <param name="option">The source option from which the value have to be extracted</param>
		/// <returns>The value of the option or default(object) if none.</returns>
		public static object SomeOrDefault(this Option option, object defaultValue = null)
			=> option.MatchSome(out var value)
				? value
				: defaultValue;
	}
}