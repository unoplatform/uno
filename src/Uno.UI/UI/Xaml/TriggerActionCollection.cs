using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml;

public partial class TriggerActionCollection : IList<TriggerAction>, IEnumerable<TriggerAction>
{
	private readonly List<TriggerAction> _actions = new List<TriggerAction>();

	public uint Size => (uint)_actions.Count;

	public int Count => _actions.Count;

	public bool IsReadOnly => false;

	public TriggerAction this[int index]
	{
		get => _actions[index];
		set => _actions[index] = value;
	}

	public void Add(TriggerAction item)
	{
		_actions.Add(item);
	}

	public void Clear()
	{
		_actions.Clear();
	}

	public bool Contains(TriggerAction item)
	{
		return _actions.Contains(item);
	}

	public void CopyTo(TriggerAction[] array, int arrayIndex)
	{
		_actions.CopyTo(array, arrayIndex);
	}

	public IEnumerator<TriggerAction> GetEnumerator()
	{
		return _actions.GetEnumerator();
	}

	public int IndexOf(TriggerAction item)
	{
		return _actions.IndexOf(item);
	}

	public void Insert(int index, TriggerAction item)
	{
		_actions.Insert(index, item);
	}

	public bool Remove(TriggerAction item)
	{
		return _actions.Remove(item);
	}

	public void RemoveAt(int index)
	{
		_actions.RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _actions.GetEnumerator();
	}
}
