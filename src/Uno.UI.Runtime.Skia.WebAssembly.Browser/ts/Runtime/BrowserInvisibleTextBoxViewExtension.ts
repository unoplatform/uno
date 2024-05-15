namespace Uno.UI.Runtime.Skia {
	export class BrowserInvisibleTextBoxViewExtension {
		private static _exports: any;
		private static readonly inputElementId = "uno-input";
		private static inputElement: HTMLInputElement;
		private static isInSelectionChange: boolean;

		public static async initialize(): Promise<any> {
			const module = <any>window.Module;
			if (BrowserInvisibleTextBoxViewExtension._exports == undefined
				&& module.getAssemblyExports !== undefined) {
				const browserExports = (await module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser"));

				BrowserInvisibleTextBoxViewExtension._exports = browserExports.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension;

				document.onselectionchange = (ev) => {
					let input = document.activeElement;
					if (input instanceof HTMLInputElement) {
						BrowserInvisibleTextBoxViewExtension.isInSelectionChange = true;
						console.log(input);
						console.log(`Got native selection change ${input.selectionStart}, ${input.selectionEnd}, ${input.selectionDirection}`);
						if (input.selectionDirection == "backward") {
							BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(input.selectionEnd, input.selectionStart - input.selectionEnd);
						} else {
							BrowserInvisibleTextBoxViewExtension._exports.OnSelectionChanged(input.selectionStart, input.selectionEnd - input.selectionStart);
						}

						BrowserInvisibleTextBoxViewExtension.isInSelectionChange = false;
					}
				}
			}
		}

		private static createInput(isPasswordBox: boolean, text: string) {
			const input = document.createElement("input");
			if (isPasswordBox) {
				input.type = "password";
				input.autocomplete = "password";
			} else {
				input.type = "text";
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

			input.oninput = ev => {
				let input = ev.target as HTMLInputElement;
				BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged(input.value, input.selectionStart, input.selectionEnd - input.selectionStart);
			};

			input.onkeydown = ev => {
				ev.stopPropagation();
			};

			document.body.appendChild(input);
			BrowserInvisibleTextBoxViewExtension.inputElement = input;
		}

		public static focus(focused: boolean, isPassword: boolean, text: string) {
			if (focused) {
				// NOTE: We can get focused as true while we have inputElement.
				// This happens when TextBox is focused twice with different FocusStates (e.g, Pointer, Programmatic, Keyboard)
				// For such case, we do call StartEntry twice without any EndEntry in between.
				// So, cleanup the existing inputElement and create a new one.
				BrowserInvisibleTextBoxViewExtension.inputElement?.remove();
				this.createInput(isPassword, text);

				// It's necessary to actually focus the native input, not just make it visible. This is particularly
				// important to mobile browsers (to open the software keyboard) and for assistive technology to not steal
				// events and properly recognize password inputs to not read it.
				console.log("Focusing native");
				BrowserInvisibleTextBoxViewExtension.inputElement.focus();
				console.log("Focused native");
			} else {
				// reset focus
				(document.activeElement as HTMLElement)?.blur();
				BrowserInvisibleTextBoxViewExtension.inputElement?.remove();
			}
		}

		public static setText(text: string) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			if (input != null) {
				// input could be null beccause we could call setText without focusing first
				input.value = text;
			}
		}

		public static updateSize(width: number, height: number) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			input.width = width;
			input.height = height;
		}

		public static updatePosition(x: number, y: number) {
			const input = BrowserInvisibleTextBoxViewExtension.inputElement;
			input.style.top = `${Math.round(y)}px`;
			input.style.left = `${Math.round(x)}px`;
		}

		public static updateSelection(start: number, length: number, direction: "forward" | "backward") {
			if (!BrowserInvisibleTextBoxViewExtension.isInSelectionChange) {
				const input = BrowserInvisibleTextBoxViewExtension.inputElement;
				console.log(`Got managed selection change ${start}, ${length}, ${direction}`);
				input.setSelectionRange(start, start + length, direction);
			}
		}
	}
}
