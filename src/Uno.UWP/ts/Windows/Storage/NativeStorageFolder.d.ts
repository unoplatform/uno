declare namespace Uno.Storage {
    class NativeStorageFolder {
        /**
         * Creates a new folder inside another folder.
         * @param parentGuid The GUID of the folder to create in.
         * @param folderName The name of the new folder.
         */
        static createFolderAsync(parentGuid: string, folderName: string): Promise<string>;
        /**
         * Creates a new file inside another folder.
         * @param parentGuid The GUID of the folder to create in.
         * @param folderName The name of the new file.
         */
        static createFileAsync(parentGuid: string, fileName: string): Promise<string>;
        /**
         * Tries to get a folder in the given parent folder by name.
         * @param parentGuid The GUID of the parent folder to get.
         * @param folderName The name of the folder to look for.
         * @returns A GUID of the folder if found, otherwise null.
         */
        static tryGetFolderAsync(parentGuid: string, folderName: string): Promise<string>;
        /**
        * Tries to get a file in the given parent folder by name.
        * @param parentGuid The GUID of the parent folder to get.
        * @param folderName The name of the folder to look for.
        * @returns A GUID of the folder if found, otherwise null.
        */
        static tryGetFileAsync(parentGuid: string, fileName: string): Promise<string>;
        static deleteItemAsync(parentGuid: string, itemName: string): Promise<string>;
        static getItemsAsync(folderGuid: string): Promise<string>;
        static getFoldersAsync(folderGuid: string): Promise<string>;
        static getFilesAsync(folderGuid: string): Promise<string>;
        static getPrivateRootAsync(): Promise<string>;
        private static getEntriesAsync;
    }
}
