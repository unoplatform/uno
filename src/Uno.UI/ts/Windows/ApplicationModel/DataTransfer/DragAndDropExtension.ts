namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core {

	export class DragDropExtension {
		// https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API

		private static _dispatchDropEventMethod: any;
		private static _dispatchDragDropArgs: number;
		private static _current: DragDropExtension;
		private static _nextDropId: number;

		private _dropHandler: any;

		private _pendingDropId: number;
		private _pendingDropData: DataTransfer;

		public static enable(pArgs: number): void {
			if (!DragDropExtension._dispatchDropEventMethod) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					DragDropExtension._dispatchDropEventMethod = (<any>globalThis).DotnetExports.UnoUI.Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.OnNativeDropEvent;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}

			if (DragDropExtension._current) {
				throw new Error("A DragDropExtension has already been enabled");
			}

			DragDropExtension._dispatchDragDropArgs = pArgs;
			DragDropExtension._nextDropId = 1;
			DragDropExtension._current = new DragDropExtension();
		}

		public static disable(pArgs: number): void {
			if (DragDropExtension._dispatchDragDropArgs != pArgs) {
				throw new Error("The current DragDropExtension does not match the provided args");
			}
			
			DragDropExtension._current.dispose();
			DragDropExtension._current = null;
			DragDropExtension._dispatchDragDropArgs = null;
		}

		constructor() {
			// Events fired on the drop target
			// Note: dragenter and dragover events will enable drop on the app
			this._dropHandler = this.dispatchDropEvent.bind(this);
			document.addEventListener("dragenter", this._dropHandler);
			document.addEventListener("dragover", this._dropHandler);
			document.addEventListener("dragleave", this._dropHandler); // Seems to be raised also on drop?
			document.addEventListener("drop", this._dropHandler);

			// Events fired on the draggable target (the source element)
			//this._dragHandler = this.dispatchDragEvent.bind(this);
			//document.addEventListener("dragstart", this._dragHandler);
			//document.addEventListener("drag", this._dragHandler);
			//document.addEventListener("dragend", this._dragHandler);
		}

		public dispose() {
			// Events fired on the drop target
			document.removeEventListener("dragenter", this._dropHandler);
			document.removeEventListener("dragover", this._dropHandler);
			document.removeEventListener("dragleave", this._dropHandler); // Seems to be raised also on drop?
			document.removeEventListener("drop", this._dropHandler);
		}

		public static registerNoOp() {
			let notifyDisabled = (evt: DragEvent) => {
				evt.dataTransfer.dropEffect = "none";
				console.debug("Drag and Drop from external sources is disabled. See the `UnoDragDropExternalSupport` msbuild property to enable it (https://aka.platform.uno/linker-configuration)");

				document.removeEventListener("dragenter", notifyDisabled);
			};

			document.addEventListener("dragenter", notifyDisabled);
		}

		private dispatchDropEvent(evt: DragEvent): any {
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
				if (this._pendingDropId > 0) {
					// For the same reason as above, we ignore all dragenter if there is already a pending active drop
					return;
				}

				this._pendingDropId = ++DragDropExtension._nextDropId;
			}

			// We must keep a reference to the dataTransfer in order to be able to retrieve data items
			this._pendingDropData = evt.dataTransfer;

			// Prepare args
			let args = new DragDropExtensionEventArgs();
			args.id = this._pendingDropId;
			args.eventName = evt.type;
			args.timestamp = evt.timeStamp;
			args.x = evt.clientX;
			args.y = evt.clientY;
			args.buttons = evt.buttons;
			args.shift = evt.shiftKey;
			args.ctrl = evt.ctrlKey;
			args.alt = evt.altKey;
			if (evt.type == "dragenter") { // We use the dataItems only for enter, no needs to copy them every time!
				const items = new Array<any>();
				for (let itemId = 0; itemId < evt.dataTransfer.items.length; itemId++) {
					const item = evt.dataTransfer.items[itemId];
					items.push({ id: itemId, kind: item.kind, type: item.type });
				}
				args.dataItems = JSON.stringify(items);
				args.allowedOperations = evt.dataTransfer.effectAllowed;
			} else {
				// Must be set for marshaling
				args.dataItems = "";
				args.allowedOperations = "";
			}
			args.acceptedOperation = evt.dataTransfer.dropEffect;

			try {
				// Raise the managed event
				args.marshal(DragDropExtension._dispatchDragDropArgs);
				DragDropExtension._dispatchDropEventMethod();

				// Read response from managed code
				args = DragDropExtensionEventArgs.unmarshal(DragDropExtension._dispatchDragDropArgs);

				evt.dataTransfer.dropEffect = ((args.acceptedOperation) as any);
			} finally {
				// No matter if the managed code handled the event, we want to prevent thee default behavior (like opening a drop link)
				evt.preventDefault();

				if (evt.type == "dragleave" || evt.type == "drop") {
					this._pendingDropData = null;
					this._pendingDropId = 0;
				}
			}
		}

		public static async retrieveText(itemId: number): Promise<string> {

			const current = DragDropExtension._current;
			const data = current?._pendingDropData;
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

			const data = DragDropExtension._current?._pendingDropData;
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
