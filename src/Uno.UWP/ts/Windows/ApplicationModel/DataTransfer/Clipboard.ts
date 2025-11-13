// Type declarations for Clipboard API
// https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API
interface ClipboardItem {
	readonly types: ReadonlyArray<string>;
	getType(type: string): Promise<Blob>;
}

interface ClipboardItemConstructor {
	new(items: Record<string, Blob | string | Promise<Blob | string>>): ClipboardItem;
}

declare var ClipboardItem: ClipboardItemConstructor;

interface Clipboard {
	writeText(newClipText: string): Promise<void>;
	readText(): Promise<string>;
	read?(): Promise<ClipboardItem[]>;
	write?(items: ClipboardItem[]): Promise<void>;
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

		public static async getHtml(): Promise<string> {
			// we return "" on failure instead of null to avoid crashing with an NRE if there
			// are no try blocks in the stack frames above.
			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard && nav.clipboard.read) {
				try {
					const items = await nav.clipboard.read();
					for (const item of items) {
						if (item.types.includes('text/html')) {
							const blob = await item.getType('text/html');
							const html = await blob.text();
							return html;
						}
					}
					return "";
				} catch (reason) {
					console.error(`Failed to read HTML from clipboard: ${reason}`);
					return "";
				}
			}
			return Promise.resolve("");
		}

		public static async setHtml(html: string, text: string): Promise<void> {
			const nav = navigator as any;
			if (nav.clipboard && nav.clipboard.write && typeof ClipboardItem !== 'undefined') {
				try {
					const htmlBlob = new Blob([html], { type: 'text/html' });
					const textBlob = new Blob([text], { type: 'text/plain' });
					const item = new ClipboardItem({
						'text/html': htmlBlob,
						'text/plain': textBlob
					});
					await nav.clipboard.write([item]);
					// Trigger change notification
					Clipboard.onClipboardChanged();
				} catch (reason) {
					console.error(`Failed to write HTML to clipboard: ${reason}`);
				}
			} else {
				// Fallback: just write text if HTML clipboard API is not available
				Clipboard.setText(text);
			}
		}

		private static onClipboardChanged() {
			if (!Clipboard.dispatchContentChanged) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					Clipboard.dispatchContentChanged = (<any>globalThis).DotnetExports.Uno.Windows.ApplicationModel.DataTransfer.Clipboard.DispatchContentChanged;
				} else {
					throw `Clipboard: Unable to find dotnet exports`;
				}
			}
			Clipboard.dispatchContentChanged();
		}
	}
}
