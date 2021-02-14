
namespace Windows.Storage {

	export class StorageFileNative {
		private static _fileMap: Map<string, FileSystemFileHandle> = new Map<string, FileSystemFileHandle>();

		public static AddHandle(guid: string, handle: FileSystemFileHandle) {
			StorageFileNative._fileMap.set(guid, handle);
		}

		public static RemoveHandle(guid: string) {
			StorageFileNative._fileMap.delete(guid);
		}

		public static GetHandle(guid: string): FileSystemFileHandle {
			return StorageFileNative._fileMap.get(guid);
		}
	}
}
