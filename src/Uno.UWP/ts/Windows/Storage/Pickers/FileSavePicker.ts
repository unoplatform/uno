namespace Windows.Storage.Pickers {

	export class FileSavePicker {
		public static isNativeSupported(): boolean {
			return typeof showSaveFilePicker === "function";
		}

		public static async nativePickSaveFileAsync(
			showAllEntry: boolean,
			fileTypesJson: string,
			suggestedFileName: string,
			id: string,
			startIn: StartInDirectory): Promise<string> {

			if (!FileSavePicker.isNativeSupported()) {
				return null;
			}

			const options: SaveFilePickerOptions = {
				excludeAcceptAllOption: !showAllEntry,
				id: id,
				startIn: startIn,
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

			if (suggestedFileName != "") {
				// In case the suggested file name does not end with any extension provided by the app
				// we attach the first one if such exists. This is because JS could otherwise truncate
				// the filename incorrectly, e.g.:
				// "this.is.a.filename" would get truncated to "this"
				var lowerCaseFileName = suggestedFileName.toLowerCase();
				if (!acceptTypes.some(f => f.accept.some(a => a.extensions.some(e => lowerCaseFileName.endsWith(e.toLowerCase())))) &&
					acceptTypes.length > 0) {
					suggestedFileName += acceptTypes[0].accept[0].extensions[0];
				}

				options.suggestedName = suggestedFileName;
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
