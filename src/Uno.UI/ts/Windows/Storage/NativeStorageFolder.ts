namespace Uno.Storage {

	export class NativeStorageFolder {

		/**
		 * Creates a new folder inside another folder.
		 * @param parentGuid The GUID of the folder to create in.
		 * @param folderName The name of the new folder.
		 */
		public static async createFolderAsync(parentGuid: string, folderName: string): Promise<string> {
			try {
				const parentHandle = <FileSystemDirectoryHandle>NativeStorageItem.getHandle(parentGuid);

				const newDirectoryHandle = await parentHandle.getDirectoryHandle(folderName, {
					create: true,
				});

				const info = NativeStorageItem.getInfos(newDirectoryHandle)[0];
				return JSON.stringify(info);
			}
			catch {
				console.log("Could not create folder" + folderName);
				return null;
			}
		}

		/**
		 * Creates a new file inside another folder.
		 * @param parentGuid The GUID of the folder to create in.
		 * @param folderName The name of the new file.
		 */
		public static async createFileAsync(parentGuid: string, fileName: string): Promise<string> {
			try {
				const parentHandle = <FileSystemDirectoryHandle>NativeStorageItem.getHandle(parentGuid);

				const newFileHandle = await parentHandle.getFileHandle(fileName, {
					create: true,
				});

				const info = NativeStorageItem.getInfos(newFileHandle)[0];
				return JSON.stringify(info);
			}
			catch {
				console.log("Could not create file " + fileName);
				return null;
			}
		}

		/**
		 * Tries to get a folder in the given parent folder by name.
		 * @param parentGuid The GUID of the parent folder to get.
		 * @param folderName The name of the folder to look for.
		 * @returns A GUID of the folder if found, otherwise null.
		 */
		public static async tryGetFolderAsync(parentGuid: string, folderName: string): Promise<string> {
			const parentHandle = <FileSystemDirectoryHandle>NativeStorageItem.getHandle(parentGuid);

			let nestedDirectoryHandle: FileSystemDirectoryHandle = undefined;

			try {
				nestedDirectoryHandle = await parentHandle.getDirectoryHandle(folderName);
			} catch (ex) {
				return null;
			}

			if (nestedDirectoryHandle) {
				return JSON.stringify(NativeStorageItem.getInfos(nestedDirectoryHandle)[0]);
			}

			return null;
		}

		/**
		* Tries to get a file in the given parent folder by name.
		* @param parentGuid The GUID of the parent folder to get.
		* @param folderName The name of the folder to look for.
		* @returns A GUID of the folder if found, otherwise null.
		*/
		public static async tryGetFileAsync(parentGuid: string, fileName: string): Promise<string> {
			const parentHandle = <FileSystemDirectoryHandle>NativeStorageItem.getHandle(parentGuid);

			let fileHandle: FileSystemFileHandle = undefined;

			try {
				fileHandle = await parentHandle.getFileHandle(fileName);
			} catch (ex) {
				return null;
			}

			if (fileHandle) {
				return JSON.stringify(NativeStorageItem.getInfos(fileHandle)[0]);
			}

			return null;
		}

		public static async deleteItemAsync(parentGuid: string, itemName: string): Promise<string> {
			try {
				const parentHandle = <FileSystemDirectoryHandle>NativeStorageItem.getHandle(parentGuid);

				await parentHandle.removeEntry(itemName, { recursive: true });

				return "OK";
			}
			catch {
				return null;
			}
		}

		public static async getItemsAsync(folderGuid: string): Promise<string> {
			return await NativeStorageFolder.getEntriesAsync(folderGuid, true, true);
		}

		public static async getFoldersAsync(folderGuid: string): Promise<string> {
			return await NativeStorageFolder.getEntriesAsync(folderGuid, false, true);
		}

		public static async getFilesAsync(folderGuid: string): Promise<string> {
			return await NativeStorageFolder.getEntriesAsync(folderGuid, true, false);
		}

		public static async getPrivateRootAsync(): Promise<string> {
			if (!navigator.storage.getDirectory) {
				return null;
			}

			const directory = await navigator.storage.getDirectory();
			if (!directory) {
				return null;
			}

			const info = NativeStorageItem.getInfos(directory)[0];
			return JSON.stringify(info);
		}

		private static async getEntriesAsync(guid: string, includeFiles: boolean, includeDirectories: boolean): Promise<string> {
			const folderHandle = <FileSystemDirectoryHandle>NativeStorageItem.getHandle(guid);

			var entries: FileSystemHandle[] = [];

			// Default to "modern" implementation
			if (folderHandle.values) {
				for await (var entry of folderHandle.values()) {
					entries.push(entry);
				}
			}
			else {
				for await (var handle of folderHandle.getEntries()) {
					entries.push(handle);
				}
			}

			var filteredHandles: FileSystemHandle[] = [];

			// Filter
			for (var handle of entries) {
				if (handle.kind == "file" && includeFiles) {
					filteredHandles.push(handle);
				}
				else if (handle.kind == "directory" && includeDirectories) {
					filteredHandles.push(handle);
				}
			}

			// Get infos
			var infos = NativeStorageItem.getInfos(...filteredHandles);
			var json = JSON.stringify(infos);
			return json;
		}
	}
}
