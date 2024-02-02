using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Composition.Interactions;

public partial class CompositionInteractionSourceCollection : CompositionObject, IEnumerable<ICompositionInteractionSource>
{
	private List<ICompositionInteractionSource> _list = new();

	internal CompositionInteractionSourceCollection(Compositor compositor) : base(compositor)
	{
	}

	public int Count => _list.Count;

	public void RemoveAll() => _list.Clear();

	public void Add(ICompositionInteractionSource value) => _list.Add(value);

	public void Remove(ICompositionInteractionSource value) => _list.Remove(value);

	public IEnumerator<ICompositionInteractionSource> GetEnumerator() => _list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
}
