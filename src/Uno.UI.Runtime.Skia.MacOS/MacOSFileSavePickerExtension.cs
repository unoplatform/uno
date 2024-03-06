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
	}

	public static void Register() => ApiExtensibility.Register<FileSavePicker>(typeof(IFileSavePickerExtension), _ => _instance);

	public async Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
	{
		var file = NativeUno.uno_pick_save_file(null);
		return file is null ? null : await StorageFile.GetFileFromPathAsync(file);
	}
}
