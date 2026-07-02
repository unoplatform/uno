using Windows.Storage.Pickers;

namespace Uno.Storage.Pickers;

/// <summary>
/// Provides information about the runtime support for storage pickers on WebAssembly.
/// </summary>
public static class FileSystemAccessApiInformation
{
	/// <summary>
	/// Checks whether the File System Access API file open picker is available.
	/// </summary>
	/// <returns>A value indicating whether the picker is supported.</returns>
	public static bool IsOpenPickerSupported =>
#if __NETSTD_REFERENCE__
		false;
#else
		FileOpenPicker.IsNativePickerSupported();
#endif

	/// <summary>
	/// Checks whether the File System Access API file save picker is available.
	/// </summary>
	/// <returns>A value indicating whether the picker is supported.</returns>
	public static bool IsSavePickerSupported =>
#if __NETSTD_REFERENCE__
		false;
#else
		FileSavePicker.IsNativePickerSupported();
#endif

	/// <summary>
	/// Checks whether both file open and file save pickers from the File System Access API are available.
	/// </summary>
	/// <returns>A value indicating whether the picker is supported.</returns>
	public static bool AreFilePickersSupported => IsOpenPickerSupported && IsSavePickerSupported;

	/// <summary>
	/// Checks whether the File System Access API folder open picker is available.
	/// </summary>
	/// <returns>A value indicating whether the picker is supported.</returns>
	public static bool IsFolderPickerSupported =>
#if __NETSTD_REFERENCE__
		false;
#else
		FolderPicker.IsNativePickerSupported();
#endif
}
