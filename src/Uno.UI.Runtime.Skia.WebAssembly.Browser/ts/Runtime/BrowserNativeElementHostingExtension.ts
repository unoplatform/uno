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

		// We have a store to put elements in before they are loaded/unloaded as native elements.
		// We have to put these elements somewhere we can access, so we do so in a hidden div.
		// Only elements in the store are considered part of the "uno world" and can be
		// embedded inside the uno canvas. Clients need to request html elements to be added
		// to the store before any native hosting takes place.
		private static getNativeElementStore(): HTMLElement {
			let nativeElementStore = document.getElementById("uno-native-element-store");
			if (!nativeElementStore) {
				nativeElementStore = document.createElement("div");
				nativeElementStore.id = "uno-native-element-store";
				nativeElementStore.style.display = "none";
				let unoBody = document.getElementById("uno-body");
				unoBody.insertBefore(nativeElementStore, unoBody.firstChild);
			}

			return nativeElementStore;
		}

		public static isNativeElement(content: string): boolean {
			for (let child of this.getNativeElementStore().children) {
				if (child.id === content) {
					return true;
				}
			}
			return false;
		}

		public static attachNativeElement(content: string) {
			let element = document.getElementById(content);
			element.remove(); // remove from the store
			this.getNativeElementHost().appendChild(element); // add to the native host
		}

		public static detachNativeElement(content: string) {
			let element = document.getElementById(content);
			element.remove(); // remove from the native host
			this.getNativeElementStore().appendChild(element); // add to the native host
		}

		public static arrangeNativeElement(content: string, x: number, y: number, width: number, height: number) {
			let element = document.getElementById(content);
			element.style.position = "absolute"
			element.style.left = `${x}px`;
			element.style.top = `${y}px`;
			element.style.width = `${width}px`;
			element.style.height = `${height}px`;
		}

		public static changeNativeElementOpacity(content: string, opacity: number) {
			let element = document.getElementById(content);
			element.style.opacity = opacity.toString();
		}

		public static createHtmlElementAndAddToStore(id: string, tagName: string) {
			let element = document.createElement(tagName);
			element.id = id;
			this.getNativeElementStore().appendChild(element);
		}

		public static addToStore(id: string) {
			this.getNativeElementStore().appendChild(document.getElementById(id));
		}

		public static disposeHtmlElement(id: string) {
			document.getElementById(id).remove();
		}

		public static createSampleComponent(parentId: string, text: string) {
			let element = document.getElementById(parentId);

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
			const element = document.getElementById(elementId);

			element.style.setProperty(name, value);
		}

		public static resetStyle(elementId: string, names: string[]) {
			const element = document.getElementById(elementId);

			for (const name of names) {
				element.style.setProperty(name, "");
			}
		}

		public static setClasses(elementId: string, cssClassesList: string[], classIndex: number) {
			const element = document.getElementById(elementId);

			for (let i = 0; i < cssClassesList.length; i++) {
				if (i === classIndex) {
					element.classList.add(cssClassesList[i]);
				} else {
					element.classList.remove(cssClassesList[i]);
				}
			}
		}

		public static setUnsetCssClasses(elementId: string, classesToUnset: string[]) {
			const element = document.getElementById(elementId);

			classesToUnset.forEach(c => {
				element.classList.remove(c);
			});
		}

		public static setAttribute(elementId: string, name: string, value: string) {
			const element = document.getElementById(elementId);

			element.setAttribute(name, value);
		}

		public static getAttribute(elementId: string, name: string) {
			const element = document.getElementById(elementId);

			return element.getAttribute(name);
		}

		public static removeAttribute(elementId: string, name: string) {
			const element = document.getElementById(elementId);

			element.removeAttribute(name);
		}

		public static setContentHtml(elementId: string, html: string) {
			const element = document.getElementById(elementId);

			element.innerHTML = html;
		}

		public static registerNativeHtmlEvent(owner: any, elementId: string, eventName: string, managedHandler: string) {
			const element = document.getElementById(elementId);

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
			const element = document.getElementById(elementId);

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
	}
}
