declare namespace Uno.Storage.Streams {
    class NativeFileReadStream {
        private static _streamMap;
        private _file;
        private constructor();
        static openAsync(streamId: string, fileId: string): Promise<string>;
        static readAsync(streamId: string, targetArrayPointer: number, offset: number, count: number, position: number): Promise<string>;
        static close(streamId: string): void;
    }
}
