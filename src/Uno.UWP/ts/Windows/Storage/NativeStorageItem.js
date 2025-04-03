var Uno;
(function (Uno) {
    var Storage;
    (function (Storage) {
        class NativeStorageItem {
            static addItem(guid, item) {
                NativeStorageItem._guidToItemMap.set(guid, item);
                NativeStorageItem._itemToGuidMap.set(item, guid);
            }
            static removeItem(guid) {
                const handle = NativeStorageItem._guidToItemMap.get(guid);
                NativeStorageItem._guidToItemMap.delete(guid);
                NativeStorageItem._itemToGuidMap.delete(handle);
            }
            static getItem(guid) {
                return NativeStorageItem._guidToItemMap.get(guid);
            }
            static async getFile(guid) {
                const item = NativeStorageItem.getItem(guid);
                if (item instanceof File) {
                    return item;
                }
                if (item instanceof FileSystemFileHandle) {
                    return await item.getFile();
                }
                if (item instanceof FileSystemDirectoryHandle) {
                    throw new Error("Item " + guid + " is a directory handle. You cannot use it as a File!");
                }
                throw new Error("Item " + guid + " is of an unknown type. You cannot use it as a File!");
            }
            static getGuid(item) {
                return NativeStorageItem._itemToGuidMap.get(item);
            }
            static getInfos(...items) {
                const itemsWithoutGuids = [];
                for (const item of items) {
                    const guid = NativeStorageItem.getGuid(item);
                    if (!guid) {
                        itemsWithoutGuids.push(item);
                    }
                }
                NativeStorageItem.storeItems(itemsWithoutGuids);
                const results = [];
                for (const item of items) {
                    const guid = NativeStorageItem.getGuid(item);
                    const info = new Storage.NativeStorageItemInfo();
                    info.id = guid;
                    info.name = item.name;
                    info.isFile = item instanceof File || item.kind === "file";
                    results.push(info);
                }
                return results;
            }
            static storeItems(handles) {
                const missingGuids = NativeStorageItem.generateGuids(handles.length);
                for (let i = 0; i < handles.length; i++) {
                    NativeStorageItem.addItem(missingGuids[i], handles[i]);
                }
            }
            static generateGuids(count) {
                if (!NativeStorageItem.generateGuidBinding) {
                    if (globalThis.DotnetExports !== undefined) {
                        NativeStorageItem.generateGuidBinding = globalThis.DotnetExports.Uno.Uno.Storage.NativeStorageItem.GenerateGuids;
                    }
                    else {
                        NativeStorageItem.generateGuidBinding = Module.mono_bind_static_method("[Uno] Uno.Storage.NativeStorageItem:GenerateGuids");
                    }
                }
                const guids = NativeStorageItem.generateGuidBinding(count);
                return guids.split(";");
            }
        }
        NativeStorageItem._guidToItemMap = new Map();
        NativeStorageItem._itemToGuidMap = new Map();
        Storage.NativeStorageItem = NativeStorageItem;
    })(Storage = Uno.Storage || (Uno.Storage = {}));
})(Uno || (Uno = {}));
