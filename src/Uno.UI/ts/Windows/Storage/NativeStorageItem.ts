namespace Uno.Storage {
	export class NativeStorageItem {

		private static generateGuidBinding: (count: number) => string;
		private static _guidToHandleMap: Map<string, FileSystemHandle> = new Map<string, FileSystemHandle>();
		private static _handleToGuidMap: Map<FileSystemHandle, string> = new Map<FileSystemHandle, string>();

		public static addHandle(guid: string, handle: FileSystemHandle) {
			NativeStorageItem._guidToHandleMap.set(guid, handle);
			NativeStorageItem._handleToGuidMap.set(handle, guid);
		}

		public static removeHandle(guid: string) {
			const handle = NativeStorageItem._guidToHandleMap.get(guid);
			NativeStorageItem._guidToHandleMap.delete(guid);
			NativeStorageItem._handleToGuidMap.delete(handle);
		}

		public static getHandle(guid: string): FileSystemHandle {
			return NativeStorageItem._guidToHandleMap.get(guid);
		}

		public static getGuid(handle: FileSystemHandle): string {
			return NativeStorageItem._handleToGuidMap.get(handle);
		}

		public static getInfos(... handles: FileSystemHandle[]): NativeStorageItemInfo[] {
			var handlesWithoutGuids: FileSystemHandle[] = [];

			for (var handle of handles) {
				var guid = NativeStorageItem.getGuid(handle);
				if (!guid) {
					handlesWithoutGuids.push(handle);
				}
			}

			NativeStorageItem.storeHandles(handlesWithoutGuids);

			var results: NativeStorageItemInfo[] = [];

			for (var handle of handles) {
				var guid = NativeStorageItem.getGuid(handle);
				var info = new NativeStorageItemInfo();
				info.id = guid;
				info.name = handle.name;
				info.isFile = handle.kind === "file";
				results.push(info);
			}

			return results;
		}

		private static storeHandles(handles: FileSystemHandle[]) {
			var missingGuids = NativeStorageItem.generateGuids(handles.length);
			for (var i = 0; i < handles.length; i++) {
				NativeStorageItem.addHandle(missingGuids[i], handles[i]);
			}
		}

		private static generateGuids(count: number): string[] {
			if (!NativeStorageItem.generateGuidBinding) {
				NativeStorageItem.generateGuidBinding = (<any>Module).mono_bind_static_method("[Uno] Uno.Storage.NativeStorageItem:GenerateGuids");
			}

			var guids = NativeStorageItem.generateGuidBinding(count);
			return guids.split(";");
		}
	}
}
