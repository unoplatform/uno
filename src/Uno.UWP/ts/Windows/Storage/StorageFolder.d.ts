declare namespace Windows.Storage {
    class StorageFolder {
        private static _isInitialized;
        private static _isSynchronizing;
        private static dispatchStorageInitialized;
        /**
         * Determine if IndexDB is available, some browsers and modes disable it.
         * */
        static isIndexDBAvailable(): boolean;
        /**
         * Setup the storage persistence of a given set of paths.
         * */
        private static makePersistent;
        /**
         * Setup the storage persistence of a given path.
         * */
        static setupStorage(path: string): void;
        private static onStorageInitialized;
        /**
         * Synchronize the IDBFS memory cache back to IndexedDB
         * populate: requests the filesystem to be popuplated from the IndexedDB
         * onSynchronized: function invoked when the synchronization finished
         * */
        private static synchronizeFileSystem;
    }
}
