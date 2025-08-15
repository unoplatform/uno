namespace Uno.UI.Runtime.Skia {

	export class WebAssemblyWindowWrapper {
		private containerElement: HTMLDivElement;
		private canvasElement: HTMLCanvasElement;
		private onResize: any;
		private prefetchFonts: any;
		private owner: any;
		private static readonly unoPersistentLoaderClassName = "uno-persistent-loader";
		private static readonly loadingElementId = "uno-loading";
		private static readonly unoKeepLoaderClassName = "uno-keep-loader";

		private static activeInstances: { [id: string]: WebAssemblyWindowWrapper } = {};

		private constructor(owner: any) {
			this.owner = owner;
			this.build();
		}

		public static initialize(owner: any) {
			WebAssemblyWindowWrapper.activeInstances[owner] = new WebAssemblyWindowWrapper(owner);
		}

		public static persistBootstrapperLoader() {
			let bootstrapperLoaders = document.getElementsByClassName(WebAssemblyWindowWrapper.unoPersistentLoaderClassName);
			if (bootstrapperLoaders.length > 0) {
				let bootstrapperLoader = bootstrapperLoaders[0] as HTMLElement;
				bootstrapperLoader.classList.add(WebAssemblyWindowWrapper.unoKeepLoaderClassName);
			}
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

			await Accessibility.setup();

			window.addEventListener("resize", x => this.resize());

			window.addEventListener("contextmenu", x => {
				x.preventDefault();
			})

			this.resize();

			await this.prefetchFonts();

			this.removeLoading();
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
				this.prefetchFonts = browserExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.PrefetchFonts;
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
