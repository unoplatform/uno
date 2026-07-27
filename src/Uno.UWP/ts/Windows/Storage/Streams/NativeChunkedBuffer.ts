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

		// Entries are namespaced per page session so a later session can reclaim the
		// leftovers of one that crashed, without touching downloads still in flight here.
		private static readonly _sessionPrefix = `s${Math.floor(Math.random() * 1e9).toString(36)}-`;

		// Long enough for a multi-GB download to be read out before the file is dropped.
		private static readonly DownloadRetentionMs = 15 * 60 * 1000;

		private static _pendingDownloadUrl: string = null;

		private _chunks: Uint8Array[] = [];
		private _length = 0;
		private _released = false;
		private _lastModified = Date.now();

		public static create(bufferId: string): void {
			NativeChunkedBuffer._bufferMap.set(bufferId, new NativeChunkedBuffer());
		}

		public static dispose(bufferId: string): void {
			NativeChunkedBuffer._bufferMap.delete(bufferId);
		}

		public static getLength(bufferId: string): number {
			return NativeChunkedBuffer._bufferMap.get(bufferId)._length;
		}

		/** Epoch milliseconds of the last write, for file modification metadata. */
		public static getLastModified(bufferId: string): number {
			return NativeChunkedBuffer._bufferMap.get(bufferId)._lastModified;
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
			instance._lastModified = Date.now();
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
			instance._lastModified = Date.now();
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

			const entryName = NativeChunkedBuffer._sessionPrefix + bufferId;
			const directory = await NativeChunkedBuffer.tryGetDownloadDirectoryAsync();

			let source: Blob;
			if (directory) {
				// Reclaim storage from earlier sessions before staging, since the payload
				// counts against the origin quota (as low as 2 GB in some browsers).
				await NativeChunkedBuffer.purgeStaleEntriesAsync(directory);
				try {
					source = await instance.writeToOpfsAsync(directory, entryName);
				}
				catch (e) {
					// Nothing was published, so reclaim the space the attempt took.
					await directory.removeEntry(entryName).catch(() => { });
					throw e;
				}
			}
			else {
				// No origin-private file system: fall back to an in-memory Blob.
				source = instance.buildBlob();
			}

			instance._chunks = [];
			instance._released = true;

			// Revoking the previous URL is safe for a download already in flight, but the
			// file backing it must stay until that download has certainly finished.
			if (NativeChunkedBuffer._pendingDownloadUrl) {
				window.URL.revokeObjectURL(NativeChunkedBuffer._pendingDownloadUrl);
			}

			const url = window.URL.createObjectURL(source);
			NativeChunkedBuffer._pendingDownloadUrl = url;

			const a = window.document.createElement('a');
			a.href = url;
			a.download = fileName;

			document.body.appendChild(a);
			a.click();
			document.body.removeChild(a);

			setTimeout(() => {
				if (NativeChunkedBuffer._pendingDownloadUrl === url) {
					window.URL.revokeObjectURL(url);
					NativeChunkedBuffer._pendingDownloadUrl = null;
				}
				if (directory) {
					directory.removeEntry(entryName).catch(() => { });
				}
			}, NativeChunkedBuffer.DownloadRetentionMs);
		}

		private static async purgeStaleEntriesAsync(directory: FileSystemDirectoryHandle): Promise<void> {
			try {
				const stale: string[] = [];
				for await (const name of (<any>directory).keys()) {
					if (!(<string>name).startsWith(NativeChunkedBuffer._sessionPrefix)) {
						stale.push(name);
					}
				}
				for (const name of stale) {
					await directory.removeEntry(name).catch(() => { });
				}
			}
			catch (e) {
				// Directory enumeration is best-effort.
			}
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
					position += n;
					remaining -= n;
				}
				await writable.close();
			}
			catch (e) {
				// The staged chunks are left intact so the commit can be retried - a
				// partially written file is discarded rather than published.
				try { await writable.abort(); } catch (ignored) { }
				throw e;
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
