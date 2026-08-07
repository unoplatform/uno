namespace Windows.UI.Core {

	export class SystemNavigationManager {
		private static _current: SystemNavigationManager;

		public static get current(): SystemNavigationManager {
			if (!this._current) {
				this._current = new SystemNavigationManager();
			}
			return this._current;
		}

		private _isEnabled: boolean;

		constructor() {
			var that = this;
			var dispatchBackRequest = (<any>globalThis).DotnetExports.Uno.Windows.UI.Core.SystemNavigationManager.DispatchBackRequest;

			window.history.replaceState(0, document.title, null);
			window.addEventListener("popstate", function (evt) {
				if (that._isEnabled) {
					if (evt.state === 0) {
						// Push something in the stack only if we know that we reached the first page.
						// There is no way to track our location in the stack, so we use indexes (in the 'state').
						window.history.pushState(1, document.title, null);
					}
					dispatchBackRequest();
				} else if (evt.state === 1) {
					// The manager is disabled, but the user requested to navigate forward to our dummy entry,
					// but we prefer to keep this dummy entry in the forward stack (is more prompt to be cleared by the browser,
					// and as it's less commonly used it should be less annoying for the user)
					window.history.back();
				}
			});
		}

		public enable(): void {
			if (this._isEnabled) {
				return;
			}

			// Clear the back stack, so the only items will be ours (and we won't have any remaining forward item)
			this.clearStack();
			window.history.pushState(1, document.title, null);

			// Then set the enabled flag so the handler will begin its work
			this._isEnabled = true;
		}

		public disable(): void {
			if (!this._isEnabled) {
				return;
			}

			// Disable the handler, then clear the history
			// Note: As a side effect, the forward button will be enabled :(
			this._isEnabled = false;
			this.clearStack();
		}

		private clearStack() {
			// There is no way to determine our position in the stack, so we only navigate back if we determine that
			// we are currently on our dummy target page.
			if (window.history.state === 1) {
				window.history.back();
			}
			window.history.replaceState(0, document.title, null);
		}
	}
}
