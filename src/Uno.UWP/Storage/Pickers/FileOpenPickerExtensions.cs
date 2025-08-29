#nullable enable

namespace Windows.Storage.Pickers;

/// <summary>
/// Contains Uno Platform-specific extesions for the <see cref="FileOpenPicker"/> class.
/// </summary>
public static class FileOpenPickerExtensions
{
	/// <summary>
	/// Sets the file limit a user can select when picking multiple files.
	/// </summary>
	/// <param name="limit">The maximum number of files that the user can pick.</param>
#if !__IOS__
	[global::Uno.NotImplemented]
#endif
	public static void SetMultipleFilesLimit(this FileOpenPicker picker, int limit)
	{
#if __IOS__
		picker.SetMultipleFileLimit(limit);
#endif
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="picker"></param>
	/// <param name="readOnly"></param>
#if !__IOS__
	[global::Uno.NotImplemented]
#endif
	public static void SetReadOnlyMode(this FileOpenPicker picker, bool readOnly)
	{
#if __IOS__
		picker.SetReadOnlyMode(readOnly);
#endif
	}
}
