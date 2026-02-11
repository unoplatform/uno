namespace Uno.UI.Runtime.Skia {

	export class Accessibility {
		private static politeElement: HTMLDivElement;
		private static assertiveElement: HTMLDivElement;
		private static enableAccessibilityButton: HTMLDivElement;
		private static semanticsRoot: HTMLDivElement;
		private static containerElement: HTMLElement;
		private static debugModeEnabled: boolean = false;

		// Managed callbacks from C#
		private static managedEnableAccessibility: any;
		private static managedOnScroll: any;
		private static managedOnInvoke: any;
		private static managedOnToggle: any;
		private static managedOnRangeValueChange: any;
		private static managedOnTextInput: any;
		private static managedOnExpandCollapse: any;
		private static managedOnSelection: any;
		private static managedOnFocus: any;
		private static managedOnBlur: any;

		private static createLiveElement(kind: string) {
			const element = document.createElement("div");
			element.classList.add("uno-aria-live");
			element.setAttribute("aria-live", kind);
			return element;
		}

		public static setup() {
			const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();

			// Wire up managed callbacks from WebAssemblyAccessibility.cs
			const accessibilityExports = browserExports.Uno.UI.Runtime.Skia.WebAssemblyAccessibility;
			this.managedEnableAccessibility = accessibilityExports.EnableAccessibility;
			this.managedOnScroll = accessibilityExports.OnScroll;
			this.managedOnInvoke = accessibilityExports.OnInvoke;
			this.managedOnToggle = accessibilityExports.OnToggle;
			this.managedOnRangeValueChange = accessibilityExports.OnRangeValueChange;
			this.managedOnTextInput = accessibilityExports.OnTextInput;
			this.managedOnExpandCollapse = accessibilityExports.OnExpandCollapse;
			this.managedOnSelection = accessibilityExports.OnSelection;
			this.managedOnFocus = accessibilityExports.OnFocus;
			this.managedOnBlur = accessibilityExports.OnBlur;

			this.containerElement = document.getElementById("uno-body");

			// Create live regions for screen reader announcements
			this.politeElement = Accessibility.createLiveElement("polite");
			this.assertiveElement = Accessibility.createLiveElement("assertive");
			this.containerElement.appendChild(this.politeElement);
			this.containerElement.appendChild(this.assertiveElement);

			// Create enable accessibility button (for screen reader activation)
			this.enableAccessibilityButton = document.createElement("div");
			this.enableAccessibilityButton.id = "uno-enable-accessibility";
			this.enableAccessibilityButton.setAttribute("aria-live", "polite");
			this.enableAccessibilityButton.setAttribute("role", "button");
			this.enableAccessibilityButton.setAttribute("tabindex", "0");
			this.enableAccessibilityButton.setAttribute("aria-label", "Enable accessibility");
			this.enableAccessibilityButton.addEventListener("click", this.onEnableAccessibilityButtonClicked.bind(this));
			this.containerElement.appendChild(this.enableAccessibilityButton);

			// Create semantic DOM root container (hidden but accessible)
			this.semanticsRoot = document.createElement("div");
			this.semanticsRoot.id = "uno-semantics-root";
			// Make semantic tree invisible but focusable for screen readers
			this.semanticsRoot.style.filter = "opacity(0%)";
			this.semanticsRoot.style.position = "absolute";
			this.semanticsRoot.style.top = "0";
			this.semanticsRoot.style.left = "0";
			this.semanticsRoot.style.overflow = "hidden";
			this.containerElement.appendChild(this.semanticsRoot);
		}

		/// <summary>
		/// Enables or disables debug mode for the accessibility layer.
		/// When enabled, semantic elements are visible with outlines.
		/// </summary>
		public static enableDebugMode(enabled: boolean) {
			this.debugModeEnabled = enabled;

			if (this.semanticsRoot) {
				if (enabled) {
					// Make semantic elements visible for debugging
					this.semanticsRoot.style.filter = "none";
					this.semanticsRoot.style.pointerEvents = "none"; // Don't interfere with canvas clicks
					this.semanticsRoot.classList.add("uno-a11y-debug");

					// Apply debug styles to all semantic elements
					const elements = this.semanticsRoot.querySelectorAll("[id^='uno-semantics-']");
					elements.forEach((el: HTMLElement) => {
						el.style.outline = "2px solid rgba(0, 255, 0, 0.7)";
						el.style.backgroundColor = "rgba(0, 255, 0, 0.1)";
					});
				} else {
					// Hide semantic elements again
					this.semanticsRoot.style.filter = "opacity(0%)";
					this.semanticsRoot.style.pointerEvents = "";
					this.semanticsRoot.classList.remove("uno-a11y-debug");

					// Remove debug styles
					const elements = this.semanticsRoot.querySelectorAll("[id^='uno-semantics-']");
					elements.forEach((el: HTMLElement) => {
						el.style.outline = "";
						el.style.backgroundColor = "";
					});
				}
			}
		}

		/// <summary>
		/// Gets whether debug mode is currently enabled.
		/// </summary>
		public static isDebugModeEnabled(): boolean {
			return this.debugModeEnabled;
		}

		// Callback accessors for SemanticElements.ts
		public static getCallbacks() {
			return {
				onInvoke: this.managedOnInvoke,
				onToggle: this.managedOnToggle,
				onRangeValueChange: this.managedOnRangeValueChange,
				onTextInput: this.managedOnTextInput,
				onExpandCollapse: this.managedOnExpandCollapse,
				onSelection: this.managedOnSelection,
				onFocus: this.managedOnFocus,
				onBlur: this.managedOnBlur
			};
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

				// Also update native checkbox/radio checked property if applicable
				if (element instanceof HTMLInputElement &&
					(element.type === 'checkbox' || element.type === 'radio')) {
					if (ariaChecked === 'true') {
						element.checked = true;
						element.indeterminate = false;
					} else if (ariaChecked === 'mixed') {
						element.indeterminate = true;
					} else {
						element.checked = false;
						element.indeterminate = false;
					}
				}
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
