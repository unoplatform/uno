namespace Uno.Helpers.Theming {

	export class SystemThemeHelper {
		private static dispatchThemeChange: () => (void | Promise<void>);
		private static dispatchHighContrastChange: () => (void | Promise<void>);

		public static getSystemTheme(): string {
			if (window.matchMedia) {
				if (window.matchMedia("(prefers-color-scheme: dark)").matches) {
					return SystemTheme.Dark;
				}
				if (window.matchMedia("(prefers-color-scheme: light)").matches) {
					return SystemTheme.Light;
				}
			}
			return null;
		}

		public static getHighContrast(): boolean {
			return window.matchMedia?.("(prefers-contrast: more)")?.matches === true
				|| window.matchMedia?.("(forced-colors: active)")?.matches === true;
		}

		public static observeSystemTheme() {
			if (!SystemThemeHelper.dispatchThemeChange) {
				if ((<any>globalThis).Uno.UI.Runtime.Skia.WebAssemblyThreading.isThreadingEnabled()) {
					SystemThemeHelper.dispatchThemeChange = (<any>globalThis).DotnetExports.Uno.Uno.Helpers.Theming.SystemThemeHelper.DispatchSystemThemeChangeAsync;
				} else {
					SystemThemeHelper.dispatchThemeChange = (<any>globalThis).DotnetExports.Uno.Uno.Helpers.Theming.SystemThemeHelper.DispatchSystemThemeChange;
				}
			}

			if (window.matchMedia) {
				window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", () => {
					SystemThemeHelper.dispatchThemeChange();
				});
			}
		}

		public static observeHighContrast() {
			if (!SystemThemeHelper.dispatchHighContrastChange) {
				if ((<any>globalThis).Uno.UI.Runtime.Skia.WebAssemblyThreading.isThreadingEnabled()) {
					SystemThemeHelper.dispatchHighContrastChange = (<any>globalThis).DotnetExports.Uno.Uno.Helpers.Theming.SystemThemeHelper.DispatchHighContrastChangeAsync;
				} else {
					SystemThemeHelper.dispatchHighContrastChange = (<any>globalThis).DotnetExports.Uno.Uno.Helpers.Theming.SystemThemeHelper.DispatchHighContrastChange;
				}
			}

			if (window.matchMedia) {
				window.matchMedia("(prefers-contrast: more)").addEventListener("change", () => {
					SystemThemeHelper.dispatchHighContrastChange();
				});
				window.matchMedia("(forced-colors: active)").addEventListener("change", () => {
					SystemThemeHelper.dispatchHighContrastChange();
				});
			}
		}
	}
}
