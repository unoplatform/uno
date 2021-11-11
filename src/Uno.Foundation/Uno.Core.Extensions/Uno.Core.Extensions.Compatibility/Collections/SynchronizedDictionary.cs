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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Decorator;
using Uno.Extensions;
using Uno.Threading;
using System;

namespace Uno.Collections
{
	internal class SynchronizedDictionary<TKey, TValue> : Decorator<IDictionary<TKey, TValue>>, IDictionary<TKey, TValue>,
		ISynchronizable<IDictionary<TKey, TValue>>
	{
		private readonly Synchronizable<IDictionary<TKey, TValue>> target;

		public SynchronizedDictionary(IDictionary<TKey, TValue> target)
			: base(target)
		{
			this.target = new Synchronizable<IDictionary<TKey, TValue>>(target);
		}

		public SynchronizedDictionary()
			: this(new Dictionary<TKey, TValue>())
		{
		}

		public SynchronizedDictionary(IEqualityComparer<TKey> equalityComparer)
			: this(new Dictionary<TKey, TValue>(equalityComparer))
		{
		}


		#region IDictionary<TKey,TValue> Members

		public void Add(TKey key, TValue value)
		{
			using (Lock.CreateWriterScope())
			{
				Target.Add(key, value);
			}
		}

		public bool ContainsKey(TKey key)
		{
			using (Lock.CreateReaderScope())
			{
				return Target.ContainsKey(key);
			}
		}

		public ICollection<TKey> Keys
		{
			get
			{
				using (Lock.CreateReaderScope())
				{
					return Target.Keys.ToList();
				}
			}
		}

		public bool Remove(TKey key)
		{
			using (Lock.CreateWriterScope())
			{
				return Target.Remove(key);
			}
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			TValue tempValue = default(TValue);

			using (Lock.CreateReaderScope())
			{
				bool found = Target.TryGetValue(key, out tempValue);

				value = tempValue;

				return found;
			}
		}

		public ICollection<TValue> Values
		{
			get
			{
				using (Lock.CreateReaderScope())
				{
					return Target.Values.ToList();
				}
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				using (Lock.CreateReaderScope())
				{
					return Target[key];
				}
			}
			set
			{
				using (Lock.CreateWriterScope())
				{
					Target[key] = value;
				}
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			using (Lock.CreateWriterScope())
			{
				Target.Add(item);
			}
		}

		public void Clear()
		{
			using (Lock.CreateWriterScope())
			{
				Target.Clear();
			}
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			using (Lock.CreateReaderScope())
			{
				return Target.Contains(item);
			}
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			using (Lock.CreateReaderScope())
			{
				Target.CopyTo(array, arrayIndex);
			}
		}

		public int Count
		{
			get
			{
				using (Lock.CreateReaderScope())
				{
					return Target.Count;
				}
			}
		}

		public bool IsReadOnly
		{
			get
			{
				using (Lock.CreateReaderScope())
				{
					return Target.IsReadOnly;
				}
			}
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			using (Lock.CreateWriterScope())
			{
				return Target.Remove(item);
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			using (Lock.CreateReaderScope())
			{
				return Target.ToList().GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ISynchronizable<IDictionary<TKey,TValue>> Members

		public ISynchronizableLock<IDictionary<TKey, TValue>> Lock
		{
			get { return target.Lock; }
		}

		#endregion

		public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
		{
			TValue value;
			using (Lock.CreateWriterScope())
			{
				if (!Target.TryGetValue(key, out value))
				{
					Target.Add(key, value = factory(key));
				}
			}

			return value;
		}

		public TValue GetOrAdd(TKey key, TValue newValue)
		{
			TValue value;
			using (Lock.CreateWriterScope())
			{
				if (!Target.TryGetValue(key, out value))
				{
					Target.Add(key, newValue);
					return newValue;
				}
			}

			return value;
		}

		public bool TryAdd(TKey key, TValue value)
		{
			using (Lock.CreateWriterScope())
			{
				if (Target.ContainsKey(key))
				{
					return false;
				}
				else
				{
					Target.Add(key, value);

					return true;
				}
			}
		}

		public bool TryRemove(TKey key, out TValue value)
		{
			using (Lock.CreateWriterScope())
			{
				return Target.TryGetValue(key, out value) 
					&& Target.Remove(key);
			}
		}
	}
}