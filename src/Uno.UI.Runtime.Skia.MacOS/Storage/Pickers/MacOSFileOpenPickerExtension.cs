using System.Runtime.InteropServices;

using Windows.Storage;
using Windows.Storage.Pickers;

using Uno.Foundation.Extensibility;
using Uno.Extensions.Storage.Pickers;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSFileOpenPickerExtension : IFileOpenPickerExtension
{
	private static readonly string[] _asteriskArray = new string[] { "*" };

	public MacOSFileOpenPickerExtension()
	{
		_filters = Array.Empty<string>();
	}

	public static void Register() => ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), _ => new MacOSFileOpenPickerExtension());

	// Mapping
	// WinUI                            AppKit (NSOpenPanel)
	// --------------------------------------------------------------
	// CommitButtonText (string)        prompt (NSString)
	// ContinuationData (ValueSet)      n/a
	// FileTypeFilter                   allowedFileTypes (NSArray)
	// SettingsIdentifier (string)      identifier (NSString)
	// SuggestedStartLocation (enum)    directoryURL (NSURL)
	// User (User)                      n/a
	// ViewMode (enum)                  n/a
	public void Customize(FileOpenPicker picker)
	{
		_prompt = picker.CommitButtonText.Length == 0 ? null : picker.CommitButtonText;
		_filters = picker.FileTypeFilter.Except(_asteriskArray).Select(ext => ext.TrimStart('.')).ToArray();
		_identifier = picker.SettingsIdentifier.Length == 0 ? null : picker.SettingsIdentifier;
		_suggestedStartLocation = picker.SuggestedStartLocation;
	}

	private string? _prompt;
	private string[] _filters;
	private string? _identifier;
	private PickerLocationId _suggestedStartLocation;

	public async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token)
	{
		await Task.Yield();
		var array = NativeUno.uno_pick_multiple_files(_prompt, _identifier, (int)_suggestedStartLocation, _filters, _filters.Length);
		var files = new List<StorageFile>();
		var ptr = Marshal.ReadIntPtr(array);
		while (ptr != IntPtr.Zero)
		{
			var filename = Marshal.PtrToStringUTF8(ptr);
			if (filename is not null)
			{
				files.Add(await StorageFile.GetFileFromPathAsync(filename));
			}
			array += IntPtr.Size;
			ptr = Marshal.ReadIntPtr(array);
		}
		return files.ToArray();
	}

	public async Task<StorageFile?> PickSingleFileAsync(CancellationToken token)
	{
		await Task.Yield();
		var file = NativeUno.uno_pick_single_file(_prompt, _identifier, (int)_suggestedStartLocation, _filters, _filters.Length);
		return file is null ? null : await StorageFile.GetFileFromPathAsync(file);
	}
}
