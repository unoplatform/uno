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

	}
}
