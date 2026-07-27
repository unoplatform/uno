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
		private static isInSelectionChange: boolean;
		private static acceptsReturn: boolean;
		private static isComposing: boolean;
		private static suppressNextInput: boolean;
		private static enterHandledByKeyDown: boolean;

		private static waitingAsyncOnSelectionChange: boolean;
		private static nextSelectionStart: number;
		private static nextSelectionEnd: number;
		private static nextSelectionDirection: "forward" | "backward" | "none";

		// Android soft keyboards report all key events with keyCode 229 ("Unidentified").
		// Text changes are synced via the oninput handler instead.
		private static readonly ANDROID_IME_KEYCODE = 229;

		public static initialize() {
			if (BrowserInvisibleTextBoxViewExtension._exports == undefined) {
				const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();

				BrowserInvisibleTextBoxViewExtension._exports = browserExports.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension;
				BrowserInvisibleTextBoxViewExtension._imeExports = browserExports.Uno.UI.Runtime.Skia.WasmImeTextBoxExtension;

				BrowserInvisibleTextBoxViewExtension.installTrailingClickGuard();

				document.onselectionchange = () => {
					let input = document.activeElement;
					if (input instanceof HTMLInputElement) {
						BrowserInvisibleTextBoxViewExtension.isInSelectionChange = true;

						if (BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange) {
							BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange = false;
							input.setSelectionRange(BrowserInvisibleTextBoxViewExtension.nextSelectionStart, BrowserInvisibleTextBoxViewExtension.nextSelectionEnd, BrowserInvisibleTextBoxViewExtension.nextSelectionDirection);
						}
						else {
							if (input.selectionDirection == "backward") {
								BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(input.selectionEnd, input.selectionStart - input.selectionEnd);
							} else {
								BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(input.selectionStart, input.selectionEnd - input.selectionStart);
							}
						}

						BrowserInvisibleTextBoxViewExtension.isInSelectionChange = false;
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

		private static createInput(isPasswordBox: boolean, text: string, acceptsReturn: boolean, inputMode: string, enterKeyHint: string) {
			BrowserInvisibleTextBoxViewExtension.acceptsReturn = acceptsReturn;
			const input = document.createElement(acceptsReturn && !isPasswordBox ? "textarea" : "input");
			if (isPasswordBox) {
				(input as HTMLInputElement).type = "password";
				input.autocomplete = "password";
			}

			input.id = UnoDomIds.input;
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

			input.oninput = ev => {
				// During IME composition, text state is managed by the composition event path.
				// The oninput event still fires but we must skip the normal text sync.
				// Also suppress the final input event after compositionend (browser fires input after compositionend).
				if (BrowserInvisibleTextBoxViewExtension.isComposing || BrowserInvisibleTextBoxViewExtension.suppressNextInput) {
					BrowserInvisibleTextBoxViewExtension.suppressNextInput = false;
					return;
				}
				let input = ev.target as HTMLInputElement;
				if (input.selectionDirection == "backward") {
					BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged(input.value, input.selectionEnd, input.selectionStart - input.selectionEnd);
				} else {
					BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged(input.value, input.selectionStart, input.selectionEnd - input.selectionStart);
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
				if ((ev.inputType === "insertLineBreak" || ev.inputType === "insertParagraph") && !BrowserInvisibleTextBoxViewExtension.acceptsReturn) {
					ev.preventDefault();

					BrowserInvisibleTextBoxViewExtension._exports.OnEnterKeyPressed();
				}
			});

			BrowserInvisibleTextBoxViewExtension.attachTextInputKeyHandlers(input, acceptsReturn);

			input.addEventListener("compositionstart", () => {
				BrowserInvisibleTextBoxViewExtension.isComposing = true;
				BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionStarted();
			});

			input.addEventListener("compositionupdate", (ev: CompositionEvent) => {
				// Use input.selectionStart for cursor position when available,
				// as the IME may place the caret within the preedit string.
				const selectionStart = input.selectionStart;
				const cursorPosition = selectionStart === null
					? ev.data.length
					: Math.max(0, Math.min(selectionStart, ev.data.length));
				BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionUpdated(ev.data, cursorPosition);
			});

			input.addEventListener("compositionend", (ev: CompositionEvent) => {
				BrowserInvisibleTextBoxViewExtension.isComposing = false;
				// The browser fires an input event after compositionend with the committed text.
				// Suppress it to avoid double-inserting — the commit is handled by OnCompositionCompleted.
				BrowserInvisibleTextBoxViewExtension.suppressNextInput = true;
				if (ev.data.length > 0) {
					BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionCompleted(ev.data);
				} else {
					BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionEnded();
				}
			});

			document.body.appendChild(input);
			BrowserInvisibleTextBoxViewExtension.inputElement = input;
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

		public static focus(handle: number, isPassword: boolean, text: string, acceptsReturn: boolean, inputMode: string, enterKeyHint: string): boolean {
			const semanticElement = document.getElementById(`uno-semantics-${handle}`);
			if (semanticElement && document.activeElement === semanticElement) {
				BrowserInvisibleTextBoxViewExtension.detach();
				return false;
			}

			// NOTE: We can get focused as true while we have inputElement.
			// This happens when TextBox is focused twice with different FocusStates (e.g, Pointer, Programmatic, Keyboard)
			// For such case, we do call StartEntry twice without any EndEntry in between.
			// So, cleanup the existing inputElement and create a new one.
			// Removing a focused input fires a native blur; suppress it since this is a managed re-focus.
			BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(BrowserInvisibleTextBoxViewExtension.detachCore);
			this.createInput(isPassword, text, acceptsReturn, inputMode, enterKeyHint);

			// It's necessary to actually focus the native input, not just make it visible. This is particularly
			// important to mobile browsers (to open the software keyboard) and for assistive technology to not steal
			// events and properly recognize password inputs to not read it.
			BrowserInvisibleTextBoxViewExtension.inputElement.focus();
			return true;
		}

		// Runs a managed-initiated focus mutation with the blur listener muted, so it isn't
		// reported back to the FocusManager (which already drove the change). Callers that
		// remove the input do so via detachCore, which blurs it explicitly first.
		private static runSuppressingBlur(action: () => void) {
			BrowserInvisibleTextBoxViewExtension.suppressBlurNotification = true;
			try {
				action();
			} finally {
				BrowserInvisibleTextBoxViewExtension.suppressBlurNotification = false;
			}
		}

		private static detachCore() {
			// Blur explicitly before removing: the .blur() method dispatches synchronously, so it
			// lands inside the suppression window. WebKit can otherwise defer the implicit blur that
			// fires on element removal past that window, which would clear the wrong TextBox's focus.
			BrowserInvisibleTextBoxViewExtension.inputElement?.blur();
			BrowserInvisibleTextBoxViewExtension.inputElement?.remove();
			BrowserInvisibleTextBoxViewExtension.inputElement = null;
		}

		public static blur() {
			// Managed-initiated blur (EndEntry): the FocusManager already knows focus is leaving.
			BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(() => {
				(document.activeElement as HTMLElement)?.blur();
				BrowserInvisibleTextBoxViewExtension.detachCore();
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

		public static updateSize(width: number, height: number) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input != null) {
				input.style.width = `${width}`;
				input.style.height = `${height}`;
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
			if (!BrowserInvisibleTextBoxViewExtension.isInSelectionChange) {
				const input = BrowserInvisibleTextBoxViewExtension.inputElement;

				// See comment in setText.
				if (BrowserInvisibleTextBoxViewExtension.waitingAsyncOnSelectionChange) {
					BrowserInvisibleTextBoxViewExtension.nextSelectionStart = start;
					BrowserInvisibleTextBoxViewExtension.nextSelectionEnd = start + length;
					BrowserInvisibleTextBoxViewExtension.nextSelectionDirection = direction;
				}

				input?.setSelectionRange(start, start + length, direction);
			}
		}
	}
}
