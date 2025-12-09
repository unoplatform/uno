namespace Uno.UI.NativeElementHosting {
	export class BrowserHtmlElement {
		private static clipPath: SVGPathElement;

		/** Native elements created with the BrowserHtmlElement class */
		private static nativeHandlersMap: { [id: string]: any } = {};

		private static dispatchEventNativeElementMethod: any;

		public static async initialize() {
			let anyModule = <any>window.Module;

			if (anyModule.getAssemblyExports !== undefined) {
				const browserExports = await anyModule.getAssemblyExports("Uno.UI");

				BrowserHtmlElement.dispatchEventNativeElementMethod = browserExports.Uno.UI.NativeElementHosting.BrowserHtmlElement.DispatchEventNativeElementMethod;
			} else {
				throw `BrowserHtmlElement: Unable to find dotnet exports`;
			}
		}

		public static setSvgClipPathForNativeElementHost(path: string) {
			if (!document.getElementById("unoNativeElementHostClipPath")) {
				const svgContainer = document.createElementNS("http://www.w3.org/2000/svg", "svg");
				svgContainer.setAttribute("width", "0");
				svgContainer.setAttribute("height", "0");
				const defs = document.createElementNS("http://www.w3.org/2000/svg", "defs");
				const clipPath = document.createElementNS("http://www.w3.org/2000/svg", "clipPath");
				clipPath.setAttribute("id", "unoNativeElementHostClipPath");
				this.clipPath = document.createElementNS("http://www.w3.org/2000/svg", "path");
				clipPath.appendChild(this.clipPath);
				defs.appendChild(clipPath);
				svgContainer.appendChild(defs);
				document.body.appendChild(svgContainer);
				clipPath.appendChild(this.clipPath);
			}
			this.clipPath.setAttributeNS(null, "d", path);
		}

		private static getNativeElementHost(): HTMLElement {
			let nativeElementHost = document.getElementById("uno-native-element-host");
			if (!nativeElementHost) {
				nativeElementHost = document.createElement("div");
				nativeElementHost.id = "uno-native-element-host";
				nativeElementHost.style.position = "absolute";
				nativeElementHost.style.height = "100%";
				nativeElementHost.style.width = "100%";
				nativeElementHost.style.overflow = "hidden";
				nativeElementHost.style.clipPath = "url(#unoNativeElementHostClipPath)";
				let unoBody = document.getElementById("uno-body");
				unoBody.appendChild(nativeElementHost);
			}

			return nativeElementHost;
		}

		public static isNativeElement(content: string): boolean {
			for (let child of this.getNativeElementHost().children) {
				if (child.id === content) {
					return true;
				}
			}
			return false;
		}

		public static attachNativeElement(content: string) {
			let element = this.getElementOrThrow(content);
			element.hidden = false;
		}

		public static detachNativeElement(content: string) {
			let element = this.getElementOrThrow(content);
			element.hidden = true;
		}

		public static arrangeNativeElement(content: string, x: number, y: number, width: number, height: number) {
			let element = this.getElementOrThrow(content);
			element.style.position = "absolute"
			element.style.left = `${x}px`;
			element.style.top = `${y}px`;
			element.style.width = `${width}px`;
			element.style.height = `${height}px`;
		}

		public static changeNativeElementOpacity(content: string, opacity: number) {
			let element = this.getElementOrThrow(content);
			element.style.opacity = opacity.toString();
		}

		public static createHtmlElement(id: string, tagName: string) {
			let element = document.createElement(tagName);
			element.id = id;
			element.hidden = true;
			this.getNativeElementHost().appendChild(element);
		}

		public static disposeHtmlElement(id: string) {
			this.getElementOrThrow(id).remove();
		}

		public static createSampleComponent(parentId: string, text: string) {
			let element = this.getElementOrThrow(parentId);

			let btn = document.createElement("button");
			btn.textContent = text;
			btn.style.display = "inline-block";
			btn.style.width = "100%";
			btn.style.height = "100%";
			btn.style.backgroundColor = "#ff69b4"; /* Hot pink */
			btn.style.color = "white";

			element.appendChild(btn);
			element.addEventListener("pointerdown", _ => alert(`button ${text} clicked`));
		}

		public static setStyleString(elementId: string, name: string, value: string) {
			const element = this.getElementOrThrow(elementId);

			element.style.setProperty(name, value);
		}

		public static resetStyle(elementId: string, names: string[]) {
			const element = this.getElementOrThrow(elementId);

			for (const name of names) {
				element.style.setProperty(name, "");
			}
		}

		public static setClasses(elementId: string, cssClassesList: string[], classIndex: number) {
			const element = this.getElementOrThrow(elementId);

			for (let i = 0; i < cssClassesList.length; i++) {
				if (i === classIndex) {
					element.classList.add(cssClassesList[i]);
				} else {
					element.classList.remove(cssClassesList[i]);
				}
			}
		}

		public static setUnsetCssClasses(elementId: string, classesToUnset: string[]) {
			const element = this.getElementOrThrow(elementId);

			classesToUnset.forEach(c => {
				element.classList.remove(c);
			});
		}

		public static setAttribute(elementId: string, name: string, value: string) {
			const element = this.getElementOrThrow(elementId);

			element.setAttribute(name, value);
		}

		public static getAttribute(elementId: string, name: string) {
			const element = this.getElementOrThrow(elementId);

			return element.getAttribute(name);
		}

		public static removeAttribute(elementId: string, name: string) {
			const element = this.getElementOrThrow(elementId);

			element.removeAttribute(name);
		}

		public static setContentHtml(elementId: string, html: string) {
			const element = this.getElementOrThrow(elementId);

			element.innerHTML = html;
		}

		public static registerNativeHtmlEvent(owner: any, elementId: string, eventName: string, managedHandler: string) {
			const element = this.getElementOrThrow(elementId);

			if (!BrowserHtmlElement.dispatchEventNativeElementMethod) {
				throw `BrowserHtmlElement: The initialize method has not been called`;
			}

			const eventHandler = (event: Event) => {
				BrowserHtmlElement.dispatchEventNativeElementMethod(owner, eventName, managedHandler, event);
			};

			// Register the handler using a string representation of the managed handler
			// the managed representation assumes that the string contains a unique id.
			BrowserHtmlElement.nativeHandlersMap["" + managedHandler] = eventHandler;

			element.addEventListener(eventName, eventHandler);
		}

		public static unregisterNativeHtmlEvent(elementId: string, eventName: string, managedHandler: any) {
			const element = this.getElementOrThrow(elementId);

			if (!BrowserHtmlElement.dispatchEventNativeElementMethod) {
				throw `BrowserHtmlElement: The initialize method has not been called`;
			}

			const key = "" + managedHandler;
			const eventHandler = BrowserHtmlElement.nativeHandlersMap[key];
			if (eventHandler) {
				element.removeEventListener(eventName, eventHandler);
				delete BrowserHtmlElement.nativeHandlersMap[key];
			}
		}

		public static invokeJS(command: string): string {
			return String(eval(command) || "");
		}

		public static async invokeAsync(command: string): Promise<string> {
			// Preseve the original emscripten marshalling semantics
			// to always return a valid string.
			var result = await eval(command);

			return String(result || "");

		}

		/**
		 * Returns the element with the given id, or throws an error if not found.
		 */
		private static getElementOrThrow(id: string): HTMLElement {
			const element = document.getElementById(id);
			if (!element) {
				throw new Error(`BrowserHtmlElement: Element with id '${id}' not found.`);
			}
			return element as HTMLElement;
		}
	}
}
