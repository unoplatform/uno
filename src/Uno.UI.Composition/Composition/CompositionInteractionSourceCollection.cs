using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Windows.UI.Composition.Interactions;

public partial class CompositionInteractionSourceCollection : CompositionObject, IEnumerable<ICompositionInteractionSource>
{
	private readonly List<ICompositionInteractionSource> _list = new();
	private readonly InteractionTracker _tracker;

	internal CompositionInteractionSourceCollection(Compositor compositor, InteractionTracker tracker) : base(compositor)
	{
		_tracker = tracker;
	}

	public int Count => _list.Count;

	public void RemoveAll()
	{
		foreach (var vis in _list.OfType<VisualInteractionSource>())
		{
			vis.Trackers.Remove(_tracker);
		}

		_list.Clear();
	}

	public void Add(ICompositionInteractionSource value)
	{
		_list.Add(value);
		if (value is VisualInteractionSource vis)
		{
			vis.Trackers.Add(_tracker);
		}
	}

	public void Remove(ICompositionInteractionSource value)
	{
		_list.Remove(value);
		if (value is VisualInteractionSource vis)
		{
			vis.Trackers.Remove(_tracker);
		}
	}

	public IEnumerator<ICompositionInteractionSource> GetEnumerator() => _list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
}
