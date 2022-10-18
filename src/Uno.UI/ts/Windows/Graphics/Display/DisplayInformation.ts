namespace Windows.Graphics.Display {
	enum DisplayOrientations {
		None = 0,
		Landscape = 1,
		Portrait = 2,
		LandscapeFlipped = 4,
		PortraitFlipped = 8,
	}

	export class DisplayInformation {
		private static readonly DpiCheckInterval = 1000;

		private static lastDpi: number;
		private static dpiWatcher: number;

		private static dispatchOrientationChanged: (type: string) => number;
		private static dispatchDpiChanged: (dpi: number) => number;

		private static lockingSupported: boolean | null;

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

		public static async setOrientationAsync(uwpOrientations: DisplayOrientations): Promise<void> {
			const oldOrientation = screen.orientation.type;
			const orientations = DisplayInformation.parseUwpOrientation(uwpOrientations);

			if (orientations.includes(oldOrientation)) {
				return;
			}

			// Setting the orientation requires briefly changing the device to fullscreen.
			// This causes a glitch, which is unnecessary for devices which does not support
			// setting the orientation, such as most desktop browsers.
			// We therefore attempt to check for support, and do nothing if the feature is
			// unavailable.
			if (DisplayInformation.lockingSupported == null) {
				try {
					await screen.orientation.lock(oldOrientation);
					DisplayInformation.lockingSupported = true;
				} catch (e) {
					if (e instanceof DOMException && e.name === "NotSupportedError") {
						DisplayInformation.lockingSupported = false;
						console.log("This browser does not support setting the orientation.")
					} else {
						// On most mobile devices we should reach this line.
						DisplayInformation.lockingSupported = true;
					}
				}
			}

			if (!DisplayInformation.lockingSupported) {
				return;
			}

			const wasFullscreen = document.fullscreenElement != null;

			if (!wasFullscreen) {
				await document.body.requestFullscreen();
			}

			for (const orientation of orientations) {
				try {
					// On success, screen.orientation should fire the 'change' event.
					await screen.orientation.lock(orientation);
					break;
				} catch (e) {
					// Absorb all errors to ensure that the exitFullscreen block below is called.
					console.log(`Failed to set the screen orientation to '${orientation}': ${e}`);
				}
			}

			if (!wasFullscreen) {
				await document.exitFullscreen();
			}
		}

		private static parseUwpOrientation(uwpOrientations: DisplayOrientations): OrientationLockType[] {
			const orientations: OrientationLockType[] = [];

			if (uwpOrientations & DisplayOrientations.Landscape) {
				orientations.push("landscape-primary");
			}

			if (uwpOrientations & DisplayOrientations.Portrait) {
				orientations.push("portrait-primary");
			}

			if (uwpOrientations & DisplayOrientations.LandscapeFlipped) {
				orientations.push("landscape-secondary");
			}

			if (uwpOrientations & DisplayOrientations.PortraitFlipped) {
				orientations.push("portrait-secondary");
			}

			return orientations;
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
