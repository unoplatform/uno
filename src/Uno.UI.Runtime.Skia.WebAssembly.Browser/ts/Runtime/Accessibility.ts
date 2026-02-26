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
			console.log('[A11y] Accessibility.setup() — initializing accessibility subsystem');
			const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();

			Accessibility.enableDebugMode(true);

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
			// Use clip-rect pattern instead of filter:opacity(0%) for better
			// VoiceOver compatibility on Safari/macOS
			this.semanticsRoot.style.position = "absolute";
			this.semanticsRoot.style.top = "0";
			this.semanticsRoot.style.left = "0";
			this.semanticsRoot.style.overflow = "hidden";
			this.semanticsRoot.style.opacity = "0";
			this.semanticsRoot.setAttribute("aria-label", "Application content");
			this.containerElement.appendChild(this.semanticsRoot);

			// Temporarily enable accessibility by default (development/debugging only).
			// This calls into managed code to initialize the accessibility subsystem
			// so the semantic DOM is active without requiring the user to press the
			// "Enable accessibility" helper button. Remove once not needed.
			if (this.managedEnableAccessibility) {
				// If the button was added, remove it (we're enabling automatically)
				if (this.enableAccessibilityButton && this.enableAccessibilityButton.parentElement) {
					this.enableAccessibilityButton.parentElement.removeChild(this.enableAccessibilityButton);
				}

				this.managedEnableAccessibility();
				// Initialize subsystem TypeScript modules
				LiveRegion.initialize();
				this.announceAssertive("Accessibility enabled by default.");
			}
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
					this.semanticsRoot.style.opacity = "1";
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
					this.semanticsRoot.style.opacity = "0";
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
			} else {
				element.removeAttribute("tabIndex");
			}
			// Semantic elements must NEVER have pointer-events: all.
			// Mouse events must pass through to the canvas below.
			// Keyboard focus (Tab) and screen reader navigation work
			// independently of pointer-events.
			element.style.pointerEvents = "none";
			element.style.touchAction = "none";
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

			// Initialize subsystem TypeScript modules
			LiveRegion.initialize();

			this.announceAssertive("Accessibility enabled successfully.");
		}

		/**
		 * Focuses a semantic element by handle.
		 * If the element isn't in the DOM yet (timing issue from batched mutations),
		 * retries once after a requestAnimationFrame. This handles the case where
		 * C# fires focus synchronously but the JS DOM mutation hasn't been flushed yet.
		 */
		public static focusSemanticElement(handle: number) {
			console.log(`[A11y] TS focusSemanticElement: handle=${handle}`);
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.focus();
			} else {
				// Element might not be in DOM yet due to batched/deferred mutations.
				// Retry once after the next animation frame.
				requestAnimationFrame(() => {
					const retryElement = Accessibility.getSemanticElementByHandle(handle);
					if (retryElement) {
						console.log(`[A11y] TS focusSemanticElement: DEFERRED SUCCESS handle=${handle}`);
						retryElement.focus();
					} else {
						console.warn(`[A11y] TS focusSemanticElement: element NOT FOUND handle=${handle} (after retry)`);
					}
				});
			}
		}

		/**
		 * Blurs a semantic element.
		 */
		public static blurSemanticElement(handle: number) {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.blur();
			}
		}

		/**
		 * Updates roving tabindex within an ARIA widget group.
		 * Sets tabindex="0" on the active element and tabindex="-1" on
		 * other members of the same group. Only affects elements that
		 * belong to the same ARIA group (e.g., radio buttons sharing the
		 * same 'name' attribute), NOT all siblings.
		 *
		 * If groupHandle is 0, infers the group from the active element's
		 * 'name' attribute (radio buttons) or 'role' (tablist children).
		 * If no group can be inferred, does nothing — general focus
		 * management should not strip tabindex from unrelated elements.
		 */
		public static updateRovingTabindex(groupHandle: number, activeHandle: number) {
			const activeElement = Accessibility.getSemanticElementByHandle(activeHandle);
			if (!activeElement) {
				return;
			}

			// Set the active element to tabindex 0
			activeElement.tabIndex = 0;

			// Determine the group scope. Only radio buttons (sharing the
			// same 'name') and tab-role children of a tablist are grouped.
			const parent = activeElement.parentElement;
			if (!parent) {
				return;
			}

			let groupSelector: string | null = null;

			if (activeElement instanceof HTMLInputElement &&
				activeElement.type === 'radio' &&
				activeElement.name) {
				// Radio group: only affect radios with the same name
				groupSelector = `input[type="radio"][name="${activeElement.name}"]`;
			} else if (activeElement.getAttribute('role') === 'tab' &&
				parent.getAttribute('role') === 'tablist') {
				// Tablist group: only affect tab-role children
				groupSelector = '[role="tab"]';
			}

			if (!groupSelector) {
				// No recognized ARIA group — do not touch sibling tabindexes.
				// General focus management relies on natural tab order.
				return;
			}

			// Only modify tabindex on elements within the same group
			const groupMembers = parent.querySelectorAll(groupSelector);
			groupMembers.forEach((member: HTMLElement) => {
				if (member !== activeElement && member.tabIndex === 0) {
					member.tabIndex = -1;
				}
			});
		}

		public static addRootElementToSemanticsRoot(rootHandle: number, width: number, height: number, x: number, y: number, isFocusable: boolean): void {
			console.debug(`[A11y] addRootElementToSemanticsRoot: handle=${rootHandle} size=${width}x${height} pos=(${x},${y}) focusable=${isFocusable}`);
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
			let parent: HTMLElement | null = Accessibility.getSemanticElementByHandle(parentHandle);
			if (!parent) {
				// Fall back to the semantics root instead of failing.
				// This matches the behavior of the SemanticElements factory path
				// and ensures elements still appear in the accessibility tree
				// even when their semantic parent was pruned.
				console.warn(`[A11y] addSemanticElement: PARENT NOT FOUND — handle=${handle} parentHandle=${parentHandle} controlType='${temporary}' role='${role}' label='${automationId}'. Falling back to semanticsRoot.`);
				parent = this.semanticsRoot;
				if (!parent) {
					console.warn(`[A11y] addSemanticElement: semanticsRoot also null. Element will NOT appear in semantic tree.`);
					return false;
				}
			}

			console.debug(`[A11y] addSemanticElement: handle=${handle} parentHandle=${parentHandle} controlType='${temporary}' role='${role}' label='${automationId}' size=${width}x${height} pos=(${x},${y}) focusable=${isFocusable} visible=${isVisible}`);

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
				if (!child) {
					console.warn(`[A11y] removeSemanticElement: child handle=${childHandle} not found in DOM (parent=${parentHandle})`);
					return;
				}
				console.debug(`[A11y] removeSemanticElement: parent=${parentHandle} child=${childHandle}`);
				parent.removeChild(child);
			} else {
				console.warn(`[A11y] removeSemanticElement: parent handle=${parentHandle} not found in DOM (child=${childHandle})`);
			}
		}

		public static updateIsFocusable(handle: number, isFocusable: boolean): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				console.log(`[A11y] TS updateIsFocusable: handle=${handle} focusable=${isFocusable}`);
				Accessibility.updateElementFocusability(element, isFocusable);
			}
			// Silently skip if element doesn't exist in the semantic DOM.
			// Many controls get IsFocusable updates but aren't in the semantic
			// tree (pruned as non-semantic). This is expected.
		}

		public static updateAriaLabel(handle: number, automationId: string): void {
			console.log(`[A11y] TS updateAriaLabel: handle=${handle} label='${automationId}'`);
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.setAttribute("aria-label", automationId);
			}
		}

		/**
		 * Updates aria-description on a semantic element.
		 * VoiceOver reads this as secondary context after the name.
		 * Falls back to title attribute for broader browser compatibility.
		 */
		public static updateAriaDescription(handle: number, description: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				// Use aria-description (modern) with title fallback (wider support)
				element.setAttribute("aria-description", description);
				element.title = description;
			}
		}

		/**
		 * Updates the ARIA landmark role on a semantic element.
		 * VoiceOver rotor uses landmarks (main, navigation, search, etc.) for quick navigation.
		 */
		public static updateLandmarkRole(handle: number, role: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.setAttribute("role", role);
			}
		}

		/**
		 * Updates aria-roledescription on a semantic element.
		 * Provides a human-readable description of the role for VoiceOver.
		 */
		public static updateAriaRoleDescription(handle: number, roleDescription: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.setAttribute("aria-roledescription", roleDescription);
			}
		}

		public static updateAriaChecked(handle: number, ariaChecked: string): void {
			console.log(`[A11y] TS updateAriaChecked: handle=${handle} checked=${ariaChecked}`);
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
			console.log(`[A11y] TS hideSemanticElement: handle=${handle}`);
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

		private static debugOverlayElement: HTMLDivElement | null = null;

		/**
		 * Updates the debug overlay panel with performance metrics and subsystem state.
		 * Called from C# AccessibilityDebugger when debug mode is enabled.
		 */
		public static updateDebugOverlay(avgFrameOverheadMs: number, totalFrames: number, modalState: string) {
			if (!this.debugModeEnabled) {
				if (this.debugOverlayElement) {
					this.debugOverlayElement.remove();
					this.debugOverlayElement = null;
				}
				return;
			}

			if (!this.debugOverlayElement) {
				this.debugOverlayElement = document.createElement("div");
				this.debugOverlayElement.id = "uno-a11y-debug-overlay";
				this.debugOverlayElement.style.cssText =
					"position:fixed;top:10px;right:10px;background:rgba(0,0,0,0.85);color:#0f0;" +
					"font:12px monospace;padding:10px;border-radius:4px;z-index:99999;" +
					"pointer-events:none;max-width:350px;";
				document.body.appendChild(this.debugOverlayElement);
			}

			// Count semantic elements
			const semanticCount = this.semanticsRoot
				? this.semanticsRoot.querySelectorAll("[id^='uno-semantics-']").length
				: 0;

			// Count virtualized containers
			const virtualizedContainers = this.semanticsRoot
				? this.semanticsRoot.querySelectorAll("[role='listbox'], [role='grid']").length
				: 0;

			// Get active element info
			const activeEl = document.activeElement as HTMLElement;
			const focusInfo = activeEl && activeEl.id?.startsWith("uno-semantics-")
				? activeEl.id.replace("uno-semantics-", "")
				: "none";

			this.debugOverlayElement.innerHTML =
				`<b>A11y Debug</b><br>` +
				`Elements: ${semanticCount}<br>` +
				`Avg frame: ${avgFrameOverheadMs.toFixed(2)}ms (${totalFrames} frames)<br>` +
				`Virtualized containers: ${virtualizedContainers}<br>` +
				`Focus: ${focusInfo}<br>` +
				`Modal: ${modalState}`;
		}
	}
}
