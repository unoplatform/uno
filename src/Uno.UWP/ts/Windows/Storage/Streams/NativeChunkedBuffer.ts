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

		private static readonly DownloadFolderName = "UnoPendingDownloads";

		private static _pendingDownloadUrl: string = null;
		private static _pendingDownloadEntry: string = null;

		private _chunks: Uint8Array[] = [];
		private _length = 0;
		private _released = false;

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
			if (instance._released) {
				// Writing again after the content was handed to the browser starts a new file.
				instance._released = false;
				instance._length = 0;
			}
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
			instance.throwIfReleased();
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
			instance.throwIfReleased();

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

		/**
		 * Triggers a browser download of the staged content.
		 * The payload is first moved into an origin-private (OPFS) file so the download
		 * streams from disk: materializing it as an in-memory Blob instead makes large
		 * files exceed the browser's blob storage, which surfaces as a failed download.
		 */
		public static async saveAsDownloadAsync(bufferId: string, fileName: string): Promise<void> {
			const instance = NativeChunkedBuffer._bufferMap.get(bufferId);
			instance.throwIfReleased();

			const entryName = bufferId;
			const directory = await NativeChunkedBuffer.tryGetDownloadDirectoryAsync();

			let source: Blob;
			if (directory) {
				source = await instance.writeToOpfsAsync(directory, entryName);
			}
			else {
				// No origin-private file system: fall back to an in-memory Blob.
				source = instance.buildBlob();
			}

			instance._chunks = [];
			instance._released = true;

			// Release the previous download now instead of waiting for its timer,
			// so at most one payload is retained at a time.
			await NativeChunkedBuffer.releasePendingDownloadAsync();

			const url = window.URL.createObjectURL(source);
			NativeChunkedBuffer._pendingDownloadUrl = url;
			NativeChunkedBuffer._pendingDownloadEntry = directory ? entryName : null;

			const a = window.document.createElement('a');
			a.href = url;
			a.download = fileName;

			document.body.appendChild(a);
			a.click();
			document.body.removeChild(a);

			// Backstop: releasing synchronously can abort the download in some browsers,
			// so clean up on a delay if no further download replaces it first.
			setTimeout(() => {
				if (NativeChunkedBuffer._pendingDownloadUrl === url) {
					NativeChunkedBuffer.releasePendingDownloadAsync();
				}
			}, 40000);
		}

		private async writeToOpfsAsync(directory: FileSystemDirectoryHandle, entryName: string): Promise<Blob> {
			const chunkSize = NativeChunkedBuffer._chunkSize;
			const handle = await directory.getFileHandle(entryName, { create: true });
			const writable = await handle.createWritable();
			try {
				let position = 0;
				let remaining = this._length;
				for (let i = 0; remaining > 0; i++) {
					const n = Math.min(remaining, chunkSize);
					await writable.write({ type: 'write', data: this._chunks[i].subarray(0, n), position: position });
					// The bytes are on disk now - drop the staged chunk as we go.
					this._chunks[i] = null;
					position += n;
					remaining -= n;
				}
			}
			finally {
				await writable.close();
			}

			// A File from an OPFS handle references the stored file rather than copying it.
			return await handle.getFile();
		}

		private buildBlob(): Blob {
			const chunkSize = NativeChunkedBuffer._chunkSize;
			const parts: Uint8Array[] = [];
			let remaining = this._length;
			for (let i = 0; remaining > 0; i++) {
				const n = Math.min(remaining, chunkSize);
				parts.push(n === chunkSize ? this._chunks[i] : this._chunks[i].subarray(0, n));
				remaining -= n;
			}
			return new Blob(parts);
		}

		private static async tryGetDownloadDirectoryAsync(): Promise<FileSystemDirectoryHandle> {
			try {
				const root = await navigator.storage.getDirectory();
				return await root.getDirectoryHandle(NativeChunkedBuffer.DownloadFolderName, { create: true });
			}
			catch (e) {
				return null;
			}
		}

		private static async releasePendingDownloadAsync(): Promise<void> {
			if (NativeChunkedBuffer._pendingDownloadUrl) {
				window.URL.revokeObjectURL(NativeChunkedBuffer._pendingDownloadUrl);
				NativeChunkedBuffer._pendingDownloadUrl = null;
			}

			if (NativeChunkedBuffer._pendingDownloadEntry) {
				const entryName = NativeChunkedBuffer._pendingDownloadEntry;
				NativeChunkedBuffer._pendingDownloadEntry = null;
				try {
					const directory = await NativeChunkedBuffer.tryGetDownloadDirectoryAsync();
					if (directory) {
						await directory.removeEntry(entryName);
					}
				}
				catch (e) {
					// The download may still hold the file; it is cleaned up on the next save.
				}
			}
		}

		private throwIfReleased(): void {
			if (this._released) {
				throw new Error("The staged content was released when the download was triggered.");
			}
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
