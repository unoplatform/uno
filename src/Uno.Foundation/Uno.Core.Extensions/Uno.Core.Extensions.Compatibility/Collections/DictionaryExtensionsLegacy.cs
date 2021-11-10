using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Collections;
using Uno.Extensions;
using Uno.Threading;

namespace Uno.Core.Collections
{
	public static class DictionaryExtensionsLegacy
	{
		public static TValue FindOrCreate<TKey, TValue>(this SynchronizedDictionary<TKey, TValue> items, TKey key, Func<TValue> factory)
		{
			TValue value = default(TValue);

			using (items.Lock.CreateWriterScope())
			{
				if (!items.TryGetValue(key, out value))
				{
					items.Add(key, value = factory());
				}

				return value;
			}
		}

		public static TValue FindOrCreate<TKey, TValue>(this ISynchronizable<IDictionary<TKey, TValue>> items, TKey key, Func<TValue> factory)
		{
			TValue value = default(TValue);

			items.Lock.Write(d => d.TryGetValue(key, out value), d => d.Add(key, value = factory()));

			return value;
		}

		public static KeyValuePair<TKey, TValue>[] ToArrayLocked<TKey, TValue>(this ISynchronizable<IDictionary<TKey, TValue>> items)
		{
			return items.Lock.Read(d => d.ToArray());
		}
	}
}
