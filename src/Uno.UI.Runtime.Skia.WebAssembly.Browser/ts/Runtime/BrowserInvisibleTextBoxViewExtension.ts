namespace Uno.UI.Runtime.Skia {
	export class BrowserInvisibleTextBoxViewExtension {
		private static _exports: any;
		private static _imeExports: any;
		private static readonly isMacOS = navigator?.platform.toUpperCase().includes('MAC') ?? false;
		// iPadOS 13+ reports itself as a Mac, so touch support is what tells it apart from macOS Safari.
		private static readonly isIOS = /iP(ad|hone|od)/.test(navigator?.platform ?? "")
			|| (navigator?.platform === "MacIntel" && (navigator.maxTouchPoints ?? 0) > 1);

		// iOS WebKit scrolls the page to reveal the focused editable, on focus and on every keyboard frame
		// change. The page is position:fixed, so that scroll pans the whole canvas and shifts hit-testing by
		// the same amount. Off-screen there is nothing to reveal; the TextBox draws its own caret, selection
		// and grippers, so the input's position and size are unused there.
		private static readonly keepsInputOffscreen = BrowserInvisibleTextBoxViewExtension.isIOS;
		private static readonly offscreenTop = "-10000px";

		// The InputPane drives the bring-into-view of the focused TextBox; the browser's own scroll-to-focus
		// would only pan the visual viewport (see keepsInputOffscreen).
		private static readonly focusOptions: FocusOptions = { preventScroll: true };
		private static inputElement: HTMLInputElement | HTMLTextAreaElement | null;

		// Issue-1 trailing-click guard state (see installTrailingClickGuard).
		private static swallowNextCanvasClick: boolean;
		private static lastPointerType: string;

		// Set while a managed-initiated blur/detach is in progress so the input's own blur
		// listener doesn't report it back to the FocusManager (that would be redundant and
		// could clear focus on the wrong TextBox during a focus switch). Genuine, browser-
		// initiated blurs happen with this false and ARE reported (see OnNativeBlur).
		private static suppressBlurNotification: boolean;

		// Visual handle of the TextBox that currently owns the shared input, so a stale managed
		// blur from a TextBox that already lost it cannot detach its successor's input.
		private static currentHandle: number = 0;

		// Bumped by focus()/detachCore() to supersede the deferred detach scheduled by blur():
		// a blur immediately followed by a focus is a TextBox-to-TextBox move, and detaching
		// would needlessly dismiss the soft keyboard (see blur).
		private static detachGeneration: number = 0;
		private static isInSelectionChange: boolean;
		private static acceptsReturn: boolean;
		private static isComposing: boolean;
		// Value of the input when the current composition started, and the latest preedit string the
		// IME reported: together they locate the preedit inside the input's value.
		private static compositionBaseValue: string;
		private static compositionText: string;
		private static compositionStart: number;
		// Commit announced by a compositionend delivered before the input event that applies it (see the
		// compositionend handler).
		private static pendingCommit: string | null = null;
		// The preedit was removed by an input event of WebKit's own, reported with what follows it (see syncFromInput).
		private static preeditRemoved: boolean = false;
		// The preedit last reported, so putting it back unchanged is not reported again.
		private static reportedText: string | null = null;
		// Value last reported to (or written from) the TextBox, to tell whether the input holds
		// text the TextBox has not seen yet.
		private static lastSyncedValue: string;
		private static enterHandledByKeyDown: boolean;

		// Android soft keyboards report all key events with keyCode 229 ("Unidentified").
		// Text changes are synced via the oninput handler instead.
		private static readonly ANDROID_IME_KEYCODE = 229;

		// Soft keyboards report key events without a physical code (Backspace, Enter, ...). The
		// browser must handle those itself so the IME's view of the text stays consistent: editing
		// the input's value from managed code in response resets the IME mid-word.
		private static isSoftKeyboardKey(ev: KeyboardEvent): boolean {
			return ev.keyCode === BrowserInvisibleTextBoxViewExtension.ANDROID_IME_KEYCODE || ev.code === "" || ev.code === "Unidentified";
		}

		// focus() may replace the input while the browser still owes the old one a compositionend (it
		// ends the composition when the element goes away), or hand the same element over to another
		// TextBox; a handler run from an event can do either mid-event. Stragglers of an input, or of
		// the TextBox it was serving, must not be applied to the TextBox now on it.
		private static isCurrentInput(input: HTMLInputElement | HTMLTextAreaElement, handle: number = BrowserInvisibleTextBoxViewExtension.currentHandle): boolean {
			return input === BrowserInvisibleTextBoxViewExtension.inputElement && handle === BrowserInvisibleTextBoxViewExtension.currentHandle;
		}

		private static isComposingOn(input: HTMLInputElement | HTMLTextAreaElement, handle: number = BrowserInvisibleTextBoxViewExtension.currentHandle): boolean {
			return BrowserInvisibleTextBoxViewExtension.isCurrentInput(input, handle) && BrowserInvisibleTextBoxViewExtension.isComposing;
		}

		// The TextBox stores line breaks as CR while the input stores them as LF; text is compared
		// and written in the input's form so a line break doesn't read as a change to write back.
		private static toInputText(text: string): string {
			return text.replace(/\r\n?/g, "\n");
		}

		public static initialize() {
			if (BrowserInvisibleTextBoxViewExtension._exports == undefined) {
				const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();

				BrowserInvisibleTextBoxViewExtension._exports = browserExports.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension;
				BrowserInvisibleTextBoxViewExtension._imeExports = browserExports.Uno.UI.Runtime.Skia.WasmImeTextBoxExtension;

				BrowserInvisibleTextBoxViewExtension.installTrailingClickGuard();

				document.onselectionchange = () => {
					let input = document.activeElement;
					if (input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) {
						BrowserInvisibleTextBoxViewExtension.isInSelectionChange = true;
						try {
							if (input.selectionDirection == "backward") {
								BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(input.selectionEnd, input.selectionStart - input.selectionEnd);
							} else {
								BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(input.selectionStart, input.selectionEnd - input.selectionStart);
							}
						} finally {
							BrowserInvisibleTextBoxViewExtension.isInSelectionChange = false;
						}
					}
				}
			}
		}

		// Neutralizes the touch-only, WebKit-internal race that dismisses the soft keyboard right
		// after a TextBox tap (see swallowNextCanvasClick). Uno already preventDefaults pointer
		// events, but per the Pointer Events spec that does NOT suppress the compatibility
		// mouse events touch browsers synthesize, so the trailing mousedown still reaches the
		// canvas and blurs #uno-input. We can't preventDefault it from the pointer path, so we
		// intercept the mouse event itself, scoped to the single tap that just focused the input.
		private static installTrailingClickGuard() {
			// Any new pointer gesture disarms a stale flag, so only the mouse events synthesized
			// from the very tap that focused the input are ever swallowed. Also records the pointer
			// type: the bug is exclusive to touch/pen, where the compat mousedown is deferred until
			// after focus. With a mouse, mousedown precedes focus, so there is nothing to guard.
			document.addEventListener("pointerdown", (ev: PointerEvent) => {
				BrowserInvisibleTextBoxViewExtension.lastPointerType = ev.pointerType;
				BrowserInvisibleTextBoxViewExtension.swallowNextCanvasClick = false;
			}, { capture: true });

			// focusin bubbles (unlike focus), so a single document-level listener catches the
			// invisible input regardless of when it is (re)created.
			document.addEventListener("focusin", (ev: FocusEvent) => {
				const target = ev.target as HTMLElement;
				if (target?.id === UnoDomIds.input
					&& (BrowserInvisibleTextBoxViewExtension.lastPointerType === "touch"
						|| BrowserInvisibleTextBoxViewExtension.lastPointerType === "pen")) {
					BrowserInvisibleTextBoxViewExtension.swallowNextCanvasClick = true;
				}
			}, { capture: true });

			const swallow = (ev: Event) => {
				if (BrowserInvisibleTextBoxViewExtension.swallowNextCanvasClick
					&& (ev.target as HTMLElement)?.id === UnoDomIds.canvas) {
					ev.preventDefault();
					ev.stopImmediatePropagation();
					BrowserInvisibleTextBoxViewExtension.swallowNextCanvasClick = false;
				}
			};
			document.addEventListener("mousedown", swallow, { capture: true });
			document.addEventListener("click", swallow, { capture: true });
		}

		private static createInput(isPasswordBox: boolean, text: string, acceptsReturn: boolean, inputMode: string, enterKeyHint: string, isReadOnly: boolean) {
			BrowserInvisibleTextBoxViewExtension.acceptsReturn = acceptsReturn;
			// A previous input may have been removed mid-composition without a compositionend;
			// never carry that state over to a fresh element.
			BrowserInvisibleTextBoxViewExtension.resetComposition();
			const input = document.createElement(acceptsReturn && !isPasswordBox ? "textarea" : "input");
			// The keydown/keyup handlers capture acceptsReturn by closure; record it so canRetarget
			// only reuses the element when the captured behavior still matches.
			(input as any).__unoAcceptsReturn = acceptsReturn;
			if (isPasswordBox) {
				(input as HTMLInputElement).type = "password";
				input.autocomplete = "password";
			}

			input.id = UnoDomIds.input;
			input.tabIndex = -1;
			input.spellcheck = false;
			input.readOnly = isReadOnly;
			input.style.whiteSpace = "pre-wrap";
			input.style.position = "absolute";
			input.style.padding = "0px";
			input.style.opacity = "0";
			input.style.color = "transparent";
			input.style.background = "transparent";
			input.style.caretColor = "transparent";
			input.style.outline = "none";
			input.style.border = "none";
			input.style.resize = "none";
			input.style.textShadow = "none";
			input.style.overflow = "hidden";
			input.style.pointerEvents = "none";
			input.style.zIndex = "99";
			// Placed before focus() so WebKit never sees an on-screen rect to reveal. Only top goes off-screen:
			// a negative left would be scrollable overflow in a right-to-left document.
			input.style.top = BrowserInvisibleTextBoxViewExtension.keepsInputOffscreen ? BrowserInvisibleTextBoxViewExtension.offscreenTop : "0px";
			input.style.left = "0px";
			input.value = BrowserInvisibleTextBoxViewExtension.toInputText(text);
			BrowserInvisibleTextBoxViewExtension.lastSyncedValue = input.value;

			input.setAttribute("inputmode", inputMode);
			input.setAttribute("enterkeyhint", enterKeyHint);

			// The input's value is the source of truth for the text; composition events only report
			// which range of it is the active preedit. Chrome fires the committed preedit's input event
			// before compositionend, so no input event may be skipped based on composition state:
			// the next one would be the space or punctuation that committed the word.
			input.oninput = (ev: Event) => {
				if (BrowserInvisibleTextBoxViewExtension.isCurrentInput(input)) {
					BrowserInvisibleTextBoxViewExtension.syncFromInput(input, ev as InputEvent);
				}
			};

			input.onpaste = ev => {
				BrowserInvisibleTextBoxViewExtension._exports.OnNativePaste(ev.clipboardData.getData("text"));
				ev.preventDefault();
			};

			// C# drives focus one-way (StartEntry/EndEntry call focus()/blur()), so a blur the
			// browser initiates on its own — e.g. tapping outside, or the touch-synthesized
			// mousedown in issue 1 — is otherwise invisible to the FocusManager and LostFocus
			// never fires. Report only those; managed-initiated blurs set suppressBlurNotification.
			input.addEventListener("blur", () => {
				if (BrowserInvisibleTextBoxViewExtension.suppressBlurNotification) {
					return;
				}
				BrowserInvisibleTextBoxViewExtension._exports.OnNativeBlur();
			});

			// Handle Enter key from Android virtual keyboards which don't fire keydown events.
			// Android keyboards typically fire beforeinput with inputType "insertLineBreak" or "insertParagraph" instead.
			input.addEventListener("beforeinput", (ev: InputEvent) => {
				if ((ev.inputType === "insertLineBreak" || ev.inputType === "insertParagraph")
					&& !BrowserInvisibleTextBoxViewExtension.acceptsReturn
					&& BrowserInvisibleTextBoxViewExtension.isCurrentInput(input)) {
					ev.preventDefault();

					BrowserInvisibleTextBoxViewExtension._exports.OnEnterKeyPressed();
				}
			});

			BrowserInvisibleTextBoxViewExtension.attachTextInputKeyHandlers(input, acceptsReturn);

			input.addEventListener("compositionstart", () => {
				if (!BrowserInvisibleTextBoxViewExtension.isCurrentInput(input)) {
					return;
				}
				// A composition still open here never got its compositionend (a commit the browser never
				// applied); it is over now. A handler of its end may have moved focus, replacing the input
				// or handing it to another TextBox.
				const handle = BrowserInvisibleTextBoxViewExtension.currentHandle;
				if (BrowserInvisibleTextBoxViewExtension.isComposing) {
					BrowserInvisibleTextBoxViewExtension.endComposition();
					if (!BrowserInvisibleTextBoxViewExtension.isCurrentInput(input, handle)) {
						return;
					}
				}
				BrowserInvisibleTextBoxViewExtension.isComposing = true;
				BrowserInvisibleTextBoxViewExtension.compositionBaseValue = input.value;
				BrowserInvisibleTextBoxViewExtension.compositionText = "";
				BrowserInvisibleTextBoxViewExtension.compositionStart = -1;
				BrowserInvisibleTextBoxViewExtension.reportedText = null;
				BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionStarted();
			});

			input.addEventListener("compositionupdate", (ev: CompositionEvent) => {
				if (!BrowserInvisibleTextBoxViewExtension.isCurrentInput(input)) {
					return;
				}
				// The value isn't updated yet; the preedit is reported from the input event that follows.
				BrowserInvisibleTextBoxViewExtension.compositionText = ev.data;
			});

			input.addEventListener("compositionend", (ev: CompositionEvent) => {
				// The browser still fires compositionend for a composition that a text change from
				// managed code already ended; that one has nothing left to report.
				if (!BrowserInvisibleTextBoxViewExtension.isComposingOn(input)) {
					return;
				}
				const handle = BrowserInvisibleTextBoxViewExtension.currentHandle;
				// Text the input holds that the TextBox has not seen yet is reported before completing, except
				// a preedit WebKit removed on its way to putting the committed text in place (see syncFromInput).
				const committed = ev.data;
				if (input.value !== BrowserInvisibleTextBoxViewExtension.lastSyncedValue
					&& !(BrowserInvisibleTextBoxViewExtension.preeditRemoved && committed.length > 0)) {
					BrowserInvisibleTextBoxViewExtension.syncFromInput(input, null);
					// A handler may have moved focus or set the text; the composition then belongs to the past.
					if (!BrowserInvisibleTextBoxViewExtension.isComposingOn(input, handle)) {
						return;
					}
				}
				// Chrome delivers compositionend once the committed text is in place, and so does iOS
				// WebKit (through input events of its own, see syncFromInput). WebKit can also deliver it
				// first, with the preedit still in the input (selected, about to be replaced by the committed
				// text or removed for a cancel) or just removed, and only then the input event that applies
				// the commit: the completion then waits for that event, so its handlers see the committed
				// text rather than the preedit.
				// A commit equal to the preedit the input holds needs no wait, and Blink finishes a composition
				// whose text was selected under the keyboard the same way, with no input event to wait for.
				const preedit = BrowserInvisibleTextBoxViewExtension.preeditRemoved ? "" : BrowserInvisibleTextBoxViewExtension.compositionText;
				const start = BrowserInvisibleTextBoxViewExtension.compositionStart;
				const preeditSelected = preedit.length > 0 && input.selectionStart === start && input.selectionEnd === start + preedit.length;
				if ((preeditSelected && committed.length === 0) || (committed.length > 0 && committed !== preedit)) {
					BrowserInvisibleTextBoxViewExtension.compositionText = committed;
					BrowserInvisibleTextBoxViewExtension.pendingCommit = committed;
					return;
				}
				BrowserInvisibleTextBoxViewExtension.completeComposition(committed);
			});

			document.body.appendChild(input);
			BrowserInvisibleTextBoxViewExtension.inputElement = input;
		}

		// ev is null when called from compositionend, for a value the browser changed as part of the composition.
		private static syncFromInput(input: HTMLInputElement | HTMLTextAreaElement, ev: InputEvent | null) {
			const handle = BrowserInvisibleTextBoxViewExtension.currentHandle;
			if (BrowserInvisibleTextBoxViewExtension.isComposing) {
				const inputType = ev?.inputType;
				// WebKit commits through input events of its own, with no compositionupdate for the committed
				// text: the preedit is removed, then the committed text is put in place (on iOS both before
				// compositionend). The removal is reported with what follows it rather than on its own, so
				// handlers do not see the composition emptied and refilled; a cancel already delivered
				// (pendingCommit "") is what the removal completes, though.
				if (inputType === "deleteCompositionText" && BrowserInvisibleTextBoxViewExtension.pendingCommit !== "") {
					BrowserInvisibleTextBoxViewExtension.preeditRemoved = true;
					return;
				}
				const removed = BrowserInvisibleTextBoxViewExtension.preeditRemoved || inputType === "deleteCompositionText";
				BrowserInvisibleTextBoxViewExtension.preeditRemoved = false;
				if (inputType === "insertFromComposition" && ev.data) {
					// The insertion's data is what the composition now holds.
					BrowserInvisibleTextBoxViewExtension.compositionText = ev.data;
					if (BrowserInvisibleTextBoxViewExtension.pendingCommit !== null) {
						BrowserInvisibleTextBoxViewExtension.pendingCommit = ev.data;
					}
				}
				const pendingCommit = BrowserInvisibleTextBoxViewExtension.pendingCommit;
				const isCompositionInput = ev === null || ev.isComposing
					|| inputType === "insertCompositionText" || inputType === "deleteCompositionText" || inputType === "insertFromComposition";
				const reinserted = inputType === "insertCompositionText" || inputType === "insertFromComposition";
				let text = BrowserInvisibleTextBoxViewExtension.compositionText;
				let start = BrowserInvisibleTextBoxViewExtension.findCompositionStart(input, text);
				if (pendingCommit !== null && pendingCommit.length > 0 && !isCompositionInput
					&& !BrowserInvisibleTextBoxViewExtension.isCommitInPlace(input, pendingCommit, start)) {
					// The commit a compositionend announced never landed and this is the next keystroke: the
					// composition ends with the text as it was, and the keystroke is a plain one.
					BrowserInvisibleTextBoxViewExtension.endComposition();
				} else if (text.length > 0 && ((removed && !reinserted) || !input.value.startsWith(text, start))) {
					// A removed preedit is an emptied one, still where it was; a commit still to land stays expected.
					text = "";
					start = BrowserInvisibleTextBoxViewExtension.findCompositionStart(input, text);
					if (pendingCommit === null) {
						BrowserInvisibleTextBoxViewExtension.compositionText = text;
					}
				}
				if (BrowserInvisibleTextBoxViewExtension.isComposing) {
					const valueChanged = input.value !== BrowserInvisibleTextBoxViewExtension.lastSyncedValue;
					const changed = valueChanged
						|| text !== BrowserInvisibleTextBoxViewExtension.reportedText
						|| start !== BrowserInvisibleTextBoxViewExtension.compositionStart;
					BrowserInvisibleTextBoxViewExtension.compositionStart = start;
					if (changed) {
						BrowserInvisibleTextBoxViewExtension.reportedText = text;
						BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionUpdated(text, start, valueChanged);
					}
				}
				// A handler may have moved focus, replacing the input or handing it to another TextBox; its
				// value is not that TextBox's.
				if (!BrowserInvisibleTextBoxViewExtension.isCurrentInput(input, handle)) {
					return;
				}
			}
			BrowserInvisibleTextBoxViewExtension.lastSyncedValue = input.value;
			if (input.selectionDirection == "backward") {
				BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged(input.value, input.selectionEnd, input.selectionStart - input.selectionEnd);
			} else {
				BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged(input.value, input.selectionStart, input.selectionEnd - input.selectionStart);
			}
			// The input event a compositionend was delivered ahead of (see there) has put the committed
			// text in place (or restored the text, for a cancel) and it is reported; the composition can complete.
			const pending = BrowserInvisibleTextBoxViewExtension.pendingCommit;
			if (pending !== null && BrowserInvisibleTextBoxViewExtension.isComposingOn(input, handle)
				&& (pending.length === 0 || BrowserInvisibleTextBoxViewExtension.isCommitInPlace(input, pending, BrowserInvisibleTextBoxViewExtension.compositionStart))) {
				BrowserInvisibleTextBoxViewExtension.completeComposition(pending);
			}
		}

		// Locates the preedit inside the input's value. The IME may have opened the composition on
		// text that was already in the input (e.g. Gboard extending a committed word), so the
		// preedit can start before the caret position the composition started at.
		private static findCompositionStart(input: HTMLInputElement | HTMLTextAreaElement, text: string): number {
			const base = BrowserInvisibleTextBoxViewExtension.compositionBaseValue;
			const value = input.value;
			const caret = input.selectionEnd ?? value.length;
			const previous = BrowserInvisibleTextBoxViewExtension.compositionStart;

			// An emptied preedit stays where it was.
			if (text.length === 0 && previous >= 0 && previous <= value.length) {
				return previous;
			}

			const maxCommon = Math.min(base.length, value.length);
			let prefix = 0;
			while (prefix < maxCommon && base[prefix] === value[prefix]) {
				prefix++;
			}
			let suffix = 0;
			while (suffix < maxCommon - prefix && base[base.length - 1 - suffix] === value[value.length - 1 - suffix]) {
				suffix++;
			}

			// Before it ever had text, an emptied preedit sits where the edit was or, with nothing edited
			// yet, at the caret.
			if (text.length === 0) {
				return value === base ? Math.min(caret, value.length) : prefix;
			}

			// A start is plausible when the value is the base with the preedit in place of some of its
			// text at that position. Several starts can be when the preedit repeats adjacent text: the
			// start found for the previous update is kept while it still fits (the IME may have moved
			// the caret inside the preedit since), then the caret decides, as the IME leaves it at the
			// end of a preedit it just inserted, then the edit.
			const isPlausibleStart = (start: number) =>
				start >= 0
				&& start + text.length <= value.length
				&& value.startsWith(text, start)
				&& base.startsWith(value.slice(0, start))
				&& base.endsWith(value.slice(start + text.length));
			const editEnd = value.length - suffix;
			for (const start of [previous, caret - text.length, editEnd - text.length, prefix]) {
				if (isPlausibleStart(start)) {
					return start;
				}
			}

			const beforeCaret = value.lastIndexOf(text, caret);
			return beforeCaret >= 0 ? beforeCaret : Math.max(0, Math.min(prefix, value.length - text.length));
		}

		// The committed text has replaced the preedit once the value is the value the composition started
		// from with the committed text in place of some of it there; a commit that merely prefixes a
		// preedit still in the input has not landed.
		private static isCommitInPlace(input: HTMLInputElement | HTMLTextAreaElement, committed: string, start: number): boolean {
			const base = BrowserInvisibleTextBoxViewExtension.compositionBaseValue;
			const value = input.value;
			return start >= 0
				&& value.startsWith(committed, start)
				&& base.startsWith(value.slice(0, start))
				&& base.endsWith(value.slice(start + committed.length));
		}

		// Applies the same keydown/keyup guards used on the invisible <input> to any text input
		// that must delegate character insertion to managed TextBox KeyDown handling.
		// Without these guards, focused text inputs (e.g. the a11y semantic <input>) would insert
		// the character natively AND via the managed path, producing duplicated input.
		public static attachTextInputKeyHandlers(input: HTMLInputElement | HTMLTextAreaElement, acceptsReturn: boolean) {
			input.addEventListener("keydown", (ev: KeyboardEvent) => {
				// During IME composition, let the browser/IME handle all keys.
				// stopPropagation prevents BrowserKeyboardInputSource from calling preventDefault.
				if (ev.isComposing) {
					ev.stopPropagation();
					return;
				}

				if (ev.ctrlKey || (ev.metaKey && BrowserInvisibleTextBoxViewExtension.isMacOS)) {
					// Due to browser security considerations, we need to let the clipboard operations be handled natively.
					// So, we do stopPropagation instead of preventDefault
					if (ev.key == "c" || ev.key == "C" || ev.key == "v" || ev.key == "V" || ev.key == "x" || ev.key == "X") {
						ev.stopPropagation();
						return;
					}
				}

				// Allow Enter key to propagate when the TextBox doesn't accept returns
				// Desktop/iOS path: keydown is the reliable signal; let it bubble to document so
				// BrowserKeyboardInputSource raises the managed KeyDown. The flag prevents the
				// keyup branch below from dispatching a duplicate OnEnterKeyPressed.
				// This enables focus navigation (e.g., Uno.Toolkit's AutoFocusNext) on mobile browsers
				if ((ev.key === "Enter" || ev.keyCode === 13) && !acceptsReturn) {
					// Don't call preventDefault() to allow the key event to propagate to document listeners
					BrowserInvisibleTextBoxViewExtension.enterHandledByKeyDown = true;
					return;
				}

				// Let the browser handle soft keyboard keys natively; text changes sync via the
				// oninput handler. stopPropagation prevents the document-level
				// BrowserKeyboardInputSource from calling preventDefault() on the event.
				if (BrowserInvisibleTextBoxViewExtension.isSoftKeyboardKey(ev)) {
					ev.stopPropagation();
					return;
				}

				ev.preventDefault();
			});

			input.addEventListener("keyup", (ev: KeyboardEvent) => {
				// Android virtual keyboards (Gboard/SwiftKey/Samsung/AOSP) report keydown
				// with keyCode 229 ("Unidentified") for Enter, which is stopPropagation'd
				// above so it never reaches BrowserKeyboardInputSource. They DO report keyup
				// with key === "Enter" though - use that to raise the managed KeyDown so
				// focus-navigation patterns (Uno.Toolkit AutoFocusNext, FocusManager) work
				// on Android browsers. The flag guards against double-dispatch on desktop/iOS,
				// where the keydown branch already routed Enter through the document listener.
				if (!acceptsReturn
					&& ev.key === "Enter"
					&& !BrowserInvisibleTextBoxViewExtension.enterHandledByKeyDown
					&& !ev.isComposing) {
					ev.preventDefault();
					BrowserInvisibleTextBoxViewExtension._exports.OnEnterKeyPressed();
				}

				if (ev.key === "Enter" || ev.keyCode === 13) {
					BrowserInvisibleTextBoxViewExtension.enterHandledByKeyDown = false;
				}

				if (BrowserInvisibleTextBoxViewExtension.isComposing || BrowserInvisibleTextBoxViewExtension.isSoftKeyboardKey(ev)) {
					ev.stopPropagation();
				}
			});
		}

		public static setEnterKeyHint(enterKeyHint: string) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input) {
				input.setAttribute("enterkeyhint", enterKeyHint);
			}
		}

		public static setReadOnly(isReadOnly: boolean) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input) {
				input.readOnly = isReadOnly;
			}
		}

		public static setInputMode(inputMode: string) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input) {
				input.setAttribute("inputmode", inputMode);
			}
		}

		public static focus(handle: number, isPassword: boolean, text: string, acceptsReturn: boolean, inputMode: string, enterKeyHint: string, isReadOnly: boolean): boolean {
			// Supersede any detach a preceding managed blur scheduled: focus is moving between
			// TextBoxes, and detaching in between would dismiss the soft keyboard (see blur).
			BrowserInvisibleTextBoxViewExtension.detachGeneration++;

			const semanticElement = document.getElementById(`uno-semantics-${handle}`);
			if (semanticElement && document.activeElement === semanticElement) {
				BrowserInvisibleTextBoxViewExtension.detach();
				return false;
			}

			const existingInput = BrowserInvisibleTextBoxViewExtension.inputElement;
			const wasComposing = BrowserInvisibleTextBoxViewExtension.isComposing;
			if (existingInput != null && BrowserInvisibleTextBoxViewExtension.canRetarget(existingInput, isPassword, acceptsReturn)) {
				// Reuse the shared input in place: mobile browsers keep the soft keyboard up across
				// a TextBox-to-TextBox move only while an editable element stays focused throughout.
				BrowserInvisibleTextBoxViewExtension.acceptsReturn = acceptsReturn;
				existingInput.setAttribute("inputmode", inputMode);
				existingInput.setAttribute("enterkeyhint", enterKeyHint);
				existingInput.readOnly = isReadOnly;
				BrowserInvisibleTextBoxViewExtension.setText(text);
				BrowserInvisibleTextBoxViewExtension.lastSyncedValue = existingInput.value;

				// It's necessary to actually focus the native input, not just make it visible. This is particularly
				// important to mobile browsers (to open the software keyboard) and for assistive technology to not steal
				// events and properly recognize password inputs to not read it.
				if (document.activeElement !== existingInput) {
					existingInput.focus(BrowserInvisibleTextBoxViewExtension.focusOptions);
				}
			}
			else {
				// The element kind must change (input/textarea/password), or an IME composition is in
				// progress and must not leak into the next TextBox. Focus the new element BEFORE
				// removing the old one so focus hands off editable-to-editable without a gap that
				// would dismiss the soft keyboard. The implicit blur of the old element is
				// managed-initiated, so suppress its notification.
				existingInput?.removeAttribute("id");
				this.createInput(isPassword, text, acceptsReturn, inputMode, enterKeyHint, isReadOnly);
				BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(() => {
					BrowserInvisibleTextBoxViewExtension.inputElement.focus(BrowserInvisibleTextBoxViewExtension.focusOptions);
					existingInput?.remove();
				});
			}

			BrowserInvisibleTextBoxViewExtension.currentHandle = Number(handle);

			// The replaced element's compositionend is ignored (see isCurrentInput), so a composition
			// that was still open is ended for the TextBox here: the same TextBox re-entering when a
			// tap moves the caret would otherwise never see it end.
			if (wasComposing) {
				BrowserInvisibleTextBoxViewExtension.endComposition();
			}

			// Set for whichever element ends up live: the shared input is reused across TextBoxes, so tagging
			// it only on creation leaves the policy unreported for every retargeted entry session. Inspectable
			// from the Web Inspector when diagnosing keyboard or IME placement reports.
			BrowserInvisibleTextBoxViewExtension.inputElement.dataset.unoPlacement =
				BrowserInvisibleTextBoxViewExtension.keepsInputOffscreen ? "offscreen" : "tracking";

			// The retarget path keeps the input focused, so no focusin fires and the trailing-click
			// guard never arms; arm it here for both paths (see installTrailingClickGuard).
			if (BrowserInvisibleTextBoxViewExtension.lastPointerType === "touch"
				|| BrowserInvisibleTextBoxViewExtension.lastPointerType === "pen") {
				BrowserInvisibleTextBoxViewExtension.swallowNextCanvasClick = true;
			}
			return true;
		}

		// The shared input can be handed to another TextBox without being recreated only when the
		// element kind it was created with still matches the target TextBox.
		private static canRetarget(input: HTMLInputElement | HTMLTextAreaElement, isPassword: boolean, acceptsReturn: boolean): boolean {
			if (BrowserInvisibleTextBoxViewExtension.isComposing) {
				return false;
			}
			const needsTextArea = acceptsReturn && !isPassword;
			if (input instanceof HTMLTextAreaElement) {
				return needsTextArea;
			}
			return !needsTextArea
				&& (input.type === "password") === isPassword
				&& (input as any).__unoAcceptsReturn === acceptsReturn;
		}

		// Runs a managed-initiated focus mutation with the blur listener muted, so it isn't
		// reported back to the FocusManager (which already drove the change). Callers make the
		// blur dispatch synchronously inside the window: detachCore blurs explicitly before
		// removing, and the swap path in focus() focuses the successor (implicitly blurring the
		// old input) before removing it.
		private static runSuppressingBlur(action: () => void) {
			BrowserInvisibleTextBoxViewExtension.suppressBlurNotification = true;
			try {
				action();
			} finally {
				BrowserInvisibleTextBoxViewExtension.suppressBlurNotification = false;
			}
		}

		private static detachCore() {
			BrowserInvisibleTextBoxViewExtension.detachGeneration++;
			BrowserInvisibleTextBoxViewExtension.currentHandle = 0;
			// Blur explicitly before removing: the .blur() method dispatches synchronously, so it
			// lands inside the suppression window. WebKit can otherwise defer the implicit blur that
			// fires on element removal past that window, which would clear the wrong TextBox's focus.
			BrowserInvisibleTextBoxViewExtension.inputElement?.blur();
			BrowserInvisibleTextBoxViewExtension.inputElement?.remove();
			BrowserInvisibleTextBoxViewExtension.inputElement = null;
			// The removed element's compositionend is ignored (see isCurrentInput), so a composition
			// still open is ended for the TextBox here; when the TextBox keeps focus (accessibility
			// handing it over to its semantic element) nothing else would.
			if (BrowserInvisibleTextBoxViewExtension.isComposing) {
				BrowserInvisibleTextBoxViewExtension.endComposition();
			}
		}

		public static blur(handle: number) {
			// Managed-initiated blur (EndEntry): the FocusManager already knows focus is leaving.
			const blurredHandle = Number(handle);
			if (blurredHandle !== 0
				&& BrowserInvisibleTextBoxViewExtension.currentHandle !== 0
				&& blurredHandle !== BrowserInvisibleTextBoxViewExtension.currentHandle) {
				// Stale blur from a TextBox that no longer owns the shared input; detaching now
				// would tear down its successor's entry session.
				return;
			}

			// Don't detach synchronously: when focus is moving to another TextBox, EndEntry runs
			// before StartEntry in the same task, and detaching in between commits the soft-keyboard
			// dismissal on iOS even though another TextBox is about to take over. Defer by one
			// microtask; an intervening focus()/detach() supersedes this via detachGeneration.
			const generation = ++BrowserInvisibleTextBoxViewExtension.detachGeneration;
			queueMicrotask(() => {
				if (generation === BrowserInvisibleTextBoxViewExtension.detachGeneration) {
					BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(BrowserInvisibleTextBoxViewExtension.detachCore);
				}
			});
		}

		public static detach() {
			BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(BrowserInvisibleTextBoxViewExtension.detachCore);
		}

		public static hasInput(): boolean {
			return BrowserInvisibleTextBoxViewExtension.inputElement != null;
		}

		public static setText(text: string) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input != null) {
				// The value being reported echoes back here unchanged and is a no-op. Any other text
				// (the TextBox rejecting or coercing the input, the delete button, Text set from code)
				// does have to reach the input, and ends the composition like it would in a native app.
				const inputText = BrowserInvisibleTextBoxViewExtension.toInputText(text);
				if (input.value != inputText) {
					// Replacing the value moves the caret to the end. Put it back right away rather than on the
					// selectionchange the browser fires later: by then the IME may already be composing the next
					// word, and a stale caret applied under it lands that word in the wrong place.
					const { selectionStart, selectionEnd, selectionDirection } = input;
					input.value = inputText;
					BrowserInvisibleTextBoxViewExtension.lastSyncedValue = input.value;
					input.setSelectionRange(selectionStart, selectionEnd, selectionDirection ?? "none");

					// Notified last: a handler may set the text again, and its write must not be overwritten.
					if (BrowserInvisibleTextBoxViewExtension.isComposing) {
						BrowserInvisibleTextBoxViewExtension.endComposition();
					}
				}
			}
		}

		public static updateSize(width: number, height: number) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			// Sized like the TextBox only where the input is positioned over it: a multi-line TextBox reports
			// its full content height, which would stretch an off-screen input back into the viewport.
			if (input != null && !BrowserInvisibleTextBoxViewExtension.keepsInputOffscreen) {
				input.style.width = `${width}px`;
				input.style.height = `${height}px`;
			}
		}

		public static updatePosition(x: number, y: number) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input != null && !BrowserInvisibleTextBoxViewExtension.keepsInputOffscreen) {
				input.style.top = `${Math.round(y)}px`;
				input.style.left = `${Math.round(x)}px`;
			}
		}

		public static updateSelection(start: number, length: number, direction: "forward" | "backward") {
			if (!BrowserInvisibleTextBoxViewExtension.isInSelectionChange) {
				const input = BrowserInvisibleTextBoxViewExtension.inputElement;
				if (input == null) {
					return;
				}

				// The selection an input event reported echoes back here and already matches the input;
				// re-applying it would only notify the IME for nothing. Anything else (a tap, Select from
				// code, even while composing) has to reach the input, where the browser then finishes
				// the composition and typing continues at the new caret.
				const end = start + length;
				if (input.selectionStart === start && input.selectionEnd === end && (length === 0 || input.selectionDirection === direction)) {
					return;
				}

				input.setSelectionRange(start, end, direction);
			}
		}

		private static resetComposition() {
			BrowserInvisibleTextBoxViewExtension.isComposing = false;
			BrowserInvisibleTextBoxViewExtension.pendingCommit = null;
			BrowserInvisibleTextBoxViewExtension.preeditRemoved = false;
		}

		// The committed text (none for a cancel) is in place and reported; the composition is over.
		private static completeComposition(committed: string) {
			BrowserInvisibleTextBoxViewExtension.resetComposition();
			if (committed.length > 0) {
				BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionCompleted(committed);
			} else {
				BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionEnded();
			}
		}

		// The composition is over without the IME committing it: a text change from managed code (the
		// browser drops its composition state when the value is replaced, without a compositionend),
		// the input being replaced or removed, or a commit the browser never applied.
		private static endComposition() {
			BrowserInvisibleTextBoxViewExtension.resetComposition();
			BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionEnded();
		}
	}
}
