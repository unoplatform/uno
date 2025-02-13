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

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Uno
{
	partial class Transactional
	{
		#region ImmutableDictionary
		/// <summary>
		/// Transactionally get or add an item to an ImmutableDictionary.  The factory is called to create the item when required.
		/// </summary>
		public static TValue GetOrAdd<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			Func<TKey, TValue> factory)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			var createdValue = default(TValue);
			var hasValue = false;

			while (true)
			{
				var capture = dictionary;
				TValue value;

				if (capture.TryGetValue(key, out value))
				{
					return value;
				}

				if (!hasValue)
				{
					createdValue = factory(key);
					hasValue = true;
				}

				var updated = (TDictionary)capture.Add(key, createdValue);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return createdValue; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally get or add an item to an ImmutableDictionary.  The factory is called to create the item when required.
		/// </summary>
		/// <remarks>This overload is used primarily to avoid creating a closure, which are expensive when running under Mono’s full AOT.</remarks>
		public static TValue GetOrAdd<TDictionary, TKey, TContext, TValue>(
			ref TDictionary dictionary,
			TKey key,
			TContext context,
			Func<TKey, TContext, TValue> factory)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			var createdValue = default(TValue);
			var hasValue = false;

			while (true)
			{
				var capture = dictionary;
				TValue value;

				if (capture.TryGetValue(key, out value))
				{
					return value;
				}

				if (!hasValue)
				{
					createdValue = factory(key, context);
					hasValue = true;
				}

				var updated = (TDictionary)capture.Add(key, createdValue);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return createdValue; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally add an item to an ImmutableDictionary if not already present.  The factory is called to create the item when required.
		/// </summary>
		public static bool TryAdd<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			Func<TKey, TValue> factory,
			out TValue value)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			var createdValue = default(TValue);
			var hasValue = false;

			while (true)
			{
				var capture = dictionary;

				if (capture.TryGetValue(key, out value))
				{
					return false;
				}

				if (!hasValue)
				{
					createdValue = factory(key);
					hasValue = true;
				}

				var updated = (TDictionary)capture.Add(key, createdValue);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					value = createdValue;
					return true; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally remove an item from an ImmutableDictionary if exists.
		/// </summary>
		public static bool TryRemove<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			out TValue value)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			while (true)
			{
				var capture = dictionary;
				if (!capture.TryGetValue(key, out value))
				{
					return false;
				}

				var updated = (TDictionary)capture.Remove(key);
				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return true; // successfully updated the dictionary
				}
			}
		}


		/// <summary>
		/// Transactionally remove an item from a list.
		/// </summary>
		/// <returns>True if the item was in the list, false else.</returns>
		public static int Remove<TDictionary, TKey, TValue>(ref TDictionary list, Func<KeyValuePair<TKey, TValue>, bool> removeSelector)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			while (true)
			{
				var removedCount = 0;
				var capture = list;
				var updated = capture;

				foreach (var item in capture)
				{
					if (removeSelector(item))
					{
						updated = (TDictionary)updated.Remove(item.Key);
						removedCount++;
					}
				}

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return removedCount;
				}
			}
		}

		/// <summary>
		/// Transactionally set an item of an ImmutableDictionary.
		/// </summary>
		public static TValue SetItem<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			TValue value)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			while (true)
			{
				var capture = dictionary;
				var updated = (TDictionary)capture.SetItem(key, value);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return value; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally set an item of an ImmutableDictionary.
		/// </summary>
		public static TValue SetItem<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			Func<TKey, TValue> factory)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			var value = factory(key);

			while (true)
			{
				var capture = dictionary;
				var updated = (TDictionary)capture.SetItem(key, value);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return value; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally update an item of an ImmutableDictionary, *but only if it already exists in the dictionary*.
		/// The factory is called to create the item when required, and may be invoked multiple times.
		/// </summary>
		public static bool TryUpdateItem<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			TValue value)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			while (true)
			{
				var capture = dictionary;
				if (!capture.ContainsKey(key))
				{
					return false;
				}

				var updated = (TDictionary)capture.SetItem(key, value);
				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Transactionally update an item of an ImmutableDictionary, *but only if it already exists in the dictionary*.
		/// The factory is called to create the item when required, and may be invoked multiple times.
		/// </summary>
		public static bool TryUpdateItem<TKey, TValue>(
			ref IImmutableDictionary<TKey, TValue> dictionary,
			TKey key,
			Func<TKey, TValue, TValue> factory)
		{
			while (true)
			{
				var capture = dictionary;
				if (!capture.TryGetValue(key, out TValue currentValue))
				{
					return false;
				}

				var updated = capture.SetItem(key, factory(key, currentValue));
				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Transactionally update an item of an ImmutableDictionary, *but only if it already exists in the dictionary*.
		/// The factory is called to create the item when required, and may be invoked multiple times.
		/// </summary>
		public static bool TryUpdateItem<TKey, TValue>(
			ref ImmutableDictionary<TKey, TValue> dictionary,
			TKey key,
			Func<TKey, TValue, TValue> factory)
		{
			while (true)
			{
				var capture = dictionary;
				if (!capture.TryGetValue(key, out TValue currentValue))
				{
					return false;
				}

				var updated = capture.SetItem(key, factory(key, currentValue));
				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Transactionally update an item of an ImmutableDictionary, *but only if it already exists in the dictionary*.
		/// The factory is called to create the item when required, and may be invoked multiple times.
		/// </summary>
		public static bool TryUpdateItem<TKey, TValue>(
			ref ImmutableSortedDictionary<TKey, TValue> dictionary,
			TKey key,
			Func<TKey, TValue, TValue> factory)
		{
			while (true)
			{
				var capture = dictionary;
				if (!capture.TryGetValue(key, out TValue currentValue))
				{
					return false;
				}

				var updated = capture.SetItem(key, factory(key, currentValue));
				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return true;
				}
			}
		}


		/// <summary>
		/// Transactionally update an item of an ImmutableDictionary. 
		/// The factory is called to update the item, and may be invoked multiple times.
		/// </summary>
		public static TValue UpdateItem<TKey, TValue>(
			ref IImmutableDictionary<TKey, TValue> dictionary,
			TKey key,
			Func<TKey, TValue, TValue> factory)
		{
			while (true)
			{
				var capture = dictionary;

				var capturedValue = capture.GetValueOrDefault(key);
				var updatedValue = factory(key, capturedValue);
				if (object.ReferenceEquals(capturedValue, updatedValue))
				{
					return capturedValue;
				}

				var updated = capture.SetItem(key, updatedValue);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return updatedValue; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally get or add an item to an ImmutableDictionary.  The factory is called to create the item when required.
		/// The factory is called to update, and may be invoked multiple times.
		/// </summary>
		public static TValue UpdateItem<TKey, TValue>(
			ref ImmutableDictionary<TKey, TValue> dictionary,
			TKey key,
			Func<TKey, TValue, TValue> factory)
		{
			while (true)
			{
				var capture = dictionary;

				var capturedValue = ((IImmutableDictionary<TKey, TValue>)capture).GetValueOrDefault(key);
				var updatedValue = factory(key, capturedValue);
				if (object.ReferenceEquals(capturedValue, updatedValue))
				{
					return capturedValue;
				}

				var updated = capture.SetItem(key, updatedValue);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return updatedValue; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally update an item of an ImmutableDictionary.
		/// The factory is called to update, and may be invoked multiple times.
		/// </summary>
		public static TValue UpdateItem<TKey, TValue>(
			ref ImmutableSortedDictionary<TKey, TValue> dictionary,
			TKey key,
			Func<TKey, TValue, TValue> factory)
		{
			while (true)
			{
				var capture = dictionary;

				var capturedValue = ((IImmutableDictionary<TKey, TValue>)capture).GetValueOrDefault(key);
				var updatedValue = factory(key, capturedValue);
				if (object.ReferenceEquals(capturedValue, updatedValue))
				{
					return capturedValue;
				}

				var updated = capture.SetItem(key, updatedValue);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return updatedValue; // successfully updated the dictionary
				}
			}
		}
		#endregion

		#region ImmutableQueue
		/// <summary>
		/// Transactionally enqueue and item into an ImmutableQueue
		/// </summary>
		public static void Enqueue<TQueue, T>(ref TQueue queue, T value)
			where TQueue : class, IImmutableQueue<T>
		{
			while (true)
			{
				var capture = queue;

				var updated = (TQueue)capture.Enqueue(value);

				if (Interlocked.CompareExchange(ref queue, updated, capture) == capture)
				{
					return; // successfully updated the queue
				}
			}
		}

		/// <summary>
		/// Transactionally enqueue and item into an ImmutableQueue
		/// </summary>
		public static T Enqueue<TQueue, T>(ref TQueue queue, Func<TQueue, T> valueFactory)
			where TQueue : class, IImmutableQueue<T>
		{
			while (true)
			{
				var capture = queue;

				var value = valueFactory(capture);
				var updated = (TQueue)capture.Enqueue(value);

				if (Interlocked.CompareExchange(ref queue, updated, capture) == capture)
				{
					return value; // successfully updated the queue
				}
			}
		}

		/// <summary>
		/// Transactionally dequeue an item from a queue.
		/// </summary>
		/// <returns>true if successful, false means queue was empty</returns>
		public static bool TryDequeue<TQueue, T>(ref TQueue queue, out T value)
			where TQueue : class, IImmutableQueue<T>
		{
			while (true)
			{
				var capture = queue;

				if (!capture.Any())
				{
					value = default(T);
					return false;
				}

				T output;
				var updated = (TQueue)capture.Dequeue(out output);

				if (Interlocked.CompareExchange(ref queue, updated, capture) == capture)
				{
					value = output;
					return true; // successfully updated the queue
				}
			}
		}

		/// <summary>
		/// Transactionally dequeue an item from a queue. An exception is thrown if queue is empty.
		/// </summary>
		/// <returns>dequeued item</returns>
		public static T Dequeue<TQueue, T>(ref TQueue queue)
			where TQueue : class, IImmutableQueue<T>
		{
			while (true)
			{
				var capture = queue;

				T output;
				var updated = (TQueue)capture.Dequeue(out output);

				if (Interlocked.CompareExchange(ref queue, updated, capture) == capture)
				{
					return output; // successfully updated the queue
				}
			}
		}
		#endregion

		#region ImmutableList

		/// <summary>
		/// Transactionally add an item to a list.
		/// </summary>
		public static TList Add<TList, T>(ref TList list, T item)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				var updated = (TList)capture.Add(item);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return updated;
				}
			}
		}

		/// <summary>
		/// Transactionally add an item to a list if not already present.
		/// </summary>
		public static TList AddDistinct<TList, T>(ref TList list, T item)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				if (capture.IndexOf(item) >= 0)
				{
					return capture;
				}

				var updated = (TList)capture.Add(item);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return updated;
				}
			}
		}

		/// <summary>
		/// Transactionally add an item to a list if not already present.
		/// </summary>
		public static TList AddDistinct<TList, T>(ref TList list, T item, IEqualityComparer<T> comparer)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				if (capture.IndexOf(item, comparer) >= 0)
				{
					return capture;
				}

				var updated = (TList)capture.Add(item);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return updated;
				}
			}
		}

		/// <summary>
		/// Transactionally try to add an item to a list if not already present.
		/// </summary>
		/// <returns>True if item was added, false if item was already present</returns>
		public static bool TryAddDistinct<TList, T>(ref TList list, T item)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				if (capture.IndexOf(item) >= 0)
				{
					return false;
				}

				var updated = (TList)capture.Add(item);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Transactionally try to add an item to a list if not already present.
		/// </summary>
		/// <returns>True if item was added, false if item was already present</returns>
		public static bool TryAddDistinct<TList, T>(ref TList list, T item, IEqualityComparer<T> comparer)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				if (capture.IndexOf(item, comparer) >= 0)
				{
					return false;
				}

				var updated = (TList)capture.Add(item);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Transactionally remove an item from a list.
		/// </summary>
		/// <returns>True if the item was in the list, false else.</returns>
		public static bool Remove<TList, T>(ref TList list, T item)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				var updated = (TList)capture.Remove(item);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return capture.Contains(item);
				}
			}
		}

		/// <summary>
		/// Remove item(s) from an immutable list using a selector.
		/// </summary>
		/// <returns>True if the item was in the list, false else.</returns>
		public static int Remove<TList, T>(ref TList list, Func<T, bool> removeSelector)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var removedCount = 0;
				var capture = list;
				var updated = capture;

				foreach (var item in capture)
				{
					if (removeSelector(item))
					{
						updated = (TList)updated.Remove(item);
						removedCount++;
					}
				}

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return removedCount;
				}
			}
		}

		/// <summary>
		/// Transactionally remove the specified items from a list.
		/// </summary>
		/// <returns>Number of items which were effectively removed from the list.</returns>
		public static int RemoveRange<TList, T>(ref TList list, T[] items)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				var updated = (TList)capture.RemoveRange(items);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return capture.Count - updated.Count;
				}
			}
		}

		/// <summary>
		/// Transactionally remove the specified items from a list.
		/// </summary>
		/// <param name="removedItems">Items which were effectively removed from the list.</param>
		/// <returns>Number of items which were effectively removed from the list.</returns>
		public static int RemoveRange<TList, T>(ref TList list, T[] items, out IEnumerable<T> removedItems)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				var updated = (TList)capture.RemoveRange(items);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					var removed = capture.Count - updated.Count;
					removedItems = removed > 0
						? items.Where(capture.Contains)
						: Enumerable.Empty<T>();
					return removed;
				}
			}
		}
		#endregion
	}
}
