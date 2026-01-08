namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core {

	export class DragDropExtension {
		// https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API

		private static _dispatchDropEventMethod: any;
		private static _nextDropId: number;
		private static _pendingDropId: number;
		private static _pendingDropData: DataTransfer;

		public static async init() {
			DragDropExtension._dispatchDropEventMethod = (await (<any>window).Module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser")).Uno.UI.Runtime.Skia.BrowserDragDropExtension.OnNativeDropEvent;
			DragDropExtension._nextDropId = 1;

			// Events fired on the drop target
			// Note: dragenter and dragover events will enable drop on the app
			document.addEventListener("dragenter", DragDropExtension.onDragDropEvent);
			document.addEventListener("dragover", DragDropExtension.onDragDropEvent);
			document.addEventListener("dragleave", DragDropExtension.onDragDropEvent); // Seems to be raised also on drop?
			document.addEventListener("drop", DragDropExtension.onDragDropEvent);

			// #18854: Prevent the browser default selection drag preview.
			document.addEventListener('dragstart', e => e.preventDefault());
		}

		private static onDragDropEvent(evt: DragEvent): any {
			console.log("ramez onDragDropEvent: ", evt);
			if (evt.type == "dragleave"
				&& evt.clientX > 0
				&& evt.clientX < document.documentElement.clientWidth
				&& evt.clientY > 0
				&& evt.clientY < document.documentElement.clientHeight) {
				// We ignore all dragleave while if pointer is still over the window.
				// This is to mute bubbling of drag leave when crossing boundaries of any elements on the app.
				return;
			}

			if (evt.type == "dragenter") {
				if (DragDropExtension._pendingDropId > 0) {
					// For the same reason as above, we ignore all dragenter if there is already a pending active drop
					return;
				}

				DragDropExtension._pendingDropId = ++DragDropExtension._nextDropId;
			}

			// We must keep a reference to the dataTransfer in order to be able to retrieve data items
			DragDropExtension._pendingDropData = evt.dataTransfer;

			let dataItems = "";
			let allowedOperations = "";
			if (evt.type == "dragenter") { // We use the dataItems only for enter, no needs to copy them every time!
				const items = new Array<any>();
				for (let itemId = 0; itemId < evt.dataTransfer.items.length; itemId++) {
					const item = evt.dataTransfer.items[itemId];
					items.push({ id: itemId, kind: item.kind, type: item.type });
				}
				dataItems = JSON.stringify(items);
				allowedOperations = evt.dataTransfer.effectAllowed;
			}

			try {
				const acceptedOperation = DragDropExtension._dispatchDropEventMethod(
					evt.type,
					allowedOperations,
					evt.dataTransfer.dropEffect,
					dataItems,
					evt.timeStamp,
					evt.clientX,
					evt.clientY,
					DragDropExtension._pendingDropId,
					evt.buttons,
					evt.shiftKey,
					evt.ctrlKey,
					evt.altKey);
				evt.dataTransfer.dropEffect = acceptedOperation;
			} finally {
				// No matter if the managed code handled the event, we want to prevent thee default behavior (like opening a drop link)
				evt.preventDefault();

				if (evt.type == "dragleave" || evt.type == "drop") {
					DragDropExtension._pendingDropData = null;
					DragDropExtension._pendingDropId = 0;
				}
			}
		}

		public static async retrieveText(itemId: number): Promise<string> {
			const data = DragDropExtension._pendingDropData;
			if (data == null) {
				throw new Error("No pending drag and drop data.");
			}

			return new Promise((resolve, reject) => {
				const item = data.items[itemId];
				const timeout = setTimeout(() => reject("Timeout: for security reason, you cannot access data before drop."), 15000);

				item.getAsString(str => {
					clearTimeout(timeout);
					resolve(str);
				});
			});
		}

		public static async retrieveFiles(itemIds: number[]): Promise<string> {

			const data = DragDropExtension._pendingDropData;
			if (data == null) {
				throw new Error("No pending drag and drop data.");
			}

			// Make sure to get **ALL** items content **before** going async
			// (data.items and each instance of item will be cleared)
			const asyncFileHandles: Array<Promise<FileSystemHandle | File>> = [];
			for (const id of itemIds) {
				asyncFileHandles.push(DragDropExtension.getAsFile(data.items[id]));
			}

			const fileHandles: Array<FileSystemHandle | File> = [];
			for (const asyncFile of asyncFileHandles) {
				fileHandles.push(await asyncFile);
			}

			const infos = Uno.Storage.NativeStorageItem.getInfos(...fileHandles);

			return JSON.stringify(infos);
		}

		private static async getAsFile(item: DataTransferItem): Promise<FileSystemHandle|File> {
			if (item.getAsFileSystemHandle) {
				return await item.getAsFileSystemHandle();
			} else {
				return item.getAsFile();
			}
		}
	}
}

// declarations from Uno winrt
declare namespace Uno.Storage {
	class NativeStorageItem {
		private static generateGuidBinding;
		private static _guidToItemMap;
		private static _itemToGuidMap;
		static addItem(guid: string, item: FileSystemHandle | File): void;
		static removeItem(guid: string): void;
		static getItem(guid: string): FileSystemHandle | File;
		static getFile(guid: string): Promise<File>;
		static getGuid(item: FileSystemHandle | File): string;
		static getInfos(...items: Array<FileSystemHandle | File>): NativeStorageItemInfo[];
		private static storeItems;
		private static generateGuids;
	}
}
declare namespace Uno.Storage {
	class NativeStorageItemInfo {
		id: string;
		name: string;
		path: string;
		isFile: boolean;
	}
}
