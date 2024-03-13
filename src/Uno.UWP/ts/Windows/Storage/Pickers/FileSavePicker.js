var Windows;
(function (Windows) {
    var Storage;
    (function (Storage) {
        var Pickers;
        (function (Pickers) {
            class FileSavePicker {
                static isNativeSupported() {
                    return typeof showSaveFilePicker === "function";
                }
                static async nativePickSaveFileAsync(showAllEntry, fileTypesJson, suggestedFileName, id, startIn) {
                    if (!FileSavePicker.isNativeSupported()) {
                        return null;
                    }
                    const options = {
                        excludeAcceptAllOption: !showAllEntry,
                        id: id,
                        startIn: startIn,
                        types: [],
                    };
                    if (suggestedFileName != "") {
                        options.suggestedName = suggestedFileName;
                    }
                    const acceptTypes = JSON.parse(fileTypesJson);
                    for (const acceptType of acceptTypes) {
                        const pickerAcceptType = {
                            accept: {},
                            description: acceptType.description,
                        };
                        for (const acceptTypeItem of acceptType.accept) {
                            pickerAcceptType.accept[acceptTypeItem.mimeType] = acceptTypeItem.extensions;
                        }
                        options.types.push(pickerAcceptType);
                    }
                    try {
                        const selectedFile = await showSaveFilePicker(options);
                        const info = Uno.Storage.NativeStorageItem.getInfos(selectedFile)[0];
                        const json = JSON.stringify(info);
                        return json;
                    }
                    catch (e) {
                        console.log("User did not make a selection or the file selected was" +
                            "deemed too sensitive or dangerous to be exposed to the website - " + e);
                        return null;
                    }
                }
                static SaveAs(fileName, dataPtr, size) {
                    const buffer = new Uint8Array(size);
                    for (var i = 0; i < size; i++) {
                        buffer[i] = Module.getValue(dataPtr + i, "i8");
                    }
                    const a = window.document.createElement('a');
                    const blob = new Blob([buffer]);
                    a.href = window.URL.createObjectURL(blob);
                    a.download = fileName;
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                }
            }
            Pickers.FileSavePicker = FileSavePicker;
        })(Pickers = Storage.Pickers || (Storage.Pickers = {}));
    })(Storage = Windows.Storage || (Windows.Storage = {}));
})(Windows || (Windows = {}));
