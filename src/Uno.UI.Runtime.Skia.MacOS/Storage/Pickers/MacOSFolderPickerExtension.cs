using Windows.Storage;
using Windows.Storage.Pickers;

using Uno.Foundation.Extensibility;
using Uno.Extensions.Storage.Pickers;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSFolderPickerExtension : IFolderPickerExtension
{
	public MacOSFolderPickerExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), _ => new MacOSFolderPickerExtension());

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
	public void Customize(FolderPicker picker)
	{
		_prompt = picker.CommitButtonText.Length == 0 ? null : picker.CommitButtonText;
		// FileTypeFilter can be set but they are not doing anything with the native picker
		_identifier = picker.SettingsIdentifier.Length == 0 ? null : picker.SettingsIdentifier;
		_suggestedStartLocation = picker.SuggestedStartLocation;
	}

	private string? _prompt;
	private string? _identifier;
	private PickerLocationId _suggestedStartLocation;

	public async Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
	{
		await Task.Yield();
		var folder = NativeUno.uno_pick_single_folder(_prompt, _identifier, (int)_suggestedStartLocation);
		return folder is null ? null : await StorageFolder.GetFolderFromPathAsync(folder);
	}
}
