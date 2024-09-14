namespace Microsoft.UI.Xaml {

	export class Application {
		private static dispatchVisibilityChange: (isVisible: boolean) => number;

		public static observeVisibility() {
			if (!Application.dispatchVisibilityChange) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					Application.dispatchVisibilityChange = (<any>globalThis).DotnetExports.UnoUI.Microsoft.UI.Xaml.Application.DispatchVisibilityChange;
				} else {
					throw `Unable to find dotnet exports`;
				}
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
