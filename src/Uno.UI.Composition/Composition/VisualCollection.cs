using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.UI.Composition
{
	public partial class VisualCollection : CompositionObject, IEnumerable<Visual>
	{
		private readonly ContainerVisual _owner;

		private readonly List<Visual> _visuals = new();

		internal VisualCollection(Compositor compositor, ContainerVisual owner) : base(compositor)
		{
			_owner = owner;
		}

		internal event EventHandler CollectionChanged;

		public int Count => _visuals.Count;

		internal List<Visual> InnerList => _visuals;

		public void InsertAbove(Visual newChild, Visual sibling)
		{
			var index = _visuals.IndexOf(sibling);
			_visuals.Insert(index, newChild);
			newChild.Parent = _owner;
			InsertAbovePartial(newChild, sibling);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}

		partial void InsertAbovePartial(Visual newChild, Visual sibling);

		public void InsertAtBottom(Visual newChild)
		{
			_visuals.Insert(0, newChild);
			newChild.Parent = _owner;
			InsertAtBottomPartial(newChild);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void InsertAtBottomPartial(Visual newChild);

		public void InsertAtTop(Visual newChild)
		{
			_visuals.Insert(_visuals.Count, newChild);
			newChild.Parent = _owner;
			InsertAtTopPartial(newChild);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void InsertAtTopPartial(Visual newChild);

		public void InsertBelow(Visual newChild, Visual sibling)
		{
			var index = _visuals.IndexOf(sibling);
			_visuals.Insert(index - 1, newChild);
			newChild.Parent = _owner;
			InsertBelowPartial(newChild, sibling);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void InsertBelowPartial(Visual newChild, Visual sibling);

		public void Remove(Visual child)
		{
			if (_visuals.Remove(child))
			{
				child.Parent = null;
				RemovePartial(child);

				CollectionChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		partial void RemovePartial(Visual child);

		public void RemoveAll()
		{
			foreach (var child in _visuals)
			{
				child.Parent = null;
			}
			_visuals.Clear();
			RemoveAllPartial();

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void RemoveAllPartial();

		public IEnumerator<Visual> GetEnumerator() => _visuals.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _visuals.GetEnumerator();
	}
}
