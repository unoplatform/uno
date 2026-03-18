namespace Uno.Helpers.Theming {

	export class AccentColorHelper {
		private static dispatchAccentColorChange: () => number;
		private static _probeElement: HTMLElement;

		/**
		 * Reads the CSS AccentColor system color keyword and returns it as an "#RRGGBB" hex string,
		 * or null if the browser does not support it.
		 */
		public static getAccentColor(): string {
			// Check if the browser supports the AccentColor CSS keyword
			if (typeof CSS === "undefined" || !CSS.supports || !CSS.supports("color", "AccentColor")) {
				return null;
			}

			const probe = AccentColorHelper.getProbeElement();
			if (!probe) {
				return null;
			}

			probe.style.color = "AccentColor";
			document.body.appendChild(probe);
			const computed = window.getComputedStyle(probe).color;
			document.body.removeChild(probe);

			if (!computed) {
				return null;
			}

			// getComputedStyle returns "rgb(r, g, b)" or "rgba(r, g, b, a)".
			const match = computed.match(/^rgba?\((\d+),\s*(\d+),\s*(\d+)/);
			if (!match) {
				return null;
			}

			const r = parseInt(match[1], 10);
			const g = parseInt(match[2], 10);
			const b = parseInt(match[3], 10);

			return AccentColorHelper.toHex(r, g, b);
		}

		public static observeAccentColor() {
			if (!AccentColorHelper.dispatchAccentColorChange) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					AccentColorHelper.dispatchAccentColorChange = (<any>globalThis).DotnetExports.Uno.Uno.Helpers.Theming.AccentColorHelper.DispatchAccentColorChange;
				} else {
					throw `AccentColorHelper: Unable to find dotnet exports`;
				}
			}

			// There is no direct CSS event for accent color change.
			// However, on some platforms the accent color may change with the theme,
			// which is already handled by SystemThemeHelper's prefers-color-scheme listener.
			// We subscribe to the same media query as a best-effort approach.
			if (window.matchMedia) {
				window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", () => {
					AccentColorHelper.dispatchAccentColorChange();
				});
			}
		}

		private static getProbeElement(): HTMLElement {
			if (!AccentColorHelper._probeElement) {
				if (typeof document === "undefined") {
					return null;
				}
				AccentColorHelper._probeElement = document.createElement("div");
				AccentColorHelper._probeElement.style.display = "none";
			}
			return AccentColorHelper._probeElement;
		}

		private static toHex(r: number, g: number, b: number): string {
			return "#" +
				("0" + r.toString(16)).slice(-2).toUpperCase() +
				("0" + g.toString(16)).slice(-2).toUpperCase() +
				("0" + b.toString(16)).slice(-2).toUpperCase();
		}
	}
}
