// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
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
using System.Linq;
using Uno.Collections;

namespace Uno.Extensions
{
	internal static class DictionaryExtensions
	{
		public static TValue FindOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> items, TKey key, Func<TValue> factory)
		{
			TValue value;

			if (!items.TryGetValue(key, out value))
			{
				value = factory();
				items.Add(key, value);
			}

			return value;
		}

		/// <summary>
		/// Gets the value associated with the specified key, or a default value.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">Default value if the key does not exists in dictionary</param>
		/// <returns>the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</returns>
		public static TValue UnoGetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			return UnoGetValueOrDefault(dictionary, key, default(TValue));
		}

		/// <summary>
		/// Gets the value associated with the specified key, or a default value.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">Default value if the key does not exists in dictionary</param>
		/// <returns>the value associated with the specified key, if the key is found; otherwise, the <paramref name="defaultValue"/>.</returns>
		public static TValue UnoGetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
		{
			if (dictionary == null)
			{
				return defaultValue;
			}
			else
			{
				TValue value;
				return dictionary.TryGetValue(key, out value)
					? value
					: defaultValue;
			}
		}

		public static IEnumerable<TKey> RemoveKeys<TKey, TValue>(this IDictionary<TKey, TValue> items, IEnumerable<TKey> range)
		{
			return range.Where(items.Remove).ToList();
		}

		public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> dictionnary, IEnumerable<KeyValuePair<TKey, TValue>> items)
		{
			foreach (var item in items)
			{
				dictionnary[item.Key] = item.Value;
			}
		}
	}
}
