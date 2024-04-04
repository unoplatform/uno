namespace Uno.UI.Runtime.Skia {

	export class WebAssemblyWindowWrapper {
		private containerElement: HTMLDivElement;
		private canvasElement: HTMLCanvasElement;
		private accessibility: Accessibility;
		private onResize: any;
		private owner: any;
		private static readonly unoPersistentLoaderClassName = "uno-persistent-loader";
		private static readonly loadingElementId = "uno-loading";

		private static activeInstances: { [id: string]: WebAssemblyWindowWrapper } = {};

		private constructor(owner: any) {
			this.owner = owner;
			this.accessibility = new Accessibility();
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

				document.body.appendChild(this.containerElement);
			}

			this.canvasElement = document.createElement("canvas");
			this.canvasElement.id = "uno-canvas";
			this.canvasElement.setAttribute("aria-hidden", "true");
			this.containerElement.appendChild(this.canvasElement);

			await this.accessibility.initialize(this.containerElement);

			document.body.addEventListener("focusin", this.onfocusin);
			window.addEventListener("resize", x => this.resize());

			this.resize();

			this.removeLoading();
		}

		public static announceA11y(owner: any, text: string) {
			const instance = this.getInstance(owner);
			instance.accessibility.announceA11y(text);
		}

		public static addRootElementToSemanticsRoot(owner: any, rootHandle: number, width: number, height: number, x: number, y: number, isFocusable: boolean): void {
			WebAssemblyWindowWrapper.getInstance(owner).accessibility.addRootElementToSemanticsRoot(
				rootHandle, width, height, x, y, isFocusable);
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
