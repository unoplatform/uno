namespace Windows.Storage.Pickers {

	export class FileOpenPicker {
		public static isNativeSupported(): boolean {
			return typeof showOpenFilePicker === "function";
		}

		public static async nativePickFilesAsync(
			multiple: boolean,
			showAllEntry: boolean,
			fileTypesJson: string): Promise<string> {

			if (!FileOpenPicker.isNativeSupported()) {
				return JSON.stringify([]);
			}

			const options: OpenFilePickerOptions = {
				excludeAcceptAllOption: !showAllEntry,
				multiple: multiple,
				types: [],
			};

			const acceptTypes = <Uno.Storage.Pickers.NativeFilePickerAcceptType[]>JSON.parse(fileTypesJson);

			for (const acceptType of acceptTypes) {
				const pickerAcceptType: FilePickerAcceptType = {
					accept: {},
					description: acceptType.description,
				};

				for (const acceptTypeItem of acceptType.accept) {
					pickerAcceptType.accept[acceptTypeItem.mimeType] = acceptTypeItem.extensions
				}

				options.types.push(pickerAcceptType);
			}

			try {
				const selectedFiles = await showOpenFilePicker(options);
				const infos = Uno.Storage.NativeStorageItem.getInfos(...selectedFiles);
				const json = JSON.stringify(infos);
				return json;
			}
			catch (e) {
				console.log("User did not make a selection or the file selected was" +
					"deemed too sensitive or dangerous to be exposed to the website - " + e);
				return JSON.stringify([]);
			}
		}

		public static uploadPickFilesAsync(multiple: boolean, targetPath: string, accept: string): Promise<string> {
			return new Promise<string>((resolve, reject) => {
				const inputElement = document.createElement("input");

				inputElement.type = "file";
				inputElement.multiple = multiple;
				inputElement.accept = accept;
				inputElement.onchange = async e => {

					const existingFileNames = new Set<string>();

					var adjustedTargetPath = targetPath;
					if (!adjustedTargetPath.endsWith('/')) {
						adjustedTargetPath += '/';
					}

					var duplicateFileId = 0;
					for (const file of inputElement.files) {
						const fileBuffer = await file.arrayBuffer();
						const fileBufferView = new Uint8Array(fileBuffer);
						var targetFileName = "";
						if (!existingFileNames.has(file.name)) {
							targetFileName = adjustedTargetPath + file.name;
							existingFileNames.add(file.name);
						}
						else {
							targetFileName = adjustedTargetPath + duplicateFileId + "_" + file.name;
							duplicateFileId++;
						}
						FS.writeFile(targetFileName, fileBufferView);
					}

					resolve(inputElement.files.length.toString());
				}
				inputElement.click();
			});
		}
	}
}
