using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents a collection of TriggerAction objects.
/// </summary>
public partial class TriggerActionCollection : IList<TriggerAction>, IEnumerable<TriggerAction>
{
	private readonly List<TriggerAction> _actions = new List<TriggerAction>();

	/// <summary>
	/// Gets the number of elements contained in the collection.
	/// </summary>
	public uint Size => (uint)_actions.Count;

	/// <inheritdoc />
	public int Count => _actions.Count;

	/// <inheritdoc />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	public TriggerAction this[int index]
	{
		get => _actions[index];
		set => _actions[index] = value;
	}

	/// <inheritdoc />
	public void Add(TriggerAction item)
	{
		_actions.Add(item);
	}

	/// <inheritdoc />
	public void Clear()
	{
		_actions.Clear();
	}

	/// <inheritdoc />
	public bool Contains(TriggerAction item)
	{
		return _actions.Contains(item);
	}

	/// <inheritdoc />
	public void CopyTo(TriggerAction[] array, int arrayIndex)
	{
		_actions.CopyTo(array, arrayIndex);
	}

	/// <inheritdoc />
	public IEnumerator<TriggerAction> GetEnumerator()
	{
		return _actions.GetEnumerator();
	}

	/// <inheritdoc />
	public int IndexOf(TriggerAction item)
	{
		return _actions.IndexOf(item);
	}

	/// <inheritdoc />
	public void Insert(int index, TriggerAction item)
	{
		_actions.Insert(index, item);
	}

	/// <inheritdoc />
	public bool Remove(TriggerAction item)
	{
		return _actions.Remove(item);
	}

	/// <inheritdoc />
	public void RemoveAt(int index)
	{
		_actions.RemoveAt(index);
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return _actions.GetEnumerator();
	}
}
