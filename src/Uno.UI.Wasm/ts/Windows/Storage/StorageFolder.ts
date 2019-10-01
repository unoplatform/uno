namespace Windows.Storage {

	export class StorageFolder {
		private static _isInit = false;

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

				return;
			}

			if (!this.isIndexDBAvailable()) {
				console.warn("IndexedDB is not available (private mode or uri starts with file:// ?), changes will not be persisted.");

				return;
			}

			console.debug("Making persistent: " + path);

			FS.mkdir(path);
			FS.mount(IDBFS, {}, path);
			// Request an initial sync to populate the file system
			const that = this;
			FS.syncfs(true, err => {
				if (err) {
					console.error(`Error synchronizing filesystem from IndexDB: ${err}`);
				}
			});

			// Ensure to sync pseudo file system on unload (and periodically for safety)
			if (!this._isInit) {

				window.addEventListener("beforeunload", this.synchronizeFileSystem);
				setInterval(this.synchronizeFileSystem, 10000);

				this._isInit = true;
			}
		}

		/**
		 * Synchronize the IDBFS memory cache back to IndexDB
		 * */
		private static synchronizeFileSystem(): void {
			FS.syncfs(err => {
				if (err) {
					console.error(`Error synchronizing filesystem from IndexDB: ${err}`);
			}});
		}
	}
}
