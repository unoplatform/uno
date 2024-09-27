namespace Uno.Helpers.Theming {

	export class SystemThemeHelper {
		private static dispatchThemeChange: () => number;

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
				if ((<any>globalThis).DotnetExports !== undefined) {
					SystemThemeHelper.dispatchThemeChange = (<any>globalThis).DotnetExports.Uno.Uno.Helpers.Theming.SystemThemeHelper.DispatchSystemThemeChange;
				} else {
					throw `Unable to find dotnet exports`;
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
