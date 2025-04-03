declare namespace Windows.Storage.Pickers {
    class FileSavePicker {
        static isNativeSupported(): boolean;
        static nativePickSaveFileAsync(showAllEntry: boolean, fileTypesJson: string, suggestedFileName: string, id: string, startIn: StartInDirectory): Promise<string>;
        static SaveAs(fileName: string, dataPtr: any, size: number): void;
    }
}
