declare namespace Uno.Storage {
    class NativeStorageItem {
        private static generateGuidBinding;
        private static _guidToItemMap;
        private static _itemToGuidMap;
        static addItem(guid: string, item: FileSystemHandle | File): void;
        static removeItem(guid: string): void;
        static getItem(guid: string): FileSystemHandle | File;
        static getFile(guid: string): Promise<File>;
        static getGuid(item: FileSystemHandle | File): string;
        static getInfos(...items: Array<FileSystemHandle | File>): NativeStorageItemInfo[];
        private static storeItems;
        private static generateGuids;
    }
}
