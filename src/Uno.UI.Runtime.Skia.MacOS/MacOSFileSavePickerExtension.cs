using Windows.Storage;
using Windows.Storage.Pickers;

using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSFileSavePickerExtension : IFileSavePickerExtension
{
	private static readonly MacOSFileSavePickerExtension _instance = new();

	private MacOSFileSavePickerExtension()
	{
		_filters = Array.Empty<string>();
	}

	public static void Register() => ApiExtensibility.Register<FileSavePicker>(typeof(IFileSavePickerExtension), _ => _instance);

	// Mapping
	// WinUI                            AppKit (NSSavePanel)
	// ----------------------------------------------------------------------
	// CommitButtonText (string)        prompt (NSString)
	// ContinuationData (ValueSet)      n/a
	// DefaultFileExtension (string)    _Do not set this property_ says WinUI
	// EnterpriseId                     n/a
	// FileTypeChoices (dictionary)     allowedFileTypes (NSArray)
	// SettingsIdentifier (string)      identifier (NSString)
	// SuggestedFileName (string)       nameFieldStringValue (NSString)
	// SuggestedSaveFile (StorageFile)  n/a
	// SuggestedStartLocation (enum)    directoryURL (NSURL)
	// User (User)                      n/a
	public void Customize(FileSavePicker picker)
	{
		_prompt = picker.CommitButtonText.Length == 0 ? null : picker.CommitButtonText;
		_identifier = picker.SettingsIdentifier.Length == 0 ? null : picker.SettingsIdentifier;
		_suggestedFileName = picker.SuggestedFileName.Length == 0 ? null : picker.SuggestedFileName;
		_suggestedStartLocation = picker.SuggestedStartLocation;
		if (picker.FileTypeChoices.Count == 0)
		{
			return;
		}

		var list = new List<string>();
		foreach (var value in picker.FileTypeChoices.Values)
		{
			foreach (var ext in value)
			{
				list.Add(ext.TrimStart('.'));
			}
		}
		_filters = list.ToArray();
	}

	private string? _prompt;
	private string[] _filters;
	private string? _identifier;
	private string? _suggestedFileName;
	private PickerLocationId _suggestedStartLocation;

	public async Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
	{
		var file = NativeUno.uno_pick_save_file(_prompt, _identifier, _suggestedFileName, (int)_suggestedStartLocation, _filters, _filters.Length);
		return file is null ? null : await StorageFile.GetFileFromPathAsync(file);
	}
}
