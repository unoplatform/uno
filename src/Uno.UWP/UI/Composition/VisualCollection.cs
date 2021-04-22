using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Windows.UI.Composition
{
	public partial class VisualCollection : CompositionObject, IEnumerable<Visual>
	{
		private readonly ContainerVisual _owner;
		private readonly ImmutableList<Visual>.Builder _visuals = ImmutableList.CreateBuilder<Visual>();

		internal VisualCollection(Compositor compositor, ContainerVisual owner) : base(compositor)
		{
			_owner = owner;
		}

		internal VisualCollection(ContainerVisual owner)
		{
			_owner = owner;
		}

		internal ImmutableList<Visual> ToImmutable() => _visuals.ToImmutable();

		public int Count => _visuals.Count;

		internal void InsertAt(int index, Visual newChild)
		{
			if (index == Count)
			{
				_visuals.Add(newChild);
			}
			else
			{
				_visuals.Insert(index, newChild);
			}

			newChild.Parent = _owner;
			InsertAtPartial(index, newChild);

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void InsertAtPartial(int index, Visual newChild);

		public void InsertAtBottom(Visual newChild)
		{
			_visuals.Insert(0, newChild);
			newChild.Parent = _owner;
			InsertAtBottomPartial(newChild);

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void InsertAtBottomPartial(Visual newChild);

		public void InsertAtTop(Visual newChild)
		{
			_visuals.Add(newChild);
			newChild.Parent = _owner;
			InsertAtTopPartial(newChild);

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void InsertAtTopPartial(Visual newChild);

		public void InsertAbove(Visual newChild, Visual sibling)
		{
			var index = _visuals.IndexOf(sibling);
			_visuals.Insert(index, newChild);
			newChild.Parent = _owner;
			InsertAbovePartial(newChild, sibling);

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void InsertAbovePartial(Visual newChild, Visual sibling);

		public void InsertBelow(Visual newChild, Visual sibling)
		{
			var index = _visuals.IndexOf(sibling);
			_visuals.Insert(index - 1, newChild);
			newChild.Parent = _owner;
			InsertBelowPartial(newChild, sibling);

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void InsertBelowPartial(Visual newChild, Visual sibling);

		internal void RemoveAt(int index)
		{
			var visual = _visuals[index];
			_visuals.RemoveAt(index);
			visual.Parent = null;
			RemoveAtPartial(index, visual);

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void RemoveAtPartial(int index, Visual newChild);

		public void Remove(Visual child)
		{
			_visuals.Remove(child);
			child.Parent = null;
			RemovePartial(child);

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void RemovePartial(Visual child);

		public void RemoveAll()
		{
			foreach (var visual in _visuals)
			{
				visual.Parent = null;
			}
			_visuals.Clear();
			RemoveAllPartial();

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void RemoveAllPartial();

		internal void Move(int fromIndex, int toIndex)
		{
			var visual = _visuals[fromIndex];
			_visuals.RemoveAt(fromIndex);
			if (toIndex == Count)
			{
				_visuals.Add(visual);
			}
			else
			{
				_visuals.Insert(toIndex, visual);
			}
			MovePartial(fromIndex, toIndex);

			_owner.Invalidate(VisualDirtyState.Dependent);
		}
		partial void MovePartial(int fromIndex, int toIndex);

		public IEnumerator<Visual> GetEnumerator() => _visuals.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _visuals.GetEnumerator();
	}
}
