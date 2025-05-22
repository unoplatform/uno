#nullable enable

using System.Diagnostics;

namespace Microsoft.UI.Xaml
{
	public partial class DependencyObjectStore
	{
		internal class DependencyPropertyChangedEventArgsPool
		{
			private readonly Element[] _elements;
			private DependencyPropertyChangedEventArgs? _spare;

			private int _index;

			public DependencyPropertyChangedEventArgsPool(int size)
			{
				Debug.Assert(size > 0, "Size must be greater than zero.");

				_elements = new Element[size - 1];

				_index = -1;
			}

			public DependencyPropertyChangedEventArgs Rent()
			{
				var result = _spare;

				if (result != null)
				{
					_spare = null;
				}
				else
				{
					result = RentSlow();
				}

				return result;
			}

			public DependencyPropertyChangedEventArgs RentSlow()
			{
				var elements = _elements;

				var index = (uint)_index;

				// This (store _elements on stack and cast _index/elements.Length to uint) ensures the JIT
				// won't emit bound checks for the array access.
				if (index < (uint)elements.Length)
				{
					var result = elements[index].Value;

					elements[index].Value = null;

					_index--;

					return result!;
				}

				return new();
			}

			public void Return(DependencyPropertyChangedEventArgs item)
			{
				if (_spare == null)
				{
					_spare = item;
				}
				else
				{
					ReturnSlow(item);
				}
			}

			private void ReturnSlow(DependencyPropertyChangedEventArgs item)
			{
				var elements = _elements;

				var index = (uint)(_index + 1);

				// See RentSlow() comment.
				if (index < (uint)elements.Length)
				{
					elements[index].Value = item;

					_index++;
				}
			}

			// We can avoid variance checks when storing elements in the array by using structs.
			// Since structs do not support inheritance, they are not subject to variance checks (stelem is used rather than stelem.ref).
			// See: https://devblogs.microsoft.com/premier-developer/managed-object-internals-part-3-the-layout-of-a-managed-array-3/
			private struct Element
			{
				public DependencyPropertyChangedEventArgs? Value;
			}
		}
	}
}
