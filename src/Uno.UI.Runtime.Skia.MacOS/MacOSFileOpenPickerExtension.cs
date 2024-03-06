using System.Runtime.InteropServices;

using Windows.Storage;
using Windows.Storage.Pickers;

using Uno.Foundation.Extensibility;
using Uno.Extensions.Storage.Pickers;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSFileOpenPickerExtension : IFileOpenPickerExtension
{
	private static readonly MacOSFileOpenPickerExtension _instance = new();

	private MacOSFileOpenPickerExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), _ => _instance);

	// TODO: we need something more in IFileOpenPickerExtension so we can customize the native picker from the user-created FileOpenPicker
	public void Customize(FileOpenPicker picker)
	{
		// TODO: call native code
	}

	public async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token)
	{
		var array = NativeUno.uno_pick_multiple_files(null);
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
		var file = NativeUno.uno_pick_single_file(null);
		return file is null ? null : await StorageFile.GetFileFromPathAsync(file);
	}
}
