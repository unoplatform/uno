namespace Uno.Helpers.Theming {

	export class SystemThemeHelper {
		private static dispatchThemeChange: () => (void | Promise<void>);

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
	}
}
