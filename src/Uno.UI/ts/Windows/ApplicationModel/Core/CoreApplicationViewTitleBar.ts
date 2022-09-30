
namespace Windows.ApplicationModel.Core {

	export class CoreApplicationViewTitleBar {
		private static dispatchLayoutMetricsChanged: () => number;

		public static isExtendedIntoTitleBar(): boolean {
			if (navigator.windowControlsOverlay) {
				return navigator.windowControlsOverlay.visible;
			}
			
			return false;
		}

		public static getLeftInset(): number {
			if (CoreApplicationViewTitleBar.isExtendedIntoTitleBar()) {
				return navigator.windowControlsOverlay.getTitlebarAreaRect().left;
			}

			return 0;
		}

		public static getRightInset(): number {
			if (CoreApplicationViewTitleBar.isExtendedIntoTitleBar()) {
				const rect = navigator.windowControlsOverlay.getTitlebarAreaRect();				
				return window.outerWidth - rect.width - rect.left;
			}

			return 0;
		}

		public static getHeight(): number {
			if (CoreApplicationViewTitleBar.isExtendedIntoTitleBar()) {
				return navigator.windowControlsOverlay.getTitlebarAreaRect().height;
			}

			return 0;
		}

		public static startLayoutMetricsChanged() {
			if (!navigator.windowControlsOverlay) {
				return;
			}

			if (!CoreApplicationViewTitleBar.dispatchLayoutMetricsChanged) {
				CoreApplicationViewTitleBar.dispatchLayoutMetricsChanged = (<any>Module).mono_bind_static_method(
					"[Uno] Windows.ApplicationModel.Core.CoreApplicationViewTitleBar:DispatchLayoutMetricsChanged");
			}

			navigator.windowControlsOverlay.addEventListener("geometrychange", CoreApplicationViewTitleBar.OnGeometryChange);
		}
		
		private static OnGeometryChange() {
			CoreApplicationViewTitleBar.dispatchLayoutMetricsChanged();
		}
	
		public static stopLayoutMetricsChanged() {
			if (!navigator.windowControlsOverlay) {
				return;
			}

			navigator.windowControlsOverlay.removeEventListener("geometrychange", CoreApplicationViewTitleBar.OnGeometryChange);
		}
	}
}
