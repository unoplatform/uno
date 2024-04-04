namespace Uno.UI.Runtime.Skia {

	export class Accessibility {
		private politeElement: HTMLDivElement;
		private enableA11y: HTMLDivElement;
		private semanticsRoot: HTMLDivElement;
		private managedEnableA11y: any;
		private containerElement: HTMLDivElement;

		async buildImports() {
			let anyModule = <any>window.Module;

			if (anyModule.getAssemblyExports !== undefined) {
				const browserExports = await anyModule.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser");

				this.managedEnableA11y = browserExports.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.EnableA11y;
			}
		}

		public async initialize(containerElement: HTMLDivElement) {
			await this.buildImports();

			this.containerElement = containerElement;
			this.politeElement = document.createElement("div");
			this.politeElement.classList.add("uno-aria-live");
			this.politeElement.setAttribute("aria-live", "polite");
			containerElement.appendChild(this.politeElement);

			this.enableA11y = document.createElement("div");
			this.enableA11y.id = "uno-enable-accessibility";
			this.enableA11y.setAttribute("aria-live", "polite");
			this.enableA11y.setAttribute("role", "button");
			this.enableA11y.setAttribute("tabindex", "0");
			this.enableA11y.setAttribute("aria-label", "Enable accessibility");
			this.enableA11y.addEventListener("click", this.onEnableA11yClicked.bind(this));
			containerElement.appendChild(this.enableA11y);

			this.semanticsRoot = document.createElement("div");
			this.semanticsRoot.id = "uno-semantics-root";
			containerElement.appendChild(this.semanticsRoot);
		}

		public static createSemanticElement(x: number, y: number, width: number, height: number, handle: number, isFocusable: boolean) {
			let element = document.createElement("div");
			element.style.position = "absolute";

			Accessibility.updateElementFocusability(element, isFocusable);

			element.style.left = `${x}px`;
			element.style.top = `${y}px`;
			element.style.width = `${width}px`;
			element.style.height = `${height}px`;
			//element.style.boxShadow = "inset 0px 0px 5px 0px red"; // FOR DEBUGGING ONLY.
			element.id = `uno-semantics-${handle}`;
			return element;
		}

		public static updateElementFocusability(element: HTMLElement, isFocusable: boolean) {
			if (isFocusable) {
				element.tabIndex = 0;
				element.style.pointerEvents = "all";
			} else {
				element.removeAttribute("tabIndex");
				element.style.pointerEvents = "none";
			}
		}

		public static getSemanticElementByHandle(handle: number): HTMLElement {
			return document.getElementById(`uno-semantics-${handle}`)
		}

		public announceA11y(text: string) {
			let child = document.createElement("div");
			child.innerText = text;
			this.politeElement.appendChild(child);
			setTimeout(() => this.politeElement.removeChild(child), 300);
		}

		private onEnableA11yClicked(evt: MouseEvent) {
			this.containerElement.removeChild(this.enableA11y);
			this.managedEnableA11y();
			this.announceA11y("Accessibility enabled successfully.");
		}

		public static focusSemanticElement(handle: number) {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.focus();
			}
		}

		public addRootElementToSemanticsRoot(rootHandle: number, width: number, height: number, x: number, y: number, isFocusable: boolean): void {
			let element = Accessibility.createSemanticElement(x, y, width, height, rootHandle, isFocusable);
			this.semanticsRoot.appendChild(element);
		}

		public static addSemanticElement(parentHandle: number, handle: number, index: number, width: number, height: number, x: number, y: number, role: string, automationId: string, isFocusable: boolean, ariaChecked: string, isVisible: boolean): boolean {
			const parent = Accessibility.getSemanticElementByHandle(parentHandle);
			if (!parent) {
				return false;
			}

			let element = Accessibility.createSemanticElement(x, y, width, height, handle, isFocusable);

			if (!isVisible) {
				element.hidden = true;
			}

			if (role) {
				element.setAttribute("role", role);
			}

			if (ariaChecked) {
				element.setAttribute("aria-checked", ariaChecked);
			}

			if (automationId) {
				element.setAttribute("aria-label", automationId);
			}

			if (index != null && index < parent.childElementCount) {
				parent.insertBefore(element, parent.children[index]);
			} else {
				parent.appendChild(element);
			}

			return true;
		}

		public static removeSemanticElement(parentHandle: number, childHandle: number): void {
			const parent = Accessibility.getSemanticElementByHandle(parentHandle);
			if (parent) {
				const child = Accessibility.getSemanticElementByHandle(childHandle);
				parent.removeChild(child)
			}
		}

		public static updateIsFocusable(handle: number, isFocusable: boolean): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				Accessibility.updateElementFocusability(element, isFocusable);
			}
		}

		public static updateAriaLabel(handle: number, automationId: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.setAttribute("aria-label", automationId);
			}
		}

		public static updateAriaChecked(handle: number, ariaChecked: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.setAttribute("aria-checked", ariaChecked);
			}
		}

		public static hideSemanticElement(handle: number) {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.hidden = true;
			}
		}

		public static updateSemanticElementPositioning(owner: any, handle: number, width: number, height: number, x: number, y: number) {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.hidden = false;
				element.style.left = `${x}px`;
				element.style.top = `${y}px`;
				element.style.width = `${width}px`;
				element.style.height = `${height}px`;
			}
		}
	}
}
