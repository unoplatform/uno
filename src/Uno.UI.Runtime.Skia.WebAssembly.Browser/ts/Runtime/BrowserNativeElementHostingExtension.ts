namespace Uno.UI.Runtime.Skia {
	export class BrowserNativeElementHostingExtension {
		private static initialized: boolean;
		private static clipPathsda: SVGPathElement;

		public static setSvgClipPathForNativeElementHost(path: string) {
			if (!this.initialized) {
				this.initialized = true;

				const svgContainer = document.createElementNS("http://www.w3.org/2000/svg", "svg");
				svgContainer.setAttribute("width", "0");
				svgContainer.setAttribute("height", "0");
				const defs = document.createElementNS("http://www.w3.org/2000/svg", "defs");
				const clipPath = document.createElementNS("http://www.w3.org/2000/svg", "clipPath");
				clipPath.setAttribute("id", "unoNativeElementHostClipPath");
				this.clipPathsda = document.createElementNS("http://www.w3.org/2000/svg", "path");
				clipPath.appendChild(this.clipPathsda);
				defs.appendChild(clipPath);
				svgContainer.appendChild(defs);
				document.body.appendChild(svgContainer);
				clipPath.appendChild(this.clipPathsda);
			}
			this.clipPathsda.setAttributeNS(null, "d", path);
		}

		public static isNativeElement(content: string): boolean {
			return document.getElementById(content) instanceof HTMLElement;
		}

		private static getNativeElementHost(): HTMLElement {
			let nativeElementHost = document.getElementById("uno-native-element-host");
			if (nativeElementHost === undefined || nativeElementHost === null) {
				nativeElementHost = document.createElement("div");
				nativeElementHost.id = "uno-native-element-host";
				nativeElementHost.style.position = "absolute";
				nativeElementHost.style.height = "100%";
				nativeElementHost.style.width = "100%";
				nativeElementHost.style.overflow = "hidden";
				nativeElementHost.style.clipPath = "url(#unoNativeElementHostClipPath)";
				let unoBody = document.getElementById("uno-body");
				unoBody.insertBefore(nativeElementHost, unoBody.firstChild);
			}

			return nativeElementHost;
		}

		public static attachNativeElement(content: string) {

			let element = document.getElementById(content);
			this.getNativeElementHost().appendChild(element);
		}

		public static detachNativeElement(content: string) {
			let element = document.getElementById(content) as HTMLElement;
			element.remove();
		}

		public static arrangeNativeElement(content: string, x: number, y: number, width: number, height: number) {
			let element = document.getElementById(content) as HTMLElement;
			element.style.position = "absolute"
			element.style.left = `${x}px`;
			element.style.top = `${y}px`;
			element.style.width = `${width}px`;
			element.style.height = `${height}px`;
		}

		public static changeNativeElementOpacity(content: string, opacity: number) {
			let element = document.getElementById(content) as HTMLElement;
			element.style.opacity = opacity.toString();
		}

		public static createSampleComponent(s: string): string {
			let element = document.createElement("div");
			let btn = document.createElement("button");
			btn.textContent = s;
			btn.style.display = "inline-block";
			btn.style.width = "100%";
			btn.style.height = "100%";
			btn.style.backgroundColor = "#ff69b4"; /* Hot pink */
			btn.style.color = "white";
			element.appendChild(btn);

			element.id = (Math.random() + 1).toString(36).substring(7);
			document.body.appendChild(element);
			element.addEventListener("pointerdown", _ => alert(`button ${s} clicked`));
			return element.id;
		}
	}
}
