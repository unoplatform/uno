declare namespace Uno.Storage {
    class NativeStorageFile {
        static getBasicPropertiesAsync(guid: string): Promise<string>;
    }
}
