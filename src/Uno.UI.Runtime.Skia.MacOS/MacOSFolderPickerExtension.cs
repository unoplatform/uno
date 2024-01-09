#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;

using Uno.Foundation.Extensibility;
using Uno.Extensions.Storage.Pickers;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSFolderPickerExtension : IFolderPickerExtension
{
	public static MacOSFolderPickerExtension Instance = new();

	private MacOSFolderPickerExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => Instance);

	public async Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
	{
		var folder = NativeUno.uno_pick_single_folder();
		return folder is null ? null : await StorageFolder.GetFolderFromPathAsync(folder);
	}
}
