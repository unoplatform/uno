namespace Uno.UI.Runtime.Skia {
	export class BrowserKeyboardInputSource {
		private static _exports: any;
		
		public static async initialize(inputSource: any): Promise<any> {
			const module = <any>window.Module;
			if (BrowserKeyboardInputSource._exports == undefined
				&& module.getAssemblyExports !== undefined) {
					
				const browserExports = (await module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser"));

				BrowserKeyboardInputSource._exports = browserExports.Uno.UI.Runtime.Skia.BrowserKeyboardInputSource;
			}

			return new BrowserKeyboardInputSource(inputSource);
		}

		private _source: any;

		private constructor(managedSource: any) {
			this._source = managedSource;

			this.subscribeKeyboardEvents();
		}

		private subscribeKeyboardEvents() {
			document.addEventListener("keydown", this.onKeyboardEvent.bind(this));
			document.addEventListener("keyup", this.onKeyboardEvent.bind(this));
		}

		private onKeyboardEvent(evt: KeyboardEvent): void {
			if ((evt.target as HTMLElement)?.id !== "uno-canvas" && (evt.target as HTMLElement)?.id !== "uno-input") {
				return;
			}

			let result = BrowserKeyboardInputSource._exports.OnNativeKeyboardEvent(
				this._source,
				evt.type == "keydown",
				evt.ctrlKey,
				evt.shiftKey,
				evt.altKey,
				evt.metaKey,
				evt.code,
				evt.key
			);

			if (result == HtmlEventDispatchResult.PreventDefault) {
				evt.preventDefault();
			}
		}
	}
}
