using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using DirectUI;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarViewDayItem
	{
		public void SetDensityColors(IEnumerable<Color> colors)
		{
			if (colors is null)
			{
				SetDensityColorsImpl(null);
			}
			else
			{
				// Wrap IEnumerable<Color> as IIterable<Color> for the internal implementation
				var iterable = new EnumerableToIterableAdapter<Color>(colors);
				SetDensityColorsImpl(iterable);
			}
		}

		private void SetDensityColorsImpl(IIterable<Color> pColors)
		{
			((CalendarViewBaseItem)this).SetDensityColors(pColors);
		}

		/// <summary>
		/// Simple adapter to convert IEnumerable to IIterable
		/// </summary>
		private class EnumerableToIterableAdapter<T> : IIterable<T>, IEnumerable<T>
		{
			private readonly IEnumerable<T> _enumerable;

			public EnumerableToIterableAdapter(IEnumerable<T> enumerable)
			{
				_enumerable = enumerable;
			}

			public DirectUI.IIterator<T> GetIterator()
			{
				return new UnoEnumeratorToIteratorAdapter<T>(_enumerable.GetEnumerator());
			}

			public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
