using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml;

public partial class TriggerCollection : IList<TriggerBase>, IEnumerable<TriggerBase>
{
	private readonly List<TriggerBase> _triggers = new List<TriggerBase>();

	public uint Size => (uint)_triggers.Count;

	public int Count => _triggers.Count;

	public bool IsReadOnly => false;

	public TriggerBase this[int index]
	{
		get => _triggers[index];
		set => _triggers[index] = value;
	}

	public void Add(TriggerBase item)
	{
		_triggers.Add(item);
	}

	public void Clear()
	{
		_triggers.Clear();
	}

	public bool Contains(TriggerBase item)
	{
		return _triggers.Contains(item);
	}

	public void CopyTo(TriggerBase[] array, int arrayIndex)
	{
		_triggers.CopyTo(array, arrayIndex);
	}

	public IEnumerator<TriggerBase> GetEnumerator()
	{
		return _triggers.GetEnumerator();
	}

	public int IndexOf(TriggerBase item)
	{
		return _triggers.IndexOf(item);
	}

	public void Insert(int index, TriggerBase item)
	{
		_triggers.Insert(index, item);
	}

	public bool Remove(TriggerBase item)
	{
		return _triggers.Remove(item);
	}

	public void RemoveAt(int index)
	{
		_triggers.RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _triggers.GetEnumerator();
	}
}
