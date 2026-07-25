declare namespace Uno.Storage.Streams {
    /**
     * An in-memory, randomly accessible byte buffer stored as a list of small
     * fixed-size chunks. Chunking avoids the browser's per-ArrayBuffer contiguous
     * allocation ceiling and the reallocate-and-copy growth spikes of a single
     * buffer, so large payloads degrade gracefully under memory pressure.
     */
    class NativeChunkedBuffer {
        private static _bufferMap;
        private static readonly _chunkSize;
        private _chunks;
        private _length;
        static create(bufferId: string): void;
        static dispose(bufferId: string): void;
        static getLength(bufferId: string): number;
        static write(bufferId: string, dataPtr: number, count: number, position: number): void;
        static read(bufferId: string, dataPtr: number, count: number, position: number): number;
        static truncate(bufferId: string, length: number): void;
        /** Builds a Blob from the chunks and triggers a browser download of it. */
        static saveAsBlob(bufferId: string, fileName: string): void;
        private ensureCapacity;
    }
}
