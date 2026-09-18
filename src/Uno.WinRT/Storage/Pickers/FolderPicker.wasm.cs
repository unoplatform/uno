#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;
using Uno.Storage.Pickers;

using NativeMethods = __Windows.Storage.Pickers.FolderPicker.NativeMethods;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private static bool? _fileSystemAccessApiSupported;

		internal static bool IsNativePickerSupported()
		{
			if (_fileSystemAccessApiSupported is null)
			{
				_fileSystemAccessApiSupported = NativeMethods.IsNativeSupported();
			}

			return _fileSystemAccessApiSupported.Value;
		}

		private async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			if (!IsNativePickerSupported())
			{
				throw new NotSupportedException("Could not handle the request using any picker implementation.");
			}

			var startIn = SuggestedStartLocation.ToStartInDirectory();

			var pickedFolderJson = await NativeMethods.PickSingleFolderAsync(SettingsIdentifier, startIn);

			if (pickedFolderJson is null)
			{
				// User did not select any folder.
				return null;
			}

			var info = JsonHelper.Deserialize<NativeStorageItemInfo>(pickedFolderJson, StorageSerializationContext.Default);

			return StorageFolder.GetFromNativeInfo(info, null);
		}
	}
}
