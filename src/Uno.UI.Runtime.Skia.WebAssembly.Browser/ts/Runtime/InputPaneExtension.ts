namespace Uno.UI.Runtime.Skia {
	export class InputPaneExtension {
		private static _exports: any;
		private static _instance: InputPaneExtension | null = null;
		private _managedInstance: any;
		private _lastViewportHeight: number = 0;
		private _isKeyboardVisible: boolean = false;

		// Threshold for considering keyboard visible (in pixels)
		private static readonly KEYBOARD_THRESHOLD_PX = 100;
		// Minimum viewport height change to trigger an update (in pixels)
		private static readonly MIN_HEIGHT_CHANGE_PX = 1;

		public static async initialize(managedInstance: any): Promise<void> {
			const module = <any>window.Module;
			if (InputPaneExtension._exports === undefined && module.getAssemblyExports !== undefined) {
				const browserExports = await module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser");
				InputPaneExtension._exports = browserExports.Uno.WinUI.Runtime.Skia.WebAssembly.InputPaneExtension;
			}

			if (InputPaneExtension._instance === null) {
				InputPaneExtension._instance = new InputPaneExtension(managedInstance);
			}
		}

		private constructor(managedInstance: any) {
			this._managedInstance = managedInstance;
			this.setupVisualViewportListeners();
		}

		private setupVisualViewportListeners(): void {
			// Use visualViewport API if available (modern browsers, mobile Safari, Chrome)
			if (window.visualViewport) {
				this._lastViewportHeight = window.visualViewport.height;

				window.visualViewport.addEventListener("resize", () => this.onVisualViewportResize());
				window.visualViewport.addEventListener("scroll", () => this.onVisualViewportScroll());
			} else {
				// Fallback for older browsers - track window resize
				this._lastViewportHeight = window.innerHeight;
				window.addEventListener("resize", () => this.onWindowResize());
			}
		}

		private onVisualViewportResize(): void {
			if (!window.visualViewport) {
				return;
			}

			const visualViewport = window.visualViewport;
			const viewportHeight = visualViewport.height;
			const windowHeight = window.innerHeight;

			// Calculate the keyboard height
			// The visual viewport height decreases when the keyboard appears
			const keyboardHeight = windowHeight - viewportHeight;

			// Consider keyboard visible if it occludes more than the threshold
			// This helps avoid false positives from small browser UI changes
			const isKeyboardVisible = keyboardHeight > InputPaneExtension.KEYBOARD_THRESHOLD_PX;

			if (isKeyboardVisible !== this._isKeyboardVisible || 
				Math.abs(viewportHeight - this._lastViewportHeight) > InputPaneExtension.MIN_HEIGHT_CHANGE_PX) {
				
				this._isKeyboardVisible = isKeyboardVisible;
				this._lastViewportHeight = viewportHeight;

				// Notify managed code about keyboard visibility change
				if (InputPaneExtension._exports) {
					InputPaneExtension._exports.OnKeyboardVisibilityChanged(
						isKeyboardVisible,
						isKeyboardVisible ? keyboardHeight : 0
					);
				}
			}
		}

		private onVisualViewportScroll(): void {
			// When the virtual keyboard appears on some devices,
			// the viewport may scroll. We should handle this as well.
			this.onVisualViewportResize();
		}

		private onWindowResize(): void {
			// Fallback handler for browsers without visualViewport API
			const currentHeight = window.innerHeight;
			
			if (this._lastViewportHeight === 0) {
				this._lastViewportHeight = currentHeight;
				return;
			}

			const heightDiff = this._lastViewportHeight - currentHeight;
			const isKeyboardVisible = heightDiff > InputPaneExtension.KEYBOARD_THRESHOLD_PX;

			if (isKeyboardVisible !== this._isKeyboardVisible) {
				this._isKeyboardVisible = isKeyboardVisible;
				
				// Update last height only when keyboard state changes
				if (!isKeyboardVisible) {
					this._lastViewportHeight = currentHeight;
				}

				if (InputPaneExtension._exports) {
					InputPaneExtension._exports.OnKeyboardVisibilityChanged(
						isKeyboardVisible,
						isKeyboardVisible ? heightDiff : 0
					);
				}
			}
		}

		public static hideKeyboard(): void {
			// Blur the active element to hide the keyboard
			if (document.activeElement instanceof HTMLElement) {
				document.activeElement.blur();
			}
		}
	}
}

// Expose InputPaneExtension methods to globalThis for C# JSImport access
if (globalThis.Uno === undefined) {
	globalThis.Uno = {} as any;
}
if (globalThis.Uno.UI === undefined) {
	globalThis.Uno.UI = {} as any;
}
if (globalThis.Uno.UI.Runtime === undefined) {
	globalThis.Uno.UI.Runtime = {} as any;
}
if (globalThis.Uno.UI.Runtime.Skia === undefined) {
	globalThis.Uno.UI.Runtime.Skia = {} as any;
}

(globalThis.Uno.UI.Runtime.Skia as any).InputPaneExtension = {
	initialize: Uno.UI.Runtime.Skia.InputPaneExtension.initialize,
	hideKeyboard: Uno.UI.Runtime.Skia.InputPaneExtension.hideKeyboard
};
