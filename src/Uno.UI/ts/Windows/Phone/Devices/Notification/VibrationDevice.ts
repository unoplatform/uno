interface Navigator {
	webkitVibrate(pattern: number | number[]): boolean;
	mozVibrate(pattern: number | number[]): boolean;
	msVibrate(pattern: number | number[]): boolean;
}

namespace Windows.Phone.Devices.Notification {

	export class VibrationDevice {
		public static initialize(): boolean {
			navigator.vibrate = navigator.vibrate || navigator.webkitVibrate || navigator.mozVibrate || navigator.msVibrate;
			if (navigator.vibrate) {
				return true;
			}
			return false;
		}

		public static vibrate(duration: number): boolean {
			return window.navigator.vibrate(duration);
		}
	}
}
