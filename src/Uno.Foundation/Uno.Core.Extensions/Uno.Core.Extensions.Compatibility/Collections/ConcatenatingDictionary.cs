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
using System.Text;
using Uno.Extensions;

namespace Uno.Collections
{
	/// <summary>
	/// This is a tricky IDictionary<string, string> which allows setting a key more than once, by concatenating previous values 
	/// with the newly added value, separated by the provided separator char. All properties, like <seealso cref="Keys"/>, 
	/// <seealso cref="Count"/>, <seealso cref="Values"/> or enumerating <seealso cref="KeyValuePair{TKey, TValue}"/>
	/// reflects the values, with possible repeated keys. This is required since a few Linq operators use Count and CopyTo
	/// for optimisations.
	/// </summary>
    internal class ConcatenatingDictionary : IDictionary<string, string>
    {
		private readonly char _separator;
		private readonly Dictionary<string, string> _innerDictionary;

		public ConcatenatingDictionary(char separator)
		{
			_separator = separator;
			_innerDictionary = new Dictionary<string, string>();
		}

		public ConcatenatingDictionary(char separator, int capacity)
		{
			_separator = separator;
			_innerDictionary = new Dictionary<string, string>(capacity);
		}

		public ConcatenatingDictionary(char separator, IEqualityComparer<string> comparer)
		{
			_separator = separator;
			_innerDictionary = new Dictionary<string, string>(comparer);
		}

		public void Add(string key, string value)
		{
			this[key] = value;
		}

		public bool ContainsKey(string key)
		{
			return _innerDictionary.ContainsKey(key);
		}

		public ICollection<string> Keys
		{
			get 
			{ 
				return _innerDictionary
					.SelectMany(pair => pair.Value.Split(_separator).Select(_ => pair.Key))
					.ToList()
					.AsReadOnly(); 
			}
		}

		public bool Remove(string key)
		{
			return _innerDictionary.Remove(key);
		}

		public bool TryGetValue(string key, out string value)
		{
			if(_innerDictionary.TryGetValue(key, out value))
			{
				if(value.Contains(_separator))
				{
					throw new NotSupportedException("This key contains multiple values. Use TryGetValues when this can happen.");
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetValues(string key, out string[] values)
		{
			string value = null;

			if(_innerDictionary.TryGetValue(key, out value))
			{
				values = value.Split(_separator);
				return true;
			}
			else
			{
				values = new string[0];
				return false;
			}
		}

		public ICollection<string> Values
		{
			get 
			{ 
				return _innerDictionary
					.Values
					.SelectMany(value => value.Split(_separator))
					.ToList()
					.AsReadOnly(); 
			}
		}

		public string this[string key]
		{
			get
			{
				var value = _innerDictionary[key];

				return value;
			}
			set
			{
				string existing;

				if(_innerDictionary.TryGetValue(key, out existing))
				{
					_innerDictionary[key] = (existing + _separator + value ?? "").Trim(_separator);
				}
				else
				{
					_innerDictionary[key] = value;
				}
			}
		}

		public void Add(KeyValuePair<string, string> item)
		{
			this[item.Key] = item.Value;
		}

		public void Clear()
		{
			_innerDictionary.Clear();
		}

		public bool Contains(KeyValuePair<string, string> item)
		{
			return _innerDictionary.Contains(item);
		}

		void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
		{
			// This enumerates split values.
			var flatPairs = this.GetFlatContent();

			flatPairs.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get 
			{
				// For Count, CopyTo and GetEnumerator, we cannot rely on other methods
				// to avoid reentrency (bypass Linq optimisations).
				return this
					.GetFlatContent()
					.Length;
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
		{
			return (_innerDictionary as ICollection<KeyValuePair<string, string>>).Remove(item);
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return this
				.GetFlatContent()
				.AsEnumerable()
				.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		private KeyValuePair<string, string>[] GetFlatContent()
		{
			return _innerDictionary
				.SelectMany(pair => pair
					.Value
					.SelectOrDefault(value => 
						value
							.Split(_separator)
							.Select(v => new KeyValuePair<string, string>(pair.Key, v)),
						new [] { pair }))
				.ToArray();
		}
	}
}
