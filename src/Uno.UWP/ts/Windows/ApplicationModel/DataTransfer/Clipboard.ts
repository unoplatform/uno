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
	write(data: ClipboardItem[]): Promise<void>;
	read(): Promise<ClipboardItem[]>;
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

		public static async setHtml(text: string, html: string): Promise<void> {
			const nav = navigator as any;
			if (nav.clipboard && typeof ClipboardItem !== 'undefined') {
				try {
					// Create a ClipboardItem with both text/plain and text/html
					const textBlob = new Blob([text], { type: 'text/plain' });
					const htmlBlob = new Blob([html], { type: 'text/html' });
					const clipboardItem = new ClipboardItem({
						'text/plain': textBlob,
						'text/html': htmlBlob
					});
					await nav.clipboard.write([clipboardItem]);
					// Trigger change notification
					Clipboard.onClipboardChanged();
				} catch (error) {
					console.error(`Failed to write HTML to clipboard: ${error}`);
					throw error;
				}
			} else {
				// Fallback for older browsers - use execCommand with a temporary element
				const listener = (e: ClipboardEvent) => {
					e.preventDefault();
					if (e.clipboardData) {
						e.clipboardData.setData('text/plain', text);
						e.clipboardData.setData('text/html', html);
					}
				};
				document.addEventListener('copy', listener);
				document.execCommand('copy');
				document.removeEventListener('copy', listener);
			}
		}

		public static async getHtml(): Promise<string> {
			const nav = navigator as any;
			if (nav.clipboard && typeof ClipboardItem !== 'undefined') {
				try {
					const items = await nav.clipboard.read();
					for (const item of items) {
						if (item.types.includes('text/html')) {
							const blob = await item.getType('text/html');
							return await blob.text();
						}
					}
				} catch (error) {
					console.error(`Failed to read HTML from clipboard: ${error}`);
				}
			}
			return "";
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
