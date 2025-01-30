namespace Uno.Storage {
	export class NativeStorageItem {

		private static generateGuidBinding: (count: number) => string;
		private static _guidToItemMap: Map<string, FileSystemHandle|File> = new Map<string, FileSystemHandle|File>();
		private static _itemToGuidMap: Map<FileSystemHandle|File, string> = new Map<FileSystemHandle|File, string>();

		public static addItem(guid: string, item: FileSystemHandle | File) {
			NativeStorageItem._guidToItemMap.set(guid, item);
			NativeStorageItem._itemToGuidMap.set(item, guid);
		}

		public static removeItem(guid: string) {
			const handle = NativeStorageItem._guidToItemMap.get(guid);
			NativeStorageItem._guidToItemMap.delete(guid);
			NativeStorageItem._itemToGuidMap.delete(handle);
		}

		public static getItem(guid: string): FileSystemHandle|File {
			return NativeStorageItem._guidToItemMap.get(guid);
		}

		public static async getFile(guid: string): Promise<File> {
			const item = NativeStorageItem.getItem(guid);

			if (item instanceof File) {
				return item as File;
			}
			if (item instanceof FileSystemFileHandle) {
				return await (item as FileSystemFileHandle).getFile();
			}
			if (item instanceof FileSystemDirectoryHandle) {
				throw new Error("Item " + guid + " is a directory handle. You cannot use it as a File!");
			}

			throw new Error("Item " + guid + " is of an unknown type. You cannot use it as a File!");
		}

		public static getGuid(item: FileSystemHandle | File): string {
			return NativeStorageItem._itemToGuidMap.get(item);
		}

		public static getInfos(...items: Array<FileSystemHandle|File>): NativeStorageItemInfo[] {
			const itemsWithoutGuids: Array<FileSystemHandle|File> = [];

			for (const item of items) {
				const guid = NativeStorageItem.getGuid(item);
				if (!guid) {
					itemsWithoutGuids.push(item);
				}
			}

			NativeStorageItem.storeItems(itemsWithoutGuids);

			const results: NativeStorageItemInfo[] = [];

			for (const item of items) {
				const guid = NativeStorageItem.getGuid(item);
				const info = new NativeStorageItemInfo();
				info.id = guid;
				info.name = item.name;
				info.isFile = item instanceof File || (item as FileSystemHandle).kind === "file";
				results.push(info);
			}

			return results;
		}

		private static storeItems(handles: Array<FileSystemHandle|File>) {
			const missingGuids = NativeStorageItem.generateGuids(handles.length);
			for (let i = 0; i < handles.length; i++) {
				NativeStorageItem.addItem(missingGuids[i], handles[i]);
			}
		}

		private static generateGuids(count: number): string[] {
			if (!NativeStorageItem.generateGuidBinding) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					NativeStorageItem.generateGuidBinding = (<any>globalThis).DotnetExports.Uno.Uno.Storage.NativeStorageItem.GenerateGuids;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}

			const guids = NativeStorageItem.generateGuidBinding(count);
			return guids.split(";");
		}
	}
}
