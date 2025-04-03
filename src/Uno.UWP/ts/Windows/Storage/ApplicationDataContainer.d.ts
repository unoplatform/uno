declare namespace Windows.Storage {
    class ApplicationDataContainer {
        private static buildStorageKey;
        private static buildStoragePrefix;
        /**
         * Try to get a value from localStorage
         * */
        private static tryGetValue;
        /**
         * Set a value to localStorage
         * */
        private static setValue;
        /**
         * Determines if a key is contained in localStorage
         * */
        private static containsKey;
        /**
         * Gets a key by index in localStorage
         * */
        private static getKeyByIndex;
        /**
         * Determines the number of items contained in localStorage
         * */
        private static getCount;
        /**
         * Clears items contained in localStorage
         * */
        private static clear;
        /**
         * Removes an item contained in localStorage
         * */
        private static remove;
        /**
         * Gets a key by index in localStorage
         * */
        private static getValueByIndex;
    }
}
