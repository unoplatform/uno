#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.UI.Composition
{
	public partial class CompositionColorGradientStopCollection : IList<CompositionColorGradientStop>
	{
		private readonly CompositionGradientBrush _owner;
		private readonly List<CompositionColorGradientStop> _gradientStops;

		internal CompositionColorGradientStopCollection(CompositionGradientBrush owner)
		{
			_owner = owner;
			_gradientStops = new List<CompositionColorGradientStop>();
		}

		public CompositionColorGradientStop this[int index]
		{
			get => _gradientStops[index];
			set
			{
				ThrowIfNull(value, nameof(value));

				CompositionColorGradientStop oldValue = _gradientStops[index];
				CompositionColorGradientStop newValue = value;

				_gradientStops[index] = newValue;

				OnItemAddedRemoved(oldValue, newValue);
				InvalidateOwner();
			}
		}

		public int Count => _gradientStops.Count;

		public bool IsReadOnly => false;

		public void Add(CompositionColorGradientStop item)
		{
			ThrowIfNull(item, nameof(item));

			_gradientStops.Add(item);

			OnItemAddedRemoved(null, item);
			InvalidateOwner();
		}

		public void Clear()
		{
			_gradientStops.ForEach(x => OnItemAddedRemoved(x, null));
			_gradientStops.Clear();

			InvalidateOwner();
		}

		public bool Contains(CompositionColorGradientStop item)
		{
			ThrowIfNull(item, nameof(item));

			return _gradientStops.Contains(item);
		}

		public void CopyTo(CompositionColorGradientStop[] array, int arrayIndex)
		{
			ThrowIfNull(array, nameof(array));

			_gradientStops.CopyTo(array, arrayIndex);
		}

		public IEnumerator<CompositionColorGradientStop> GetEnumerator() => _gradientStops.GetEnumerator();

		public int IndexOf(CompositionColorGradientStop item)
		{
			ThrowIfNull(item, nameof(item));

			return _gradientStops.IndexOf(item);
		}

		public void Insert(int index, CompositionColorGradientStop item)
		{
			ThrowIfNull(item, nameof(item));

			_gradientStops.Insert(index, item);

			OnItemAddedRemoved(null, item);
			InvalidateOwner();
		}

		public bool Remove(CompositionColorGradientStop item)
		{
			ThrowIfNull(item, nameof(item));

			bool removed = _gradientStops.Remove(item);
			if (removed)
			{
				OnItemAddedRemoved(item, null);
				InvalidateOwner();
			}

			return removed;
		}

		public void RemoveAt(int index)
		{
			var item = _gradientStops[index];

			_gradientStops.RemoveAt(index);

			OnItemAddedRemoved(item, null);
			InvalidateOwner();
		}

		IEnumerator IEnumerable.GetEnumerator() => _gradientStops.GetEnumerator();

		private void OnItemAddedRemoved(CompositionColorGradientStop? oldItem, CompositionColorGradientStop? newItem)
		{
			if (oldItem != null)
			{
				oldItem.RemoveContext(_owner, nameof(CompositionGradientBrush.ColorStops));
			}

			if (newItem != null)
			{
				newItem.AddContext(_owner, nameof(CompositionGradientBrush.ColorStops));
			}
		}

		private void InvalidateOwner()
		{
			_owner.InvalidateColorStops();
		}

		private void ThrowIfNull<T>(T? item, string propertyName) where T : class
		{
			if (item == null)
			{
				throw new ArgumentNullException(propertyName);
			}
		}
	}
}
