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
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Uno
{
	/// <summary>
	/// A <see cref="ConditionalWeakTable{TKey,TValue}"/> dedicated to delegates.
	/// <remarks>
	/// This helps to attach values to a delegate, but be aware that you may have **lots** of instances of the _same_ delegate (i.e. target + method).
	/// This should be used **only** for performance consideration, and you must assume that you may have multiple attached values 
	/// for the _same_ delegate.
	/// </remarks>
	/// </summary>
	/// <typeparam name="TValue">The field's type. This must be a reference type.</typeparam>
	public class DelegateConditionalWeakTable<TValue>
		where TValue : class
	{
		/*
		*  We don't want to weak attach a result directly to the delegate, because the runtime creates a new delegate instance 
		*  almost every times you use it, even if the delagate is built from a static method (the only exception is if you
		*  capture it into a variable).
		*  
		*  The goal is to be able to share the value for multiple instances of the same delegate.
		*  
		*  Based on https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/Delegate.cs:
		*  	- Target is already available (may be null if delegate was created from a static method) => we can use it
		*  	- Method is intialized lazily on get and involves reflection => so we don't want to use it.
		*  	- GetHashCode() always returns the Target.GetType().GetHasCode() => it's use less
		*  	- Equals() use internal stuff to properly determines equality without using reflection
		*  
		*  A good compromise for performance is to attach values to the Target and then use the Equals() (a.k.a. Mode.Performance)
		*  The strong mode will also attach values to the target, but will also validate the method.
		*  
		*/

		private readonly object _staticTarget = new object();
		private readonly ConditionalWeakTable<object, ValuesStore> _stores = new ConditionalWeakTable<object, ValuesStore>();

		/// <summary>
		/// Atomically searches for a specified key in the table and returns the corresponding value. 
		/// If the key does not exist in the table, the method invokes a callback method to create a value that is bound to the specified key.
		/// </summary>
		/// <param name="key">The key to search for. key represents the object to which the property is attached.</param>
		/// <param name="factory">A delegate to a method that can create a value for the given key. It has a single parameter of type TKey, and returns a value of type TValue.</param>
		/// <returns>The value attached to key, if key already exists in the table; otherwise, the new value returned by the createValueCallback delegate.</returns>
		public TValue GetValue(Delegate key, Func<Delegate, TValue> factory)
		{

			return _stores
				.GetValue(key.Target ?? _staticTarget, _ => new ValuesStore())
				.GetValue(key, factory);
		}

		/// <summary>
		/// Gets the value of the specified key.
		/// </summary>
		/// <param name="key">The key that represents an object with an attached property.</param>
		/// <param name="value">When this method returns, contains the attached property value. If key is not found, value contains the default value.</param>
		/// <returns>true if key is found; otherwise, false.</returns>
		public bool TryGetValue(Delegate key, out TValue value)
		{
			ValuesStore store;
			if (_stores.TryGetValue(key, out store))
			{
				return store.TryGetValue(key, out value);
			}
			else
			{
				value = default(TValue);
				return false;
			}
		}

		/// <summary>
		/// Removes a key and its value from the table.
		/// </summary>
		/// <param name="key">The key to remove.</param>
		/// <returns>rue if the key is found and removed; otherwise, false.</returns>
		public bool Remove(Delegate key)
		{
			ValuesStore store;
			return _stores.TryGetValue(key, out store)
				   && store.Remove(key);
		}

		private class ValuesStore
		{
			private ImmutableDictionary<Delegate, TValue> _values = ImmutableDictionary<Delegate, TValue>.Empty.WithComparers(EqualityComparer<Delegate>.Default);

			public TValue GetValue(Delegate owner, Func<Delegate, TValue> factory)
				=> Transactional.GetOrAdd(ref _values, owner, factory);

			public bool TryGetValue(Delegate owner, out TValue value)
				=> _values.TryGetValue(owner, out value);

			public bool Remove(Delegate owner)
			{
				TValue _;
				return Transactional.TryRemove(ref _values, owner, out _);
			}
		}
	}
}
