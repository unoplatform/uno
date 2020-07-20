namespace Windows.Storage.Pickers  {

	export class FileSavePicker {
		public static SelectFileLocation(fileTypeChoices: { [fileType: string]: string[] }, suggestedStartLocation: string, suggestedFileName: string): string {
			console.log(fileTypeChoices);
			console.log(suggestedStartLocation);
			console.log(suggestedFileName);

			return 'C:\\Users\\Alex\\Documents\\Test.txt';
		}
	}
}
