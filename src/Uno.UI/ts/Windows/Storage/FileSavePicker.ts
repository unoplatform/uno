namespace Windows.Storage.Pickers {

	export class FileSavePicker {
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
