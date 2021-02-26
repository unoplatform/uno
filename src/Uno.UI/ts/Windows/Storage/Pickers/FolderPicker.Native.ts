namespace Windows.Storage.Pickers {

	export class FolderPicker {
		public static async pickSingleFolderAsync(): Promise<string> {
			try {
				const selectedFolder = await showDirectoryPicker();

				const guid = Uno.Utils.Guid.NewGuid();

				NativeStorageFolder.AddHandle(guid, selectedFolder);

				return guid;
			}
			catch (e) {
				console.log("The user dismissed the prompt without making a selection, " +
					"or the user agent deems the selected content to be too sensitive or dangerous - " + e);
				return null;
			}
		}
	}
}
