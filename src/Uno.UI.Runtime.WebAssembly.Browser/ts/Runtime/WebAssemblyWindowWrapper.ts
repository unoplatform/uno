namespace Uno.UI.Runtime {

	export class WebAssemblyWindowWrapper {
		private containerElement: HTMLDivElement;
		private canvasElement: HTMLCanvasElement;
		private onResize: any;
		private onViewportOcclusionChanged: any;
		private onWindowFocusChanged: any;
		private onDocumentVisibilityChanged: any;
		private owner: any;
		private lastReportedOcclusion: number = -1;
		private isKeyboardOcclusionReportScheduled = false;
		private isKeyboardBlurPending = false;
		private viewportResizeObserver: ResizeObserver | undefined;
		private lastObservedViewportSize: { width: number, height: number } | undefined;
		// The size last reported as the window's, which the layout viewport can differ from around the on-screen
		// keyboard (see updateWindowSize).
		private windowSize: { width: number, height: number } | undefined;
		private isHeightHeld = false;
		private isLayoutSizeForced = false;
		private heightReleaseTimer: ReturnType<typeof setTimeout> | undefined;
		private growthSettleTimer: ReturnType<typeof setTimeout> | undefined;
		private touchTextEntryEndedAt = -Infinity;
		// How long after a touch text entry ends its keyboard is still going away: a held height is released then, and
		// growths keep being settled until then.
		private static readonly textEntryEndDelay = 1000;
		// A growth during text entry is reported once it has lasted this long: browsers that hide their bottom
		// toolbar with the keyboard briefly grow the layout viewport when the keyboard hides (793 -> 845 -> 793 px,
		// back after about 130 ms, measured in Edge on Android).
		private static readonly growthSettleDelay = 300;
		private static lastPointerType: string | undefined;
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
			WebAssemblyWindowWrapper.assemblyExports = await (<any>window).Module.getAssemblyExports("Uno.UI.Runtime.WebAssembly.Browser");
			this.onResize = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.WebAssemblyWindowWrapper.OnResize;
			this.onViewportOcclusionChanged = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.WebAssemblyWindowWrapper.OnViewportOcclusionChanged;
			this.onWindowFocusChanged = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.WebAssemblyWindowWrapper.OnWindowFocusChanged;
			this.onDocumentVisibilityChanged = WebAssemblyWindowWrapper.assemblyExports.Uno.UI.Runtime.WebAssemblyWindowWrapper.OnDocumentVisibilityChanged;

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

			Accessibility.setup();

			window.addEventListener("resize", x => this.resize());
			this.observeViewportSize();

			// Recorded from startup on the window, ahead of the document listeners: the first TextBox to take focus can be
			// one that the tap on another control just brought up (a flyout opening on a button).
			window.addEventListener("pointerdown", ev => WebAssemblyWindowWrapper.lastPointerType = ev.pointerType, { capture: true });
			document.addEventListener("focusout", ev => {
				if ((ev.target as HTMLElement)?.id !== UnoDomIds.input) {
					return;
				}

				if (WebAssemblyWindowWrapper.isTouchInput()) {
					this.touchTextEntryEndedAt = performance.now();
				}

				if (this.isHeightHeld) {
					this.scheduleHeightRelease();
				}
			}, { capture: true });

			window.addEventListener("contextmenu", x => {
				x.preventDefault();
			})

			// The browser has no window activation; focus/blur and page visibility are the equivalents.
			window.addEventListener("focus", () => this.onWindowFocus());
			window.addEventListener("blur", () => this.onWindowBlur());
			document.addEventListener("visibilitychange", () => this.onVisibilityChange());

			// The on-screen keyboard shrinks the visual viewport without firing window "resize",
			// so track it separately to report keyboard occlusion to the InputPane (issue 3).
			if (window.visualViewport) {
				window.visualViewport.addEventListener("resize", () => this.scheduleKeyboardOcclusionReport());
				window.visualViewport.addEventListener("scroll", () => this.scheduleKeyboardOcclusionReport());
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

		// iOS WebKit blurs the window when the on-screen keyboard is dismissed and focuses it again on the next tap.
		// That isn't a deactivation: reporting it would light-dismiss the open popups.
		private onWindowBlur() {
			if (BrowserInputHelper.isIOS && this.lastReportedOcclusion > 0) {
				this.isKeyboardBlurPending = true;
				return;
			}

			this.isKeyboardBlurPending = false;
			this.onWindowFocusChanged(this.owner, false);
		}

		private onWindowFocus() {
			if (this.isKeyboardBlurPending) {
				this.isKeyboardBlurPending = false;
				return;
			}

			this.onWindowFocusChanged(this.owner, true);
		}

		private onVisibilityChange() {
			// Hiding the page after a keyboard blur does deactivate it.
			if (document.hidden && this.isKeyboardBlurPending) {
				this.isKeyboardBlurPending = false;
				this.onWindowFocusChanged(this.owner, false);
			}

			this.onDocumentVisibilityChanged(this.owner, !document.hidden);
		}

		// The visual viewport fires "resize" and "scroll" together, and on every frame while the keyboard slides: report once per frame.
		private scheduleKeyboardOcclusionReport() {
			if (this.isKeyboardOcclusionReportScheduled) {
				return;
			}

			this.isKeyboardOcclusionReportScheduled = true;
			requestAnimationFrame(() => {
				this.isKeyboardOcclusionReportScheduled = false;
				this.reportKeyboardOcclusion();
			});
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
					this.onKeyboardHidden();
				}
				return;
			}

			// Measured from the window size rather than the layout viewport: when the keyboard shrinks the layout
			// viewport too, the part it took away is occluded as well.
			const layout = this.windowSize ?? document.documentElement.getBoundingClientRect();
			const occludedHeight = Math.max(0, layout.height - viewport.height - viewport.offsetTop);
			if (occludedHeight === this.lastReportedOcclusion) {
				return;
			}

			this.lastReportedOcclusion = occludedHeight;
			this.onViewportOcclusionChanged(this.owner, layout.width, layout.height, occludedHeight);
			if (occludedHeight === 0) {
				this.onKeyboardHidden();
			}
		}

		public static removeLoading() {
			const element = document.getElementById(WebAssemblyWindowWrapper.loadingElementId);
			if (element) {
				element.parentElement.removeChild(element);
			}

			let bootstrapperLoaders = document.getElementsByClassName(WebAssemblyWindowWrapper.unoPersistentLoaderClassName);
			if (bootstrapperLoaders.length > 0) {
				// Newer bootstrappers fade the loader out themselves; older ones don't expose dismissLoader.
				const dismissLoader = (globalThis as any).Uno?.WebAssembly?.Bootstrap?.Bootstrapper?.dismissLoader;
				if (typeof dismissLoader === "function") {
					dismissLoader();
				} else {
					let bootstrapperLoader = bootstrapperLoaders[0] as HTMLElement;
					bootstrapperLoader.parentElement.removeChild(bootstrapperLoader);
				}
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

		public static getSearchParams(): string {
			if (typeof URLSearchParams === "function") {
				return new URLSearchParams(window.location.search).toString();
			}
			else {
				const queryIndex = document.location.search.indexOf("?");

				if (queryIndex !== -1) {
					return document.location.search.substring(queryIndex + 1);
				}

				return "";
			}
		}

		private resize() {
			// Measures the same element observeViewportSize() watches — keep the two in sync.
			var rect = document.documentElement.getBoundingClientRect();
			this.lastObservedViewportSize = { width: rect.width, height: rect.height };
			const size = this.updateWindowSize(rect);
			this.onResize(this.owner, size.width, size.height, globalThis.devicePixelRatio);

			// The occlusion is measured against the window size, which the visual viewport events can precede.
			this.scheduleKeyboardOcclusionReport();
		}

		// As in WinUI, the on-screen keyboard occludes the window (InputPane) rather than resizing it, which would
		// light-dismiss the popups. Around a touch text entry, a shrink or a growth under the keyboard is held, and any
		// other growth (a browser toolbar going and coming back) is reported once it has settled.
		private updateWindowSize(rect: DOMRect) {
			const last = this.windowSize;
			const isHeightChange = last !== undefined && rect.width === last.width && !this.isLayoutSizeForced;
			const isSoftKeyboardEntry = WebAssemblyWindowWrapper.isSoftKeyboardInputFocused();
			this.isLayoutSizeForced = false;
			this.clearGrowthSettle();

			if (isHeightChange
				&& rect.height < last.height
				&& (this.isHeightHeld || isSoftKeyboardEntry)) {
				this.holdHeight(isSoftKeyboardEntry);
			} else if (isHeightChange
				&& rect.height > last.height
				&& (isSoftKeyboardEntry ? WebAssemblyWindowWrapper.isKeyboardCovering(rect) : this.isHeightHeld)) {
				// Under the keyboard, or held until the text entry that ended is released.
				this.holdHeight(isSoftKeyboardEntry);
			} else if (isHeightChange && rect.height > last.height && this.isTouchTextEntryRecent()) {
				this.growthSettleTimer = setTimeout(() => {
					this.growthSettleTimer = undefined;
					this.forceLayoutSize();
				}, WebAssemblyWindowWrapper.growthSettleDelay);
			} else {
				this.endHeightHold();
				this.windowSize = { width: rect.width, height: rect.height };
			}

			// The canvas is sized to 100% of the layout viewport by default: keep it at the window height.
			this.canvasElement.style.height = rect.height !== this.windowSize.height ? `${this.windowSize.height}px` : "";
			return this.windowSize;
		}

		// Reports the layout size as it is, however its height changed.
		private forceLayoutSize() {
			this.isLayoutSizeForced = true;

			// Dispatched rather than calling resize(): the renderer sizes the canvas from this event too.
			window.dispatchEvent(new Event("resize"));
		}

		// The keyboard went down during the text entry: a height held for it is looked at again (a growth is settled).
		// Once the text entry ended, the release after it takes care of that instead.
		private onKeyboardHidden() {
			if (this.isHeightHeld && WebAssemblyWindowWrapper.isSoftKeyboardInputFocused()) {
				this.resize();
			}
		}

		private holdHeight(isSoftKeyboardEntry: boolean) {
			this.isHeightHeld = true;

			// Without a soft keyboard text entry, nothing else would release it.
			if (!isSoftKeyboardEntry && this.heightReleaseTimer === undefined) {
				this.scheduleHeightRelease();
			}
		}

		private scheduleHeightRelease() {
			clearTimeout(this.heightReleaseTimer);
			this.heightReleaseTimer = setTimeout(() => {
				this.heightReleaseTimer = undefined;

				// A text entry that started again (focus moved to another TextBox) keeps holding it.
				if (this.isHeightHeld && !WebAssemblyWindowWrapper.isSoftKeyboardInputFocused()) {
					this.forceLayoutSize();
				}
			}, WebAssemblyWindowWrapper.textEntryEndDelay);
		}

		private endHeightHold() {
			this.isHeightHeld = false;
			clearTimeout(this.heightReleaseTimer);
			this.heightReleaseTimer = undefined;
		}

		private clearGrowthSettle() {
			clearTimeout(this.growthSettleTimer);
			this.growthSettleTimer = undefined;
		}

		private isTouchTextEntryRecent() {
			return WebAssemblyWindowWrapper.isSoftKeyboardInputFocused()
				|| performance.now() - this.touchTextEntryEndedAt < WebAssemblyWindowWrapper.textEntryEndDelay;
		}

		// The keyboard covers the bottom of the layout viewport through the visual viewport.
		private static isKeyboardCovering(rect: DOMRect) {
			const viewport = window.visualViewport;
			return !!viewport && viewport.height + viewport.offsetTop < rect.height - 1;
		}

		private static isTextInputFocused() {
			return document.activeElement?.id === UnoDomIds.input;
		}

		private static isSoftKeyboardInputFocused() {
			return WebAssemblyWindowWrapper.isTextInputFocused() && WebAssemblyWindowWrapper.isTouchInput();
		}

		// Whether the last pointer pressed was a touch or a pen, the input types that bring up the on-screen keyboard.
		public static isTouchInput(): boolean {
			return WebAssemblyWindowWrapper.lastPointerType === "touch" || WebAssemblyWindowWrapper.lastPointerType === "pen";
		}

		// The size the window was last reported at, which the canvas is drawn at.
		public static getWindowSize(): { width: number, height: number } {
			for (const id in WebAssemblyWindowWrapper.activeInstances) {
				const size = WebAssemblyWindowWrapper.activeInstances[id].windowSize;
				if (size) {
					return size;
				}
			}

			return document.documentElement.getBoundingClientRect();
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
