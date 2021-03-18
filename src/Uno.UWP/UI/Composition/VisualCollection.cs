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

		public int Count => _visuals.Count;

		public void InsertAbove(Visual newChild, Visual sibling)
		{
			var index = _visuals.IndexOf(sibling);
			_visuals.Insert(index, newChild);

			InsertAbovePartial(newChild, sibling);
		}
		partial void InsertAbovePartial(Visual newChild, Visual sibling);

		public void InsertAtBottom(Visual newChild)
		{
			_visuals.Insert(0, newChild);
			InsertAtBottomPartial(newChild);
		}
		partial void InsertAtBottomPartial(Visual newChild);

		public void InsertAtTop(Visual newChild)
		{
			_visuals.Insert(_visuals.Count, newChild);
			InsertAtTopPartial(newChild);
		}
		partial void InsertAtTopPartial(Visual newChild);

		public void InsertBelow(Visual newChild, Visual sibling)
		{
			var index = _visuals.IndexOf(sibling);
			_visuals.Insert(index - 1, newChild);
			InsertBelowPartial(newChild, sibling);
		}
		partial void InsertBelowPartial(Visual newChild, Visual sibling);

		public void Remove(Visual child)
		{
			_visuals.Remove(child);
			RemovePartial(child);
		}
		partial void RemovePartial(Visual child);

		public void RemoveAll()
		{
			_visuals.Clear();
			RemoveAllPartial();
		}
		partial void RemoveAllPartial();

		public IEnumerator<Visual> GetEnumerator() => _visuals.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _visuals.GetEnumerator();
	}
}
