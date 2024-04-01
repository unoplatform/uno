namespace Uno.UI.Runtime.Skia {

	export class WebAssemblyWindowWrapper {
		private containerElement: HTMLDivElement;
		private canvasElement: HTMLCanvasElement;
		private a11yElement: HTMLDivElement;
		private enableA11y: HTMLDivElement;
		private semanticsRoot: HTMLDivElement;
		private onResize: any;
		private managedEnableA11y: any;
		private owner: any;
		private static readonly unoPersistentLoaderClassName = "uno-persistent-loader";
		private static readonly loadingElementId = "uno-loading";

		private static activeInstances: { [id: string]: WebAssemblyWindowWrapper } = {};

		private constructor(owner: any) {
			this.owner = owner;
			this.build();
		}

		public static initialize(owner: any) {
			WebAssemblyWindowWrapper.activeInstances[owner] = new WebAssemblyWindowWrapper(owner);
		}

		private async build() {
			await this.buildImports();

			this.containerElement = (document.getElementById("uno-body") as HTMLDivElement);

			if (!this.containerElement) {
				// If not found, we simply create a new one.
				this.containerElement = document.createElement("div");
				this.containerElement.id = "uno-root";
				this.containerElement.style.overflow = "hidden";

				document.body.appendChild(this.containerElement);
			}

			this.canvasElement = document.createElement("canvas");
			this.canvasElement.id = "uno-canvas";
			this.canvasElement.style.position = "absolute";
			this.canvasElement.setAttribute("aria-hidden", "true");
			this.containerElement.appendChild(this.canvasElement);

			this.a11yElement = document.createElement("div");
			this.a11yElement.setAttribute("aria-live", "polite");
			this.a11yElement.style.position = "fixed";
			this.a11yElement.style.overflow = "hidden";
			this.a11yElement.style.transform = "translate(-99999px, -99999px)";
			this.a11yElement.style.width = "1px";
			this.a11yElement.style.height = "1px";
			this.containerElement.appendChild(this.a11yElement);

			this.enableA11y = document.createElement("div");
			this.enableA11y.setAttribute("aria-live", "polite");
			this.enableA11y.setAttribute("role", "button");
			this.enableA11y.setAttribute("tabindex", "0");
			this.enableA11y.setAttribute("aria-label", "Enable accessibility");
			this.enableA11y.style.position = "absolute";
			this.enableA11y.style.left = "-1px";
			this.enableA11y.style.top = "-1px";
			this.enableA11y.style.width = "1px";
			this.enableA11y.style.height = "1px";
			this.enableA11y.addEventListener("click", this.onEnableA11yClicked.bind(this));
			this.containerElement.appendChild(this.enableA11y);

			this.semanticsRoot = document.createElement("div");
			this.semanticsRoot.id = "uno-semantics-root";
			this.semanticsRoot.style.filter = "opacity(0%)";
			this.semanticsRoot.style.pointerEvents = "none";
			this.containerElement.appendChild(this.semanticsRoot);

			document.body.addEventListener("focusin", this.onfocusin);
			window.addEventListener("resize", x => this.resize());

			this.resize();

			this.removeLoading();
		}

		public static announceA11y(owner: any, text: string) {
			const instance = this.getInstance(owner);
			instance.announceA11y(text);
		}

		private announceA11y(text: string) {
			let child = document.createElement("div");
			child.innerText = text;
			this.a11yElement.appendChild(child);
			setTimeout(() => this.a11yElement.removeChild(child), 300);
		}

		private onEnableA11yClicked(evt: MouseEvent) {
			this.containerElement.removeChild(this.enableA11y);
			this.managedEnableA11y(this.owner);
			this.announceA11y("Accessibility enabled successfullly.");
		}

		public static focusSemanticElement(handle: Number) {
			const element = document.getElementById(`uno-semantics-${handle}`);
			if (element) {
				console.log("Focus!!" + element);
				element.focus();
			}
		}

		private static createAccessibilityElement(x: Number, y: Number, width: Number, height: Number, handle: Number, isFocusable: boolean) {
			let element = document.createElement("div");
			element.style.position = "absolute";
			element.tabIndex = isFocusable ? 0 : -1;
			element.style.pointerEvents = isFocusable ? "all" : "none";
			element.style.left = `${x}px`;
			element.style.top = `${y}px`;
			element.style.width = `${width}px`;
			element.style.height = `${height}px`;
			element.id = `uno-semantics-${handle}`;
			return element;
		}

		public static addRootElementToSemanticsRoot(owner: any, rootHandle: Number, width: Number, height: Number, x: Number, y: Number, isFocusable: boolean): void {
			let element = this.createAccessibilityElement(x, y, width, height, rootHandle, isFocusable);
			let semanticsRoot = WebAssemblyWindowWrapper.getInstance(owner).semanticsRoot;

			// For now, we re-create the whole tree every time.
			// TODO: Optimize this for better perf.
			if (semanticsRoot.childNodes.length === 1) {
				semanticsRoot.removeChild(semanticsRoot.childNodes[0]);
			}

			semanticsRoot.appendChild(element);
		}

		public static addSemanticElement(owner: any, parentHandle: Number, handle: Number, width: Number, height: Number, x: Number, y: Number, role: string, automationId: string, isFocusable: boolean, ariaChecked: string): void {
			let element = this.createAccessibilityElement(x, y, width, height, handle, isFocusable);
			if (role) {
				element.setAttribute("role", role);
			}

			if (ariaChecked) {
				element.setAttribute("aria-checked", ariaChecked);
			}

			if (automationId) {
				element.setAttribute("aria-label", automationId);
			}

			document.getElementById(`uno-semantics-${parentHandle}`).appendChild(element);
		}

		public static updateAriaLabel(owner: any, handle: Number, automationId: string): void {
			document.getElementById(`uno-semantics-${handle}`).setAttribute("aria-label", automationId);
		}

		public static updateAriaChecked(owner: any, handle: Number, ariaChecked: string): void {
			document.getElementById(`uno-semantics-${handle}`).setAttribute("aria-checked", ariaChecked);
		}

		private removeLoading() {
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

		async buildImports() {
			let anyModule = <any>window.Module;

			if (anyModule.getAssemblyExports !== undefined) {
				const browserExports = await anyModule.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser");

				this.onResize = browserExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.OnResize;
				this.managedEnableA11y = browserExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.EnableA11y;
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
			this.onResize(this.owner, document.documentElement.clientWidth, document.documentElement.clientHeight);
		}

		private onfocusin(event: Event) {
			//const newFocus = event.target;
			//const handle = (newFocus as HTMLElement).getAttribute("XamlHandle");
			//const htmlId = handle ? Number(handle) : -1; // newFocus may not be an Uno element
			//WindowManager.focusInMethod(htmlId);
		}
	}
}
