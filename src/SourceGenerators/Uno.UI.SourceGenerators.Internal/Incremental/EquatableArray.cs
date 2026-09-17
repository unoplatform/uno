#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Uno.UI.SourceGenerators.Internal.Incremental;

/// <summary>
/// An immutable array with value equality, so incremental pipeline models can hold collections and still compare equal.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
	where T : IEquatable<T>
{
	private readonly ImmutableArray<T> _array;

	public EquatableArray(ImmutableArray<T> array)
	{
		_array = array;
	}

	public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

	public int Count => _array.IsDefault ? 0 : _array.Length;

	public T this[int index] => _array[index];

	public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

	public bool Equals(EquatableArray<T> other)
	{
		var left = AsImmutableArray();
		var right = other.AsImmutableArray();

		if (left.Length != right.Length)
		{
			return false;
		}

		for (var i = 0; i < left.Length; i++)
		{
			if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
			{
				return false;
			}
		}

		return true;
	}

	public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

	public override int GetHashCode()
	{
		var hash = 17;
		foreach (var item in AsImmutableArray())
		{
			hash = unchecked((hash * 31) + (item is null ? 0 : item.GetHashCode()));
		}

		return hash;
	}

	public ImmutableArray<T>.Enumerator GetEnumerator() => AsImmutableArray().GetEnumerator();

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)AsImmutableArray()).GetEnumerator();

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

	public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);
}

internal static class EquatableArray
{
	public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> items)
		where T : IEquatable<T>
		=> new(ImmutableArray.CreateRange(items));
}
