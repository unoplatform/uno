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
					Clipboard.logReadFailure("Failed to read from clipboard", reason);
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
				let items: ClipboardItem[];
				try {
					items = await nav.clipboard.read();
				} catch (reason) {
					Clipboard.logReadFailure("Failed to read HTML from clipboard", reason);
					return "";
				}

				try {
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

		public static async getImage(): Promise<string> {
			// we return "" on failure instead of null to avoid crashing with an NRE if there
			// are no try blocks in the stack frames above.
			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard && nav.clipboard.read) {
				let items: ClipboardItem[];
				try {
					items = await nav.clipboard.read();
				} catch (reason) {
					Clipboard.logReadFailure("Failed to read image from clipboard", reason);
					return "";
				}

				try {
					for (const item of items) {
						const imageType = item.types.find(t => t.startsWith('image/'));
						if (imageType) {
							const blob = await item.getType(imageType);
							const dataUrl = await new Promise<string>((resolve, reject) => {
								const reader = new FileReader();
								reader.onload = () => {
									if (typeof reader.result === "string") {
										resolve(reader.result);
									} else {
										reject(new Error("Unexpected FileReader result type when reading image from clipboard."));
									}
								};
								reader.onerror = () => {
									reject(reader.error ?? new Error("Failed to read image from clipboard using FileReader."));
								};
								reader.readAsDataURL(blob);
							});

							const commaIndex = dataUrl.indexOf(",");
							const base64 = commaIndex >= 0 ? dataUrl.substring(commaIndex + 1) : dataUrl;
							return base64;
						}
					}
					return "";
				} catch (reason) {
					console.error(`Failed to read image from clipboard: ${reason}`);
					return "";
				}
			}
			return Promise.resolve("");
		}

		// A read the browser refuses (no permission or user activation, page not focused, blocked by the
		// permissions policy) is an expected outcome and is logged at debug level; any other failure is an error.
		private static logReadFailure(message: string, reason: unknown) {
			if (reason instanceof DOMException && reason.name === "NotAllowedError") {
				console.debug(`${message}: ${reason}`);
			} else {
				console.error(`${message}: ${reason}`);
			}
		}

		public static async setImage(base64: string, mimeType: string): Promise<void> {
			const nav = navigator as any;
			if (nav.clipboard && nav.clipboard.write && typeof ClipboardItem !== 'undefined') {
				try {
					const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
					const blob = new Blob([bytes], { type: mimeType });
					const item = new ClipboardItem({ [mimeType]: blob });
					await nav.clipboard.write([item]);
					Clipboard.onClipboardChanged();
				} catch (reason) {
					console.error(`Failed to write image to clipboard: ${reason}`);
				}
			}
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
