namespace Windows.Graphics.Display {

	export class DisplayInformation {
		private static readonly DpiCheckInterval = 1000;

		private static lastDpi: number;
		private static dpiWatcher: number;

		private static dispatchOrientationChanged: (type: string) => number;
		private static dispatchDpiChanged: (dpi: number) => number;

		public static startOrientationChanged() {
			window.screen.orientation.addEventListener("change", DisplayInformation.onOrientationChange);
		}

		public static stopOrientationChanged() {
			window.screen.orientation.removeEventListener("change", DisplayInformation.onOrientationChange);
		}

		public static startDpiChanged() {
			// DPI can be observed using matchMedia query, but only for certain breakpoints
			// for accurate observation, we use polling

			DisplayInformation.lastDpi = window.devicePixelRatio;

			// start polling the devicePixel
			DisplayInformation.dpiWatcher = window.setInterval(
				DisplayInformation.updateDpi, 
				DisplayInformation.DpiCheckInterval);
		}

		public static stopDpiChanged() {
			window.clearInterval(DisplayInformation.dpiWatcher);
		}

		private static updateDpi() {
			const currentDpi = window.devicePixelRatio;
			if (Math.abs(DisplayInformation.lastDpi - currentDpi) > 0.001) {				
				if (DisplayInformation.dispatchDpiChanged == null) {
					DisplayInformation.dispatchDpiChanged =
						(<any>Module).mono_bind_static_method(
							"[Uno] Windows.Graphics.Display.DisplayInformation:DispatchDpiChanged");
				}
				DisplayInformation.dispatchDpiChanged(currentDpi);
			}
			DisplayInformation.lastDpi = currentDpi;
		}

		private static onOrientationChange() {
			if (DisplayInformation.dispatchOrientationChanged == null) {
				DisplayInformation.dispatchOrientationChanged =
					(<any>Module).mono_bind_static_method(
						"[Uno] Windows.Graphics.Display.DisplayInformation:DispatchOrientationChanged");
			}
			DisplayInformation.dispatchOrientationChanged(window.screen.orientation.type);
		}
	}
}
