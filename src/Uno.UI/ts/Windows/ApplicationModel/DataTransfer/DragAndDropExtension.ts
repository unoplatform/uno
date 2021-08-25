namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core {

	export class DragDropExtension {
		// https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API

		private static _dispatchDragAndDropMethod: any;
		private static _dispatchDragAndDropArgs: number;
		private static _current: DragDropExtension;

		private _pendingDropData: DataTransfer;

		public static enable(pArgs: number): void {
			if (DragDropExtension._current) {
				throw new Error("A DragDropExtension has already been enabled");
			}

			this._dispatchDragAndDropMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension:OnNativeDragAndDrop");
			this._dispatchDragAndDropArgs = pArgs;

			this._current = new DragDropExtension();
		}

		public static disable(pArgs: number): void {
			if (DragDropExtension._dispatchDragAndDropArgs != pArgs) {
				throw new Error("The current DragDropExtension does not match the provided args");
			}

			DragDropExtension._current.dispose();
			DragDropExtension._current = null;
		}

		constructor() {
			// Events fired on the drop target
			// Note: dragenter and dragover events will enable drop on the app
			document.addEventListener("dragenter", this.dispatchDropEvent);
			document.addEventListener("dragover", this.dispatchDropEvent);
			document.addEventListener("dragleave", this.dispatchDropEvent); // Seems to be raised also on drop?
			document.addEventListener("drop", this.dispatchDropEvent);

			// Events fired on the draggable target (the source element)
			//document.addEventListener("dragstart", this.dispatchDragStart);
			//document.addEventListener("drag", this.dispatchDrag);
			//document.addEventListener("dragend", this.dispatchDragEnd);
		}

		public dispose() {
			// Events fired on the drop target
			// Note: dragenter and dragover events will enable drop on the app
			document.removeEventListener("dragenter", this.dispatchDropEvent);
			document.removeEventListener("dragover", this.dispatchDropEvent);
			document.removeEventListener("dragleave", this.dispatchDropEvent); // Seems to be raised also on drop?
			document.removeEventListener("drop", this.dispatchDropEvent);

			// Events fired on the draggable target (the source element)
			//document.removeEventListener("dragstart", this.dispatchDragStart);
			//document.removeEventListener("drag", this.dispatchDrag);
			//document.removeEventListener("dragend", this.dispatchDragEnd);
		}

		private dispatchDropEvent(evt: DragEvent): any {
			// We must keep a reference to the dataTransfer in order to be able to retrieve data items
			this._pendingDropData = evt.dataTransfer;

			// Prepare args
			let args = new DragDropExtensionEventArgs();
			args.eventName = evt.type;
			args.timestamp = evt.timeStamp;
			args.x = evt.clientX;
			args.y = evt.clientY;
			args.buttons = evt.buttons;
			args.shift = evt.shiftKey;
			args.ctrl = evt.ctrlKey;
			args.alt = evt.altKey;
			if (evt.type == "dragenter") { // We use the dataItems only for enter, no needs to copy them every times!
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
			args.acceptedOperation = "none";

			// Raise the managed event
			args.marshal(DragDropExtension._dispatchDragAndDropArgs);
			DragDropExtension._dispatchDragAndDropMethod();

			// Read response from managed code
			args = DragDropExtensionEventArgs.unmarshal(DragDropExtension._dispatchDragAndDropArgs);

			evt.dataTransfer.dropEffect = ((args.acceptedOperation) as any);

			// No matter if the managed code handled the event, we want to prevent thee default behavior (like opening a drop link)
			evt.preventDefault();
		}

		public static async retrieveFiles(itemIds: number[]): Promise<Uno.Storage.NativeStorageItemInfo[]> {

			const data = DragDropExtension._current?._pendingDropData;
			if (data == null) {
				throw new Error("No pending drag and drop data.");
			}

			const fileHandles: FileSystemHandle[] = [];
			for (let id of itemIds) {
				fileHandles.push(await data.items[id].getAsFileSystemHandle());
			}

			return Uno.Storage.NativeStorageItem.getInfos(...fileHandles);
		}

		public static async retrieveText(itemId: number): Promise<string> {

			const data = DragDropExtension._current?._pendingDropData;
			if (data == null) {
				throw new Error("No pending drag and drop data.");
			}

			return new Promise((resolve, reject) => {
				var timeout = setTimeout(() => reject("Timeout: for security reason, you cannot access data before drop."), 15000);
				data.items[itemId].getAsString(str => {
					clearTimeout(timeout);
					resolve(str);
				});
			});
		}
	}
}
