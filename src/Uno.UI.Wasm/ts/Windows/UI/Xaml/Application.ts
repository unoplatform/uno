namespace Windows.UI.Xaml {

    export class Application {
        public static getDefaultSystemTheme() : string {
            if (window.matchMedia("(prefers-color-scheme: dark)").matches) {
                return ApplicationTheme.Dark;
            }
            if (window.matchMedia("(prefers-color-scheme: light)").matches) {
                return ApplicationTheme.Light;
            }
			//if none matches or not supported
            return ApplicationTheme.Light;
        }
    }
}
