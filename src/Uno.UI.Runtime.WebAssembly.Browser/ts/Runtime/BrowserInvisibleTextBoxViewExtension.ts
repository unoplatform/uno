namespace Uno.UI.Runtime {
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
		private static compositionStart: number = 0;
		private static suppressNextInput: boolean;
		// The presses of Enter that are down, by the code of their key ("" for an event that has none, as a
		// soft keyboard's), so that each is raised once: those whose keydown was seen as Enter's (the ones
		// that have a code are raised by BrowserKeyboardInputSource), and those the line break raised,
		// release included. A press can outlive the input it started on (a handler moving focus off it),
		// so an entry lasts until the keyup of its key, wherever that goes, or its next keydown.
		private static enterKeyDowns = new Set<string>();
		private static enterLineBreaks = new Set<string>();
		// The code of the last keydown when it can be Enter's: the line break that follows is that key's.
		private static lastKeyDownEnterCode: string = "";
		private static compositionGeneration: number = 0;

		private static waitingAsyncOnSelectionChange: boolean;
		private static nextSelectionStart: number;
		private static nextSelectionEnd: number;
		private static nextSelectionDirection: "forward" | "backward" | "none";

		// Android soft keyboards report all key events with keyCode 229 ("Unidentified").
		// Text changes are synced via the oninput handler instead.
		private static readonly ANDROID_IME_KEYCODE = 229;

		public static getNativePasteSourceLimit(): number {
			BrowserInvisibleTextBoxViewExtension.initialize();
			return BrowserInvisibleTextBoxViewExtension._exports.GetNativePasteSourceLimit();
		}

		public static onNativePaste(source: string): void {
			BrowserInvisibleTextBoxViewExtension.initialize();
			BrowserInvisibleTextBoxViewExtension._exports.OnNativePaste(source);
		}

		public static initialize() {
			if (BrowserInvisibleTextBoxViewExtension._exports == undefined) {
				const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();

				BrowserInvisibleTextBoxViewExtension._exports = browserExports.Uno.UI.Runtime.BrowserInvisibleTextBoxViewExtension;
				BrowserInvisibleTextBoxViewExtension._imeExports = browserExports.Uno.UI.Runtime.WasmImeTextBoxExtension;

				BrowserInvisibleTextBoxViewExtension.installTrailingClickGuard();

				document.onselectionchange = () => {
					let input = document.activeElement;
					if (input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) {
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
			// A previous input may have been removed mid-composition without a compositionend;
			// never carry that state over to a fresh element.
			BrowserInvisibleTextBoxViewExtension.isComposing = false;
			BrowserInvisibleTextBoxViewExtension.suppressNextInput = false;
			const input = document.createElement(acceptsReturn && !isPasswordBox ? "textarea" : "input");
			// The keydown/keyup handlers capture acceptsReturn by closure; record it so canRetarget
			// only reuses the element when the captured behavior still matches.
			(input as any).__unoAcceptsReturn = acceptsReturn;
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
			// Placed before focus() so WebKit never sees an on-screen rect to reveal. Only top goes off-screen:
			// a negative left would be scrollable overflow in a right-to-left document.
			input.style.top = BrowserInvisibleTextBoxViewExtension.keepsInputOffscreen ? BrowserInvisibleTextBoxViewExtension.offscreenTop : "0px";
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
				const source = ev.clipboardData.getData("text");
				const sourceLimit = BrowserInvisibleTextBoxViewExtension._exports.GetNativePasteSourceLimit();
				BrowserInvisibleTextBoxViewExtension._exports.OnNativePaste(
					source.length > sourceLimit ? source.substring(0, sourceLimit) : source);
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

					// The line break the browser sets out to insert for a keydown BrowserKeyboardInputSource
					// raised is not another press of the key.
					const enterCode = BrowserInvisibleTextBoxViewExtension.lastKeyDownEnterCode;
					BrowserInvisibleTextBoxViewExtension.lastKeyDownEnterCode = "";
					if (enterCode === "" || !BrowserInvisibleTextBoxViewExtension.enterKeyDowns.has(enterCode)) {
						BrowserInvisibleTextBoxViewExtension.enterLineBreaks.add(enterCode);
						BrowserInvisibleTextBoxViewExtension._exports.OnEnterKeyPressed();
					}
				}
			});

			BrowserInvisibleTextBoxViewExtension.attachTextInputKeyHandlers(input, acceptsReturn);

			let activeCompositionGeneration = 0;
			input.addEventListener("compositionstart", () => {
				activeCompositionGeneration = ++BrowserInvisibleTextBoxViewExtension.compositionGeneration;
				BrowserInvisibleTextBoxViewExtension.isComposing = true;
				BrowserInvisibleTextBoxViewExtension.compositionStart = input.selectionStart ?? 0;
				BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionStarted();
			});

			input.addEventListener("compositionupdate", (ev: CompositionEvent) => {
				if (activeCompositionGeneration !== BrowserInvisibleTextBoxViewExtension.compositionGeneration) {
					return;
				}
				// Use input.selectionStart for cursor position when available,
				// as the IME may place the caret within the preedit string.
				const selectionStart = input.selectionStart;
				const cursorPosition = selectionStart === null
					? ev.data.length
					: Math.max(0, Math.min(
						selectionStart - BrowserInvisibleTextBoxViewExtension.compositionStart,
						ev.data.length));
				BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionUpdated(ev.data, cursorPosition);
			});

			input.addEventListener("compositionend", (ev: CompositionEvent) => {
				if (activeCompositionGeneration !== BrowserInvisibleTextBoxViewExtension.compositionGeneration) {
					return;
				}
				BrowserInvisibleTextBoxViewExtension.isComposing = false;
				// The browser fires an input event after compositionend with the committed text.
				// Suppress it to avoid double-inserting — the commit is handled by OnCompositionCompleted.
				BrowserInvisibleTextBoxViewExtension.suppressNextInput = true;
				if (ev.data.length > 0) {
					BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionCompleted(ev.data);
				} else {
					BrowserInvisibleTextBoxViewExtension._imeExports.OnCompositionCanceled();
				}
			});

			document.body.appendChild(input);
			BrowserInvisibleTextBoxViewExtension.inputElement = input;
		}

		private static isEnterKeyEvent(ev: KeyboardEvent): boolean {
			return ev.key === "Enter" || ev.keyCode === 13 || ev.code === "Enter" || ev.code === "NumpadEnter";
		}

		// The code BrowserKeyboardInputSource tells Enter by, or "" for an event that has none of them.
		private static enterCodeOf(ev: KeyboardEvent): string {
			return ev.code === "Enter" || ev.code === "NumpadEnter" ? ev.code : "";
		}

		// Called by BrowserKeyboardInputSource for a keydown that reached the document. It raises the one
		// that has the code of Enter, so the press is recorded here too: it may have started off the
		// TextBox inputs, and whatever an earlier press of the key left behind is over.
		public static onKeyDownAtDocument(ev: KeyboardEvent) {
			const enterCode = BrowserInvisibleTextBoxViewExtension.enterCodeOf(ev);
			BrowserInvisibleTextBoxViewExtension.lastKeyDownEnterCode = enterCode;
			if (enterCode !== "") {
				BrowserInvisibleTextBoxViewExtension.enterLineBreaks.delete(enterCode);
				BrowserInvisibleTextBoxViewExtension.enterKeyDowns.add(enterCode);
			}
		}

		// Called by BrowserKeyboardInputSource for a keyup that reached the document: ends the press of
		// Enter, and returns whether its release was raised along with the press, in which case it must
		// not be raised again. The input a press started on may be gone by its keyup, which then does
		// not pass the input's handler.
		public static onKeyUpAtDocument(ev: KeyboardEvent): boolean {
			if (!BrowserInvisibleTextBoxViewExtension.isEnterKeyEvent(ev)) {
				return false;
			}

			const enterCode = BrowserInvisibleTextBoxViewExtension.enterCodeOf(ev);
			const raised = enterCode !== "" && BrowserInvisibleTextBoxViewExtension.enterLineBreaks.has(enterCode);
			BrowserInvisibleTextBoxViewExtension.endEnterPress(enterCode);
			return raised;
		}

		// The key that has this code is released: its press is over, and a line break that comes after
		// this does not belong to its keydown.
		private static endEnterPress(enterCode: string) {
			BrowserInvisibleTextBoxViewExtension.enterKeyDowns.delete(enterCode);
			BrowserInvisibleTextBoxViewExtension.enterLineBreaks.delete(enterCode);
			if (BrowserInvisibleTextBoxViewExtension.lastKeyDownEnterCode === enterCode) {
				BrowserInvisibleTextBoxViewExtension.lastKeyDownEnterCode = "";
			}
		}

		// Applies the same keydown/keyup guards used on the invisible <input> to any text input
		// that must delegate character insertion to managed TextBox KeyDown handling.
		// Without these guards, focused text inputs (e.g. the a11y semantic <input>) would insert
		// the character natively AND via the managed path, producing duplicated input.
		public static attachTextInputKeyHandlers(input: HTMLInputElement | HTMLTextAreaElement, acceptsReturn: boolean) {
			input.addEventListener("keydown", (ev: KeyboardEvent) => {
				const acceptsReturnNow = input === BrowserInvisibleTextBoxViewExtension.inputElement
					? BrowserInvisibleTextBoxViewExtension.acceptsReturn
					: acceptsReturn;

				// A key press starts here, and the line break that may follow belongs to this keydown.
				// Whatever an earlier press of the same key left behind is over. The keydown of another key
				// leaves an Enter that is still down alone. A keydown without a code may be a soft
				// keyboard's Enter, so it counts as a press of it.
				const enterCode = BrowserInvisibleTextBoxViewExtension.enterCodeOf(ev);
				BrowserInvisibleTextBoxViewExtension.lastKeyDownEnterCode = enterCode;
				const isOtherKey = ev.code !== "" && ev.code !== "Unidentified" && enterCode === ""
					&& ev.key !== "Enter" && ev.keyCode !== 13;
				if (!isOtherKey) {
					BrowserInvisibleTextBoxViewExtension.enterKeyDowns.delete(enterCode);
					BrowserInvisibleTextBoxViewExtension.enterLineBreaks.delete(enterCode);
				}

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
				// BrowserKeyboardInputSource raises the managed KeyDown. Recording the keydown prevents
				// the keyup branch below from dispatching a duplicate OnEnterKeyPressed.
				// This enables focus navigation (e.g., Uno.Toolkit's AutoFocusNext) on mobile browsers
				if ((ev.key === "Enter" || ev.keyCode === 13) && !acceptsReturnNow) {
					// Don't call preventDefault() to allow the key event to propagate to document listeners
					BrowserInvisibleTextBoxViewExtension.enterKeyDowns.add(enterCode);
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
				const acceptsReturnNow = input === BrowserInvisibleTextBoxViewExtension.inputElement
					? BrowserInvisibleTextBoxViewExtension.acceptsReturn
					: acceptsReturn;

				// The line break raised the key, its release included. A keyup that has the code
				// BrowserKeyboardInputSource tells the key by must not reach it, or it would raise the
				// release again.
				const enterCode = BrowserInvisibleTextBoxViewExtension.enterCodeOf(ev);
				const raisedByLineBreak = BrowserInvisibleTextBoxViewExtension.enterLineBreaks.has(enterCode);
				if (raisedByLineBreak && enterCode !== "") {
					ev.stopPropagation();
				}

				// Android virtual keyboards (Gboard/SwiftKey/Samsung/AOSP) report keydown
				// with keyCode 229 ("Unidentified") for Enter, which is stopPropagation'd
				// in the keydown handler so it never reaches BrowserKeyboardInputSource. They DO report keyup
				// with key === "Enter" though - use that to raise the managed KeyDown so
				// focus-navigation patterns (Uno.Toolkit AutoFocusNext, FocusManager) work
				// on Android browsers. The recorded keydown guards against double-dispatch on desktop/iOS,
				// where the keydown branch already routed Enter through the document listener.
				if (!acceptsReturnNow
					&& ev.key === "Enter"
					&& !BrowserInvisibleTextBoxViewExtension.enterKeyDowns.has(enterCode)
					&& !raisedByLineBreak
					&& !ev.isComposing) {
					ev.preventDefault();
					// OnEnterKeyPressed raises the release along with the press. A keyup that has a code
					// must not reach BrowserKeyboardInputSource, or it would raise the release again.
					ev.stopPropagation();
					BrowserInvisibleTextBoxViewExtension._exports.OnEnterKeyPressed();
				}

				if (BrowserInvisibleTextBoxViewExtension.isEnterKeyEvent(ev)) {
					BrowserInvisibleTextBoxViewExtension.endEnterPress(enterCode);
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
				// Password inputs keep their password-manager hint.
				if (!(input instanceof HTMLInputElement && input.type === "password")) {
					input.autocomplete = enabled ? "on" : "off";
				}
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
			BrowserInvisibleTextBoxViewExtension.acceptsReturn = acceptsReturn;
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input) {
				input.dataset.unoAcceptsReturn = acceptsReturn ? "true" : "false";
			}
		}

		public static focus(handle: number, isPassword: boolean, text: string, acceptsReturn: boolean, inputMode: string, enterKeyHint: string): boolean {
			// Supersede any detach a preceding managed blur scheduled: focus is moving between
			// TextBoxes, and detaching in between would dismiss the soft keyboard (see blur).
			BrowserInvisibleTextBoxViewExtension.detachGeneration++;

			const semanticElement = document.getElementById(`uno-semantics-${handle}`);
			if (semanticElement && document.activeElement === semanticElement) {
				BrowserInvisibleTextBoxViewExtension.detach();
				return false;
			}

			const existingInput = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (existingInput != null && BrowserInvisibleTextBoxViewExtension.canRetarget(existingInput, isPassword, acceptsReturn)) {
				// Reuse the shared input in place: mobile browsers keep the soft keyboard up across
				// a TextBox-to-TextBox move only while an editable element stays focused throughout.
				BrowserInvisibleTextBoxViewExtension.acceptsReturn = acceptsReturn;
				existingInput.setAttribute("inputmode", inputMode);
				existingInput.setAttribute("enterkeyhint", enterKeyHint);
				BrowserInvisibleTextBoxViewExtension.setText(text);

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
				this.createInput(isPassword, text, acceptsReturn, inputMode, enterKeyHint);
				BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(() => {
					BrowserInvisibleTextBoxViewExtension.inputElement.focus(BrowserInvisibleTextBoxViewExtension.focusOptions);
					existingInput?.remove();
				});
			}

			BrowserInvisibleTextBoxViewExtension.currentHandle = Number(handle);

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
			// Focus left the text inputs: a line break that comes after this is not the last keydown's.
			BrowserInvisibleTextBoxViewExtension.lastKeyDownEnterCode = "";
			// Blur explicitly before removing: the .blur() method dispatches synchronously, so it
			// lands inside the suppression window. WebKit can otherwise defer the implicit blur that
			// fires on element removal past that window, which would clear the wrong TextBox's focus.
			BrowserInvisibleTextBoxViewExtension.inputElement?.blur();
			BrowserInvisibleTextBoxViewExtension.inputElement?.remove();
			BrowserInvisibleTextBoxViewExtension.inputElement = null;
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
			if (input == null || BrowserInvisibleTextBoxViewExtension.isComposing) {
				return;
			}

			start = Math.max(0, Math.min(start, input.value.length));
			const end = Math.max(start, Math.min(start + length, input.value.length));
			input.setRangeText(replacement, start, end, "preserve");
		}

		public static invalidateComposition() {
			const wasComposing = BrowserInvisibleTextBoxViewExtension.isComposing;
			BrowserInvisibleTextBoxViewExtension.compositionGeneration++;
			BrowserInvisibleTextBoxViewExtension.isComposing = false;
			BrowserInvisibleTextBoxViewExtension.suppressNextInput = wasComposing;
		}

		public static restartComposition() {
			BrowserInvisibleTextBoxViewExtension.invalidateComposition();
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input == null || document.activeElement !== input) {
				return;
			}

			const selectionStart = input.selectionStart;
			const selectionEnd = input.selectionEnd;
			const selectionDirection = input.selectionDirection;
			// Managed-initiated: reporting this blur would unfocus the TextBox that owns the session.
			BrowserInvisibleTextBoxViewExtension.runSuppressingBlur(() => {
				input.blur();
				input.focus({ preventScroll: true });
			});
			if (selectionStart !== null && selectionEnd !== null) {
				input.setSelectionRange(selectionStart, selectionEnd, selectionDirection);
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
