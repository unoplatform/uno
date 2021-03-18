#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.Storage.Pickers
{
	/// <summary>
	/// Represents a collection of storage files that the user has selected by using a file picker.
	/// </summary>
	public partial class FilePickerSelectedFilesArray : IReadOnlyList<StorageFile>, IEnumerable<StorageFile>
	{
		private readonly IReadOnlyList<StorageFile> _items;

		internal FilePickerSelectedFilesArray(StorageFile[] items)
		{
			_items = items ?? throw new ArgumentNullException(nameof(items));
		}

		internal static FilePickerSelectedFilesArray Empty { get; } = new FilePickerSelectedFilesArray(Array.Empty<StorageFile>());

		public uint Size => (uint)_items.Count;

		public StorageFile this[int index]
		{
			get => _items[index];
			set => throw new InvalidOperationException();
		}

		public IEnumerator<StorageFile> GetEnumerator() => _items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count
		{
			get => _items.Count;
			set => throw new InvalidOperationException();
		}
	}
}
