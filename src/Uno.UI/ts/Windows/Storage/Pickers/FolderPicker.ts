namespace Windows.Storage.Pickers {

	export class FolderPicker {
		public static async pickSingleFolderAsync(): Promise<string> {
			const selectedFolder = await showDirectoryPicker();

			const guid = Uno.Utils.Guid.NewGuid();

			StorageFolderNative.AddHandle(guid, selectedFolder);

			return guid;
		}
	}
}
