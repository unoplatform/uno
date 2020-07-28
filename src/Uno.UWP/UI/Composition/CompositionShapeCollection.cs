using System.Collections;
using System.Collections.Generic;

namespace Windows.UI.Composition
{
	public partial class CompositionShapeCollection : CompositionObject, IList<CompositionShape>, IEnumerable<CompositionShape>
	{
		private List<CompositionShape> _shapes = new List<CompositionShape>();
		private ShapeVisual shapeVisual;

		internal CompositionShapeCollection(ShapeVisual shapeVisual) => this.shapeVisual = shapeVisual;

		public int Count => _shapes.Count;

		public int IndexOf(CompositionShape item)
			=> _shapes.IndexOf(item);

		public void Insert(int index, CompositionShape item)
			=> _shapes.Insert(index, item);

		public void RemoveAt(int index)
			=> _shapes.RemoveAt(index);

		public CompositionShape this[int index]
		{
			get => _shapes[index];
			set => _shapes[index] = value;
		}

		public void Add(CompositionShape item)
			=> _shapes.Add(item);

		public void Clear()
			=> _shapes.Clear();

		public bool Contains(CompositionShape item)
			=> _shapes.Contains(item);

		public void CopyTo(CompositionShape[] array, int arrayIndex)
			=> _shapes.CopyTo(array, arrayIndex);

		public bool Remove(CompositionShape item)
			=> _shapes.Remove(item);

		public bool IsReadOnly => false;

		public IEnumerator<CompositionShape> GetEnumerator()
			=> _shapes.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> _shapes.GetEnumerator();
	}
}
