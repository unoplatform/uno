using Windows.Storage;
using Windows.Storage.Pickers;

using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Extensibility;
using Uno.UI.Dispatching;

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
		// Always Enqueue — running NSSavePanel.runModal reentrantly from an in-flight
		// pointer handler crashes InputManager and corrupts return state. See
		// MacOSFileOpenPickerExtension.PickMultipleFilesAsync for full context.
		var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = token.CanBeCanceled ? token.Register(() => tcs.TrySetCanceled(token)) : default;
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (token.IsCancellationRequested)
			{
				tcs.TrySetCanceled(token);
				return;
			}
			try
			{
				tcs.TrySetResult(NativeUno.uno_pick_save_file(_prompt, _identifier, _suggestedFileName, (int)_suggestedStartLocation, _filters, _filters.Length));
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		});
		var file = await tcs.Task;

		return file is null ? null : await StorageFile.GetFileFromPathAsync(file);
	}
}
