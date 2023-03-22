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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Extensions;

namespace Uno.Collections
{
	/// <summary>
	/// A dictionary of values that live as long as the owner is alive. This class is thread-safe.
	/// </summary>
	/// <typeparam name="TOwner">The type of the owner</typeparam>
	/// <typeparam name="TKey">The key type</typeparam>
	internal class WeakAttachedDictionary<TOwner, TKey>
		where TOwner : class
		where TKey : class
	{
		private readonly ConditionalWeakTable<TOwner, System.Collections.Concurrent.ConcurrentDictionary<TKey, object>> _instances =
			new ConditionalWeakTable<TOwner, System.Collections.Concurrent.ConcurrentDictionary<TKey, object>>();

		private static System.Collections.Concurrent.ConcurrentDictionary<TKey, object> CreateDictionary(TOwner key)
		{
			return new System.Collections.Concurrent.ConcurrentDictionary<TKey, object>();
		}

		/// <summary>
		/// Gets the value associated with the specified key, for the specified owner instance.
		/// </summary>
		/// <param name="owner">The owner instance for the specified key</param>
		/// <param name="key">The key to get</param>
		/// <param name="defaultSelector">The selector called when the value does not exist for the specified owner. Otherwise, default(TValue) is used.</param>
		/// <returns>The value</returns>
		public TValue GetValue<TValue>(TOwner owner, TKey key, Func<TValue> defaultSelector = null)
		{
			defaultSelector ??= (() => default);

			var values = _instances.GetValue(owner, CreateDictionary);

			return (TValue)values.GetOrAdd(key, k => defaultSelector());
		}

		/// <summary>
		/// Gets the value associated with the specified key, for the specified owner instance.
		/// </summary>
		/// <param name="owner">The owner instance for the specified key</param>
		/// <param name="key">The key to get</param>
		/// <param name="defaultSelector">The selector called when the value does not exist for the specified owner. Otherwise, default(TValue) is used.</param>
		/// <returns>The value</returns>
		public TValue GetValue<TValue>(TOwner owner, TKey key, Func<TKey, TValue> defaultSelector)
		{
			var values = _instances.GetValue(owner, CreateDictionary);

			return (TValue)values.GetOrAdd(key, k => defaultSelector(k) as object);
		}

		/// <summary>
		/// Sets the value for the specified key, for the specified owner.
		/// </summary>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="owner"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetValue<TValue>(TOwner owner, TKey key, TValue value)
		{
			var values = _instances.GetValue(owner, CreateDictionary);

			values[key] = value;
		}
	}
}
