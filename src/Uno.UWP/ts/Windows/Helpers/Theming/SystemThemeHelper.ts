namespace Uno.Helpers.Theming {

	export class SystemThemeHelper {
		private static dispatchThemeChange: () => number;
		private static dispatchHighContrastChange: () => number;

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
			if (window.matchMedia) {
				return window.matchMedia("(prefers-contrast: more)").matches ||
					window.matchMedia("(forced-colors: active)").matches;
			}
			return false;
		}

		public static observeSystemTheme() {
			if (!SystemThemeHelper.dispatchThemeChange) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					SystemThemeHelper.dispatchThemeChange = (<any>globalThis).DotnetExports.Uno.Uno.Helpers.Theming.SystemThemeHelper.DispatchSystemThemeChange;
				} else {
					throw `SystemThemeHelper: Unable to find dotnet exports`;
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
				if ((<any>globalThis).DotnetExports !== undefined) {
					SystemThemeHelper.dispatchHighContrastChange = (<any>globalThis).DotnetExports.Uno.Uno.Helpers.Theming.SystemThemeHelper.DispatchHighContrastChange;
				} else {
					throw `SystemThemeHelper: Unable to find dotnet exports for high contrast`;
				}
			}

			if (window.matchMedia) {
				window.matchMedia('(prefers-contrast: more)').addEventListener("change", () => {
					SystemThemeHelper.dispatchHighContrastChange();
				});
				window.matchMedia('(forced-colors: active)').addEventListener("change", () => {
					SystemThemeHelper.dispatchHighContrastChange();
				});
			}
		}
	}
}

