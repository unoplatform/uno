var __asyncValues = (this && this.__asyncValues) || function (o) {
    if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
    var m = o[Symbol.asyncIterator], i;
    return m ? m.call(o) : (o = typeof __values === "function" ? __values(o) : o[Symbol.iterator](), i = {}, verb("next"), verb("throw"), verb("return"), i[Symbol.asyncIterator] = function () { return this; }, i);
    function verb(n) { i[n] = o[n] && function (v) { return new Promise(function (resolve, reject) { v = o[n](v), settle(resolve, reject, v.done, v.value); }); }; }
    function settle(resolve, reject, d, v) { Promise.resolve(v).then(function(v) { resolve({ value: v, done: d }); }, reject); }
};
var Uno;
(function (Uno) {
    var Storage;
    (function (Storage) {
        class NativeStorageFolder {
            /**
             * Creates a new folder inside another folder.
             * @param parentGuid The GUID of the folder to create in.
             * @param folderName The name of the new folder.
             */
            static async createFolderAsync(parentGuid, folderName) {
                try {
                    const parentHandle = Storage.NativeStorageItem.getItem(parentGuid);
                    const newDirectoryHandle = await parentHandle.getDirectoryHandle(folderName, {
                        create: true,
                    });
                    const info = Storage.NativeStorageItem.getInfos(newDirectoryHandle)[0];
                    return JSON.stringify(info);
                }
                catch (_a) {
                    console.log("Could not create folder" + folderName);
                    return null;
                }
            }
            /**
             * Creates a new file inside another folder.
             * @param parentGuid The GUID of the folder to create in.
             * @param folderName The name of the new file.
             */
            static async createFileAsync(parentGuid, fileName) {
                try {
                    const parentHandle = Storage.NativeStorageItem.getItem(parentGuid);
                    const newFileHandle = await parentHandle.getFileHandle(fileName, {
                        create: true,
                    });
                    const info = Storage.NativeStorageItem.getInfos(newFileHandle)[0];
                    return JSON.stringify(info);
                }
                catch (_a) {
                    console.log("Could not create file " + fileName);
                    return null;
                }
            }
            /**
             * Tries to get a folder in the given parent folder by name.
             * @param parentGuid The GUID of the parent folder to get.
             * @param folderName The name of the folder to look for.
             * @returns A GUID of the folder if found, otherwise null.
             */
            static async tryGetFolderAsync(parentGuid, folderName) {
                const parentHandle = Storage.NativeStorageItem.getItem(parentGuid);
                let nestedDirectoryHandle = undefined;
                try {
                    nestedDirectoryHandle = await parentHandle.getDirectoryHandle(folderName);
                }
                catch (ex) {
                    return null;
                }
                if (nestedDirectoryHandle) {
                    return JSON.stringify(Storage.NativeStorageItem.getInfos(nestedDirectoryHandle)[0]);
                }
                return null;
            }
            /**
            * Tries to get a file in the given parent folder by name.
            * @param parentGuid The GUID of the parent folder to get.
            * @param folderName The name of the folder to look for.
            * @returns A GUID of the folder if found, otherwise null.
            */
            static async tryGetFileAsync(parentGuid, fileName) {
                const parentHandle = Storage.NativeStorageItem.getItem(parentGuid);
                let fileHandle = undefined;
                try {
                    fileHandle = await parentHandle.getFileHandle(fileName);
                }
                catch (ex) {
                    return null;
                }
                if (fileHandle) {
                    return JSON.stringify(Storage.NativeStorageItem.getInfos(fileHandle)[0]);
                }
                return null;
            }
            static async deleteItemAsync(parentGuid, itemName) {
                try {
                    const parentHandle = Storage.NativeStorageItem.getItem(parentGuid);
                    await parentHandle.removeEntry(itemName, { recursive: true });
                    return "OK";
                }
                catch (_a) {
                    return null;
                }
            }
            static async getItemsAsync(folderGuid) {
                return await NativeStorageFolder.getEntriesAsync(folderGuid, true, true);
            }
            static async getFoldersAsync(folderGuid) {
                return await NativeStorageFolder.getEntriesAsync(folderGuid, false, true);
            }
            static async getFilesAsync(folderGuid) {
                return await NativeStorageFolder.getEntriesAsync(folderGuid, true, false);
            }
            static async getPrivateRootAsync() {
                if (!navigator.storage.getDirectory) {
                    return null;
                }
                const directory = await navigator.storage.getDirectory();
                if (!directory) {
                    return null;
                }
                const info = Storage.NativeStorageItem.getInfos(directory)[0];
                return JSON.stringify(info);
            }
            static async getEntriesAsync(guid, includeFiles, includeDirectories) {
                var e_1, _a, e_2, _b;
                const folderHandle = Storage.NativeStorageItem.getItem(guid);
                var entries = [];
                // Default to "modern" implementation
                if (folderHandle.values) {
                    try {
                        for (var _c = __asyncValues(folderHandle.values()), _d; _d = await _c.next(), !_d.done;) {
                            var entry = _d.value;
                            entries.push(entry);
                        }
                    }
                    catch (e_1_1) { e_1 = { error: e_1_1 }; }
                    finally {
                        try {
                            if (_d && !_d.done && (_a = _c.return)) await _a.call(_c);
                        }
                        finally { if (e_1) throw e_1.error; }
                    }
                }
                else {
                    try {
                        for (var _e = __asyncValues(folderHandle.getEntries()), _f; _f = await _e.next(), !_f.done;) {
                            var handle = _f.value;
                            entries.push(handle);
                        }
                    }
                    catch (e_2_1) { e_2 = { error: e_2_1 }; }
                    finally {
                        try {
                            if (_f && !_f.done && (_b = _e.return)) await _b.call(_e);
                        }
                        finally { if (e_2) throw e_2.error; }
                    }
                }
                var filteredHandles = [];
                // Filter
                for (var handle of entries) {
                    if (handle.kind == "file" && includeFiles) {
                        filteredHandles.push(handle);
                    }
                    else if (handle.kind == "directory" && includeDirectories) {
                        filteredHandles.push(handle);
                    }
                }
                // Get infos
                var infos = Storage.NativeStorageItem.getInfos(...filteredHandles);
                var json = JSON.stringify(infos);
                return json;
            }
        }
        Storage.NativeStorageFolder = NativeStorageFolder;
    })(Storage = Uno.Storage || (Uno.Storage = {}));
})(Uno || (Uno = {}));
