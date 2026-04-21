namespace Uno.UI.Runtime.Skia {

	export class WebAssemblyWindowWrapper {
		private containerElement: HTMLDivElement;
		private canvasElement: HTMLCanvasElement;
		private onResize: any;
		private onInputPaneChanged: any;
		private owner: any;
		private _isKeyboardShowing: boolean = false;
		private _lastLayoutWidth: number = 0;
		private _lastLayoutHeight: number = 0;
		private static readonly unoPersistentLoaderClassName = "uno-persistent-loader";
		private static readonly loadingElementId = "uno-loading";
		private static readonly unoKeepLoaderClassName = "uno-keep-loader";

		private static assemblyExports: any;

		private static activeInstances: { [id: string]: WebAssemblyWindowWrapper } = {};

		private constructor(owner: any) {
			this.owner = owner;
		}

		public static getAssemblyExports(): any {
			return WebAssemblyWindowWrapper.assemblyExports;
		}

		public static async initialize(owner: any) {
			const instance = new WebAssemblyWindowWrapper(owner);
			await instance.build();
			WebAssemblyWindowWrapper.activeInstances[owner] = instance;
		}

		public static persistBootstrapperLoader() {
			let bootstrapperLoaders = document.getElementsByClassName(WebAssemblyWindowWrapper.unoPersistentLoaderClassName);
			if (bootstrapperLoaders.length > 0) {
				let bootstrapperLoader = bootstrapperLoaders[0] as HTMLElement;
				bootstrapperLoader.classList.add(WebAssemblyWindowWrapper.unoKeepLoaderClassName);
			}
		}

		private async build() {
			WebAssemblyWindowWrapper.assemblyExports = await (<any>window).Module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser");
			this.onResize = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.OnResize;
			this.onInputPaneChanged = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.OnInputPaneChanged;

			this.containerElement = (document.getElementById("uno-body") as HTMLDivElement);

			if (!this.containerElement) {
				// If not found, we simply create a new one.
				this.containerElement = document.createElement("div");
				this.containerElement.id = "uno-root";

				document.body.appendChild(this.containerElement);
			}

			this.canvasElement = document.createElement("canvas");
			this.canvasElement.id = "uno-canvas";
			this.canvasElement.setAttribute("aria-hidden", "true");
			this.containerElement.appendChild(this.canvasElement);

			Accessibility.setup();

			window.addEventListener("resize", x => this.resize());

			// Subscribe to visualViewport resize for soft keyboard detection on mobile browsers.
			// The visualViewport shrinks when the soft keyboard appears, while window.innerHeight
			// may or may not change depending on the browser.
			if (window.visualViewport) {
				window.visualViewport.addEventListener("resize", () => this.onVisualViewportResize());
			}

			window.addEventListener("contextmenu", x => {
				x.preventDefault();
			})

			this.resize();
		}

		public static removeLoading() {
			const element = document.getElementById(WebAssemblyWindowWrapper.loadingElementId);
			if (element) {
				element.parentElement.removeChild(element);
			}

			let bootstrapperLoaders = document.getElementsByClassName(WebAssemblyWindowWrapper.unoPersistentLoaderClassName);
			if (bootstrapperLoaders.length > 0) {
				let bootstrapperLoader = bootstrapperLoaders[0] as HTMLElement;
				bootstrapperLoader.parentElement.removeChild(bootstrapperLoader);
			}
		}

		public static getInstance(owner: any): WebAssemblyWindowWrapper {
			const instance = this.activeInstances[owner];
			if (!instance) {
				throw `WebAssemblyWindowWrapper for instance ${owner} not found.`;
			}
			return instance;
		}

		public static getContainerId(owner: any): string {
			return WebAssemblyWindowWrapper.getInstance(owner).containerElement.id;
		}

		public static getCanvasId(owner: any): string {
			return WebAssemblyWindowWrapper.getInstance(owner).canvasElement.id;
		}

		private resize() {
			var rect = document.documentElement.getBoundingClientRect();

			// When the soft keyboard is showing on mobile browsers, some browsers shrink
			// the layout viewport (triggering window.resize). We suppress this by reporting
			// the pre-keyboard layout size, preventing XAML from re-laying out (which would
			// cause flyouts to dismiss/reposition).
			if (this._isKeyboardShowing) {
				this.onResize(this.owner, this._lastLayoutWidth, this._lastLayoutHeight, globalThis.devicePixelRatio);
			} else {
				this._lastLayoutWidth = rect.width;
				this._lastLayoutHeight = rect.height;
				this.onResize(this.owner, rect.width, rect.height, globalThis.devicePixelRatio);
			}
		}

		// Dual-signal soft keyboard detection:
		// Signal 1: Our invisible text <input>/<textarea> is focused
		// Signal 2: visualViewport.height < window.innerHeight
		// When both are true, the soft keyboard is showing.
		private onVisualViewportResize() {
			const vv = window.visualViewport;
			if (!vv) {
				return;
			}

			const hasInputFocus =
				document.activeElement instanceof HTMLInputElement ||
				document.activeElement instanceof HTMLTextAreaElement;

			const keyboardHeight = window.innerHeight - vv.height;

			if (hasInputFocus && keyboardHeight > 0) {
				// Keyboard is showing
				if (!this._isKeyboardShowing) {
					this._isKeyboardShowing = true;
				}
				// Report occluded rect: x=0, y=top of keyboard, width=viewport width, height=keyboard height
				this.onInputPaneChanged(this.owner, 0, vv.height, vv.width, keyboardHeight);
			} else if (this._isKeyboardShowing) {
				// Keyboard is hiding
				this._isKeyboardShowing = false;
				this.onInputPaneChanged(this.owner, 0, 0, 0, 0);
			}
		}

		public static setCursor(cssCursor: string) {
			document.body.style.cursor = cssCursor;
		}

		public resizeWindow(width: number, height: number) {
			window.resizeTo(width, height);
		}

		public moveWindow(x: number, y: number) {
			window.moveTo(x, y);
		}
	}
}
