
// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Windows.Storage {

	export class StorageFolder {
		private static _isInit = false;
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
		private static makePersistent(pParams: number): void {
			const params = StorageFolderMakePersistentParams.unmarshal(pParams);

			for (var i = 0; i < params.Paths.length; i++) {
				this.setupStorage(params.Paths[i])
			}
		}

		/**
		 * Setup the storage persistence of a given path.
		 * */
		public static setupStorage(path: string): void {
			if (Uno.UI.WindowManager.isHosted) {
				console.debug("Hosted Mode: skipping IndexDB initialization");

				StorageFolder.onStorageInitialized();
				return;
			}

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

			// Ensure to sync pseudo file system on unload (and periodically for safety)
			if (!this._isInit) {

			// Request an initial sync to populate the file system
				FS.syncfs(true, err => {
					if (err) {
						console.error(`Error synchronizing filesystem from IndexDB: ${err} (errno: ${err.errno})`);
					}
					StorageFolder.onStorageInitialized();
				});

				window.addEventListener("beforeunload", this.synchronizeFileSystem);
				setInterval(this.synchronizeFileSystem, 10000);

				this._isInit = true;
			}
		}

		private static onStorageInitialized() {
			if (!StorageFolder.dispatchStorageInitialized) {
				StorageFolder.dispatchStorageInitialized =
					(<any>Module).mono_bind_static_method(
						"[Uno] Windows.Storage.StorageFolder:DispatchStorageInitialized");
			}
			StorageFolder.dispatchStorageInitialized();
		}

		/**
		 * Synchronize the IDBFS memory cache back to IndexDB
		 * */
		private static synchronizeFileSystem(): void {
			FS.syncfs(err => {
				if (err) {
					console.error(`Error synchronizing filesystem from IndexDB: ${err} (errno: ${err.errno})`);
			}});
		}
	}
}
