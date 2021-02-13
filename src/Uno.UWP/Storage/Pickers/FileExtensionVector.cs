#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.Storage.Pickers
{
	/// <summary>
	/// Represents a random-access collection of file name extensions.
	/// </summary>
	public partial class FileExtensionVector : IList<string>, IEnumerable<string>
	{
		private readonly List<string> _items = new List<string>();

		internal FileExtensionVector()
		{
		}

		public uint Size => (uint)_items.Count;

		public int IndexOf(string item) => _items.IndexOf(item);

		public void Insert(int index, string item) => _items.Insert(index, item);

		public void RemoveAt(int index) => _items.RemoveAt(index);

		public string this[int index]
		{
			get => _items[index];
			set => _items[index] = value;
		}

		public void Add(string item) => _items.Add(item);

		public void Clear() => _items.Clear();

		public bool Contains(string item) => _items.Contains(item);

		public void CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

		public bool Remove(string item) => _items.Remove(item);

		public int Count
		{
			get => _items.Count;
			set => throw new InvalidOperationException();
		}

		public bool IsReadOnly
		{
			get => false;
			set => throw new InvalidOperationException();
		}

		public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
