// Type declarations for Clipboard API
// https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API
interface ClipboardItem {
	readonly types: ReadonlyArray<string>;
	getType(type: string): Promise<Blob>;
}

interface ClipboardItemConstructor {
	new(items: Record<string, Blob | string | Promise<Blob | string>>): ClipboardItem;
	// https://developer.mozilla.org/en-US/docs/Web/API/ClipboardItem/supports_static
	supports?(type: string): boolean;
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

	interface ClipboardTextEntry {
		type: string;
		value: string;
	}

	interface ClipboardWriteEntry {
		type: string;
		value: string;
		custom: boolean;
	}

	interface PasteSnapshot {
		time: number;
		texts: ClipboardTextEntry[];
		files: File[];
	}

	interface OwnContent {
		texts: ClipboardTextEntry[];
		imageBlob: Blob;
	}

	export class Clipboard {
		private static dispatchContentChanged: () => number;

		// The DOM paste event is the only way browsers expose clipboard files (and the only
		// permission-free read), but it dies when the handler returns. This single-slot snapshot
		// bridges it to the asynchronous managed reads that follow a paste gesture.
		private static lastPaste: PasteSnapshot = null;
		private static lastPasteShortcutTime = -1;
		private static pasteWaiters: Array<(snapshot: PasteSnapshot) => void> = [];

		// Content written by the last managed SetContent/Clear, valid until an event indicates
		// the clipboard may have changed under us; null when the clipboard state is unknown.
		// Serving reads from this cache avoids permission-gated clipboard reads for content
		// this application wrote itself.
		private static ownContent: OwnContent = null;
		private static blurredSinceOwnWrite = false;

		private static readonly pasteFreshnessMs = 2000;
		private static readonly pasteRetentionMs = 30000;
		private static readonly pasteWaitTimeoutMs = 250;
		private static readonly pasteShortcutCorrelationMs = 1000;

		public static setup() {
			if (typeof document === "undefined") {
				return;
			}

			// Capture-phase so the snapshot is taken even when a control handles the paste itself.
			document.addEventListener("paste", Clipboard.onPasteCaptured, true);
			document.addEventListener("keydown", Clipboard.onKeyDownCaptured, true);

			// In-page copy/cut and returning from another window can change the clipboard content,
			// making the last managed write no longer authoritative. Focus alone is not enough —
			// spurious focus events fire at startup and around browser UI (e.g. permission
			// bubbles), so invalidation requires an actual blur since the last write.
			const invalidateOwnContent = () => { Clipboard.ownContent = null; };
			document.addEventListener("copy", invalidateOwnContent);
			document.addEventListener("cut", invalidateOwnContent);
			window.addEventListener("blur", () => { Clipboard.blurredSinceOwnWrite = true; });
			window.addEventListener("focus", () => {
				if (Clipboard.blurredSinceOwnWrite) {
					Clipboard.ownContent = null;
				}
			});
		}

		private static onKeyDownCaptured(event: KeyboardEvent) {
			const isPasteShortcut =
				((event.ctrlKey || event.metaKey) && (event.key === "v" || event.key === "V")) ||
				(event.shiftKey && event.key === "Insert");
			if (isPasteShortcut) {
				Clipboard.lastPasteShortcutTime = performance.now();
			}
		}

		private static onPasteCaptured(event: ClipboardEvent) {
			try {
				const snapshot = Clipboard.capturePaste(event);
				if (!snapshot) {
					return;
				}

				Clipboard.lastPaste = snapshot;

				if (Clipboard.pasteWaiters.length > 0) {
					const waiters = Clipboard.pasteWaiters.splice(0, Clipboard.pasteWaiters.length);
					for (const waiter of waiters) {
						waiter(snapshot);
					}
				}

				// The snapshot is only advertised while fresh, but is retained longer so a view
				// built from it can still resolve its providers; release the file references
				// once no such view can reasonably remain.
				setTimeout(() => {
					if (Clipboard.lastPaste === snapshot) {
						Clipboard.lastPaste = null;
					}
				}, Clipboard.pasteRetentionMs);
			} catch (e) {
				console.error(`Clipboard: failed to capture paste event: ${e}`);
			}
		}

		private static capturePaste(event: ClipboardEvent): PasteSnapshot {
			if (!event.clipboardData) {
				return null;
			}

			const texts: ClipboardTextEntry[] = [];
			const files: File[] = [];
			const seenFiles = new Set<File>();

			const fileList = event.clipboardData.files;
			if (fileList) {
				for (const file of fileList) {
					seenFiles.add(file);
					files.push(file);
				}
			}

			const items = event.clipboardData.items;
			if (items) {
				for (const item of items) {
					if (item.kind === "file") {
						const file = item.getAsFile();
						if (file && !seenFiles.has(file)) {
							seenFiles.add(file);
							files.push(file);
						}
					} else if (item.kind === "string") {
						const type = item.type || "text/plain";
						// getData is synchronous within a paste event (unlike item.getAsString),
						// so the payload survives past the event handler.
						// An empty payload is treated as absent: on the web an empty text write
						// is the representation of a cleared clipboard.
						const value = event.clipboardData.getData(type) || "";
						if (value) {
							texts.push({ type: Clipboard.toManagedType(type), value: value });
						}
					}
				}
			}

			if (texts.length === 0 && files.length === 0) {
				return null;
			}

			return { time: performance.now(), texts: texts, files: files };
		}

		private static getFreshPasteSnapshot(): PasteSnapshot {
			const snapshot = Clipboard.lastPaste;
			if (snapshot && (performance.now() - snapshot.time) <= Clipboard.pasteFreshnessMs) {
				return snapshot;
			}
			return null;
		}

		private static emptyContent(status: string) {
			return { status: status, texts: <ClipboardTextEntry[]>[], files: <any[]>[], image: <any>null };
		}

		// Web custom formats surface with a "web " prefix; managed code uses the bare id.
		private static toManagedType(type: string): string {
			return type.startsWith("web ") ? type.substring(4) : type;
		}

		private static isPasteImminent(): boolean {
			return Clipboard.lastPasteShortcutTime >= 0 &&
				(performance.now() - Clipboard.lastPasteShortcutTime) <= Clipboard.pasteShortcutCorrelationMs;
		}

		private static getOwnFormats(): string[] {
			const own = Clipboard.ownContent;
			if (!own) {
				return null;
			}

			const formats = own.texts.map(t => t.type);
			if (own.imageBlob) {
				formats.push(own.imageBlob.type);
			}
			return formats;
		}

		// Synchronous probe used by the managed GetContent() to decide which formats to advertise.
		public static getSnapshotFormats(): string {
			const snapshot = Clipboard.getFreshPasteSnapshot();
			return JSON.stringify({
				ownFormats: Clipboard.getOwnFormats(),
				pasteFormats: snapshot ? snapshot.texts.map(t => t.type) : null,
				pasteHasFiles: snapshot ? snapshot.files.length > 0 : false,
				pasteHasImage: snapshot ? snapshot.files.some(f => f.type && f.type.startsWith("image/")) : false,
				pasteImminent: !snapshot && Clipboard.isPasteImminent(),
			});
		}

		// fromPaste is true when the caller built its view from a paste snapshot (or an imminent
		// paste) and must resolve against it even once it is no longer fresh.
		public static async getContentAsync(fromPaste: boolean): Promise<string> {
			let snapshot = fromPaste ? Clipboard.lastPaste : Clipboard.getFreshPasteSnapshot();

			// A paste shortcut newer than the retained snapshot means new content is incoming;
			// wait for it rather than serving the previous paste.
			if (snapshot && Clipboard.lastPasteShortcutTime > snapshot.time) {
				snapshot = null;
			}

			if (!snapshot && Clipboard.isPasteImminent()) {
				// The paste shortcut can reach managed code before the DOM paste event fires;
				// wait briefly for the event instead of falling back to a permission-gated read.
				snapshot = await Clipboard.waitForPasteAsync();
			}

			let content: any;
			if (snapshot) {
				content = Clipboard.buildContentFromPaste(snapshot);
			} else if (Clipboard.ownContent) {
				content = Clipboard.buildContentFromOwn(Clipboard.ownContent);
			} else {
				content = await Clipboard.readAsyncClipboard();
			}

			return JSON.stringify(content);
		}

		private static waitForPasteAsync(): Promise<PasteSnapshot> {
			return new Promise<PasteSnapshot>(resolve => {
				const waiter = (snapshot: PasteSnapshot) => resolve(snapshot);
				Clipboard.pasteWaiters.push(waiter);
				setTimeout(() => {
					const index = Clipboard.pasteWaiters.indexOf(waiter);
					if (index >= 0) {
						Clipboard.pasteWaiters.splice(index, 1);
						resolve(null);
					}
				}, Clipboard.pasteWaitTimeoutMs);
			});
		}

		private static buildContentFromPaste(snapshot: PasteSnapshot) {
			// Registering the files as native storage items lets managed code stream them
			// on demand instead of copying their content eagerly.
			const files = snapshot.files.length > 0
				? Uno.Storage.NativeStorageItem.getInfos(...snapshot.files)
				: [];

			const imageFile = snapshot.files.find(f => f.type && f.type.startsWith("image/"));
			const image = imageFile ? Uno.Storage.NativeStorageItem.getInfos(imageFile)[0] : null;

			return { status: "paste", texts: snapshot.texts, files: files, image: image };
		}

		private static buildContentFromOwn(own: OwnContent) {
			let image: any = null;
			if (own.imageBlob) {
				const fileName = "clipboard" + Clipboard.getImageExtension(own.imageBlob.type);
				const file = new File([own.imageBlob], fileName, { type: own.imageBlob.type });
				image = Uno.Storage.NativeStorageItem.getInfos(file)[0];
			}

			return { status: "own", texts: own.texts, files: <any[]>[], image: image };
		}

		private static async readAsyncClipboard() {
			const nav = navigator as NavigatorClipboard;
			if (!nav.clipboard) {
				return Clipboard.emptyContent("unavailable");
			}

			if (nav.clipboard.read) {
				try {
					const items = await nav.clipboard.read();
					const texts: ClipboardTextEntry[] = [];
					let image: any = null;

					for (const item of items) {
						for (const type of item.types) {
							if (type.startsWith("image/")) {
								if (!image) {
									const blob = await item.getType(type);
									const file = new File([blob], "clipboard" + Clipboard.getImageExtension(type), { type: type });
									image = Uno.Storage.NativeStorageItem.getInfos(file)[0];
								}
							} else {
								const blob = await item.getType(type);
								const value = await blob.text();
								// An empty payload is treated as absent: on the web an empty text
								// write is the representation of a cleared clipboard.
								if (value) {
									texts.push({ type: Clipboard.toManagedType(type), value: value });
								}
							}
						}
					}

					const status = (texts.length > 0 || image) ? "async" : "empty";
					return { status: status, texts: texts, files: <any[]>[], image: image };
				} catch (e) {
					console.error(`Clipboard: failed to read from clipboard: ${e}`);
					return Clipboard.emptyContent("denied");
				}
			}

			// Older engines without read(): plain text is the best we can do.
			try {
				const text = await nav.clipboard.readText();
				return text
					? { status: "async", texts: [{ type: "text/plain", value: text }], files: <any[]>[], image: <any>null }
					: Clipboard.emptyContent("empty");
			} catch (e) {
				console.error(`Clipboard: failed to read text from clipboard: ${e}`);
				return Clipboard.emptyContent("denied");
			}
		}

		private static getImageExtension(mimeType: string): string {
			switch (mimeType) {
				case "image/png": return ".png";
				case "image/jpeg": return ".jpg";
				case "image/gif": return ".gif";
				case "image/bmp": return ".bmp";
				case "image/webp": return ".webp";
				default: return "";
			}
		}

		public static async setContentAsync(entriesJson: string, imageBytes: any, imageMimeType: string): Promise<void> {
			const entries: ClipboardWriteEntry[] = JSON.parse(entriesJson);
			const nav = navigator as NavigatorClipboard;
			const hasImage = !!imageMimeType && !!imageBytes;

			let imageBlob: Blob = null;
			if (hasImage) {
				const bytes = imageBytes instanceof Uint8Array ? imageBytes : new Uint8Array(imageBytes);
				imageBlob = new Blob([bytes], { type: imageMimeType });
			}

			// Cache optimistically (with every entry, even formats the browser rejects) so a
			// GetContent immediately following SetContent sees the new state, and in-process
			// reads round-trip with full fidelity as they would on WinUI.
			const ownContent: OwnContent = {
				imageBlob: imageBlob,
				texts: entries.map(e => ({ type: e.type, value: e.value })),
			};
			Clipboard.ownContent = ownContent;
			// A write issued while the window is already blurred must still invalidate on refocus.
			Clipboard.blurredSinceOwnWrite = !document.hasFocus();
			Clipboard.onClipboardChanged();

			// The system-clipboard write below is best-effort: browsers reject it outside a user
			// gesture. The cache above keeps the content readable in-process either way (matching
			// WinUI semantics); only sharing with other applications is lost. A rejection
			// propagates so the managed side can log it.
			if (nav.clipboard && nav.clipboard.write && typeof ClipboardItem !== "undefined") {
				const record: Record<string, Blob> = {};

				for (const entry of entries) {
					if (entry.custom) {
						const webType = "web " + entry.type;
						if (Clipboard.supportsCustomFormat(webType)) {
							// The blob type must match the ClipboardItem key or the write is rejected.
							record[webType] = new Blob([entry.value], { type: webType });
						} else {
							console.warn(`Clipboard: custom format '${entry.type}' is not supported by this browser and was skipped.`);
						}
					} else {
						record[entry.type] = new Blob([entry.value], { type: entry.type });
					}
				}

				if (imageBlob && imageBlob.type !== "image/png") {
					// Browsers only accept image/png for clipboard writes.
					const png = await Clipboard.tryTranscodeToPng(imageBlob);
					if (png) {
						imageBlob = png;
						ownContent.imageBlob = png;
					}
				}

				if (imageBlob) {
					if (imageBlob.type === "image/png") {
						record[imageBlob.type] = imageBlob;
					} else {
						// Including a non-PNG blob would make the whole atomic write reject,
						// losing the other formats too; the cache still serves the image in-process.
						console.warn("Clipboard: the image could not be transcoded to PNG and was not written to the system clipboard.");
					}
				}

				if (Object.keys(record).length > 0) {
					// A single ClipboardItem so all formats are written atomically, as WinUI does.
					const item = new ClipboardItem(record);
					await nav.clipboard.write([item]);
				}

				return;
			}

			// Fallbacks can only carry plain text.
			const text = entries.find(e => e.type === "text/plain");
			if (nav.clipboard) {
				await nav.clipboard.writeText(text ? text.value : "");
				return;
			}

			const textarea = document.createElement("textarea");
			textarea.value = text ? text.value : "";
			document.body.appendChild(textarea);
			textarea.select();
			document.execCommand("copy");
			document.body.removeChild(textarea);

			// execCommand dispatched a copy event, which the invalidation listener handled;
			// restore the cache it just cleared.
			Clipboard.ownContent = ownContent;
			Clipboard.blurredSinceOwnWrite = !document.hasFocus();
		}

		// Guarded so an engine rejecting an unparsable format id cannot abort the whole write.
		private static supportsCustomFormat(webType: string): boolean {
			try {
				return !!(ClipboardItem.supports && ClipboardItem.supports(webType));
			} catch (e) {
				return false;
			}
		}

		private static async tryTranscodeToPng(blob: Blob): Promise<Blob> {
			try {
				if (typeof createImageBitmap === "undefined" || typeof (globalThis as any).OffscreenCanvas === "undefined") {
					return null;
				}

				const bitmap = await createImageBitmap(blob);
				try {
					const canvas = new (globalThis as any).OffscreenCanvas(bitmap.width, bitmap.height);
					const context = canvas.getContext("2d");
					if (!context) {
						return null;
					}

					context.drawImage(bitmap, 0, 0);
					return await canvas.convertToBlob({ type: "image/png" });
				} finally {
					bitmap.close();
				}
			} catch (e) {
				console.warn(`Clipboard: failed to transcode image to PNG: ${e}`);
				return null;
			}
		}

		public static async clearAsync(): Promise<void> {
			Clipboard.lastPaste = null;

			Clipboard.ownContent = { texts: [], imageBlob: null };
			Clipboard.blurredSinceOwnWrite = !document.hasFocus();
			Clipboard.onClipboardChanged();

			const nav = navigator as NavigatorClipboard;
			if (nav.clipboard) {
				// Browsers cannot truly empty the clipboard; an empty text write is the closest
				// equivalent. The cleared state is kept for in-process reads even when the
				// browser rejects the write (no user gesture).
				await nav.clipboard.writeText("");
			}
		}

		public static startContentChanged() {
			['cut', 'copy', 'paste'].forEach(function (event) {
				document.addEventListener(event, Clipboard.onClipboardChanged);
			});

			// Browsers cannot observe external clipboard changes; re-raising on focus lets
			// subscribers re-query after the user may have copied content in another window.
			window.addEventListener("focus", Clipboard.onClipboardChanged);
		}

		public static stopContentChanged() {
			['cut', 'copy', 'paste'].forEach(function (event) {
				document.removeEventListener(event, Clipboard.onClipboardChanged);
			});

			window.removeEventListener("focus", Clipboard.onClipboardChanged);
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

	Clipboard.setup();
}
