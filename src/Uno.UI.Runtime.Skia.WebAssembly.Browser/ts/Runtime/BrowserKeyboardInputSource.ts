namespace Uno.UI.Runtime.Skia {
	export class BrowserKeyboardInputSource {
		private static _exports: any;
		private static _source: any;
		// True once the user has Tab'd past the "Enable Accessibility" button into the XAML app.
		// Without a11y enabled, document.activeElement stays on the button (no FocusSynchronizer),
		// so we can't infer in-app focus from the DOM. This flag tracks it instead so subsequent
		// Shift+Tab presses are routed to the managed FocusManager rather than exiting the app.
		private static _hasTabbedIntoApp: boolean = false;

		public static initialize(inputSource: any): any {
			if (BrowserKeyboardInputSource._exports == undefined) {
				const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();
				BrowserKeyboardInputSource._exports = browserExports.Uno.UI.Runtime.Skia.BrowserKeyboardInputSource;
			}

			BrowserKeyboardInputSource._source = inputSource;
			BrowserKeyboardInputSource.subscribeKeyboardEvents();
		}

		private static subscribeKeyboardEvents() {
			document.addEventListener("keydown", BrowserKeyboardInputSource.onKeyboardEvent);
			document.addEventListener("keyup", BrowserKeyboardInputSource.onKeyboardEvent);
		}

		private static onKeyboardEvent(evt: KeyboardEvent): void {
			// When the Enable Accessibility button is still in the DOM,
			// allow Tab to reach it via native browser focus navigation
			// before the managed FocusManager intercepts it.
			if (evt.key === "Tab" && Accessibility.isEnableAccessibilityButtonActive()) {
				const active = document.activeElement;
				const isOnButton = active instanceof HTMLElement && active.id === "uno-enable-accessibility";

				if (evt.type === "keydown") {
					// No element focused yet — let browser Tab to the prepended button
					const isUnfocused = !active || active === document.body || active === document.documentElement;
					if (isUnfocused) {
						return;
					}

					// Button focused + Shift+Tab BEFORE user has entered the XAML app — let
					// browser move focus back to the previous browser-level focusable (e.g.
					// the address bar). Once focus has entered the app, Shift+Tab must reach
					// the managed FocusManager so the user can navigate backward in XAML.
					if (isOnButton && evt.shiftKey && !BrowserKeyboardInputSource._hasTabbedIntoApp) {
						return;
					}

					// Button focused + Tab (no shift) — fall through to managed FocusManager.
					// This is the moment XAML takes focus from the button, so remember it
					// to keep future Shift+Tab presses routed through managed.
					if (isOnButton && !evt.shiftKey) {
						BrowserKeyboardInputSource._hasTabbedIntoApp = true;
					}
				}

				// Don't route keyup to managed code while on the button
				if (evt.type === "keyup" && isOnButton) {
					return;
				}
			}

			let result = BrowserKeyboardInputSource._exports.OnNativeKeyboardEvent(
				BrowserKeyboardInputSource._source,
				evt.type == "keydown",
				evt.ctrlKey,
				evt.shiftKey,
				evt.altKey,
				evt.metaKey,
				evt.code,
				evt.key
			);

			if (result == HtmlEventDispatchResult.PreventDefault) {
				evt.preventDefault();
			}
		}
	}
}
