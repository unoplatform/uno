using System.Runtime.InteropServices;

using Windows.Storage;
using Windows.Storage.Pickers;

using Uno.Foundation.Extensibility;
using Uno.Extensions.Storage.Pickers;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSFileOpenPickerExtension : IFileOpenPickerExtension
{
	private static readonly MacOSFileOpenPickerExtension _instance = new();

	private static readonly string[] _asteriskArray = new string[] { "*" };

	private MacOSFileOpenPickerExtension()
	{
		_filters = Array.Empty<string>();
	}

	public static void Register() => ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), _ => _instance);

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
		IntPtr array;
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			array = NativeUno.uno_pick_multiple_files(_prompt, _identifier, (int)_suggestedStartLocation, _filters, _filters.Length);
		}
		else
		{
			var tcs = new TaskCompletionSource<IntPtr>(TaskCreationOptions.RunContinuationsAsynchronously);
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
					tcs.TrySetResult(NativeUno.uno_pick_multiple_files(_prompt, _identifier, (int)_suggestedStartLocation, _filters, _filters.Length));
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			});
			array = await tcs.Task;
		}

		if (array == IntPtr.Zero)
		{
			return Array.Empty<StorageFile>();
		}

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
		string? file;
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			file = NativeUno.uno_pick_single_file(_prompt, _identifier, (int)_suggestedStartLocation, _filters, _filters.Length);
		}
		else
		{
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
					tcs.TrySetResult(NativeUno.uno_pick_single_file(_prompt, _identifier, (int)_suggestedStartLocation, _filters, _filters.Length));
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			});
			file = await tcs.Task;
		}

		return file is null ? null : await StorageFile.GetFileFromPathAsync(file);
	}
}
