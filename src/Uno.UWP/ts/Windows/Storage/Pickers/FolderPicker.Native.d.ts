declare namespace Windows.Storage.Pickers {
    class FolderPicker {
        static isNativeSupported(): boolean;
        static pickSingleFolderAsync(id: string, startIn: StartInDirectory): Promise<string>;
    }
}
