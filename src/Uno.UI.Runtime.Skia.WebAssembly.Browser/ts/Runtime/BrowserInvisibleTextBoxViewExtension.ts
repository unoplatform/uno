namespace Uno.UI.Runtime.Skia {
	export class BrowserInvisibleTextBoxViewExtension {
		private static _exports: any;
		private static readonly inputElementId = "uno-input";
		private static inputElement: HTMLInputElement;

		public static async initialize(): Promise<any> {
			const module = <any>window.Module;
			if (BrowserInvisibleTextBoxViewExtension._exports == undefined
				&& module.getAssemblyExports !== undefined) {
				const browserExports = (await module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser"));

				BrowserInvisibleTextBoxViewExtension._exports = browserExports.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension;
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
			input.style.opacity = "1";
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
				BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged((ev.target as HTMLInputElement).value)
			};

			document.body.appendChild(input);
			BrowserInvisibleTextBoxViewExtension.inputElement = input;
		}

		public static focus(focused: boolean, isPassword: boolean, text: string) {
			if (focused) {
				this.createInput(isPassword, text);

				// It's necessary to actually focus the native input, not just make it visible. This is particularly
				// important to mobile browsers (to open the software keyboard) and for assistive technology to not steal
				// events and properly recognize password inputs to not read it.
				BrowserInvisibleTextBoxViewExtension.inputElement.focus();
			} else {
				// reset focus
				(document.activeElement as HTMLElement)?.blur();
				BrowserInvisibleTextBoxViewExtension.inputElement.remove();
			}
		}

		public static setText(text: string) {
			BrowserInvisibleTextBoxViewExtension.inputElement.textContent = text;
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
	}
}
