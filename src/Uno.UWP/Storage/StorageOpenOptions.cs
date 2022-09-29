#nullable disable

using System;

namespace Windows.Storage
{
	/// <summary>
	/// Provides options to use when opening a file.
	/// </summary>
	[Flags]
	public enum StorageOpenOptions : uint
	{
		/// <summary>
		/// No options are specified.
		/// </summary>
		None = 0U,
		/// <summary>
		/// Only allow the file to be read.
		/// </summary>
		AllowOnlyReaders = 1U,
		/// <summary>
		/// Allows both readers and writers to coexist.
		/// </summary>
		AllowReadersAndWriters = 2U
	}
}
