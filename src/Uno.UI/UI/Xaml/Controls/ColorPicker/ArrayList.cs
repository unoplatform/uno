using global::System;

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
		private int _Capacity;
		private T[] _Array;

		public ArrayList()
		{
			_Capacity = 0;
			_Array = global::System.Array.Empty<T>();
		}

		public ArrayList(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("Capacity cannot be less than zero.");
			}
			else if (capacity == 0)
			{
				_Array = global::System.Array.Empty<T>();
			}
			else
			{
				_Array = new T[capacity];
			}

			_Capacity = capacity;
		}

		internal T[] Array
		{
			get => _Array;
		}

		public int Capacity
		{
			get => _Capacity;
			set
			{
				if (value < Count)
				{
					throw new IndexOutOfRangeException("Cannot resize the list smaller than its current length.");
				}

				if (value != _Capacity)
				{
					_Capacity = value;

					if (value > 0)
					{
						var newList = new T[value];

						if (Count > 0)
						{
							global::System.Array.Copy(_Array, 0, newList, 0, Count);
						}

						_Array = newList;
					}
					else
					{
						_Array = global::System.Array.Empty<T>();
					}
				}
			}
		}

		public int Count { get; private set; } = 0;

		public void Add(T item)
		{
			if (Count == _Array.Length)
			{
				if (_Array.Length == 0)
				{
					_Capacity = 4;
				}
				else
				{
					_Capacity = _Array.Length * 2;
				}

				global::System.Array.Resize(ref _Array, _Capacity);
			}

			_Array[Count] = item;
			Count++;
		}
	}
}
