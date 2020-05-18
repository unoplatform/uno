namespace Windows.ApplicationModel.DataTransfer {
	export class Clipboard {
		private static dispatchContentChanged: () => number;

		public static clear() {
			navigator.clipboard.
		}

		public static startContentChanged() {
			['cut', 'copy', 'paste'].forEach(function (event) {
				document.addEventListener(event, Clipboard.onClipboardChanged);
			});
		}

		public static stopContentChanged() {
			['cut', 'copy', 'paste'].forEach(function (event) {
				document.removeEventListener(event, Clipboard.onClipboardChanged);
			});
		}

		private static onClipboardChanged() {
			Clipboard.dispatchContentChanged();
		}
	}
}
