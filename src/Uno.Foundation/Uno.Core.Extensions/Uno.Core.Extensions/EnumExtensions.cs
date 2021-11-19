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
using System.Linq;
using System.Reflection;

namespace Uno.Extensions
{
	internal static class EnumHelper
	{
		/// <summary>
		/// This is an alternative to Enum.GetNames() who is faster because are
		/// not sorting the results
		/// </summary>
		/// <remarks>
		/// The result order is the same than EnumHelper.GetValues().
		/// Note: Considerer using memoization if called often.
		/// </remarks>
		public static string[] GetNames<T>()
        {
#if WINDOWS_UWP || HAS_CRIPPLEDREFLECTION
            return Enum.GetNames(typeof (T));
#else
			var flds = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

			var names = new string[flds.Length];

			for (int i = 0; i < flds.Length; i++)
			{
				names[i] = flds[i].Name;
			}

			return names;
#endif
		}

		/// <summary>
		/// This is an alternative to Enum.GetValues() who is faster because are
		/// not sorting the results.
		/// </summary>
		/// <remarks>
		/// The result order is the same than EnumHelper.GetNames().
		/// Note: Considerer using memoization if called often.
		/// </remarks>
		public static T[] GetValues<T>()
		{
#if WINDOWS_UWP || HAS_CRIPPLEDREFLECTION
			return Enum.GetValues(typeof (T)).Cast<T>().ToArray();
#else
			var flds = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

			var values = new T[flds.Length];

			for (int i = 0; i < flds.Length; i++)
			{
				values[i] = (T)flds[i].GetRawConstantValue();
			}

			return values;
#endif
		}
	}
}
