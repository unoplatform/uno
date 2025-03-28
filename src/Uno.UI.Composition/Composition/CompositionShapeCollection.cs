#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.UI.Composition
{
	public partial class CompositionShapeCollection : CompositionObject, IList<CompositionShape>
	{
		private readonly List<CompositionShape> _shapes = new List<CompositionShape>();
		private readonly ShapeVisual _shapeVisual;

		internal CompositionShapeCollection(Compositor compositor, ShapeVisual shapeVisual) : base(compositor)
			=> this._shapeVisual = shapeVisual;

		public int Count => _shapes.Count;

		public int IndexOf(CompositionShape item)
			=> _shapes.IndexOf(item);

		public void Insert(int index, CompositionShape item)
		{
			ThrowIfNull(item, nameof(item));

			_shapes.Insert(index, item);

			OnCompositionPropertyChanged(null, item);
			OnChanged();
		}

		public void RemoveAt(int index)
		{
			var shape = _shapes[index];

			_shapes.RemoveAt(index);

			OnCompositionPropertyChanged(shape, null);
			OnChanged();
		}

		public CompositionShape this[int index]
		{
			get => _shapes[index];
			set
			{
				ThrowIfNull(value, nameof(value));

				var oldShape = _shapes[index];

				_shapes[index] = value;

				OnCompositionPropertyChanged(oldShape, value);
				OnChanged();
			}
		}

		public void Add(CompositionShape item)
		{
			ThrowIfNull(item, nameof(item));
			_shapes.Add(item);

			OnCompositionPropertyChanged(null, item);
			OnChanged();
		}

		public void Clear()
		{
			foreach (var shape in _shapes)
			{
				OnCompositionPropertyChanged(shape, null);
			}

			_shapes.Clear();
			OnChanged();
		}

		public bool Contains(CompositionShape item)
			=> _shapes.Contains(item);

		public void CopyTo(CompositionShape[] array, int arrayIndex)
			=> _shapes.CopyTo(array, arrayIndex);

		public bool Remove(CompositionShape item)
		{
			ThrowIfNull(item, nameof(item));

			int index = _shapes.IndexOf(item);
			if (index < 0)
			{
				return false;
			}

			_shapes.RemoveAt(index);

			OnCompositionPropertyChanged(item, null);
			OnChanged();

			return true;
		}

		public bool IsReadOnly => false;

		public IEnumerator<CompositionShape> GetEnumerator()
			=> _shapes.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> _shapes.GetEnumerator();

		private void ThrowIfNull<T>(T? item, string propertyName) where T : class
		{
			if (item == null)
			{
				throw new ArgumentNullException(propertyName);
			}
		}
	}
}
