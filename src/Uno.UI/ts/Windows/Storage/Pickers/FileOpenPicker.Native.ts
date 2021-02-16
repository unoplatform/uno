namespace Windows.Storage.Pickers {

	export class FileOpenPicker {
		public static async pickFilesAsync(multiple: boolean, showAllEntry: boolean, fileTypes: any): Promise<string> {

			if (!showOpenFilePicker) {
				return "";
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
				var results = "";
				for (const selectedFile of selectedFiles) {
					const guid = Uno.Storage.NativeStorageItem.generateGuid();
					NativeStorageFile.AddHandle(guid, selectedFile);
					const fileInfo = await selectedFile.getFile();
					const name = fileInfo.name;
					const contentType = fileInfo.type;
					results += guid + "\\" + name + "\\" + contentType + "\\\\";
				}

				return results;
			}
			catch (e) {			
				console.log("User did not make a selection or it file selected was" +
					"deemed too sensitive or dangerous to be exposed to the website - " + e);
				return "";
			}
		}
	}
}
