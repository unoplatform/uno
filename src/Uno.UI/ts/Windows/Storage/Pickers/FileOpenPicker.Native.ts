namespace Windows.Storage.Pickers {

	export class FileOpenPicker {
		public static async pickFilesAsync(multiple: boolean, showAllEntry: boolean, fileTypes: any): Promise<string> {

			if (!showOpenFilePicker) {
				return "";
			}

			var options: OpenFilePickerOptions  = {
				multiple: multiple,
				excludeAcceptAllOption: !showAllEntry,
				types: []
			};

			for (var property in fileTypes) {
				var mimeType = property;
				var extensions = fileTypes[property];
				var acceptType: FilePickerAcceptType = {
					description: "",
					accept: {
						[mimeType]: extensions
					}
				};
				options.types.push(acceptType);
			}

			const selectedFiles = await showOpenFilePicker(options);

			var results = "";
			for (var i = 0; i < selectedFiles.length; i++) {
				const guid = Uno.Utils.Guid.NewGuid();
				StorageFileNative.AddHandle(guid, selectedFiles[i]);
				results = results + guid + ";";
			}

			return results;
		}
	}
}
