namespace Windows.UI.ViewManagement {

	export class ApplicationView {

		public static setFullScreenMode(turnOn: boolean): boolean {
			if (turnOn) {
				if (document.fullscreenEnabled) {
					document.documentElement.requestFullscreen();
					return true;
				} else {
					return false;
				}
			} else {
				document.exitFullscreen();
				return true;
			}
		}

		/**
			* Sets the browser window title
			* @param message the new title
			*/
		public static setWindowTitle(title: string): string {
			document.title = title /* TODO JELA || UnoAppManifest.displayName */;
			return "ok";
		}

		/**
			* Gets the currently set browser window title
			*/
		public static getWindowTitle(): string {
			return document.title /* TODO JELA || UnoAppManifest.displayName */;
		}
	}
}
