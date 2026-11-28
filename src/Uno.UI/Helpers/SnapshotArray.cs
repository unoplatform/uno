#nullable enable

using System;
using System.Threading;

namespace Uno.UI.Helpers;

/// <summary>
/// A list kept in a plain array field that appends in place, and that a loop reads as a snapshot: the items of the
/// moment it read the field, in order, whatever its callbacks add or remove.
/// </summary>
/// <remarks>
/// <para>
/// The items are never null and the slots after them always are, so the count is where the nulls start. An append
/// fills the first null slot, which no earlier reader counted, or publishes a larger copy; a removal publishes a new
/// array, so an array never changes below the count a reader found in it. Appending N items costs O(N) where a copy
/// per append costs O(N²), and reading allocates nothing.
/// </para>
/// <para>
/// The owner's thread only, like the rest of a <see cref="Microsoft.UI.Xaml.DependencyObject"/>'s store: appends and
/// removals aren't synchronized, and a loop on another thread during an in-place append isn't supported (the immutable
/// list this replaces allowed such a reader; nothing uses it).
/// </para>
/// <para>
/// A type of its own so that the snapshot rule can be unit tested apart from the store.
/// </para>
/// </remarks>
internal static class SnapshotArray
{
	// Up to this many items the array is exact, which costs a small parent no more than the copy per append it
	// replaces; past it the array doubles, and a removal keeps one spare slot, so that removing an item and adding
	// one (a list recycling its containers) fills that slot instead of growing.
	private const int ExactUpTo = 8;

	/// <summary>The number of items in <paramref name="items"/>: read the field once, then loop to this count.</summary>
	public static int CountOf<T>(T?[] items) where T : class
	{
		var length = items.Length;
		if (length == 0 || items[length - 1] is not null)
		{
			return length;
		}

		// The first null slot, after the items.
		int low = 0, high = length - 1;
		while (low < high)
		{
			var middle = (low + high) >>> 1;
			if (items[middle] is null)
			{
				high = middle;
			}
			else
			{
				low = middle + 1;
			}
		}

		return low;
	}

	public static void Add<T>(ref T?[] field, T item) where T : class
	{
		var items = field;
		var count = CountOf(items);
		if (count < items.Length)
		{
			items[count] = item;
			return;
		}

		var grown = new T?[count < ExactUpTo ? count + 1 : count * 2];
		Array.Copy(items, grown, count);
		grown[count] = item;
		Volatile.Write(ref field, grown);
	}

	// Removes the first occurrence: an item is added once, since a child registers with its parent only while it
	// isn't registered (DependencyObject.TryRegisterInheritedProperties).
	public static void Remove<T>(ref T?[] field, T item) where T : class
	{
		var items = field;
		var count = CountOf(items);
		var index = Array.IndexOf(items, item, 0, count);
		if (index < 0)
		{
			return;
		}

		if (count == 1)
		{
			Volatile.Write(ref field, Array.Empty<T?>());
			return;
		}

		var copy = new T?[count > ExactUpTo ? count : count - 1];
		Array.Copy(items, copy, index);
		Array.Copy(items, index + 1, copy, index, count - index - 1);
		Volatile.Write(ref field, copy);
	}
}
