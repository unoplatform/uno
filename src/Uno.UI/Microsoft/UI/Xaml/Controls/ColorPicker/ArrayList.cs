using System;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Simplified implementation of List<typeparamref name="T"/> that supports direct access to the internal array.
	/// </summary>
	/// <remarks>
	/// This is needed in the ColorPicker to avoid an extra copy when converting a list of bytes into a WritableBitmap.
	/// The performance gains justify creation of this class and a slight deviation from WinUI.
	/// This class is not considered safe to use outside of the ColorPicker so it is not in a generic location.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	internal class ArrayList<T>
	{
		private T[] _list;

		public ArrayList()
		{
			_list = System.Array.Empty<T>();
		}

		public ArrayList(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("Capacity cannot be less than zero.");
			}
            else if (capacity == 0)
			{
				_list = System.Array.Empty<T>();
			}
			else
			{
				_list = new T[capacity];
			}
        }

		internal T[] Array
		{
			get => _list;
		}

		public int Capacity
		{
			set
			{
				if (value < Count)
				{
					throw new IndexOutOfRangeException("Cannot resize the list smaller than it was.");
				}

				if (value != Count)
				{
					if (value > 0)
					{
						T[] newList = new T[value];

						if (Count > 0)
						{
							System.Array.Copy(_list, 0, newList, 0, Count);
						}

						_list = newList;
					}
					else
					{
						_list = System.Array.Empty<T>();
					}
				}
			}
		}

		public int Count { get; private set; } = 0;

		public void Add(T item)
		{
			if (Count == _list.Length)
			{
				System.Array.Resize(ref _list, _list.Length * 2);;
			}

			_list[Count] = item;
			Count++;
		}
	}
}
