// Type definitions for non-npm package File System Access API 2020.09
// Project: https://github.com/WICG/file-system-access
// Definitions by: Ingvar Stepanyan <https://github.com/RReverser>
// Definitions: https://github.com/DefinitelyTyped/DefinitelyTyped
// Minimum TypeScript Version: 3.5

export { };

declare class BaseFileSystemHandle {
	protected constructor();

	readonly kind: FileSystemHandleKind;
	readonly name: string;

	isSameEntry(other: FileSystemHandle): Promise<boolean>;

    /**
     * @deprecated Old property just for Chromium <=85. Use `kind` property in the new API.
     */
	readonly isFile: this['kind'] extends 'file' ? true : false;

    /**
     * @deprecated Old property just for Chromium <=85. Use `kind` property in the new API.
     */
	readonly isDirectory: this['kind'] extends 'directory' ? true : false;
}

declare global {
	interface FilePickerAcceptType {
		description?: string;
	}

	interface FilePickerOptions {
		types?: FilePickerAcceptType[];
		excludeAcceptAllOption?: boolean;
	}

	interface OpenFilePickerOptions extends FilePickerOptions {
		multiple?: boolean;
	}

	// tslint:disable-next-line:no-empty-interface
	interface SaveFilePickerOptions extends FilePickerOptions { }

	// tslint:disable-next-line:no-empty-interface
	interface DirectoryPickerOptions { }

	type FileSystemPermissionMode = 'read' | 'readwrite';

	interface FileSystemHandlePermissionDescriptor {
		mode?: FileSystemPermissionMode;
	}

	type FileSystemHandleKind = 'file' | 'directory';

	interface FileSystemCreateWritableOptions {
		keepExistingData?: boolean;
	}

	interface FileSystemGetFileOptions {
		create?: boolean;
	}

	interface FileSystemGetDirectoryOptions {
		create?: boolean;
	}

	interface FileSystemRemoveOptions {
		recursive?: boolean;
	}

	// TODO: remove this once https://github.com/microsoft/TSJS-lib-generator/issues/881 is fixed.
	// Native File System API especially needs this method.
	interface WritableStream {
		close(): Promise<void>;
	}

	class FileSystemWritableFileStream {
		seek(position: number): Promise<void>;
		truncate(size: number): Promise<void>;
		close(): Promise<void>;
	}

	const FileSystemHandle: typeof BaseFileSystemHandle;
	type FileSystemHandle = FileSystemFileHandle | FileSystemDirectoryHandle;

	class FileSystemFileHandle extends FileSystemHandle {
		readonly kind: 'file';
		createWritable(options?: FileSystemCreateWritableOptions): Promise<FileSystemWritableFileStream>;
	}

	class FileSystemDirectoryHandle extends BaseFileSystemHandle {
		readonly kind: 'directory';

		getFileHandle(name: string, options?: FileSystemGetFileOptions): Promise<FileSystemFileHandle>;
		getDirectoryHandle(name: string, options?: FileSystemGetDirectoryOptions): Promise<FileSystemDirectoryHandle>;
		removeEntry(name: string, options?: FileSystemRemoveOptions): Promise<void>;
		resolve(possibleDescendant: FileSystemHandle): Promise<string[] | null>;

		/**
		 * @deprecated Old method just for Chromium <=85. Use `navigator.storage.getDirectory()` in the new API.
		 */
		static getSystemDirectory(options: GetSystemDirectoryOptions): Promise<FileSystemDirectoryHandle>;
	}

	interface DataTransferItem {
		getAsFileSystemHandle(): Promise<FileSystemHandle | null>;
	}

	interface StorageManager {
		getDirectory(): Promise<FileSystemDirectoryHandle>;
	}

	function showOpenFilePicker(
		options?: OpenFilePickerOptions & { multiple?: false },
	): Promise<[FileSystemFileHandle]>;
	function showOpenFilePicker(options?: OpenFilePickerOptions): Promise<FileSystemFileHandle[]>;
	function showSaveFilePicker(options?: SaveFilePickerOptions): Promise<FileSystemFileHandle>;
	function showDirectoryPicker(options?: DirectoryPickerOptions): Promise<FileSystemDirectoryHandle>;

	// Old methods available on Chromium 85 instead of the ones above.

	interface ChooseFileSystemEntriesOptionsAccepts {
		description?: string;
		mimeTypes?: string[];
		extensions?: string[];
	}

	interface ChooseFileSystemEntriesFileOptions {
		accepts?: ChooseFileSystemEntriesOptionsAccepts[];
		excludeAcceptAllOption?: boolean;
	}

	/**
	 * @deprecated Old method just for Chromium <=85. Use `showOpenFilePicker()` in the new API.
	 */
	function chooseFileSystemEntries(
		options?: ChooseFileSystemEntriesFileOptions & {
			type?: 'open-file';
			multiple?: false;
		},
	): Promise<FileSystemFileHandle>;
	/**
	 * @deprecated Old method just for Chromium <=85. Use `showOpenFilePicker()` in the new API.
	 */
	function chooseFileSystemEntries(
		options: ChooseFileSystemEntriesFileOptions & {
			type?: 'open-file';
			multiple: true;
		},
	): Promise<FileSystemFileHandle[]>;
	/**
	 * @deprecated Old method just for Chromium <=85. Use `showSaveFilePicker()` in the new API.
	 */
	function chooseFileSystemEntries(
		options: ChooseFileSystemEntriesFileOptions & {
			type: 'save-file';
		},
	): Promise<FileSystemFileHandle>;
	/**
	 * @deprecated Old method just for Chromium <=85. Use `showDirectoryPicker()` in the new API.
	 */
	function chooseFileSystemEntries(options: { type: 'open-directory' }): Promise<FileSystemDirectoryHandle>;

	interface GetSystemDirectoryOptions {
		type: 'sandbox';
	}

	interface FileSystemDirectoryHandle {
		/**
		 * @deprecated Old property just for Chromium <=85. Use `.getFileHandle()` in the new API.
		 */
		getFile: FileSystemDirectoryHandle['getFileHandle'];

		/**
		 * @deprecated Old property just for Chromium <=85. Use `.getDirectoryHandle()` in the new API.
		 */
		getDirectory: FileSystemDirectoryHandle['getDirectoryHandle'];
	}

	interface FileSystemHandlePermissionDescriptor {
		/**
		 * @deprecated Old property just for Chromium <=85. Use `mode: ...` in the new API.
		 */
		writable?: boolean;
	}
}
