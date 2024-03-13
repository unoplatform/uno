// eslint-disable-next-line @typescript-eslint/no-namespace
var Windows;
(function (Windows) {
    var Storage;
    (function (Storage) {
        class StorageFolder {
            /**
             * Determine if IndexDB is available, some browsers and modes disable it.
             * */
            static isIndexDBAvailable() {
                try {
                    // IndexedDB may not be available in private mode
                    window.indexedDB;
                    return true;
                }
                catch (err) {
                    return false;
                }
            }
            /**
             * Setup the storage persistence of a given set of paths.
             * */
            static makePersistent(paths) {
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
            static setupStorage(path) {
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
            static onStorageInitialized() {
                if (!StorageFolder.dispatchStorageInitialized) {
                    if (globalThis.DotnetExports !== undefined) {
                        StorageFolder.dispatchStorageInitialized = globalThis.DotnetExports.Uno.Windows.Storage.StorageFolder.DispatchStorageInitialized;
                    }
                    else {
                        StorageFolder.dispatchStorageInitialized =
                            Module.mono_bind_static_method("[Uno] Windows.Storage.StorageFolder:DispatchStorageInitialized");
                    }
                }
                StorageFolder.dispatchStorageInitialized();
            }
            /**
             * Synchronize the IDBFS memory cache back to IndexedDB
             * populate: requests the filesystem to be popuplated from the IndexedDB
             * onSynchronized: function invoked when the synchronization finished
             * */
            static synchronizeFileSystem(populate, onSynchronized = null) {
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
        StorageFolder._isInitialized = false;
        StorageFolder._isSynchronizing = false;
        Storage.StorageFolder = StorageFolder;
    })(Storage = Windows.Storage || (Windows.Storage = {}));
})(Windows || (Windows = {}));
