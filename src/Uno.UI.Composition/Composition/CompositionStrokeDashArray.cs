using System.Collections;
using System.Collections.Generic;
using Uno;

namespace Windows.UI.Composition;

[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__MACOS__")]
public partial class CompositionStrokeDashArray : CompositionObject, IList<float>, IEnumerable<float>
{
	private readonly List<float> _list;

	internal CompositionStrokeDashArray()
	{
		_list = new List<float>();
	}

	public uint Size => (uint)_list.Count;

	public int IndexOf(float item)
		=> _list.IndexOf(item);

	public void Insert(int index, float item)
		=> _list.Insert(index, item);

	public void RemoveAt(int index)
		=> _list.RemoveAt(index);

	public float this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	public void Add(float item)
		=> _list.Add(item);

	public void Clear()
		=> _list.Clear();

	public bool Contains(float item)
		=> _list.Contains(item);

	public void CopyTo(float[] array, int arrayIndex)
		=> _list.CopyTo(array, arrayIndex);

	public bool Remove(float item)
		=> _list.Remove(item);

	public int Count
		=> _list.Count;

	public bool IsReadOnly
		=> false;

	public IEnumerator<float> GetEnumerator()
		=> _list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> _list.GetEnumerator();

	internal float[] ToEvenArray()
	{
		return _list.Count % 2 == 0
			? _list.ToArray()
			: [.. _list, .. _list];
	}
}
