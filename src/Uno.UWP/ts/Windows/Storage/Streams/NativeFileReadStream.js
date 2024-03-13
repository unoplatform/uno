var Uno;
(function (Uno) {
    var Storage;
    (function (Storage) {
        var Streams;
        (function (Streams) {
            class NativeFileReadStream {
                constructor(file) {
                    this._file = file;
                }
                static async openAsync(streamId, fileId) {
                    const file = await Storage.NativeStorageItem.getFile(fileId);
                    const fileSize = file.size;
                    const stream = new NativeFileReadStream(file);
                    NativeFileReadStream._streamMap.set(streamId, stream);
                    return fileSize.toString();
                }
                static async readAsync(streamId, targetArrayPointer, offset, count, position) {
                    var streamReader;
                    var readerNeedsRelease = true;
                    try {
                        const instance = NativeFileReadStream._streamMap.get(streamId);
                        var totalRead = 0;
                        var stream = await instance._file.slice(position, position + count).stream();
                        streamReader = stream.getReader();
                        var chunk = await streamReader.read();
                        while (!chunk.done && chunk.value) {
                            for (var i = 0; i < chunk.value.length; i++) {
                                Module.HEAPU8[targetArrayPointer + offset + totalRead + i] = chunk.value[i];
                            }
                            totalRead += chunk.value.length;
                            chunk = await streamReader.read();
                        }
                        // If this is the end of stream, it closed itself
                        readerNeedsRelease = !chunk.done;
                        return totalRead.toString();
                    }
                    finally {
                        // Reader must be released only if the underlying stream has not already closed it.				
                        // Otherwise the release operation sets a new Promise.reject as reader.closed which
                        // raises silent but observable exception in Chromium-based browsers.
                        if (streamReader && readerNeedsRelease) {
                            // Silently handling TypeError exceptions on closed event as the releaseLock()
                            // raises one in case of a successful close.
                            streamReader.closed.catch(reason => {
                                if (!(reason instanceof TypeError)) {
                                    throw reason;
                                }
                            });
                            streamReader.cancel();
                            streamReader.releaseLock();
                        }
                    }
                }
                static close(streamId) {
                    NativeFileReadStream._streamMap.delete(streamId);
                }
            }
            NativeFileReadStream._streamMap = new Map();
            Streams.NativeFileReadStream = NativeFileReadStream;
        })(Streams = Storage.Streams || (Storage.Streams = {}));
    })(Storage = Uno.Storage || (Uno.Storage = {}));
})(Uno || (Uno = {}));
