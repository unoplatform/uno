namespace Uno.Storage {

	export class NativeStorageFile {

		public static async getBasicPropertiesAsync(guid: string): Promise<string> {
			const file = await NativeStorageItem.getFile(guid);
			var propertyString = "";
			propertyString += file.size;
			propertyString += "|";
			propertyString += file.lastModified;
			return propertyString;
		}
	}
}
