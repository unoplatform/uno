namespace Uno.UI.Runtime.Skia {

	export class Accessibility {
		private static politeElement: HTMLDivElement;
		private static assertiveElement: HTMLDivElement;
		private static enableAccessibilityButton: HTMLDivElement;
		private static semanticsRoot: HTMLDivElement;
		private static managedEnableAccessibility: any;
		private static managedOnScroll: any;
		private static containerElement: HTMLElement;

		private static createLiveElement(kind: string) {
			const element = document.createElement("div");
			element.classList.add("uno-aria-live");
			element.setAttribute("aria-live", kind);
			return element;
		}

		public static setup() {
			const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();
			this.managedEnableAccessibility = browserExports.Uno.UI.Runtime.Skia.WebAssemblyAccessibility.EnableAccessibility;
			this.managedOnScroll = browserExports.Uno.UI.Runtime.Skia.WebAssemblyAccessibility.OnScroll;

			this.containerElement = document.getElementById("uno-body");
			this.politeElement = Accessibility.createLiveElement("polite");
			this.assertiveElement = Accessibility.createLiveElement("assertive");
			this.containerElement.appendChild(this.politeElement);
			this.containerElement.appendChild(this.assertiveElement);

			this.enableAccessibilityButton = document.createElement("div");
			this.enableAccessibilityButton.id = "uno-enable-accessibility";
			this.enableAccessibilityButton.setAttribute("aria-live", "polite");
			this.enableAccessibilityButton.setAttribute("role", "button");
			this.enableAccessibilityButton.setAttribute("tabindex", "0");
			this.enableAccessibilityButton.setAttribute("aria-label", "Enable accessibility");
			this.enableAccessibilityButton.addEventListener("click", this.onEnableAccessibilityButtonClicked.bind(this));
			this.containerElement.appendChild(this.enableAccessibilityButton);

			this.semanticsRoot = document.createElement("div");
			this.semanticsRoot.id = "uno-semantics-root";
			this.containerElement.appendChild(this.semanticsRoot);
		}

		private static createSemanticElement(x: number, y: number, width: number, height: number, handle: number, isFocusable: boolean) {
			let element = document.createElement("div");
			element.style.position = "absolute";

			element.addEventListener('wheel', (e) => {
				// When scrolling with wheel, we want to prevent scroll events.
				e.preventDefault();
			}, {passive:false});

			element.addEventListener('scroll', (e) => {
				let element = e.target as HTMLElement;
				this.managedOnScroll(handle, element.scrollLeft, element.scrollTop);
			});

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
				element.style.touchAction = "";
			} else {
				element.removeAttribute("tabIndex");
				element.style.pointerEvents = "none";
				element.style.touchAction = "none";
			}
		}

		public static getSemanticElementByHandle(handle: number): HTMLElement {
			return document.getElementById(`uno-semantics-${handle}`)
		}

		public static announcePolite(text: string) {
			Accessibility.announce(Accessibility.politeElement, text);
		}

		public static announceAssertive(text: string) {
			Accessibility.announce(Accessibility.assertiveElement, text);
		}

		private static announce(ariaLiveElement: HTMLDivElement, text: string) {
			let child = document.createElement("div");
			child.innerText = text;
			ariaLiveElement.appendChild(child);
			setTimeout(() => ariaLiveElement.removeChild(child), 300);
		}

		private static onEnableAccessibilityButtonClicked(evt: MouseEvent) {
			this.containerElement.removeChild(this.enableAccessibilityButton);
			this.managedEnableAccessibility();
			this.announceAssertive("Accessibility enabled successfully.");
		}

		public static focusSemanticElement(handle: number) {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.focus();
			}
		}

		public static addRootElementToSemanticsRoot(rootHandle: number, width: number, height: number, x: number, y: number, isFocusable: boolean): void {
			let element = Accessibility.createSemanticElement(x, y, width, height, rootHandle, isFocusable);
			this.semanticsRoot.appendChild(element);
		}

		public static addSemanticElement(
			parentHandle: number,
			handle: number,
			index: number,
			width: number,
			height: number,
			x: number,
			y: number,
			role: string,
			automationId: string,
			isFocusable: boolean,
			ariaChecked: string,
			isVisible: boolean,
			horizontallyScrollable: boolean,
			verticallyScrollable: boolean,
			temporary: string): boolean {
			const parent = Accessibility.getSemanticElementByHandle(parentHandle);
			if (!parent) {
				return false;
			}

			let element = Accessibility.createSemanticElement(x, y, width, height, handle, isFocusable);
			element.setAttribute('ElementType', temporary);
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

			if (horizontallyScrollable) {
				element.style.overflowX = "scroll";
			}

			if (verticallyScrollable) {
				element.style.overflowY = "scroll";
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

		public static updateNativeScrollOffsets(handle: number, horizontalOffset: number, verticalOffset: number): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.scrollLeft = horizontalOffset;
				element.scrollTop = verticalOffset;
			}
		}

		public static hideSemanticElement(handle: number) {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.hidden = true;
			}
		}

		public static updateSemanticElementPositioning(handle: number, width: number, height: number, x: number, y: number) {
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
