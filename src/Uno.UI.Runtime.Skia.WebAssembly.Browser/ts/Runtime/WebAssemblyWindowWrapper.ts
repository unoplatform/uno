namespace Uno.UI.Runtime.Skia {

	export class WebAssemblyWindowWrapper {
		private containerElement: HTMLDivElement;
		private canvasElement: HTMLCanvasElement;
		private onResize: any;
		private onViewportOcclusionChanged: any;
		private owner: any;
		private lastReportedOcclusion: number = -1;
		private viewportResizeObserver: ResizeObserver | undefined;
		private lastObservedViewportSize: { width: number, height: number } | undefined;
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

			if (WebAssemblyThreading.isThreadingEnabled()) {
				this.onResize = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.OnResizeAsync;
			}
			else {
				this.onResize = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.OnResize;
			}

			this.onViewportOcclusionChanged = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.OnViewportOcclusionChanged;

			this.containerElement = (document.getElementById("uno-body") as HTMLDivElement);

			if (!this.containerElement) {
				// If not found, we simply create a new one.
				this.containerElement = document.createElement("div");
				this.containerElement.id = "uno-root";

				document.body.appendChild(this.containerElement);
			}

			this.canvasElement = document.createElement("canvas");
			this.canvasElement.id = UnoDomIds.canvas;
			this.canvasElement.setAttribute("aria-hidden", "true");
			this.containerElement.appendChild(this.canvasElement);

			await Accessibility.setup();

<<<<<<< HEAD
			const resizeObserver = new ResizeObserver(() => this.resize());
			resizeObserver.observe(document.documentElement);
=======
			window.addEventListener("resize", x => this.resize());
			this.observeViewportSize();
>>>>>>> origin/master

			window.addEventListener("contextmenu", x => {
				x.preventDefault();
			})

			// The on-screen keyboard shrinks the visual viewport without firing window "resize",
			// so track it separately to report keyboard occlusion to the InputPane (issue 3).
			if (window.visualViewport) {
				window.visualViewport.addEventListener("resize", () => this.reportKeyboardOcclusion());
				window.visualViewport.addEventListener("scroll", () => this.reportKeyboardOcclusion());
			}

			this.resize();
		}

		// Some mobile browsers (Chrome on iPad) can raise window "resize" before the layout
		// viewport has reflowed to the new orientation size, without raising a corrective
		// event afterward — leaving the Skia surface sized from a stale documentElement rect.
		// Observe the element resize() measures from and re-dispatch "resize" once it reports
		// its settled dimensions (ResizeObserver fires post-layout, so no polling is needed).
		private observeViewportSize() {
			if (typeof ResizeObserver === "undefined") {
				console.debug("ResizeObserver is not available; the viewport settle correction is disabled.");
				return;
			}

			const rootElement = document.documentElement;
			const initialRect = rootElement.getBoundingClientRect();
			this.lastObservedViewportSize = { width: initialRect.width, height: initialRect.height };
			this.viewportResizeObserver = new ResizeObserver(() => {
				const rect = rootElement.getBoundingClientRect();
				const lastSize = this.lastObservedViewportSize;

				if (lastSize && rect.width === lastSize.width && rect.height === lastSize.height) {
					return;
				}

				this.lastObservedViewportSize = { width: rect.width, height: rect.height };

				// Dispatch globally rather than calling this.resize(): other listeners size from
				// this event too (BrowserRenderer sizes the canvas), and app code is owed the
				// corrective resize the browser never fired.
				window.dispatchEvent(new Event("resize"));
			});
			this.viewportResizeObserver.observe(rootElement);
		}

		// Reports how much of the viewport the on-screen keyboard occludes, in the same CSS pixels
		// as the window bounds. Gated on the invisible text input (#uno-input) being the active
		// element: the soft keyboard is only up during text entry, so viewport changes at any other
		// time (e.g. the mobile address bar collapsing) are deliberately reported as no occlusion.
		private reportKeyboardOcclusion() {
			const viewport = window.visualViewport;
			if (!viewport || !this.onViewportOcclusionChanged) {
				return;
			}

			// Check focus before touching layout: visualViewport scroll/resize fires per frame during
			// momentum scroll and keyboard animation, and getBoundingClientRect forces a reflow. When
			// no text input is focused there is no keyboard, so skip the reflow entirely and report a
			// single zero only if the last report was non-zero.
			if (document.activeElement?.id !== UnoDomIds.input) {
				if (this.lastReportedOcclusion !== 0) {
					this.lastReportedOcclusion = 0;
					this.onViewportOcclusionChanged(this.owner, 0, 0, 0);
				}
				return;
			}

			const layout = document.documentElement.getBoundingClientRect();
			const occludedHeight = Math.max(0, layout.height - viewport.height - viewport.offsetTop);
			if (occludedHeight === this.lastReportedOcclusion) {
				return;
			}

			this.lastReportedOcclusion = occludedHeight;
			this.onViewportOcclusionChanged(this.owner, layout.width, layout.height, occludedHeight);
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
			// Measures the same element observeViewportSize() watches — keep the two in sync.
			var rect = document.documentElement.getBoundingClientRect();
			this.lastObservedViewportSize = { width: rect.width, height: rect.height };
			this.onResize(this.owner, rect.width, rect.height, globalThis.devicePixelRatio);
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
