namespace Uno.UI.Runtime.Skia {
	export class BrowserInvisibleTextBoxViewExtension {
		private static _exports: any;
		private static readonly inputElementId = "uno-input";
		private static readonly isMacOS = navigator?.platform.toUpperCase().includes('MAC') ?? false;
		private static inputElement: HTMLInputElement | HTMLTextAreaElement;
		private static isInSelectionChange: boolean;

		private static waitingAsyncOnSelectionChange: boolean;
		private static nextSelectionStart: number;
		private static nextSelectionEnd: number;
		private static nextSelectionDirection: "forward" | "backward" | "none";

		// Track whether the next keydown should skip preventDefault because
		// a beforeinput event indicated a delete operation.
		private static skipPreventDefaultForDelete: boolean = false;

		public static async initialize(): Promise<any> {
			const module = <any>window.Module;
			if (BrowserInvisibleTextBoxViewExtension._exports == undefined
				&& module.getAssemblyExports !== undefined) {
				const browserExports = (await module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser"));

				BrowserInvisibleTextBoxViewExtension._exports = browserExports.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension;

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

		private static createInput(isPasswordBox: boolean, text: string, acceptsReturn: boolean, inputMode: string, enterKeyHint: string) {
			const input = document.createElement(acceptsReturn && !isPasswordBox ? "textarea" : "input");
			if (isPasswordBox) {
				(input as HTMLInputElement).type = "password";
				input.autocomplete = "password";
			}

			input.id = BrowserInvisibleTextBoxViewExtension.inputElementId;
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

			// Use beforeinput to detect delete operations reliably on Android soft keyboards.
			// On Android, keydown events return keyCode 229 (IME processing) for all keys,
			// making it impossible to detect Backspace/Delete via keydown. The beforeinput
			// event provides inputType which reliably indicates the operation type.
			input.addEventListener('beforeinput', (ev: InputEvent) => {
				if (ev.inputType &&
					(ev.inputType === 'deleteContentBackward' ||
					 ev.inputType === 'deleteContentForward' ||
					 ev.inputType === 'deleteByCut' ||
					 ev.inputType === 'deleteByDrag' ||
					 ev.inputType === 'deleteContent' ||
					 ev.inputType === 'deleteWordBackward' ||
					 ev.inputType === 'deleteWordForward' ||
					 ev.inputType === 'deleteSoftLineBackward' ||
					 ev.inputType === 'deleteSoftLineForward' ||
					 ev.inputType === 'deleteHardLineBackward' ||
					 ev.inputType === 'deleteHardLineForward')) {
					// Set flag so onkeydown knows to skip preventDefault for this event.
					// This allows the browser to handle the deletion natively, and the
					// oninput event will fire with the updated text.
					BrowserInvisibleTextBoxViewExtension.skipPreventDefaultForDelete = true;
				}
			});

			input.onkeydown = ev => {
				if (ev.ctrlKey || (ev.metaKey && BrowserInvisibleTextBoxViewExtension.isMacOS)) {
					// Due to browser security considerations, we need to let the clipboard operations be handled natively.
					// So, we do stopPropagation instead of preventDefault
					if (ev.key == "c" || ev.key == "C" || ev.key == "v" || ev.key == "V" || ev.key == "x" || ev.key == "X") {
						ev.stopPropagation();
						return;
					}
				}

				// Check if beforeinput indicated this is a delete operation.
				// If so, let the browser handle it natively (don't preventDefault).
				if (BrowserInvisibleTextBoxViewExtension.skipPreventDefaultForDelete) {
					BrowserInvisibleTextBoxViewExtension.skipPreventDefaultForDelete = false;
					ev.stopPropagation();
					return;
				}

				// Also check ev.key for desktop browsers where beforeinput may fire after keydown
				// or for cases where beforeinput isn't supported.
				if (ev.key === 'Backspace' || ev.key === 'Delete') {
					ev.stopPropagation();
					return;
				}

				ev.preventDefault();
			};

			document.body.appendChild(input);
			BrowserInvisibleTextBoxViewExtension.inputElement = input;
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

		public static focus(isPassword: boolean, text: string, acceptsReturn: boolean, inputMode: string, enterKeyHint: string) {
			// NOTE: We can get focused as true while we have inputElement.
			// This happens when TextBox is focused twice with different FocusStates (e.g, Pointer, Programmatic, Keyboard)
			// For such case, we do call StartEntry twice without any EndEntry in between.
			// So, cleanup the existing inputElement and create a new one.
			BrowserInvisibleTextBoxViewExtension.inputElement?.remove();
			this.createInput(isPassword, text, acceptsReturn, inputMode, enterKeyHint);

			// It's necessary to actually focus the native input, not just make it visible. This is particularly
			// important to mobile browsers (to open the software keyboard) and for assistive technology to not steal
			// events and properly recognize password inputs to not read it.
			BrowserInvisibleTextBoxViewExtension.inputElement.focus();
		}

		public static blur() {
			// reset focus
			(document.activeElement as HTMLElement)?.blur();
			BrowserInvisibleTextBoxViewExtension.inputElement?.remove();
		}

		public static setText(text: string) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input != null) {
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
