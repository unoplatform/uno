var Uno;
(function (Uno) {
    var Storage;
    (function (Storage) {
        var Streams;
        (function (Streams) {
            /**
             * An in-memory, randomly accessible byte buffer stored as a list of small
             * fixed-size chunks. Chunking avoids the browser's per-ArrayBuffer contiguous
             * allocation ceiling and the reallocate-and-copy growth spikes of a single
             * buffer, so large payloads degrade gracefully under memory pressure.
             */
            class NativeChunkedBuffer {
                constructor() {
                    this._chunks = [];
                    this._length = 0;
                }
                static create(bufferId) {
                    NativeChunkedBuffer._bufferMap.set(bufferId, new NativeChunkedBuffer());
                }
                static dispose(bufferId) {
                    NativeChunkedBuffer._bufferMap.delete(bufferId);
                }
                static getLength(bufferId) {
                    return NativeChunkedBuffer._bufferMap.get(bufferId)._length;
                }
                static write(bufferId, dataPtr, count, position) {
                    const instance = NativeChunkedBuffer._bufferMap.get(bufferId);
                    const chunkSize = NativeChunkedBuffer._chunkSize;
                    instance.ensureCapacity(position + count);
                    let src = dataPtr;
                    let pos = position;
                    let remaining = count;
                    while (remaining > 0) {
                        const chunkIndex = Math.floor(pos / chunkSize);
                        const offsetInChunk = pos % chunkSize;
                        const n = Math.min(remaining, chunkSize - offsetInChunk);
                        instance._chunks[chunkIndex].set(Module.HEAPU8.subarray(src, src + n), offsetInChunk);
                        src += n;
                        pos += n;
                        remaining -= n;
                    }
                    instance._length = Math.max(instance._length, position + count);
                }
                static read(bufferId, dataPtr, count, position) {
                    const instance = NativeChunkedBuffer._bufferMap.get(bufferId);
                    const chunkSize = NativeChunkedBuffer._chunkSize;
                    const available = Math.max(0, Math.min(count, instance._length - position));
                    let dst = dataPtr;
                    let pos = position;
                    let remaining = available;
                    while (remaining > 0) {
                        const chunkIndex = Math.floor(pos / chunkSize);
                        const offsetInChunk = pos % chunkSize;
                        const n = Math.min(remaining, chunkSize - offsetInChunk);
                        Module.HEAPU8.set(instance._chunks[chunkIndex].subarray(offsetInChunk, offsetInChunk + n), dst);
                        dst += n;
                        pos += n;
                        remaining -= n;
                    }
                    return available;
                }
                static truncate(bufferId, length) {
                    const instance = NativeChunkedBuffer._bufferMap.get(bufferId);
                    const chunkSize = NativeChunkedBuffer._chunkSize;
                    if (length < instance._length) {
                        const chunkCount = Math.ceil(length / chunkSize);
                        instance._chunks.length = chunkCount;
                        // Keep the invariant that retained bytes beyond _length are zero,
                        // so re-extending exposes zeros rather than stale data.
                        if (chunkCount > 0 && length % chunkSize !== 0) {
                            instance._chunks[chunkCount - 1].fill(0, length % chunkSize);
                        }
                    }
                    else {
                        instance.ensureCapacity(length);
                    }
                    instance._length = length;
                }
                /** Builds a Blob from the chunks and triggers a browser download of it. */
                static saveAsBlob(bufferId, fileName) {
                    const instance = NativeChunkedBuffer._bufferMap.get(bufferId);
                    const chunkSize = NativeChunkedBuffer._chunkSize;
                    const parts = [];
                    let remaining = instance._length;
                    for (let i = 0; remaining > 0; i++) {
                        const n = Math.min(remaining, chunkSize);
                        parts.push(n === chunkSize ? instance._chunks[i] : instance._chunks[i].subarray(0, n));
                        remaining -= n;
                    }
                    const blob = new Blob(parts);
                    const a = window.document.createElement('a');
                    a.href = window.URL.createObjectURL(blob);
                    a.download = fileName;
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    // Revoke on a delay - revoking synchronously can abort the download in some browsers.
                    // Without this the blob (~the whole file) stays pinned until the page closes.
                    setTimeout(() => window.URL.revokeObjectURL(a.href), 40000);
                }
                ensureCapacity(bytes) {
                    const chunkSize = NativeChunkedBuffer._chunkSize;
                    const requiredChunks = Math.ceil(bytes / chunkSize);
                    while (this._chunks.length < requiredChunks) {
                        this._chunks.push(new Uint8Array(chunkSize));
                    }
                }
            }
            NativeChunkedBuffer._bufferMap = new Map();
            NativeChunkedBuffer._chunkSize = 2 * 1024 * 1024;
            Streams.NativeChunkedBuffer = NativeChunkedBuffer;
        })(Streams = Storage.Streams || (Storage.Streams = {}));
    })(Storage = Uno.Storage || (Uno.Storage = {}));
})(Uno || (Uno = {}));
