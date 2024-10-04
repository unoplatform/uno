
// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Windows.Storage {

	export class StorageFolder {
		private static _isInitialized = false;
		private static _isSynchronizing = false;
		private static dispatchStorageInitialized: () => number;

		/**
		 * Determine if IndexDB is available, some browsers and modes disable it.
		 * */
		public static isIndexDBAvailable(): boolean {
			try {
				// IndexedDB may not be available in private mode
				window.indexedDB;
				return true;
			} catch (err) {
				return false;
			}
		}

		/**
		 * Setup the storage persistence of a given set of paths.
		 * */
		private static makePersistent(paths: string[]): void {
			for (var i = 0; i < paths.length; i++) {
				this.setupStorage(paths[i]);
			}

			// Ensure to sync pseudo file system on unload (and periodically for safety)
			if (!this._isInitialized) {
				// Request an initial sync to populate the file system
				StorageFolder.synchronizeFileSystem(true, () => StorageFolder.onStorageInitialized());

				window.addEventListener("beforeunload", () => this.synchronizeFileSystem(false));
				setInterval(() => this.synchronizeFileSystem(false), 10000);

				this._isInitialized = true;
			}
		}

		/**
		 * Setup the storage persistence of a given path.
		 * */
		public static setupStorage(path: string): void {
			if (!this.isIndexDBAvailable()) {
				console.warn("IndexedDB is not available (private mode or uri starts with file:// ?), changes will not be persisted.");

				StorageFolder.onStorageInitialized();
				return;
			}

			if (typeof IDBFS === 'undefined') {
				console.warn(`IDBFS is not enabled in mono's configuration, persistence is disabled`);

				StorageFolder.onStorageInitialized();
				return;
			}

			console.debug("Making persistent: " + path);

			FS.mkdir(path);

			FS.mount(IDBFS, {}, path);
		}

		private static onStorageInitialized() {
			if (!StorageFolder.dispatchStorageInitialized) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					StorageFolder.dispatchStorageInitialized = (<any>globalThis).DotnetExports.Uno.Windows.Storage.StorageFolder.DispatchStorageInitialized;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}
			StorageFolder.dispatchStorageInitialized();
		}

		/**
		 * Synchronize the IDBFS memory cache back to IndexedDB
		 * populate: requests the filesystem to be popuplated from the IndexedDB
		 * onSynchronized: function invoked when the synchronization finished
		 * */
		private static synchronizeFileSystem(populate: boolean, onSynchronized: Function = null): void {

			if (!StorageFolder._isSynchronizing) {
				StorageFolder._isSynchronizing = true;

				FS.syncfs(populate, err => {
					StorageFolder._isSynchronizing = false;

					if (onSynchronized) {
						onSynchronized();
					}

					if (err) {
						console.error(`Error synchronizing filesystem from IndexDB: ${err} (errno: ${err.errno})`);
					}
				});
			}
		}
	}
}
