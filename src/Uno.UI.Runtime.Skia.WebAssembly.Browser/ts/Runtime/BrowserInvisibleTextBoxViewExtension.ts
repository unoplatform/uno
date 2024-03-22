namespace Uno.UI.Runtime.Skia {
	export class BrowserInvisibleTextBoxViewExtension {
		private static _inputIdPrefix: string = "UnoInvisibleTextBoxViewInput"
		private static _inputIdSuffix: number = 1;

		private static _exports: any;

		public static async initialize(): Promise<any> {
			const module = <any>window.Module;
			if (BrowserInvisibleTextBoxViewExtension._exports == undefined
				&& module.getAssemblyExports !== undefined) {
				const browserExports = (await module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser"));

				BrowserInvisibleTextBoxViewExtension._exports = browserExports.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension;
			}
		}

		public static createInput(instance: any, isPasswordBox: boolean): string {
			const input = document.createElement("input");
			if (isPasswordBox) {
				input.type = "password";
				input.autocomplete = "password";
			} else {
				input.type = "text";
			}

			input.id = BrowserInvisibleTextBoxViewExtension._inputIdPrefix + String(BrowserInvisibleTextBoxViewExtension._inputIdSuffix);
			BrowserInvisibleTextBoxViewExtension._inputIdSuffix++;

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

			input.oninput = ev => {
				BrowserInvisibleTextBoxViewExtension._exports.OnInputTextChanged(instance, (ev.target as HTMLInputElement).value)
			};

			// bubble the key events up to be handled in uno without actually inserting any character
			input.onkeydown = ev => ev.preventDefault();

			document.body.appendChild(input);

			return input.id;
		}

		public static focus(id: string, focused: boolean) {
			if (focused) {
				// It's necessary to actually focus the native input, not just make it visible. This is particularly
				// important to mobile browsers (to open the software keyboard) and
				document.getElementById(id).focus();
			} else {
				// reset focus
				(document.activeElement as HTMLElement)?.blur();
			}
		}

		public static setText(id: string, text: string) {
			(document.getElementById(id) as HTMLInputElement).textContent = text;
		}

		public static updateSize(id: string, width: number, height: number) {
			const input = (document.getElementById(id) as HTMLInputElement);
			input.width = width;
			input.height = height;
		}

		public static updatePosition(id: string, x: number, y: number) {
			const input = (document.getElementById(id) as HTMLInputElement);
			input.style.top = `${Math.round(y)}px`;
			input.style.left = `${Math.round(x)}px`;
		}

		public static disposeInput(id: string) {
			document.getElementById(id).remove();
		}
	}
}
