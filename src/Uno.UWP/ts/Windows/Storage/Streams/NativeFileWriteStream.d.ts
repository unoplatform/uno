declare namespace Uno.Storage.Streams {
    class NativeFileWriteStream {
        private static _streamMap;
        private _stream;
        private _buffer;
        private constructor();
        static openAsync(streamId: string, fileId: string): Promise<string>;
        private static verifyPermissionAsync;
        static writeAsync(streamId: string, dataArrayPointer: number, offset: number, count: number, position: number): Promise<string>;
        static closeAsync(streamId: string): Promise<string>;
        static truncateAsync(streamId: string, length: number): Promise<string>;
    }
}
