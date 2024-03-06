using Windows.Storage;
using Windows.Storage.Pickers;

using Uno.Foundation.Extensibility;
using Uno.Extensions.Storage.Pickers;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSFolderPickerExtension : IFolderPickerExtension
{
	private static readonly MacOSFolderPickerExtension _instance = new();

	private MacOSFolderPickerExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), _ => _instance);

	public async Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
	{
		var folder = NativeUno.uno_pick_single_folder();
		return folder is null ? null : await StorageFolder.GetFolderFromPathAsync(folder);
	}
}
