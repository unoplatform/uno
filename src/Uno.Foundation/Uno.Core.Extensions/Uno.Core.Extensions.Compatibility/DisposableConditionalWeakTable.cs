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
using System.Runtime.CompilerServices;
using System.Text;

namespace Uno.Core
{
	/// <summary>Enables compilers to dynamically attach IDisposable fields to managed objects. Attached disposable will be disposed on when the key object is collected.</summary>
	/// <typeparam name="TKey">The reference type to which the field is attached. </typeparam>
	/// <typeparam name="TValue">The field's type. This must be a reference type.</typeparam>
	internal class DisposableConditionalWeakTable<TKey, TValue>
		where TKey : class
		where TValue : class, IDisposable
	{
		private readonly ConditionalWeakTable<TKey, Handle> _values = new ConditionalWeakTable<TKey, Handle>();

		/// <summary>Adds a key to the table.</summary>
		/// <param name="key">The key to add. <paramref name="key" /> represents the object to which the property is attached.</param>
		/// <param name="value">The key's property value.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="key" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException">
		/// <paramref name="key" /> already exists.</exception>
		public void Add(TKey key, TValue value)
			=> _values.Add(key, new Handle(value));

		/// <summary>Atomically searches for a specified key in the table and returns the corresponding value. If the key does not exist in the table, the method invokes the default constructor of the class that represents the table's value to create a value that is bound to the specified key. </summary>
		/// <returns>The value that corresponds to <paramref name="key" />, if <paramref name="key" /> already exists in the table; otherwise, a new value created by the default constructor of the class defined by the TValue generic type parameter.</returns>
		/// <param name="key">The key to search for. <paramref name="key" /> represents the object to which the property is attached.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="key" /> is null.</exception>
		/// <exception cref="T:System.MissingMethodException">In the .NET for Windows Store apps or the Portable Class Library, catch the base class exception, <see cref="T:System.MissingMemberException" />, instead.The class that represents the table's value does not define a default constructor.</exception>
		public TValue GetOrCreateValue(TKey key)
			=> _values.GetOrCreateValue(key)?.Value;

		/// <summary>Atomically searches for a specified key in the table and returns the corresponding value. If the key does not exist in the table, the method invokes a callback method to create a value that is bound to the specified key.</summary>
		/// <returns>The value attached to <paramref name="key" />, if <paramref name="key" /> already exists in the table; otherwise, the new value returned by the <paramref name="createValueCallback" /> delegate.</returns>
		/// <param name="key">The key to search for. <paramref name="key" /> represents the object to which the property is attached.</param>
		/// <param name="createValueCallback">A delegate to a method that can create a value for the given <paramref name="key" />. It has a single parameter of type TKey, and returns a value of type TValue.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="key" /> or <paramref name="createValueCallback" /> is null.</exception>
		public TValue GetValue(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
			=> _values.GetValue(key, k => new Handle(createValueCallback(k))).Value;

		/// <summary>Removes a key and its value from the table.</summary>
		/// <returns>true if the key is found and removed; otherwise, false.</returns>
		/// <param name="key">The key to remove. </param>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="key" /> is null.</exception>
		public bool Remove(TKey key)
			=> _values.Remove(key);

		/// <summary>Gets the value of the specified key.</summary>
		/// <returns>true if <paramref name="key" /> is found; otherwise, false.</returns>
		/// <param name="key">The key that represents an object with an attached property.</param>
		/// <param name="value">When this method returns, contains the attached property value. If <paramref name="key" /> is not found, <paramref name="value" /> contains the default value.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="key" /> is null.</exception>
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (_values.TryGetValue(key, out var handle))
			{
				value = handle.Value;
				return true;
			}
			else
			{
				value = default(TValue);
				return false;
			}
		}

		private class Handle
		{
			public Handle(TValue value) => Value = value;

			public TValue Value { get; }

			~Handle() => Value.Dispose();
		}
	}
}
