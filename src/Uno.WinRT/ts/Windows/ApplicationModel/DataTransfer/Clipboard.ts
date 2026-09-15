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
		// The File registered for the image, created once so every view shares one registration.
		imageFile: File;
	}

	// What managed code gets for a view: the content itself when it is known (a captured paste or
	// this application's own write), or the status telling it how the content is to be read.
	interface ClipboardContent {
		status: string;
		texts: ClipboardTextEntry[];
		files: Uno.Storage.NativeStorageItemInfo[];
		image: Uno.Storage.NativeStorageItemInfo;
		// The native file registrations this content holds; released by the managed side.
		handles: string[];
		pasteShortcutTime: number;
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
		private static blurredSinceKnownContent = false;

		// Managed SetContent/Clear calls are numbered; one overtaken by a later call while
		// preparing its data must neither write nor publish, or the clipboard would end up with
		// content older than the last call's.
		private static latestWriteGeneration = 0;

		// Files handed to managed code are registered as native storage items; a registration is
		// shared by every view built from the same content and counted per view, so it is only
		// removed once the managed side has released the last one.
		private static handleReferences: Map<string, number> = new Map<string, number>();

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
			// making the known content (the last managed write, or the last captured paste) no
			// longer authoritative. Focus alone is not enough — spurious focus events fire at
			// startup and around browser UI (e.g. permission bubbles), so invalidation requires an
			// actual blur since the content became known.
			document.addEventListener("copy", Clipboard.invalidateKnownContent);
			document.addEventListener("cut", Clipboard.invalidateKnownContent);
			window.addEventListener("blur", () => { Clipboard.blurredSinceKnownContent = true; });
			window.addEventListener("focus", () => {
				if (Clipboard.blurredSinceKnownContent) {
					Clipboard.invalidateKnownContent();
				}
			});
		}

		private static invalidateKnownContent() {
			Clipboard.ownContent = null;
			Clipboard.lastPaste = null;
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

				// The paste is what the clipboard holds now; an earlier own write is no longer
				// what a read should return once the snapshot is no longer fresh.
				Clipboard.lastPaste = snapshot;
				Clipboard.ownContent = null;
				Clipboard.blurredSinceKnownContent = false;

				if (Clipboard.pasteWaiters.length > 0) {
					const waiters = Clipboard.pasteWaiters.splice(0, Clipboard.pasteWaiters.length);
					for (const waiter of waiters) {
						waiter(snapshot);
					}
				}

				// The snapshot is only advertised while fresh, but is retained longer so a view
				// built for a paste shortcut can still resolve against it; the files themselves
				// live on with the views that registered them.
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

			// clipboardData.files is a projection of the file-kind items, so items alone yields
			// each file exactly once. Enumerating both and deduplicating by identity is unreliable:
			// Chromium mints a fresh File instance per access for pasted image data.
			const items = event.clipboardData.items;
			if (items) {
				for (const item of items) {
					if (item.kind === "file") {
						const file = item.getAsFile();
						if (file) {
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

		// Copied image data (e.g. a screenshot) surfaces as a single synthesized image file,
		// so only that shape maps to the Bitmap format; multi-file pastes are file transfers
		// and surface as storage items alone, as they would on Windows.
		private static getPasteImageFile(snapshot: PasteSnapshot): File {
			return snapshot.files.length === 1 && snapshot.files[0].type && snapshot.files[0].type.startsWith("image/")
				? snapshot.files[0]
				: null;
		}

		private static emptyContent(status: string): ClipboardContent {
			return { status: status, texts: [], files: [], image: null, handles: [], pasteShortcutTime: -1 };
		}

		// Web custom formats surface with a "web " prefix; managed code uses the bare id.
		private static toManagedType(type: string): string {
			return type.startsWith("web ") ? type.substring(4) : type;
		}

		private static isPasteImminent(): boolean {
			return Clipboard.lastPasteShortcutTime >= 0 &&
				(performance.now() - Clipboard.lastPasteShortcutTime) <= Clipboard.pasteShortcutCorrelationMs;
		}

		// Synchronous probe used by the managed GetContent(). Known content is handed over
		// whole, so the view holds what it advertised whatever happens to the clipboard next.
		public static getSnapshot(): string {
			const snapshot = Clipboard.getFreshPasteSnapshot();
			let content: ClipboardContent;
			if (snapshot) {
				content = Clipboard.buildContentFromPaste(snapshot);
			} else if (Clipboard.isPasteImminent()) {
				// A paste shortcut was just pressed; the paste event carrying the content is on its way.
				content = Clipboard.emptyContent("imminent");
				content.pasteShortcutTime = Clipboard.lastPasteShortcutTime;
			} else if (Clipboard.ownContent) {
				content = Clipboard.buildContentFromOwn(Clipboard.ownContent);
			} else {
				content = Clipboard.emptyContent((navigator as NavigatorClipboard).clipboard ? "unknown" : "unavailable");
			}
			return JSON.stringify(content);
		}

		// Resolves a view whose content was not known when it was built: for a paste shortcut
		// (pasteShortcutTime >= 0) the paste captured at or after it, which may still be on its
		// way; otherwise whatever the clipboard holds now.
		public static async getContentAsync(pasteShortcutTime: number): Promise<string> {
			if (pasteShortcutTime >= 0) {
				let snapshot = Clipboard.lastPaste;
				if (snapshot && snapshot.time < pasteShortcutTime) {
					// The retained paste predates the shortcut; new content is incoming.
					snapshot = null;
				}

				if (!snapshot && Clipboard.isPasteImminent()) {
					// The paste shortcut can reach managed code before the DOM paste event fires;
					// wait briefly for the event instead of falling back to a permission-gated read.
					snapshot = await Clipboard.waitForPasteAsync();
				}

				if (snapshot) {
					return JSON.stringify(Clipboard.buildContentFromPaste(snapshot));
				}
			}

			const content = Clipboard.ownContent
				? Clipboard.buildContentFromOwn(Clipboard.ownContent)
				: await Clipboard.readAsyncClipboard();

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

		private static buildContentFromPaste(snapshot: PasteSnapshot): ClipboardContent {
			// Registering the files as native storage items lets managed code stream them
			// on demand instead of copying their content eagerly.
			const content = Clipboard.emptyContent("paste");
			content.texts = snapshot.texts;
			content.files = Clipboard.retainHandles(snapshot.files, content);

			const imageFile = Clipboard.getPasteImageFile(snapshot);
			if (imageFile) {
				content.image = content.files[snapshot.files.indexOf(imageFile)];
			}

			return content;
		}

		private static buildContentFromOwn(own: OwnContent): ClipboardContent {
			const content = Clipboard.emptyContent("own");
			content.texts = own.texts;

			if (own.imageBlob) {
				if (!own.imageFile) {
					const fileName = "clipboard" + Clipboard.getImageExtension(own.imageBlob.type);
					own.imageFile = new File([own.imageBlob], fileName, { type: own.imageBlob.type });
				}
				content.image = Clipboard.retainHandles([own.imageFile], content)[0];
			}

			return content;
		}

		// Registers the files (a File already registered keeps its id) and counts a reference
		// to each on behalf of the content, which the managed side releases when it is done.
		private static retainHandles(files: File[], content: ClipboardContent): Uno.Storage.NativeStorageItemInfo[] {
			if (files.length === 0) {
				return [];
			}

			const infos = Uno.Storage.NativeStorageItem.getInfos(...files);
			for (const info of infos) {
				if (content.handles.indexOf(info.id) < 0) {
					content.handles.push(info.id);
					Clipboard.handleReferences.set(info.id, (Clipboard.handleReferences.get(info.id) || 0) + 1);
				}
			}

			return infos;
		}

		public static releaseHandles(ids: string) {
			for (const id of ids.split(";")) {
				const references = Clipboard.handleReferences.get(id);
				if (!references) {
					continue;
				}

				if (references > 1) {
					Clipboard.handleReferences.set(id, references - 1);
				} else {
					Clipboard.handleReferences.delete(id);
					Uno.Storage.NativeStorageItem.removeItem(id);
				}
			}
		}

		private static async readAsyncClipboard(): Promise<ClipboardContent> {
			const nav = navigator as NavigatorClipboard;
			if (!nav.clipboard) {
				return Clipboard.emptyContent("unavailable");
			}

			if (nav.clipboard.read) {
				const content = Clipboard.emptyContent("async");
				try {
					const items = await nav.clipboard.read();

					for (const item of items) {
						for (const type of item.types) {
							if (type.startsWith("image/")) {
								if (!content.image) {
									const blob = await item.getType(type);
									const file = new File([blob], "clipboard" + Clipboard.getImageExtension(type), { type: type });
									content.image = Clipboard.retainHandles([file], content)[0];
								}
							} else {
								const blob = await item.getType(type);
								const value = await blob.text();
								// An empty payload is treated as absent: on the web an empty text
								// write is the representation of a cleared clipboard.
								if (value) {
									content.texts.push({ type: Clipboard.toManagedType(type), value: value });
								}
							}
						}
					}

					if (content.texts.length === 0 && !content.image) {
						content.status = "empty";
					}
					return content;
				} catch (e) {
					console.error(`Clipboard: failed to read from clipboard: ${e}`);
					// An image registered before a later representation failed is never handed out.
					if (content.handles.length > 0) {
						Clipboard.releaseHandles(content.handles.join(";"));
					}
					return Clipboard.emptyContent("denied");
				}
			}

			// Older engines without read(): plain text is the best we can do.
			try {
				const text = await nav.clipboard.readText();
				const content = Clipboard.emptyContent(text ? "async" : "empty");
				if (text) {
					content.texts.push({ type: "text/plain", value: text });
				}
				return content;
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

		// The call is dropped when a later SetContent/Clear got here first.
		private static beginWrite(generation: number): boolean {
			if (generation < Clipboard.latestWriteGeneration) {
				return false;
			}
			Clipboard.latestWriteGeneration = generation;
			return true;
		}

		private static publishOwnContent(ownContent: OwnContent) {
			Clipboard.ownContent = ownContent;
			// The write replaces whatever a paste captured before it.
			Clipboard.lastPaste = null;
			Clipboard.lastPasteShortcutTime = -1;
			// A write issued while the window is already blurred must still invalidate on refocus.
			Clipboard.blurredSinceKnownContent = !document.hasFocus();
			Clipboard.onClipboardChanged();
		}

		public static async setContentAsync(generation: number, entriesJson: string, imageBytes: any, imageMimeType: string): Promise<void> {
			if (!Clipboard.beginWrite(generation)) {
				return;
			}

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
				imageFile: null,
				texts: entries.map(e => ({ type: e.type, value: e.value })),
			};
			Clipboard.publishOwnContent(ownContent);

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

					// A later call may have written while the image was being transcoded; this
					// write must not land on top of it.
					if (generation !== Clipboard.latestWriteGeneration) {
						return;
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
				} else {
					// Nothing the browser can carry, but SetContent replaces the clipboard: the
					// previous content must not stay visible to other applications.
					await nav.clipboard.writeText("");
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
			// restore the cache it just cleared (unless a later call has replaced it since).
			if (generation === Clipboard.latestWriteGeneration) {
				Clipboard.ownContent = ownContent;
				Clipboard.blurredSinceKnownContent = !document.hasFocus();
			}
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

		public static async clearAsync(generation: number): Promise<void> {
			if (!Clipboard.beginWrite(generation)) {
				return;
			}

			Clipboard.publishOwnContent({ texts: [], imageBlob: null, imageFile: null });

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
