using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using static Uno.WinRTFeatureConfiguration.Storage;

namespace Uno.UI.Runtime.Skia.Win32.Storage.Pickers;

internal static class SuggestedStartLocationHandler
{
	internal static unsafe IDisposable SetDefaultFolder(PickerLocationId startLocation, Func<ComScope<IShellItem>, HRESULT> defaultFolderSetter)
	{
		if (startLocation == PickerLocationId.Unspecified)
		{
			return Disposable.Empty;
		}

		Guid? folderGuid = startLocation is PickerLocationId.ComputerFolder ?
			PickerHelpers.WindowsComputerFolderGUID :
			null;

		var folderPath = folderGuid is null ?
			PickerHelpers.GetInitialDirectory(startLocation) :
			null;

		if (folderGuid is not null || !string.IsNullOrEmpty(folderPath))
		{
			void* defaultFolderItemRaw;
			var hResult = folderGuid is not null
				? PInvoke.SHCreateItemInKnownFolder(folderGuid.Value, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, IShellItem.IID_Guid, out defaultFolderItemRaw)
				: PInvoke.SHCreateItemFromParsingName(folderPath, null, IShellItem.IID_Guid, out defaultFolderItemRaw);

			if (hResult.Failed)
			{
				var methodName = folderGuid is not null ?
					nameof(PInvoke.SHCreateItemInKnownFolder) :
					nameof(PInvoke.SHCreateItemFromParsingName);
				typeof(SuggestedStartLocationHandler).LogError()?.Error($"{methodName} failed: {Win32Helper.GetErrorMessage(hResult)}");
			}

			ComScope<IShellItem> defaultFolderItem = new((IShellItem*)defaultFolderItemRaw);

			hResult = defaultFolderSetter.Invoke(defaultFolderItem);
			if (hResult.Failed)
			{
				typeof(SuggestedStartLocationHandler).LogError()?.Error($"{nameof(IFileDialog.SetDefaultFolder)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			}

			return Disposable.Create(() =>
			{
				defaultFolderItem.Dispose();
			});
		}

		return Disposable.Empty;
	}
}
