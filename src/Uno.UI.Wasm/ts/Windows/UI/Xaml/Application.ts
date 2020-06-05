namespace Windows.UI.Xaml {

	export class Application {
		private static dispatchThemeChange: () => number;

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
			if (!this.dispatchThemeChange) {
				this.dispatchThemeChange = (<any>Module).mono_bind_static_method("[Uno] Windows.UI.Xaml.Application:DispatchSystemThemeChange");
			}

			if (window.matchMedia)
			{
				window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", () => {
					Application.dispatchThemeChange();
				});
			}
		}
    }
}
