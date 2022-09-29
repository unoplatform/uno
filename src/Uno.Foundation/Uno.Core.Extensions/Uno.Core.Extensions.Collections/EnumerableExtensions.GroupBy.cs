#nullable disable

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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Uno.Extensions
{
	internal static partial class EnumerableExtensions
	{
		public static IEnumerable<IEnumerable<T>> GroupBy<T>(this IEnumerable<T> items, int itemsByGroup)
		{
			var enumerator = items.GetEnumerator();

			while (enumerator.MoveNext())
			{
				yield return Take(enumerator, enumerator.Current, itemsByGroup).ToList();
			}
		}

		private static IEnumerable<T> Take<T>(IEnumerator<T> enumerator, T first, int count)
		{
			yield return first;

			for (int i = 1; i < count && enumerator.MoveNext(); i++)
			{
				yield return enumerator.Current;
			}
		}

		private static readonly char[] _allowedGroupNames = new[] { '#', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
		public static IEnumerable<IBindableGrouping<string, T>> GroupAlphabetically<T>(this IEnumerable<T> items, Func<T, string> keySelector, bool includeEmptyGroups = true)
		{
			// Cannot yield return with GroupBy and OrderBy ... so classic foreach !
			var groups = _allowedGroupNames.ToDictionary(k => k, k => new BindableGroup<string, T>(k.ToString()));

			foreach (var item in items)
			{
				var character = keySelector(item) switch
				{
					string k when k.Length > 0 => k[0],
					_ => (char)0
				};

				var key = char.ToLowerInvariant(character);

				if (key == default(Char) || !_allowedGroupNames.Contains(key))
				{
					key = '#';
				}

				groups[key].Items.Add(item);
			}

			return includeEmptyGroups
				? groups.Values.Cast<IBindableGrouping<string, T>>()
				: groups.Values.Where(g => g.HasItems).Cast<IBindableGrouping<string, T>>();
		}


		public static IEnumerable<IBindableGrouping<TKey, TItem>> GroupBy<TKey, TItem>(this IEnumerable<TItem> items, params GroupDescriptor<TKey, TItem>[] descriptors)
		{
			// TODO: Check if this is the faster path !
			var groupedItems = items
				.GroupBy(item => descriptors.FirstOrDefault(descriptor => descriptor.Selector(item)) is { } descriptor ? descriptor.Key : default)
				.Select(group => new BindableGroup<TKey, TItem>(group.Key, group.ToArray()));

			var result = from descriptor in descriptors
			             join gItem in groupedItems on descriptor.Key equals gItem.Key into descrWithItems
			             from descrIncludingEmptyItems in descrWithItems.DefaultIfEmpty()
			             let groupItemsNoNull = descrIncludingEmptyItems ?? new BindableGroup<TKey, TItem>(descriptor.Key, Array.Empty<TItem>())
			             where descriptor.Required || groupItemsNoNull.Any()
			             select new BindableGroup<TKey, TItem>(descriptor.Key, groupItemsNoNull.ToArray());

			return result
				.OrderBy(group => group.Key, new GroupDescriptorComparer<TKey>(descriptors.Select(descriptor => descriptor.Key)))
				.Cast<IBindableGrouping<TKey, TItem>>();
		}
	}

	internal interface IBindableGrouping<TKey, TItem> : IGrouping<TKey, TItem>
	{
		bool HasItems { get; }
	}

	internal class BindableGroup<TKey, TItem> : IEnumerable<TItem>, IBindableGrouping<TKey, TItem>
	{
		public BindableGroup(TKey key)
			: this(key, new List<TItem>())
		{
		}

		public BindableGroup(TKey key, IList<TItem> items)
		{
			Key = key;
			Items = items;
		}

		public TKey Key { get; private set; }

		public IList<TItem> Items { get; private set; }

		public bool HasItems
		{
			get { return Items != null && Items.Count > 0; }
		}

		public IEnumerator<TItem> GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IGrouping<TKey, TItem>;

			return other != null
				&& Equals(Key, other.Key)
				&& this.SequenceEqual(other);
		}

		public override int GetHashCode()
		{
			return (Key?.GetHashCode() ?? 0)
				^ Items.Aggregate(0, (hash, i) => hash ^ i.GetHashCode());
		}
	}

	internal class GroupDescriptor<TKey, TItem>
	{
		public GroupDescriptor(TKey key)
			: this(key, item => true)
		{
		}

		public GroupDescriptor(TKey key, Func<TItem, bool> selector, bool required = false)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (selector == null)
				throw new ArgumentNullException("selector");

			Key = key;
			Selector = selector;
			Required = required;
		}

		public Func<TItem, bool> Selector { get; private set; }

		public TKey Key { get; private set; }

		/// <summary> Group need to be there even if group is empty </summary>
		public bool Required { get; private set; }
	}

	internal class GroupDescriptorComparer<TKey> : IComparer<TKey>
	{
		private readonly List<TKey> _keys;

		public GroupDescriptorComparer(IEnumerable<TKey> descriptors)
		{
			_keys = descriptors.ToList();
		}

		public int Compare(TKey x, TKey y)
		{
			var notContainsX = !_keys.Contains(x);
			var notContainsY = !_keys.Contains(y);

			if (notContainsX && notContainsY)
				return 0;

			if (notContainsX)
				return 1;

			if (notContainsY)
				return -1;

			return _keys.IndexOf(x) - _keys.IndexOf(y);
		}
	}

}
