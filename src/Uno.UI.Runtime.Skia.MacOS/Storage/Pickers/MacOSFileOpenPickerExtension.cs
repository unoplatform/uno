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
		// Always dispatch to the main thread via Enqueue (never call synchronously, even
		// when already on the main thread). NSOpenPanel.runModal pumps the Cocoa event
		// loop, and running it reentrantly from inside an in-flight pointer/click handler
		// crashes the managed InputManager ("A pointer is already being processed") and
		// can leave the returned char** pointer in a corrupt state, triggering an
		// AccessViolationException when the C# side walks the array.
		var tcs = new TaskCompletionSource<IntPtr>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = token.CanBeCanceled ? token.Register(() => tcs.TrySetCanceled(token)) : default;
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (token.IsCancellationRequested)
			{
				tcs.TrySetCanceled(token);
				return;
			}
			IntPtr nativeArray = IntPtr.Zero;
			try
			{
				nativeArray = NativeUno.uno_pick_multiple_files(_prompt, _identifier, (int)_suggestedStartLocation, _filters, _filters.Length);
				if (!tcs.TrySetResult(nativeArray) && nativeArray != IntPtr.Zero)
				{
					// Late cancellation: the awaiter has already observed cancellation and
					// will not consume the array, so free it here to avoid leaking the
					// malloc'd char** plus its strdup'd entries.
					NativeUno.uno_free_string_array(nativeArray);
				}
			}
			catch (Exception ex)
			{
				if (nativeArray != IntPtr.Zero)
				{
					NativeUno.uno_free_string_array(nativeArray);
				}
				tcs.TrySetException(ex);
			}
		});
		var array = await tcs.Task;

		if (array == IntPtr.Zero)
		{
			return Array.Empty<StorageFile>();
		}

		try
		{
			var files = new List<StorageFile>();
			var cursor = array;
			var ptr = Marshal.ReadIntPtr(cursor);
			while (ptr != IntPtr.Zero)
			{
				var filename = Marshal.PtrToStringUTF8(ptr);
				if (filename is not null)
				{
					files.Add(await StorageFile.GetFileFromPathAsync(filename));
				}
				cursor += IntPtr.Size;
				ptr = Marshal.ReadIntPtr(cursor);
			}
			return files.ToArray();
		}
		finally
		{
			NativeUno.uno_free_string_array(array);
		}
	}

	public async Task<StorageFile?> PickSingleFileAsync(CancellationToken token)
	{
		// See PickMultipleFilesAsync for why we always Enqueue instead of branching on HasThreadAccess.
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
		var file = await tcs.Task;

		return file is null ? null : await StorageFile.GetFileFromPathAsync(file);
	}
}
