namespace Uno.UI.Runtime.Skia {
	export class BrowserKeyboardInputSource {
		private static _exports: any;
		private static _source: any;
		
		public static initialize(inputSource: any): any {
			if (BrowserKeyboardInputSource._exports == undefined) {
				const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();
				BrowserKeyboardInputSource._exports = browserExports.Uno.UI.Runtime.Skia.BrowserKeyboardInputSource;
			}

			BrowserKeyboardInputSource._source = inputSource;
			BrowserKeyboardInputSource.subscribeKeyboardEvents();
		}

		private static subscribeKeyboardEvents() {
			document.addEventListener("keydown", BrowserKeyboardInputSource.onKeyboardEvent);
			document.addEventListener("keyup", BrowserKeyboardInputSource.onKeyboardEvent);
		}

		private static onKeyboardEvent(evt: KeyboardEvent): void {
			let result = BrowserKeyboardInputSource._exports.OnNativeKeyboardEvent(
				BrowserKeyboardInputSource._source,
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
