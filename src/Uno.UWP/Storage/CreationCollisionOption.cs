namespace Windows.Storage;

/// <summary>
/// Specifies what to do if a file or folder with the specified name already exists in the current folder when you create a new file or folder.
/// </summary>
public enum CreationCollisionOption
{
	/// <summary>
	/// Automatically append a number to the base of the specified name if the file or folder already exists.
	/// For example, <c>if MyFile.txt</c> already exists, then the new file is named <c>MyFile (2).txt</c>.
	/// If <c>MyFolder</c> already exists, then the new folder is named <c>MyFolder (2)</c>.
	/// </summary>
	GenerateUniqueName,

	/// <summary>
	/// Replace the existing item if the file or folder already exists.
	/// </summary>
	ReplaceExisting,

	/// <summary>
	/// Raise an exception of type System.Exception if the file or folder already exists.
	/// Methods that don't explicitly pass a value from the CreationCollisionOption enumeration use the FailIfExists value as the default when you try to create, rename, copy, or move a file or folder.
	/// </summary>
	FailIfExists,

	/// <summary>
	/// Return the existing item if the file or folder already exists.
	/// </summary>
	OpenIfExists,
}
