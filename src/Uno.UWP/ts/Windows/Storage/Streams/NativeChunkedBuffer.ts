namespace Uno.Storage.Streams {
	/**
	 * An in-memory, randomly accessible byte buffer stored as a list of small
	 * fixed-size chunks. Chunking avoids the browser's per-ArrayBuffer contiguous
	 * allocation ceiling and the reallocate-and-copy growth spikes of a single
	 * buffer, so large payloads degrade gracefully under memory pressure.
	 */
	export class NativeChunkedBuffer {

		private static _bufferMap: Map<string, NativeChunkedBuffer> = new Map<string, NativeChunkedBuffer>();

		private static readonly _chunkSize = 2 * 1024 * 1024;

		private _chunks: Uint8Array[] = [];
		private _length = 0;

		public static create(bufferId: string): void {
			NativeChunkedBuffer._bufferMap.set(bufferId, new NativeChunkedBuffer());
		}

		public static dispose(bufferId: string): void {
			NativeChunkedBuffer._bufferMap.delete(bufferId);
		}

		public static getLength(bufferId: string): number {
			return NativeChunkedBuffer._bufferMap.get(bufferId)._length;
		}

		public static write(bufferId: string, dataPtr: number, count: number, position: number): void {
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

		public static read(bufferId: string, dataPtr: number, count: number, position: number): number {
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

		public static truncate(bufferId: string, length: number): void {
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
		public static saveAsBlob(bufferId: string, fileName: string): void {
			const instance = NativeChunkedBuffer._bufferMap.get(bufferId);
			const chunkSize = NativeChunkedBuffer._chunkSize;

			const parts: Uint8Array[] = [];
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

		private ensureCapacity(bytes: number): void {
			const chunkSize = NativeChunkedBuffer._chunkSize;
			const requiredChunks = Math.ceil(bytes / chunkSize);
			while (this._chunks.length < requiredChunks) {
				this._chunks.push(new Uint8Array(chunkSize));
			}
		}
	}
}
