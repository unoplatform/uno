namespace Windows.Phone.Devices.Notification {

	export class VibrationDevice {
		public static vibrate(duration: number): boolean {
			return window.navigator.vibrate(duration);
		}
	}
}
