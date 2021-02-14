namespace Windows.Storage.Pickers {

	export class FileOpenPicker {
		public static async pickFilesAsync(multiple: boolean): Promise<string> {

			if (!showOpenFilePicker) {
				return null;
			}

			const selectedFiles = await showOpenFilePicker({
				multiple: multiple,
				excludeAcceptAllOption: false
			});

			const guid = Uno.Utils.Guid.NewGuid();

			for (var i = 0; i < selectedFiles.length; i++) {
				StorageFileNative.AddHandle(guid, selectedFiles[i]);
			}

			return guid;
		}
	}
}
