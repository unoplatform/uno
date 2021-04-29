using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Windows.UI.Composition
{
	public partial class VisualCollection : CompositionObject, IEnumerable<Visual>
	{
		private readonly ContainerVisual _owner;
		private readonly ImmutableList<Visual>.Builder _items = ImmutableList.CreateBuilder<Visual>();
		private ImmutableList<Visual> _committedItems = ImmutableList<Visual>.Empty;

		internal VisualCollection(ContainerVisual owner)
			: base(owner.Compositor)
		{
			_owner = owner;
			Subscribe(owner); // TODO: CONvert to OnInvalidated to avoid create too much list
		}

		/// <summary>
		/// Gets the committed items
		/// </summary>
		internal IImmutableList<Visual> Committed => _committedItems;

		/// <inheritdoc />
		private protected override void OnCommit()
		{
			base.OnCommit();

			_committedItems = _items.ToImmutable();

			// Note: Its the responsibility of the _owner container to Commit() all children.
			//		 Here we only commit the local changes of this collection, we are not interacting with items in any way.
		}

		public int Count => _items.Count;

		internal void InsertAt(int index, Visual newChild)
		{
			if (index == Count)
			{
				_items.Add(newChild);
			}
			else
			{
				_items.Insert(index, newChild);
			}

			newChild.Parent = _owner;
			InsertAtPartial(index, newChild);

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void InsertAtPartial(int index, Visual newChild);

		public void InsertAtBottom(Visual newChild)
		{
			_items.Insert(0, newChild);
			newChild.Parent = _owner;
			InsertAtBottomPartial(newChild);

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void InsertAtBottomPartial(Visual newChild);

		public void InsertAtTop(Visual newChild)
		{
			_items.Add(newChild);
			newChild.Parent = _owner;
			InsertAtTopPartial(newChild);

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void InsertAtTopPartial(Visual newChild);

		public void InsertAbove(Visual newChild, Visual sibling)
		{
			var index = _items.IndexOf(sibling);
			_items.Insert(index, newChild);
			newChild.Parent = _owner;
			InsertAbovePartial(newChild, sibling);

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void InsertAbovePartial(Visual newChild, Visual sibling);

		public void InsertBelow(Visual newChild, Visual sibling)
		{
			var index = _items.IndexOf(sibling);
			_items.Insert(index - 1, newChild);
			newChild.Parent = _owner;
			InsertBelowPartial(newChild, sibling);

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void InsertBelowPartial(Visual newChild, Visual sibling);

		internal void RemoveAt(int index)
		{
			var visual = _items[index];
			_items.RemoveAt(index);
			visual.Parent = null;
			RemoveAtPartial(index, visual);

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void RemoveAtPartial(int index, Visual newChild);

		public void Remove(Visual child)
		{
			_items.Remove(child);
			child.Parent = null;
			RemovePartial(child);

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void RemovePartial(Visual child);

		public void RemoveAll()
		{
			foreach (var visual in _items)
			{
				visual.Parent = null;
			}
			_items.Clear();
			RemoveAllPartial();

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void RemoveAllPartial();

		internal void Move(int fromIndex, int toIndex)
		{
			var visual = _items[fromIndex];
			_items.RemoveAt(fromIndex);
			if (toIndex == Count)
			{
				_items.Add(visual);
			}
			else
			{
				_items.Insert(toIndex, visual);
			}
			MovePartial(fromIndex, toIndex);

			Invalidate(CompositionPropertyType.Dependent);
		}
		partial void MovePartial(int fromIndex, int toIndex);

		public IEnumerator<Visual> GetEnumerator() => _items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
	}
}
