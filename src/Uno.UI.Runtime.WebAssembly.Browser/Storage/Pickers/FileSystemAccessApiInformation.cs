using Windows.Storage.Pickers;

namespace Uno.Storage.Pickers;

/// <summary>
/// Provides information about the runtime support for storage pickers on WebAssembly.
/// </summary>
public static class FileSystemAccessApiInformation
{
	/// <summary>
	/// Checks whether <see cref="FileOpenPicker"/> can pick files under the current
	/// <see cref="WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration"/>, with the File System Access API or the upload picker.
	/// </summary>
	/// <returns>A value indicating whether the picker is supported.</returns>
	public static bool IsOpenPickerSupported => FileOpenPicker.IsSupported();

	/// <summary>
	/// Checks whether <see cref="FileSavePicker"/> can pick a file under the current
	/// <see cref="WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration"/>, with the File System Access API or the download picker.
	/// </summary>
	/// <returns>A value indicating whether the picker is supported.</returns>
	public static bool IsSavePickerSupported => FileSavePicker.IsSupported();

	/// <summary>
	/// Checks whether both <see cref="FileOpenPicker"/> and <see cref="FileSavePicker"/> can pick files under the current
	/// <see cref="WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration"/>.
	/// </summary>
	/// <returns>A value indicating whether the picker is supported.</returns>
	public static bool AreFilePickersSupported => IsOpenPickerSupported && IsSavePickerSupported;

	/// <summary>
	/// Checks whether the File System Access API folder open picker is available.
	/// <see cref="FolderPicker"/> has no other implementation.
	/// </summary>
	/// <returns>A value indicating whether the picker is supported.</returns>
	public static bool IsFolderPickerSupported => FolderPicker.IsNativePickerSupported();
}
