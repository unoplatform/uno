namespace Uno.UI.Runtime.Skia {
	export class BrowserInvisibleTextBoxViewExtension {
		private static _exports: any;
		private static _imeExports: any;
		private static readonly isMacOS = navigator?.platform.toUpperCase().includes('MAC') ?? false;
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
		private static isComposing: boolean;
		private static compositionStart: number = 0;
		private static compositionInput: HTMLInputElement | HTMLTextAreaElement | null;
		private static compositionHandle: number = 0;
		private static enterHandledByKeyDown: boolean;
		private static compositionGeneration: number = 0;
		private static inputOrigin: { input: HTMLInputElement | HTMLTextAreaElement, handle: number, isComposition: boolean } | null;
		private static readonly managedInputStates = new WeakMap<HTMLInputElement | HTMLTextAreaElement, {
			handle: number, revision: number, text?: string, start?: number, end?: number, backward?: boolean
		}>();

		private static waitingAsyncOnSelectionChange: boolean;
		private static nextSelectionStart: number;
		private static nextSelectionEnd: number;
		private static nextSelectionDirection: "forward" | "backward" | "none";

		// Android soft keyboards report all key events with keyCode 229 ("Unidentified").
		// Text changes are synced via the oninput handler instead.
		private static readonly ANDROID_IME_KEYCODE = 229;

		public static getNativePasteSourceLimit(handle: number): number {
			BrowserInvisibleTextBoxViewExtension.initialize();
			return BrowserInvisibleTextBoxViewExtension._exports.GetNativePasteSourceLimit(handle);
		}

		public static onNativePaste(handle: number, source: string): void {
			BrowserInvisibleTextBoxViewExtension.initialize();
			BrowserInvisibleTextBoxViewExtension._exports.OnNativePaste(handle, source);
		}

		public static initialize() {
			if (BrowserInvisibleTextBoxViewExtension._exports == undefined) {
				const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();

				BrowserInvisibleTextBoxViewExtension._exports = browserExports.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension;
				BrowserInvisibleTextBoxViewExtension._imeExports = browserExports.Uno.UI.Runtime.Skia.WasmImeTextBoxExtension;

				BrowserInvisibleTextBoxViewExtension.installTrailingClickGuard();

				document.onselectionchange = () => {
					const input = BrowserInvisibleTextBoxViewExtension.inputElement;
					if (document.activeElement === input
						&& input != null
						&& !BrowserInvisibleTextBoxViewExtension.isComposing) {
						if (BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange) {
							BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange = false;
							input.setSelectionRange(BrowserInvisibleTextBoxViewExtension.nextSelectionStart, BrowserInvisibleTextBoxViewExtension.nextSelectionEnd, BrowserInvisibleTextBoxViewExtension.nextSelectionDirection);
						}
						else {
							if (input.selectionDirection == "backward") {
								BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(BrowserInvisibleTextBoxViewExtension.currentHandle, input.selectionEnd, input.selectionStart - input.selectionEnd);
							} else {
								BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(BrowserInvisibleTextBoxViewExtension.currentHandle, input.selectionStart, input.selectionEnd - input.selectionStart);
							}
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

		private static createInput(isPasswordBox: boolean, text: string, acceptsReturn: boolean, isRichEditBox: boolean, inputMode: string, enterKeyHint: string) {
			// A previous input may have been removed mid-composition without a compositionend;
			// never carry that state over to a fresh element.
			BrowserInvisibleTextBoxViewExtension.isComposing = false;
			const input = document.createElement((acceptsReturn || isRichEditBox) && !isPasswordBox ? "textarea" : "input");
			if (isPasswordBox) {
				(input as HTMLInputElement).type = "password";
				input.autocomplete = "password";
			}

			input.id = UnoDomIds.input;
			input.dataset.unoAcceptsReturn = acceptsReturn ? "true" : "false";
			input.tabIndex = -1;
			input.spellcheck = false;
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
			input.style.top = "0px";
			input.style.left = "0px";
			input.value = text;

			input.setAttribute("inputmode", inputMode);
			input.setAttribute("enterkeyhint", enterKeyHint);

			BrowserInvisibleTextBoxViewExtension.attachTextInputEvents(input, () => {
				BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange = false;
				if (input.selectionDirection == "backward") {
					BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged(BrowserInvisibleTextBoxViewExtension.currentHandle, input.value, input.selectionEnd, input.selectionStart - input.selectionEnd);
				} else {
					BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged(BrowserInvisibleTextBoxViewExtension.currentHandle, input.value, input.selectionStart, input.selectionEnd - input.selectionStart);
				}
			});

			// C# drives focus one-way (StartEntry/EndEntry call focus()/blur()), so a blur the
			// browser initiates on its own — e.g. tapping outside, or the touch-synthesized
			// mousedown in issue 1 — is otherwise invisible to the FocusManager and LostFocus
			// never fires. Report only those; managed-initiated blurs set suppressBlurNotification.
			input.addEventListener("blur", (ev: FocusEvent) => {
				if (input !== BrowserInvisibleTextBoxViewExtension.inputElement
					|| BrowserInvisibleTextBoxViewExtension.suppressBlurNotification) {
					return;
				}
				const handle = BrowserInvisibleTextBoxViewExtension.currentHandle;
				if ((ev.relatedTarget as HTMLElement)?.id === `uno-semantics-${handle}`) {
					BrowserInvisibleTextBoxViewExtension.invalidateComposition();
					BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionEnded(handle);
					return;
				}
				BrowserInvisibleTextBoxViewExtension._exports.OnNativeBlur(handle);
			});

			BrowserInvisibleTextBoxViewExtension.attachTextInputKeyHandlers(input, acceptsReturn);

			document.body.appendChild(input);
			BrowserInvisibleTextBoxViewExtension.inputElement = input;
		}

		private static getInputHandle(input: HTMLInputElement | HTMLTextAreaElement): number {
			return input === BrowserInvisibleTextBoxViewExtension.inputElement
				? BrowserInvisibleTextBoxViewExtension.currentHandle
				: Number(input.id.substring("uno-semantics-".length));
		}

		private static ownsInput(input: HTMLInputElement | HTMLTextAreaElement, handle: number): boolean {
			return input.isConnected
				&& (input === BrowserInvisibleTextBoxViewExtension.inputElement
					? BrowserInvisibleTextBoxViewExtension.currentHandle === handle
					: input.id === `uno-semantics-${handle}` && document.getElementById(input.id) === input);
		}

		private static isActiveInput(input: HTMLInputElement | HTMLTextAreaElement, handle: number): boolean {
			return document.activeElement === input && BrowserInvisibleTextBoxViewExtension.ownsInput(input, handle);
		}

		public static isComposingInput(input: HTMLInputElement | HTMLTextAreaElement): boolean {
			return BrowserInvisibleTextBoxViewExtension.isComposing && BrowserInvisibleTextBoxViewExtension.compositionInput === input;
		}

		public static isManagedBlur(): boolean {
			return BrowserInvisibleTextBoxViewExtension.suppressBlurNotification;
		}

		private static synchronizeNativeClipboardOwner(input: HTMLInputElement | HTMLTextAreaElement) {
			if (input === BrowserInvisibleTextBoxViewExtension.inputElement && document.activeElement === input) {
				BrowserInvisibleTextBoxViewExtension._exports.SynchronizeNativeClipboardOwner(BrowserInvisibleTextBoxViewExtension.currentHandle);
			}
		}

		// Compare published managed state, not DOM state that may already contain an unreported edit.
		public static recordManagedInputState(
			input: HTMLInputElement | HTMLTextAreaElement,
			text?: string,
			start?: number,
			end?: number,
			backward?: boolean) {
			const handle = BrowserInvisibleTextBoxViewExtension.getInputHandle(input);
			let state = BrowserInvisibleTextBoxViewExtension.managedInputStates.get(input);
			if (state == null) {
				state = { handle, revision: 0 };
				BrowserInvisibleTextBoxViewExtension.managedInputStates.set(input, state);
			}
			const hasSelection = start !== undefined && end !== undefined && start >= 0 && end >= 0;
			const isBackward = hasSelection && start !== end && backward === true;
			const changed = (text !== undefined && text !== state.text)
				|| (hasSelection && (start !== state.start || end !== state.end || isBackward !== state.backward));
			const origin = BrowserInvisibleTextBoxViewExtension.inputOrigin;
			if (handle !== state.handle
				|| (changed && (!origin?.isComposition || origin.input !== input || origin.handle !== handle))) {
				state.revision++;
			}
			state.handle = handle;
			if (text !== undefined) {
				state.text = text;
			}
			if (hasSelection) {
				state.start = start;
				state.end = end;
				state.backward = isBackward;
			}
		}

		private static getManagedInputRevision(input: HTMLInputElement | HTMLTextAreaElement): number {
			return BrowserInvisibleTextBoxViewExtension.managedInputStates.get(input)?.revision ?? 0;
		}

		private static runInputCallback(input: HTMLInputElement | HTMLTextAreaElement, handle: number, callback: () => void, isComposition: boolean = false) {
			const previousOrigin = BrowserInvisibleTextBoxViewExtension.inputOrigin;
			BrowserInvisibleTextBoxViewExtension.inputOrigin = { input, handle, isComposition };
			try {
				callback();
			} finally {
				try {
					// RenderDocument can run synchronously, including app edits in TextChanging.
					// Reconcile the final document, not its incremental echo into the already-edited DOM.
					if (BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
						BrowserInvisibleTextBoxViewExtension._exports.SynchronizeTextInput(handle);
					}
				} finally {
					BrowserInvisibleTextBoxViewExtension.inputOrigin = previousOrigin;
				}
			}
		}

		public static attachTextInputEvents(input: HTMLInputElement | HTMLTextAreaElement, onInput: () => void) {
			BrowserInvisibleTextBoxViewExtension.initialize();
			const usesNativeComposition = input instanceof HTMLInputElement && input.type === "password";
			let activeCompositionGeneration = -1;
			let pendingCompositionInput: { handle: number, revision: number, data: string, nativeValue: string, acceptedValue: string } | null = null;

			input.addEventListener("input", (ev: InputEvent) => {
				const handle = BrowserInvisibleTextBoxViewExtension.getInputHandle(input);
				if (!BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
					return;
				}
				if (BrowserInvisibleTextBoxViewExtension.isComposingInput(input)) {
					return;
				}
				const isCompositionEcho = pendingCompositionInput != null
					&& pendingCompositionInput.handle === handle
					&& pendingCompositionInput.revision === BrowserInvisibleTextBoxViewExtension.getManagedInputRevision(input)
					&& ev.data === pendingCompositionInput.data
					&& (input.value === pendingCompositionInput.nativeValue || input.value === pendingCompositionInput.acceptedValue);
				pendingCompositionInput = null;
				BrowserInvisibleTextBoxViewExtension.runInputCallback(input, handle, () => {
					if (!isCompositionEcho && (usesNativeComposition
						|| (!ev.isComposing && ev.inputType !== "insertCompositionText" && ev.inputType !== "insertFromComposition"))) {
						onInput();
					}
				});
			});

			input.addEventListener("paste", (ev: ClipboardEvent) => {
				const handle = BrowserInvisibleTextBoxViewExtension.getInputHandle(input);
				if (!BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
					return;
				}
				ev.preventDefault();
				const source = ev.clipboardData?.getData("text") ?? "";
				BrowserInvisibleTextBoxViewExtension.runInputCallback(input, handle, () => {
					BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(
						handle,
						input.selectionDirection === "backward" ? input.selectionEnd : input.selectionStart,
						(input.selectionEnd - input.selectionStart) * (input.selectionDirection === "backward" ? -1 : 1));
					const sourceLimit = BrowserInvisibleTextBoxViewExtension.getNativePasteSourceLimit(handle);
					BrowserInvisibleTextBoxViewExtension.onNativePaste(
						handle, source.length > sourceLimit ? source.substring(0, sourceLimit) : source);
				});
			});

			const onClipboard = (ev: ClipboardEvent) => {
				BrowserInvisibleTextBoxViewExtension.synchronizeNativeClipboardOwner(input);
				try {
					const handle = BrowserInvisibleTextBoxViewExtension.getInputHandle(input);
					if (!BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)
						|| !BrowserInvisibleTextBoxViewExtension._exports.HandlesNativeClipboard(handle)) {
						return;
					}
					ev.preventDefault();
					const clipboardData = ev.clipboardData;
					if (!clipboardData) {
						return;
					}
					BrowserInvisibleTextBoxViewExtension.runInputCallback(input, handle, () => {
						BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(
							handle,
							input.selectionDirection === "backward" ? input.selectionEnd : input.selectionStart,
							(input.selectionEnd - input.selectionStart) * (input.selectionDirection === "backward" ? -1 : 1));
						const operation: [string, string, string | null] | null = BrowserInvisibleTextBoxViewExtension._exports.PrepareNativeClipboard(handle, ev.type === "cut");
						if (operation == null) {
							return;
						}
						const token = Number(operation[0]);
						const text = operation[1];
						const rtf = operation[2];
						let clipboardWritten = false;
						try {
							if (!BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
								return;
							}
							clipboardData.setData("text/plain", text);
							if (clipboardData.getData("text/plain") !== text) {
								return;
							}
							if (rtf !== null) {
								clipboardData.setData("text/rtf", rtf);
								if (!clipboardData.types.includes("text/rtf") || clipboardData.getData("text/rtf") !== rtf) {
									return;
								}
							}
							clipboardWritten = true;
						} finally {
							// A cut is destructive only after every payload was accepted by the event.
							BrowserInvisibleTextBoxViewExtension._exports.CompleteNativeClipboard(
								handle, token, clipboardWritten && BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle));
						}
					});
				} finally {
					BrowserInvisibleTextBoxViewExtension.synchronizeNativeClipboardOwner(input);
				}
			};
			input.addEventListener("copy", onClipboard);
			input.addEventListener("cut", onClipboard);

			// Handle Enter key from Android virtual keyboards which don't fire keydown events.
			// Android keyboards typically fire beforeinput with inputType "insertLineBreak" or "insertParagraph" instead.
			input.addEventListener("beforeinput", (ev: InputEvent) => {
				if ((ev.inputType === "insertLineBreak" || ev.inputType === "insertParagraph") && input.dataset.unoAcceptsReturn === "false") {
					ev.preventDefault();

					BrowserInvisibleTextBoxViewExtension._exports.OnEnterKeyPressed(BrowserInvisibleTextBoxViewExtension.getInputHandle(input));
				}
			});

			input.addEventListener("compositionstart", () => {
				const handle = BrowserInvisibleTextBoxViewExtension.getInputHandle(input);
				if (!BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
					return;
				}
				pendingCompositionInput = null;
				// PasswordBox deliberately has no managed composition session; its value still
				// follows native input, without publishing password text as IME event payloads.
				if (usesNativeComposition) {
					return;
				}
				BrowserInvisibleTextBoxViewExtension.runInputCallback(input, handle, () => {
					BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(
						handle,
						input.selectionDirection === "backward" ? input.selectionEnd : input.selectionStart,
						(input.selectionEnd - input.selectionStart) * (input.selectionDirection === "backward" ? -1 : 1));
					if (!BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
						return;
					}
					activeCompositionGeneration = ++BrowserInvisibleTextBoxViewExtension.compositionGeneration;
					BrowserInvisibleTextBoxViewExtension.isComposing = true;
					BrowserInvisibleTextBoxViewExtension.compositionInput = input;
					BrowserInvisibleTextBoxViewExtension.compositionHandle = handle;
					BrowserInvisibleTextBoxViewExtension.compositionStart = input.selectionStart ?? 0;
					BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionStarted(handle);
				}, true);
			});

			input.addEventListener("compositionupdate", (ev: CompositionEvent) => {
				if (usesNativeComposition) {
					return;
				}
				const handle = BrowserInvisibleTextBoxViewExtension.getInputHandle(input);
				if (!BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
					return;
				}
				BrowserInvisibleTextBoxViewExtension.runInputCallback(input, handle, () => {
					if (activeCompositionGeneration !== BrowserInvisibleTextBoxViewExtension.compositionGeneration
						|| !BrowserInvisibleTextBoxViewExtension.isComposingInput(input)) {
						return;
					}
					const selectionStart = input.selectionStart;
					const cursorPosition = selectionStart === null
						? ev.data.length
						: Math.max(0, Math.min(selectionStart - BrowserInvisibleTextBoxViewExtension.compositionStart, ev.data.length));
					BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionUpdated(handle, ev.data, cursorPosition);
				}, true);
			});

			input.addEventListener("compositionend", (ev: CompositionEvent) => {
				const handle = BrowserInvisibleTextBoxViewExtension.getInputHandle(input);
				if (!BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
					return;
				}
				const pending = pendingCompositionInput = {
					handle, revision: 0, data: ev.data, nativeValue: input.value, acceptedValue: input.value
				};
				// A final input may arrive in a later task. Match its value as well as data,
				// but expire that correlation when a later managed text/selection change supersedes it.
				try {
					BrowserInvisibleTextBoxViewExtension.runInputCallback(input, handle, () => {
						if (usesNativeComposition) {
							onInput();
							return;
						}
						if (activeCompositionGeneration !== BrowserInvisibleTextBoxViewExtension.compositionGeneration
							|| !BrowserInvisibleTextBoxViewExtension.isComposingInput(input)) {
							return;
						}
						BrowserInvisibleTextBoxViewExtension.isComposing = false;
						if (ev.data.length > 0) {
							BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionCompleted(handle, ev.data);
						} else {
							BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionCanceled(handle);
						}
					}, true);
				} finally {
					if (pendingCompositionInput === pending) {
						pending.acceptedValue = input.value;
						pending.revision = BrowserInvisibleTextBoxViewExtension.getManagedInputRevision(input);
					}
				}
			});
		}

		// Applies the same keydown/keyup guards used on the invisible <input> to any text input
		// that must delegate character insertion to managed TextBox KeyDown handling.
		// Without these guards, focused text inputs (e.g. the a11y semantic <input>) would insert
		// the character natively AND via the managed path, producing duplicated input.
		public static attachTextInputKeyHandlers(input: HTMLInputElement | HTMLTextAreaElement, acceptsReturn: boolean) {
			input.addEventListener("keydown", (ev: KeyboardEvent) => {
				const acceptsReturnNow = input.dataset.unoAcceptsReturn === undefined
					? acceptsReturn
					: input.dataset.unoAcceptsReturn === "true";

				// During IME composition, let the browser/IME handle all keys.
				// stopPropagation prevents BrowserKeyboardInputSource from calling preventDefault.
				if (ev.isComposing) {
					ev.stopPropagation();
					return;
				}

				if (!ev.altKey && (ev.ctrlKey || (ev.metaKey && BrowserInvisibleTextBoxViewExtension.isMacOS))) {
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
				if ((ev.key === "Enter" || ev.keyCode === 13) && !acceptsReturnNow) {
					// Don't call preventDefault() to allow the key event to propagate to document listeners
					BrowserInvisibleTextBoxViewExtension.enterHandledByKeyDown = true;
					return;
				}

				// Android soft keyboards fire all keys as keyCode 229 / key "Unidentified".
				// The C# side cannot identify these (maps to VirtualKey.None), so let the browser
				// handle them natively. Text changes sync via the oninput handler.
				// stopPropagation prevents the document-level BrowserKeyboardInputSource from
				// calling preventDefault() on the event.
				if (ev.keyCode === BrowserInvisibleTextBoxViewExtension.ANDROID_IME_KEYCODE) {
					ev.stopPropagation();
					return;
				}

				ev.preventDefault();
			});

			input.addEventListener("keyup", (ev: KeyboardEvent) => {
				const acceptsReturnNow = input.dataset.unoAcceptsReturn === undefined
					? acceptsReturn
					: input.dataset.unoAcceptsReturn === "true";

				// Android virtual keyboards (Gboard/SwiftKey/Samsung/AOSP) report keydown
				// with keyCode 229 ("Unidentified") for Enter, which is stopPropagation'd
				// above so it never reaches BrowserKeyboardInputSource. They DO report keyup
				// with key === "Enter" though - use that to raise the managed KeyDown so
				// focus-navigation patterns (Uno.Toolkit AutoFocusNext, FocusManager) work
				// on Android browsers. The flag guards against double-dispatch on desktop/iOS,
				// where the keydown branch already routed Enter through the document listener.
				if (!acceptsReturnNow
					&& ev.key === "Enter"
					&& !BrowserInvisibleTextBoxViewExtension.enterHandledByKeyDown
					&& !ev.isComposing) {
					ev.preventDefault();
					BrowserInvisibleTextBoxViewExtension._exports.OnEnterKeyPressed(BrowserInvisibleTextBoxViewExtension.getInputHandle(input));
				}

				if (ev.key === "Enter" || ev.keyCode === 13) {
					BrowserInvisibleTextBoxViewExtension.enterHandledByKeyDown = false;
				}

				if (BrowserInvisibleTextBoxViewExtension.isComposing || ev.keyCode === BrowserInvisibleTextBoxViewExtension.ANDROID_IME_KEYCODE) {
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

		public static setInputMode(inputMode: string) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input) {
				input.setAttribute("inputmode", inputMode);
			}
		}

		public static setTextPredictionEnabled(enabled: boolean) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input) {
				input.autocomplete = enabled ? "on" : "off";
				input.setAttribute("autocorrect", enabled ? "on" : "off");
			}
		}

		public static setSpellCheckEnabled(enabled: boolean) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input) {
				input.spellcheck = enabled;
			}
		}

		public static setAcceptsReturn(acceptsReturn: boolean) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement ?? document.activeElement;
			if (input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) {
				input.dataset.unoAcceptsReturn = acceptsReturn ? "true" : "false";
			}
		}

		public static focus(handle: number, isPassword: boolean, text: string, acceptsReturn: boolean, isRichEditBox: boolean, inputMode: string, enterKeyHint: string): boolean {
			// Supersede any detach a preceding managed blur scheduled: focus is moving between
			// TextBoxes, and detaching in between would dismiss the soft keyboard (see blur).
			BrowserInvisibleTextBoxViewExtension.detachGeneration++;

			const semanticElement = document.getElementById(`uno-semantics-${handle}`);
			if (semanticElement && document.activeElement === semanticElement) {
				BrowserInvisibleTextBoxViewExtension.detach();
				return false;
			}

			const existingInput = BrowserInvisibleTextBoxViewExtension.inputElement;
			BrowserInvisibleTextBoxViewExtension.currentHandle = Number(handle);
			BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange = false;
			if (existingInput != null && BrowserInvisibleTextBoxViewExtension.canRetarget(existingInput, isPassword, acceptsReturn, isRichEditBox)) {
				// Reuse the shared input in place: mobile browsers keep the soft keyboard up across
				// a TextBox-to-TextBox move only while an editable element stays focused throughout.
				BrowserInvisibleTextBoxViewExtension.setAcceptsReturn(acceptsReturn);
				existingInput.setAttribute("inputmode", inputMode);
				existingInput.setAttribute("enterkeyhint", enterKeyHint);
				BrowserInvisibleTextBoxViewExtension.setText(text);

				// It's necessary to actually focus the native input, not just make it visible. This is particularly
				// important to mobile browsers (to open the software keyboard) and for assistive technology to not steal
				// events and properly recognize password inputs to not read it.
				if (document.activeElement !== existingInput) {
					existingInput.focus();
				}
			}
			else {
				// The element kind must change (input/textarea/password), or an IME composition is in
				// progress and must not leak into the next TextBox. Focus the new element BEFORE
				// removing the old one so focus hands off editable-to-editable without a gap that
				// would dismiss the soft keyboard. The implicit blur of the old element is
				// managed-initiated, so suppress its notification.
				existingInput?.removeAttribute("id");
				this.createInput(isPassword, text, acceptsReturn, isRichEditBox, inputMode, enterKeyHint);
				BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(() => {
					BrowserInvisibleTextBoxViewExtension.inputElement.focus();
					existingInput?.remove();
				});
			}

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
		private static canRetarget(input: HTMLInputElement | HTMLTextAreaElement, isPassword: boolean, acceptsReturn: boolean, isRichEditBox: boolean): boolean {
			if (BrowserInvisibleTextBoxViewExtension.isComposing) {
				return false;
			}
			const needsTextArea = (acceptsReturn || isRichEditBox) && !isPassword;
			if (input instanceof HTMLTextAreaElement) {
				return needsTextArea;
			}
			return !needsTextArea && (input.type === "password") === isPassword;
		}

		// Runs a managed-initiated focus mutation with the blur listener muted, so it isn't
		// reported back to the FocusManager (which already drove the change). Callers make the
		// blur dispatch synchronously inside the window: detachCore blurs explicitly before
		// removing, and the swap path in focus() focuses the successor (implicitly blurring the
		// old input) before removing it.
		private static runSuppressingBlur(action: () => void) {
			const wasSuppressed = BrowserInvisibleTextBoxViewExtension.suppressBlurNotification;
			BrowserInvisibleTextBoxViewExtension.suppressBlurNotification = true;
			try {
				action();
			} finally {
				BrowserInvisibleTextBoxViewExtension.suppressBlurNotification = wasSuppressed;
			}
		}

		private static detachCore() {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			const handle = BrowserInvisibleTextBoxViewExtension.currentHandle;
			if (input != null && BrowserInvisibleTextBoxViewExtension.isComposingInput(input)) {
				BrowserInvisibleTextBoxViewExtension.invalidateComposition();
				BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionEnded(handle);
				if (BrowserInvisibleTextBoxViewExtension.inputElement !== input
					|| BrowserInvisibleTextBoxViewExtension.currentHandle !== handle) {
					return;
				}
			}
			BrowserInvisibleTextBoxViewExtension.detachGeneration++;
			BrowserInvisibleTextBoxViewExtension.currentHandle = 0;
			BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange = false;
			// Blur explicitly before removing: the .blur() method dispatches synchronously, so it
			// lands inside the suppression window. WebKit can otherwise defer the implicit blur that
			// fires on element removal past that window, which would clear the wrong TextBox's focus.
			input?.blur();
			if (BrowserInvisibleTextBoxViewExtension.inputElement !== input
				|| BrowserInvisibleTextBoxViewExtension.currentHandle === 0) {
				input?.remove();
			}
			if (BrowserInvisibleTextBoxViewExtension.inputElement === input
				&& BrowserInvisibleTextBoxViewExtension.currentHandle === 0) {
				BrowserInvisibleTextBoxViewExtension.inputElement = null;
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
				// During IME composition the browser manages the hidden input's value.
				// Overwriting it would destroy the native composition state and cursor.
				if (BrowserInvisibleTextBoxViewExtension.isComposing) {
					return;
				}

				BrowserInvisibleTextBoxViewExtension.recordManagedInputState(input, text);
				// input could be null beccause we could call setText without focusing first

				if (input.value != text) {
					// When setting input.value, the browser will try to set the selection to the end, which isn't what we want.
					// The browser doesn't raise onselectionchange synchronously though, so we set a flag that we're waiting
					// for a future selection change that is the result of setting value.
					// And we set the existing values of selection start and selection end.
					// On the next onselectionchange event, we will ignore the browser provided selection and use these values.
					// Also, in case we got a managed selection in between here and the next onselectionchange, we will
					// use that instead (see updateSelection below).
					BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange = true;
					BrowserInvisibleTextBoxViewExtension.nextSelectionStart = input.selectionStart;
					BrowserInvisibleTextBoxViewExtension.nextSelectionEnd = input.selectionEnd;
					BrowserInvisibleTextBoxViewExtension.nextSelectionDirection = input.selectionDirection;
					input.value = text;
				}
			}
		}

		public static replaceText(start: number, length: number, replacement: string) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			const origin = BrowserInvisibleTextBoxViewExtension.inputOrigin;
			if (input == null || BrowserInvisibleTextBoxViewExtension.isComposingInput(input)
				|| (origin?.input === input && origin.handle === BrowserInvisibleTextBoxViewExtension.currentHandle)) {
				return;
			}

			start = Math.max(0, Math.min(start, input.value.length));
			const end = Math.max(start, Math.min(start + length, input.value.length));
			const previousText = BrowserInvisibleTextBoxViewExtension.managedInputStates.get(input)?.text ?? input.value;
			BrowserInvisibleTextBoxViewExtension.recordManagedInputState(
				input, previousText.substring(0, start) + replacement + previousText.substring(end));
			input.setRangeText(replacement, start, end, "preserve");
		}

		public static invalidateComposition() {
			BrowserInvisibleTextBoxViewExtension.compositionGeneration++;
			BrowserInvisibleTextBoxViewExtension.isComposing = false;
		}

		public static restartComposition() {
			BrowserInvisibleTextBoxViewExtension.invalidateComposition();
			const input = BrowserInvisibleTextBoxViewExtension.compositionInput;
			const handle = BrowserInvisibleTextBoxViewExtension.compositionHandle;
			if (input == null || !BrowserInvisibleTextBoxViewExtension.isActiveInput(input, handle)) {
				return;
			}

			const generation = BrowserInvisibleTextBoxViewExtension.compositionGeneration;
			const selectionStart = input.selectionStart;
			const selectionEnd = input.selectionEnd;
			const selectionDirection = input.selectionDirection;
			BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(() => {
				input.blur();
				// A blur listener may have focused another control. Never steal that focus back.
				if (document.activeElement === document.body
					&& generation === BrowserInvisibleTextBoxViewExtension.compositionGeneration
					&& BrowserInvisibleTextBoxViewExtension.ownsInput(input, handle)) {
					input.focus({ preventScroll: true });
					if (selectionStart !== null && selectionEnd !== null && document.activeElement === input) {
						input.setSelectionRange(selectionStart, selectionEnd, selectionDirection);
					}
				}
			});
			if (document.activeElement !== input) {
				BrowserInvisibleTextBoxViewExtension._exports.OnNativeBlur(handle);
			}
		}

		public static synchronizeTextInput(handle: number, text: string, start: number, length: number, direction: "forward" | "backward") {
			const nativeInput = BrowserInvisibleTextBoxViewExtension.inputElement;
			const input = nativeInput != null && BrowserInvisibleTextBoxViewExtension.currentHandle === Number(handle)
				? nativeInput
				: document.getElementById(`uno-semantics-${handle}`) as HTMLInputElement | HTMLTextAreaElement;
			if (input == null || !BrowserInvisibleTextBoxViewExtension.isActiveInput(input, Number(handle))
				|| BrowserInvisibleTextBoxViewExtension.isComposingInput(input)) {
				return;
			}
			if (input === nativeInput) {
				BrowserInvisibleTextBoxViewExtension.setText(text);
				BrowserInvisibleTextBoxViewExtension.updateSelection(start, length, direction);
			} else {
				SemanticElements.updateTextBoxValue(handle, text, start, start + length, direction === "backward");
			}
		}

		public static updateSize(width: number, height: number) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input != null) {
				input.style.width = `${width}px`;
				input.style.height = `${height}px`;
			}
		}

		public static updatePosition(x: number, y: number) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input != null) {
				input.style.top = `${Math.round(y)}px`;
				input.style.left = `${Math.round(x)}px`;
			}
		}

		public static updateSelection(start: number, length: number, direction: "forward" | "backward") {
			// During IME composition the browser manages the hidden input's selection.
			if (BrowserInvisibleTextBoxViewExtension.isComposing) {
				return;
			}
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input != null) {
				BrowserInvisibleTextBoxViewExtension.recordManagedInputState(input, undefined, start, start + length, direction === "backward");
			}

			// See comment in setText.
			if (BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange) {
				BrowserInvisibleTextBoxViewExtension.nextSelectionStart = start;
				BrowserInvisibleTextBoxViewExtension.nextSelectionEnd = start + length;
				BrowserInvisibleTextBoxViewExtension.nextSelectionDirection = direction;
			}

			// No-op echoes need no DOM write, but managed cancellation/reentrancy must still win.
			if (input != null
				&& (input.selectionStart !== start || input.selectionEnd !== start + length
					|| (length > 0 && (input.selectionDirection === "backward") !== (direction === "backward")))) {
				input.setSelectionRange(start, start + length, direction);
			}
		}
	}
}
