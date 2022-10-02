// Type declarations for Clipboard API
// https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API
interface ClipboardItem {
	readonly types: ReadonlyArray<string>;
	getType(type: string): Promise<Blob>;
}

declare var ClipboardItem: {
	prototype: ClipboardItem;
	new(items: Record<string, string | Blob | PromiseLike<string | Blob>>, options?: any): ClipboardItem;
};

interface Clipboard {
	read(): Promise<ClipboardItem[]>;
	readText(): Promise<string>;
	write(data: ClipboardItem[]): Promise<void>;
	writeText(newClipText: string): Promise<void>;
}

interface NavigatorClipboard {
	// Only available in a secure context.
	readonly clipboard?: Clipboard;
}

interface Navigator extends NavigatorClipboard { }

namespace Uno.Utils {
	interface MarshalledData {
		ptr: number;
		len: number;
	}

	interface MarshalledDataWithMime extends MarshalledData {
		mime: string;
	}

	export class Clipboard {
		private static dispatchContentChanged: () => number;
		private static dispatchGetContent: (requestId : string, content : string) => number;

		public static startContentChanged() {
			['cut', 'copy', 'paste'].forEach(function (event) {
				document.addEventListener(event, Clipboard.onClipboardChanged);
			});
		}

		public static stopContentChanged() {
			['cut', 'copy', 'paste'].forEach(function (event) {
				document.removeEventListener(event, Clipboard.onClipboardChanged);
			});
		}

		public static setText(text: string): string {
			const nav = navigator as any;
			if (nav.clipboard) {
				// Use clipboard object when available
				nav.clipboard.writeText(text);
				// Trigger change notification, as clipboard API does
				// not execute "copy".
				Clipboard.onClipboardChanged();
			} else {
				// Hack when the clipboard is not available
				const textarea = document.createElement("textarea");
				textarea.value = text;
				document.body.appendChild(textarea);
				textarea.select();
				document.execCommand("copy");
				document.body.removeChild(textarea);
			}

			return "ok";
		}

		public static getText(): Promise<string> {
			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard) {
				return nav.clipboard.readText();
			}
			return Promise.resolve(null)
		}

		public static setData(data: MarshalledDataWithMime[]): string {
			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard) {
				nav.clipboard.write(
					[
						new ClipboardItem(Object.assign({}, ...data.map(d =>
							({[d.mime]: new Blob([Module.HEAPU8.subarray(d.ptr, d.ptr + d.len)], { type: d.mime })})
						)))
					]
				);
				Clipboard.onClipboardChanged();
				return "ok";
			}
			return null;
		}

		public static async getDataMimes(): Promise<string[]> {
			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard) {
				const clipboardContents = await nav.clipboard.read();
				const mimes = [];
				for (const content of clipboardContents) {
					mimes.push(...content.types);
				}
				return mimes;
			}
			return [];
		}

		public static async getData(mime: string): Promise<MarshalledData> {
			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard) {
				const clipboardContents = await nav.clipboard.read();
				for (const content of clipboardContents) {
					if (content.types.includes(mime)) {
						const blob = await content.getType(mime);
						const buffer = await blob.arrayBuffer();
						const ptr = Module._malloc(buffer.byteLength);
						Module.HEAPU8.set(new Uint8Array(buffer), ptr);
						return {ptr: ptr, len: buffer.byteLength};
					}
				}
			}
			return null;
		}

		private static onClipboardChanged() {
			if (!Clipboard.dispatchContentChanged) {
				Clipboard.dispatchContentChanged =
					(<any>Module).mono_bind_static_method(
						"[Uno] Windows.ApplicationModel.DataTransfer.Clipboard:DispatchContentChanged");
			}
			Clipboard.dispatchContentChanged();
		}
	}
}
