namespace Windows.UI.ViewManagement {

	export class ApplicationView {

		public static setFullScreenMode(turnOn: boolean) {
			if (turnOn) {
				document.documentElement.requestFullscreen();
			} else {
				document.exitFullscreen();
			}
		}

	}
}
