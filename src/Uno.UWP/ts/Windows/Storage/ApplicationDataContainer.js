// eslint-disable-next-line @typescript-eslint/no-namespace
var Windows;
(function (Windows) {
    var Storage;
    (function (Storage) {
        class ApplicationDataContainer {
            static buildStorageKey(locality, key) {
                return `UnoApplicationDataContainer_${locality}_${key}`;
            }
            static buildStoragePrefix(locality) {
                return `UnoApplicationDataContainer_${locality}_`;
            }
            /**
             * Try to get a value from localStorage
             * */
            static tryGetValue(locality, key) {
                const storageKey = ApplicationDataContainer.buildStorageKey(locality, key);
                try {
                    if (localStorage.hasOwnProperty(storageKey)) {
                        return { hasValue: true, value: localStorage.getItem(storageKey) };
                    }
                    else {
                        return { hasValue: false, value: "" };
                    }
                }
                catch (e) {
                    console.debug(`ApplicationDataContainer.tryGetValue failed: ${e}`);
                    return { hasValue: false, value: "" };
                }
            }
            /**
             * Set a value to localStorage
             * */
            static setValue(locality, key, value) {
                try {
                    const storageKey = ApplicationDataContainer.buildStorageKey(locality, key);
                    localStorage.setItem(storageKey, value);
                }
                catch (e) {
                    console.debug(`ApplicationDataContainer.setValue failed: ${e}`);
                }
                return true;
            }
            /**
             * Determines if a key is contained in localStorage
             * */
            static containsKey(locality, key) {
                const storageKey = ApplicationDataContainer.buildStorageKey(locality, key);
                try {
                    return localStorage.hasOwnProperty(storageKey);
                }
                catch (e) {
                    console.debug(`ApplicationDataContainer.containsKey failed: ${e}`);
                }
                return false;
            }
            /**
             * Gets a key by index in localStorage
             * */
            static getKeyByIndex(locality, index) {
                let localityIndex = 0;
                let returnKey = "";
                const prefix = ApplicationDataContainer.buildStoragePrefix(locality);
                try {
                    for (let i = 0; i < localStorage.length; i++) {
                        const storageKey = localStorage.key(i);
                        if (storageKey.startsWith(prefix)) {
                            if (localityIndex === index) {
                                returnKey = storageKey.substr(prefix.length);
                            }
                            localityIndex++;
                        }
                    }
                }
                catch (e) {
                    console.debug(`ApplicationDataContainer.getKeyByIndex failed: ${e}`);
                }
                return returnKey;
            }
            /**
             * Determines the number of items contained in localStorage
             * */
            static getCount(locality) {
                let count = 0;
                const prefix = ApplicationDataContainer.buildStoragePrefix(locality);
                try {
                    for (let i = 0; i < localStorage.length; i++) {
                        const storageKey = localStorage.key(i);
                        if (storageKey.startsWith(prefix)) {
                            count++;
                        }
                    }
                }
                catch (e) {
                    console.debug(`ApplicationDataContainer.getCount failed: ${e}`);
                }
                return count;
            }
            /**
             * Clears items contained in localStorage
             * */
            static clear(locality) {
                const prefix = ApplicationDataContainer.buildStoragePrefix(locality);
                const itemsToRemove = [];
                try {
                    for (let i = 0; i < localStorage.length; i++) {
                        const storageKey = localStorage.key(i);
                        if (storageKey.startsWith(prefix)) {
                            itemsToRemove.push(storageKey);
                        }
                    }
                    for (const item in itemsToRemove) {
                        localStorage.removeItem(itemsToRemove[item]);
                    }
                }
                catch (e) {
                    console.debug(`ApplicationDataContainer.clear failed: ${e}`);
                }
            }
            /**
             * Removes an item contained in localStorage
             * */
            static remove(locality, key) {
                const storageKey = ApplicationDataContainer.buildStorageKey(locality, key);
                let removed = false;
                try {
                    removed = localStorage.hasOwnProperty(storageKey);
                }
                catch (e) {
                    removed = false;
                    console.debug(`ApplicationDataContainer.remove failed: ${e}`);
                }
                if (removed) {
                    localStorage.removeItem(storageKey);
                }
                return removed;
            }
            /**
             * Gets a key by index in localStorage
             * */
            static getValueByIndex(locality, index) {
                let localityIndex = 0;
                let returnValue = "";
                const prefix = ApplicationDataContainer.buildStoragePrefix(locality);
                try {
                    for (let i = 0; i < localStorage.length; i++) {
                        const storageKey = localStorage.key(i);
                        if (storageKey.startsWith(prefix)) {
                            if (localityIndex === index) {
                                returnValue = localStorage.getItem(storageKey);
                            }
                            localityIndex++;
                        }
                    }
                }
                catch (e) {
                    console.debug(`ApplicationDataContainer.getValueByIndex failed: ${e}`);
                }
                return returnValue;
            }
        }
        Storage.ApplicationDataContainer = ApplicationDataContainer;
    })(Storage = Windows.Storage || (Windows.Storage = {}));
})(Windows || (Windows = {}));
