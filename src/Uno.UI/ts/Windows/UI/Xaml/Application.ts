namespace Microsoft.UI.Xaml {

	export class Application {
		private static dispatchThemeChange: () => number;
		private static dispatchVisibilityChange: (isVisible: boolean) => number;

		public static getDefaultSystemTheme(): string {
			if (window.matchMedia) {
				if (window.matchMedia("(prefers-color-scheme: dark)").matches) {
					return ApplicationTheme.Dark;
				}
				if (window.matchMedia("(prefers-color-scheme: light)").matches) {
					return ApplicationTheme.Light;
				}
			}
			return null;
		}

		public static observeSystemTheme() {
			if (!Application.dispatchThemeChange) {
				Application.dispatchThemeChange = (<any>Module).mono_bind_static_method("[Uno.UI] Microsoft.UI.Xaml.Application:DispatchSystemThemeChange");
			}

			if (window.matchMedia) {
				window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", () => {
					Application.dispatchThemeChange();
				});
			}
		}

		public static observeVisibility() {
			if (!Application.dispatchVisibilityChange) {
				Application.dispatchVisibilityChange = (<any>Module).mono_bind_static_method("[Uno.UI] Microsoft.UI.Xaml.Application:DispatchVisibilityChange");
			}

			if (document.onvisibilitychange !== undefined) {
				document.addEventListener("visibilitychange", () => {
					Application.dispatchVisibilityChange(document.visibilityState == "visible");
				});
			}

			if (window.onpagehide !== undefined) {
				window.addEventListener("pagehide", () => {
					Application.dispatchVisibilityChange(false);
				});
			}

			if (window.onpageshow !== undefined) {
				window.addEventListener("pageshow", () => {
					Application.dispatchVisibilityChange(true);
				});
			}
		}
	}
}
