namespace Windows.Storage.Pickers {

	export class FolderPicker {
		public static isNativeSupported(): boolean {
			return typeof showDirectoryPicker === "function";
		}

		public static async pickSingleFolderAsync(id: string, startIn: StartInDirectory): Promise<string> {
			if (!FolderPicker.isNativeSupported()) {
				return null;
			}

			try {
				const options: DirectoryPickerOptions = {
					id: id,
					startIn: startIn,
				};

				const selectedFolder = await showDirectoryPicker(options);

				const info = Uno.Storage.NativeStorageItem.getInfos(selectedFolder)[0];
				return JSON.stringify(info);
			}
			catch (e) {
				console.log("The user dismissed the prompt without making a selection, " +
					"or the user agent deems the selected content to be too sensitive or dangerous - " + e);
				return null;
			}
		}
	}
}
