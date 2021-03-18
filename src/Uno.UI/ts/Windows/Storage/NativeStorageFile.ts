namespace Uno.Storage {

	export class NativeStorageFile {

		public static async getBasicPropertiesAsync(guid: string): Promise<string> {
			const handle = <FileSystemFileHandle>NativeStorageItem.getHandle(guid);
			var file = await handle.getFile();
			var propertyString = "";
			propertyString += file.size;
			propertyString += "|";
			propertyString += file.lastModified;
			return propertyString;
		}
	}
}
