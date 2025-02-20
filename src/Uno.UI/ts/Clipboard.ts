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
				nav.clipboard.writeText(text).catch((reason: any) => {
					console.error(`Failed to write to clipboard: ${reason}`);
				});
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
			// we return "" on failure instead of null to avoid crashing with an NRE if there
			// are no try blocks in the stack frames above.
			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard) {
				return nav.clipboard.readText().catch(reason => {
					console.error(`Failed to read from clipboard: ${reason}`);
					return "";
				});
			}
			return Promise.resolve("")
		}

		private static onClipboardChanged() {
			if (!Clipboard.dispatchContentChanged) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					Clipboard.dispatchContentChanged = (<any>globalThis).DotnetExports.Uno.Windows.ApplicationModel.DataTransfer.Clipboard.DispatchContentChanged;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}
			Clipboard.dispatchContentChanged();
		}
	}
}
