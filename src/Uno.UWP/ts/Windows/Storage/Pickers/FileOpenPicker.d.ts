declare namespace Windows.Storage.Pickers {
    class FileOpenPicker {
        static isNativeSupported(): boolean;
        static nativePickFilesAsync(multiple: boolean, showAllEntry: boolean, fileTypesJson: string, id: string, startIn: StartInDirectory): Promise<string>;
        static uploadPickFilesAsync(multiple: boolean, targetPath: string, accept: string): Promise<string>;
    }
}
