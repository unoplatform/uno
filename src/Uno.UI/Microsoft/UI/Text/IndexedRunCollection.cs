#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.UI.Text;

internal sealed class IndexedRunCollection<T> : IReadOnlyList<T>
	where T : class
{
	private readonly Func<T, int> _getLength;
	private readonly Action<T, int> _setLength;
	private Node? _root;
	private uint _prioritySequence;

	internal IndexedRunCollection(Func<T, int> getLength, Action<T, int> setLength)
	{
		_getLength = getLength;
		_setLength = setLength;
	}

	public int Count => GetCount(_root);

	internal int TotalLength => GetLength(_root);

	internal int TreeHeight => GetHeight(_root);

	public T this[int index] => GetNode(index).Item;

	internal void Reset(IReadOnlyList<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		_root = null;
		_prioritySequence = 0;
		for (var i = 0; i < items.Count; i++)
		{
			_root = Merge(_root, CreateNode(items[i]));
		}
		SetParent(_root, null);
		AssertInvariants();
	}

	internal int FindIndex(int position)
	{
		if ((uint)position >= (uint)TotalLength)
		{
			throw new ArgumentOutOfRangeException(nameof(position));
		}

		var node = _root;
		var index = 0;
		while (node is not null)
		{
			var leftLength = GetLength(node.Left);
			if (position < leftLength)
			{
				node = node.Left;
			}
			else if (position < leftLength + node.ItemLength)
			{
				return index + GetCount(node.Left);
			}
			else
			{
				position -= leftLength + node.ItemLength;
				index += GetCount(node.Left) + 1;
				node = node.Right;
			}
		}

		throw new InvalidOperationException("The run position is not covered by the index.");
	}

	internal int GetStart(int index)
	{
		var node = GetNode(index);
		var start = GetLength(node.Left);
		while (node.Parent is { } parent)
		{
			if (ReferenceEquals(node, parent.Right))
			{
				start = checked(start + GetLength(parent.Left) + parent.ItemLength);
			}
			node = parent;
		}
		return start;
	}

	internal int GetEnd(int index)
	{
		var node = GetNode(index);
		return checked(GetStart(node) + node.ItemLength);
	}

	internal void ReplaceRange(int index, int count, IReadOnlyList<T> replacement)
	{
		ArgumentNullException.ThrowIfNull(replacement);
		ValidateRange(index, count);
		Split(_root, index, out var left, out var tail);
		Split(tail, count, out _, out var right);

		Node? inserted = null;
		for (var i = 0; i < replacement.Count; i++)
		{
			inserted = Merge(inserted, CreateNode(replacement[i]));
		}

		_root = Merge(Merge(left, inserted), right);
		SetParent(_root, null);
		AssertInvariants();
	}

	internal void RemoveAt(int index) => ReplaceRange(index, 1, Array.Empty<T>());

	internal void SetLength(int index, int length)
	{
		if (length <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(length));
		}

		var node = GetNode(index);
		_setLength(node.Item, length);
		node.ItemLength = length;
		UpdateToRoot(node);
		AssertInvariants();
	}

	internal Cursor GetCursor(int index)
	{
		if ((uint)index > (uint)Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		if (index == Count)
		{
			return default;
		}

		var node = GetNode(index);
		return new Cursor(node, GetStart(node));
	}

	internal bool AreInvariantsValid()
		=> ValidateNode(_root, null, out _, out _);

	public IEnumerator<T> GetEnumerator()
	{
		var stack = new Stack<Node>();
		var node = _root;
		while (node is not null || stack.Count > 0)
		{
			while (node is not null)
			{
				stack.Push(node);
				node = node.Left;
			}

			node = stack.Pop();
			yield return node.Item;
			node = node.Right;
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private Node CreateNode(T item)
	{
		var length = _getLength(item);
		if (length <= 0)
		{
			throw new ArgumentException("Run lengths must be positive.", nameof(item));
		}
		return new Node(item, length, NextPriority());
	}

	private Node GetNode(int index)
	{
		if ((uint)index >= (uint)Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		var node = _root;
		while (node is not null)
		{
			var leftCount = GetCount(node.Left);
			if (index < leftCount)
			{
				node = node.Left;
			}
			else if (index == leftCount)
			{
				return node;
			}
			else
			{
				index -= leftCount + 1;
				node = node.Right;
			}
		}

		throw new InvalidOperationException("The run index is inconsistent.");
	}

	private static int GetStart(Node node)
	{
		var start = GetLength(node.Left);
		while (node.Parent is { } parent)
		{
			if (ReferenceEquals(node, parent.Right))
			{
				start = checked(start + GetLength(parent.Left) + parent.ItemLength);
			}
			node = parent;
		}
		return start;
	}

	private void Split(Node? node, int count, out Node? left, out Node? right)
	{
		if (node is null)
		{
			left = null;
			right = null;
			return;
		}

		var leftCount = GetCount(node.Left);
		if (count <= leftCount)
		{
			Split(node.Left, count, out left, out var splitRight);
			node.Left = splitRight;
			SetParent(splitRight, node);
			Update(node);
			right = node;
		}
		else
		{
			Split(node.Right, count - leftCount - 1, out var splitLeft, out right);
			node.Right = splitLeft;
			SetParent(splitLeft, node);
			Update(node);
			left = node;
		}

		SetParent(left, null);
		SetParent(right, null);
	}

	private static Node? Merge(Node? left, Node? right)
	{
		if (left is null)
		{
			SetParent(right, null);
			return right;
		}
		if (right is null)
		{
			SetParent(left, null);
			return left;
		}

		if (left.Priority <= right.Priority)
		{
			left.Right = Merge(left.Right, right);
			SetParent(left.Right, left);
			Update(left);
			return left;
		}

		right.Left = Merge(left, right.Left);
		SetParent(right.Left, right);
		Update(right);
		return right;
	}

	private static Node? GetSuccessor(Node node)
	{
		if (node.Right is { } right)
		{
			while (right.Left is { } left)
			{
				right = left;
			}
			return right;
		}

		while (node.Parent is { } parent)
		{
			if (ReferenceEquals(node, parent.Left))
			{
				return parent;
			}
			node = parent;
		}
		return null;
	}

	private static void UpdateToRoot(Node node)
	{
		for (Node? current = node; current is not null; current = current.Parent)
		{
			Update(current);
		}
	}

	private static void Update(Node node)
	{
		node.SubtreeCount = checked(GetCount(node.Left) + 1 + GetCount(node.Right));
		node.SubtreeLength = checked(GetLength(node.Left) + node.ItemLength + GetLength(node.Right));
	}

	private static void SetParent(Node? node, Node? parent)
	{
		if (node is not null)
		{
			node.Parent = parent;
		}
	}

	private static int GetCount(Node? node) => node?.SubtreeCount ?? 0;

	private static int GetLength(Node? node) => node?.SubtreeLength ?? 0;

	private static int GetHeight(Node? node)
		=> node is null ? 0 : 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));

	private uint NextPriority()
	{
		var value = ++_prioritySequence;
		value ^= value >> 16;
		value *= 0x7feb352d;
		value ^= value >> 15;
		value *= 0x846ca68b;
		value ^= value >> 16;
		return value;
	}

	private void ValidateRange(int index, int count)
	{
		if ((uint)index > (uint)Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		if (count < 0 || count > Count - index)
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}
	}

	private bool ValidateNode(Node? node, Node? parent, out int count, out int length)
	{
		if (node is null)
		{
			count = 0;
			length = 0;
			return true;
		}

		if (!ReferenceEquals(node.Parent, parent)
			|| node.ItemLength <= 0
			|| node.ItemLength != _getLength(node.Item)
			|| node.Left is not null && node.Left.Priority < node.Priority
			|| node.Right is not null && node.Right.Priority < node.Priority
			|| !ValidateNode(node.Left, node, out var leftCount, out var leftLength)
			|| !ValidateNode(node.Right, node, out var rightCount, out var rightLength))
		{
			count = 0;
			length = 0;
			return false;
		}

		count = checked(leftCount + 1 + rightCount);
		length = checked(leftLength + node.ItemLength + rightLength);
		return node.SubtreeCount == count && node.SubtreeLength == length;
	}

	[Conditional("DEBUG")]
	private void AssertInvariants()
		=> Debug.Assert(AreInvariantsValid(), "The indexed run collection is inconsistent.");

	internal struct Cursor
	{
		private Node? _node;
		private int _start;

		internal Cursor(Node node, int start)
		{
			_node = node;
			_start = start;
		}

		internal bool IsValid => _node is not null;

		internal T Current => _node?.Item ?? throw new InvalidOperationException();

		internal int Start => _start;

		internal int End => checked(_start + (_node?.ItemLength ?? 0));

		internal void MoveNext()
		{
			if (_node is null)
			{
				return;
			}

			_start = checked(_start + _node.ItemLength);
			_node = GetSuccessor(_node);
		}
	}

	internal sealed class Node
	{
		internal Node(T item, int itemLength, uint priority)
		{
			Item = item;
			ItemLength = itemLength;
			Priority = priority;
			SubtreeCount = 1;
			SubtreeLength = itemLength;
		}

		internal readonly T Item;
		internal int ItemLength;
		internal readonly uint Priority;
		internal Node? Parent;
		internal Node? Left;
		internal Node? Right;
		internal int SubtreeCount;
		internal int SubtreeLength;
	}
}
