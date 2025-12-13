using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents a collection of TriggerBase objects.
/// </summary>
public partial class TriggerCollection : IList<TriggerBase>, IEnumerable<TriggerBase>
{
	private readonly List<TriggerBase> _triggers = new List<TriggerBase>();

	/// <summary>
	/// Gets the number of elements contained in the collection.
	/// </summary>
	public uint Size => (uint)_triggers.Count;

	/// <inheritdoc />
	public int Count => _triggers.Count;

	/// <inheritdoc />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	public TriggerBase this[int index]
	{
		get => _triggers[index];
		set => _triggers[index] = value;
	}

	/// <inheritdoc />
	public void Add(TriggerBase item)
	{
		_triggers.Add(item);
	}

	/// <inheritdoc />
	public void Clear()
	{
		_triggers.Clear();
	}

	/// <inheritdoc />
	public bool Contains(TriggerBase item)
	{
		return _triggers.Contains(item);
	}

	/// <inheritdoc />
	public void CopyTo(TriggerBase[] array, int arrayIndex)
	{
		_triggers.CopyTo(array, arrayIndex);
	}

	/// <inheritdoc />
	public IEnumerator<TriggerBase> GetEnumerator()
	{
		return _triggers.GetEnumerator();
	}

	/// <inheritdoc />
	public int IndexOf(TriggerBase item)
	{
		return _triggers.IndexOf(item);
	}

	/// <inheritdoc />
	public void Insert(int index, TriggerBase item)
	{
		_triggers.Insert(index, item);
	}

	/// <inheritdoc />
	public bool Remove(TriggerBase item)
	{
		return _triggers.Remove(item);
	}

	/// <inheritdoc />
	public void RemoveAt(int index)
	{
		_triggers.RemoveAt(index);
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return _triggers.GetEnumerator();
	}
}
