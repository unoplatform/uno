namespace Windows.Storage.Pickers {

	export class FileSavePicker {
		public static isNativeSupported(): boolean {
			return typeof showSaveFilePicker === "function";
		}

		public static async nativePickSaveFileAsync(showAllEntry: boolean, fileTypesJson: string): Promise<string> {

			if (!FileSavePicker.isNativeSupported()) {
				return null;
			}

			const options: SaveFilePickerOptions = {
				excludeAcceptAllOption: !showAllEntry,
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
				const selectedFile = await showSaveFilePicker(options);
				const info = Uno.Storage.NativeStorageItem.getInfos(selectedFile)[0];
				const json = JSON.stringify(info);
				return json;
			}
			catch (e) {
				console.log("User did not make a selection or the file selected was" +
					"deemed too sensitive or dangerous to be exposed to the website - " + e);
				return null;
			}
		}

		public static SaveAs(fileName: string, dataPtr: any, size: number): void {

			const buffer = new Uint8Array(size);

			for (var i = 0; i < size; i++) {
				buffer[i] = Module.getValue(dataPtr + i, "i8");
			}

			const a = window.document.createElement('a');
			const blob = new Blob([buffer]);

			a.href = window.URL.createObjectURL(blob);
			a.download = fileName;

			document.body.appendChild(a);
			a.click();
			document.body.removeChild(a);
		} 
	}
}
