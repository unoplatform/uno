namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core {

	export class BrowserDragDropExtension {
		// https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API

		private static _dispatchDropEventMethod: any;
		private static _nextDropId: number = 0;
		private static _pendingDropId: number;
		private static _idToContent: Map<number, Array<Promise<FileSystemHandle | File | string | null>>> = new Map<number, Array<Promise<FileSystemHandle | File | string | null>>>();

		public static async init() {
			BrowserDragDropExtension._dispatchDropEventMethod = (await (<any>window).Module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser")).Uno.UI.Runtime.Skia.BrowserDragDropExtension.OnNativeDropEvent;

			// Events fired on the drop target
			// Note: dragenter and dragover events will enable drop on the app
			document.addEventListener("dragenter", BrowserDragDropExtension.onDragDropEvent);
			document.addEventListener("dragover", BrowserDragDropExtension.onDragDropEvent);
			document.addEventListener("dragleave", BrowserDragDropExtension.onDragDropEvent); // Seems to be raised also on drop?
			document.addEventListener("drop", BrowserDragDropExtension.onDragDropEvent);

			// #18854: Prevent the browser default selection drag preview.
			document.addEventListener('dragstart', e => e.preventDefault());
		}

		private static onDragDropEvent(evt: DragEvent): any {
			if (evt.type == "dragleave"
				&& evt.clientX > 0
				&& evt.clientX < document.documentElement.clientWidth
				&& evt.clientY > 0
				&& evt.clientY < document.documentElement.clientHeight) {
				// We ignore all dragleave events if the pointer is still over the window.
				// This is to mute bubbling of drag leave when crossing boundaries of any elements on the app.
				return;
			}

			// We use the dataItems only for enter, no needs to copy them every time!
			let dataItems = "";
			let allowedOperations = "";
			if (evt.type == "dragenter") {
				if (BrowserDragDropExtension._pendingDropId > 0) {
					// For the same reason as above, we ignore all dragenter if there is already a pending active drop
					return;
				}
				BrowserDragDropExtension._pendingDropId = ++BrowserDragDropExtension._nextDropId;

				const items = new Array<any>();
				for (let itemId = 0; itemId < evt.dataTransfer.items.length; itemId++) {
					const item = evt.dataTransfer.items[itemId];
					items.push({ id: itemId, kind: item.kind, type: item.type });
				}
				dataItems = JSON.stringify(items);
				allowedOperations = evt.dataTransfer.effectAllowed;
			} else if (evt.type == "drop") {
				// Make sure to get **ALL** items content **before** returning from drop
				// (data.items and each instance of item will be cleared)
				BrowserDragDropExtension._idToContent.set(BrowserDragDropExtension._pendingDropId, BrowserDragDropExtension.beginRetrieveItems(evt.dataTransfer));
			}

			try {
				const acceptedOperation = BrowserDragDropExtension._dispatchDropEventMethod(
					evt.type,
					allowedOperations,
					evt.dataTransfer.dropEffect,
					dataItems,
					evt.timeStamp,
					evt.clientX,
					evt.clientY,
					BrowserDragDropExtension._pendingDropId,
					evt.buttons,
					evt.shiftKey,
					evt.ctrlKey,
					evt.altKey);
				evt.dataTransfer.dropEffect = acceptedOperation;
			} finally {
				// No matter if the managed code handled the event, we want to prevent the default behavior (like opening a drop link)
				evt.preventDefault();

				if (evt.type == "dragleave" || evt.type == "drop") {
					BrowserDragDropExtension._pendingDropId = 0;
				}
			}
		}

		private static beginRetrieveItems(data: DataTransfer): Array<Promise<FileSystemHandle | File | string | null>> {
			const promises: Array<Promise<FileSystemHandle | File | string | null>> = [];
			for (let i = 0; i < data.items.length; i++) {
				if (data.items[i].kind == "string") {
					promises.push(BrowserDragDropExtension.getText(data.items[i]));
				} else {
					promises.push(BrowserDragDropExtension.getAsFile(data.items[i]));
				}
			}
			return promises;
		}

		public static retrieveText(pendingDropId: number, itemId: number): Promise<string> {
			const data = BrowserDragDropExtension._idToContent.get(pendingDropId);
			if (!data) {
				throw new Error(`retrieveFiles failed to find pending drag and drop data for id ${pendingDropId}.`);
			}

			return data[itemId] as Promise<string>;
		}

		public static async retrieveFiles(pendingDropId: number, itemIds: Int32Array): Promise<string> {
			const data = BrowserDragDropExtension._idToContent.get(pendingDropId);
			if (!data) {
				throw new Error(`retrieveFiles failed to find pending drag and drop data for id ${pendingDropId}.`);
			}

			const selected = Array.from(itemIds).map(i => data[i] as Promise<FileSystemHandle | File>);
			const fileHandles = await Promise.all(selected);
			const infos = Uno.Storage.NativeStorageItem.getInfos(...fileHandles);
			return JSON.stringify(infos);
		}

		public static removeId(id: number) {
			BrowserDragDropExtension._idToContent.delete(id);
		}

		private static async getAsFile(item: DataTransferItem): Promise<FileSystemHandle|File> {
			if (item.getAsFileSystemHandle) {
				return await item.getAsFileSystemHandle();
			} else {
				return item.getAsFile();
			}
		}

		private static getText(item: DataTransferItem): Promise<string> {
			return new Promise((resolve, reject) => {
				const timeout = setTimeout(() => reject("Timeout: for security reason, you cannot access data before drop."), 15000);

				item.getAsString(str => {
					clearTimeout(timeout);
					resolve(str);
				});
			});
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
