#nullable disable

using System;

namespace Windows.Storage
{
	/// <summary>
	/// Describes whether an item that implements the IStorageItem interface is a file or a folder.
	/// </summary>
	[Flags]
	public enum StorageItemTypes : uint
	{
		/// <summary>
		/// A storage item that is neither a file nor a folder.
		/// </summary>
		None = 0U,
		/// <summary>
		/// A file that is represented as a StorageFile instance.
		/// </summary>
		File = 1U,
		/// <summary>
		/// A folder that is represented as a StorageFolder instance.
		/// </summary>
		Folder = 2U
	}
}
