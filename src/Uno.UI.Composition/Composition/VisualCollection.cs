#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.UI.Composition
{
	public partial class VisualCollection : CompositionObject, IEnumerable<Visual>
	{
		private readonly Visual _owner;

		private List<Visual> _visuals = new List<Visual>();

		internal VisualCollection(Compositor compositor, Visual owner) : base(compositor)
		{
			_owner = owner;
		}

		internal VisualCollection(Visual owner)
		{
			_owner = owner;
		}

		internal event EventHandler CollectionChanged;

		public int Count => _visuals.Count;

		internal IList<Visual> InnerList => _visuals;

		public void InsertAbove(Visual newChild, Visual sibling)
		{
			var index = _visuals.IndexOf(sibling);
			_visuals.Insert(index, newChild);

			InsertAbovePartial(newChild, sibling);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}

		partial void InsertAbovePartial(Visual newChild, Visual sibling);

		public void InsertAtBottom(Visual newChild)
		{
			_visuals.Insert(0, newChild);
			InsertAtBottomPartial(newChild);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void InsertAtBottomPartial(Visual newChild);

		public void InsertAtTop(Visual newChild)
		{
			_visuals.Insert(_visuals.Count, newChild);
			InsertAtTopPartial(newChild);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void InsertAtTopPartial(Visual newChild);

		public void InsertBelow(Visual newChild, Visual sibling)
		{
			var index = _visuals.IndexOf(sibling);
			_visuals.Insert(index - 1, newChild);
			InsertBelowPartial(newChild, sibling);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void InsertBelowPartial(Visual newChild, Visual sibling);

		public void Remove(Visual child)
		{
			_visuals.Remove(child);
			RemovePartial(child);

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void RemovePartial(Visual child);

		public void RemoveAll()
		{
			_visuals.Clear();
			RemoveAllPartial();

			CollectionChanged?.Invoke(this, EventArgs.Empty);
		}
		partial void RemoveAllPartial();

		public IEnumerator<Visual> GetEnumerator() => _visuals.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _visuals.GetEnumerator();
	}
}
