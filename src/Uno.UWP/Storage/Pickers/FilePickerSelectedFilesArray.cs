#if __MACOS__ || __ANDROID__
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
		private readonly IList<StorageFile> _files;

		internal FilePickerSelectedFilesArray(StorageFile[] files)
		{
			_files = files ?? throw new ArgumentNullException(nameof(files));
		}

		/// <summary>
		/// Gets the number of StorageFile objects in the collection.
		/// </summary>
		public uint Size => (uint)_files.Count;

		public StorageFile this[int index]
		{
			get => _files[index];
			set
			{
				throw new InvalidOperationException("The list is read-only");
			}
		}

		public IEnumerator<StorageFile> GetEnumerator() => _files.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count
		{
			get => _files.Count;
			set
			{
				throw new InvalidOperationException("The list is read-only");
			}
		}
	}
}
#endif
