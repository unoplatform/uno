namespace Windows.Storage.Pickers {

	export class FolderPicker {
		public static async pickSingleFolderAsync(): Promise<string> {
			try {
				const selectedFolder = await showDirectoryPicker();

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
