// Type declarations for Clipboard API
// https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API
interface Clipboard {
	writeText(newClipText: string): Promise<void>;
	readText(): Promise<string>;
}

interface NavigatorClipboard {
	// Only available in a secure context.
	readonly clipboard?: Clipboard;
}

interface Navigator extends NavigatorClipboard { }

namespace Uno.Utils {
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

		public static getText(requestId: string): void {
			if (!Clipboard.dispatchGetContent) {
				Clipboard.dispatchGetContent = 
					(<any>Module).mono_bind_static_method(
						"[Uno] Windows.ApplicationModel.DataTransfer.Clipboard:DispatchGetContent");
			}

			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard) {
				const promise = nav.clipboard.readText();
				promise.then(
					(clipText : string) => Clipboard.dispatchGetContent(requestId, clipText),
					(_ : any) => Clipboard.dispatchGetContent(requestId, null));
			} else {
				Clipboard.dispatchGetContent(requestId, null);
			}
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
