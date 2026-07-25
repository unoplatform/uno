var Uno;
(function (Uno) {
    var Storage;
    (function (Storage) {
        var Streams;
        (function (Streams) {
            class NativeFileWriteStream {
                constructor(stream) {
                    this._stream = stream;
                }
                static async openAsync(streamId, fileId) {
                    const item = Storage.NativeStorageItem.getItem(fileId);
                    if (item instanceof File) {
                        return "PermissionNotGranted";
                    }
                    const handle = item;
                    if (!await NativeFileWriteStream.verifyPermissionAsync(handle)) {
                        return "PermissionNotGranted";
                    }
                    const writableStream = await handle.createWritable({ keepExistingData: true });
                    const fileSize = (await handle.getFile()).size;
                    const stream = new NativeFileWriteStream(writableStream);
                    NativeFileWriteStream._streamMap.set(streamId, stream);
                    return fileSize.toString();
                }
                static async verifyPermissionAsync(fileHandle) {
                    const options = {};
                    options.mode = "readwrite";
                    // Check if permission was already granted. If so, return true.
                    if ((await fileHandle.queryPermission(options)) === 'granted') {
                        return true;
                    }
                    // Request permission. If the user grants permission, return true.
                    if ((await fileHandle.requestPermission(options)) === 'granted') {
                        return true;
                    }
                    // The user didn't grant permission, so return false.
                    return false;
                }
                static async writeAsync(streamId, dataArrayPointer, offset, count, position) {
                    const instance = NativeFileWriteStream._streamMap.get(streamId);
                    // Copy out of the WASM heap in a single operation. The copy must not alias
                    // Module.HEAPU8 - the heap may grow (and detach the buffer) during the await below.
                    const data = Module.HEAPU8.slice(dataArrayPointer + offset, dataArrayPointer + offset + count);
                    await instance._stream.write({
                        type: 'write',
                        data: data.buffer,
                        position: position
                    });
                    return "";
                }
                static async closeAsync(streamId) {
                    var instance = NativeFileWriteStream._streamMap.get(streamId);
                    if (instance) {
                        await instance._stream.close();
                        NativeFileWriteStream._streamMap.delete(streamId);
                    }
                    return "";
                }
                static async truncateAsync(streamId, length) {
                    var instance = NativeFileWriteStream._streamMap.get(streamId);
                    await instance._stream.truncate(length);
                    return "";
                }
            }
            NativeFileWriteStream._streamMap = new Map();
            Streams.NativeFileWriteStream = NativeFileWriteStream;
        })(Streams = Storage.Streams || (Storage.Streams = {}));
    })(Storage = Uno.Storage || (Uno.Storage = {}));
})(Uno || (Uno = {}));
