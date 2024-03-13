var Windows;
(function (Windows) {
    var Storage;
    (function (Storage) {
        var Pickers;
        (function (Pickers) {
            class FolderPicker {
                static isNativeSupported() {
                    return typeof showDirectoryPicker === "function";
                }
                static async pickSingleFolderAsync(id, startIn) {
                    if (!FolderPicker.isNativeSupported()) {
                        return null;
                    }
                    try {
                        const options = {
                            id: id,
                            startIn: startIn,
                        };
                        const selectedFolder = await showDirectoryPicker(options);
                        const info = Uno.Storage.NativeStorageItem.getInfos(selectedFolder)[0];
                        return JSON.stringify(info);
                    }
                    catch (e) {
                        console.log("The user dismissed the prompt without making a selection, " +
                            "or the user agent deems the selected content to be too sensitive or dangerous - " + e);
                        return null;
                    }
                }
            }
            Pickers.FolderPicker = FolderPicker;
        })(Pickers = Storage.Pickers || (Storage.Pickers = {}));
    })(Storage = Windows.Storage || (Windows.Storage = {}));
})(Windows || (Windows = {}));
