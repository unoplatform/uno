namespace Uno.UI.Runtime.Skia {
	export class InputPaneExtension {
		private static _exports: any;
		private static _instance: InputPaneExtension | null = null;
		private _managedInstance: any;
		private _lastViewportHeight: number = 0;
		private _isKeyboardVisible: boolean = false;

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

			// Consider keyboard visible if it occludes more than 100px
			// This helps avoid false positives from small browser UI changes
			const isKeyboardVisible = keyboardHeight > 100;

			if (isKeyboardVisible !== this._isKeyboardVisible || 
				Math.abs(viewportHeight - this._lastViewportHeight) > 1) {
				
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
			const isKeyboardVisible = heightDiff > 100;

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
