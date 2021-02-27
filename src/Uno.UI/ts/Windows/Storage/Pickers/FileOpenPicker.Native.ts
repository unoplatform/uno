namespace Windows.Storage.Pickers {

	export class FileOpenPicker {
		public static isNativeSupported(): boolean {
			return typeof showOpenFilePicker === "function";
		}

		public static async pickFilesAsync(multiple: boolean, showAllEntry: boolean, fileTypes: any): Promise<string> {

			if (!FileOpenPicker.isNativeSupported()) {
				return JSON.stringify([]);
			}

			const options: OpenFilePickerOptions  = {
				multiple: multiple,
				excludeAcceptAllOption: !showAllEntry,
				types: [],
			};

			var fullAccept:any = {};

			for (var property in fileTypes) {
				const mimeType = property;
				const extensions = fileTypes[property];
				const acceptType: FilePickerAcceptType = {
					accept: {
						[mimeType]: extensions
					},
					description: extensions.length > 1 ? "" : extensions[0],
				};
				fullAccept[mimeType] = extensions;
				options.types.push(acceptType);
			}

			if (fileTypes.length > 1) {
				options.types.unshift({
					accept: fullAccept,
					description: "All"
				});
			}

			try {
				const selectedFiles = await showOpenFilePicker(options);
				const infos = Uno.Storage.NativeStorageItem.getInfos(...selectedFiles);
				const json = JSON.stringify(infos);
				return json;
			}
			catch (e) {			
				console.log("User did not make a selection or it file selected was" +
					"deemed too sensitive or dangerous to be exposed to the website - " + e);
				return JSON.stringify([]);
			}
		}
	}
}
