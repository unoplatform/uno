namespace Uno.UI.Runtime.Skia {

	export class Accessibility {
		private static politeElement: HTMLDivElement;
		private static assertiveElement: HTMLDivElement;
		private static enableAccessibilityButton: HTMLDivElement;
		private static semanticsRoot: HTMLDivElement;
		private static containerElement: HTMLElement;
		private static debugModeEnabled: boolean = false;
		private static roleOverrideSnapshots = new WeakMap<HTMLElement, {
			role: string | null;
			attributes: Map<string, string | null>;
		}>();
		private static readonly roleSpecificAriaAttributes = [
			"aria-activedescendant", "aria-checked", "aria-colcount", "aria-colindex", "aria-colspan",
			"aria-expanded", "aria-level", "aria-modal", "aria-multiselectable", "aria-orientation",
			"aria-posinset", "aria-pressed", "aria-readonly", "aria-required", "aria-rowcount",
			"aria-rowindex", "aria-rowspan", "aria-selected", "aria-setsize", "aria-sort",
			"aria-valuemax", "aria-valuemin", "aria-valuenow", "aria-valuetext"
		];

		private static focusSentinelStart: HTMLDivElement | null = null;
		private static focusSentinelEnd: HTMLDivElement | null = null;
		private static isDepartingFocus: boolean = false;

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
		private static managedOnSentinelFocus: any;

		private static managedIsAutoEnableAccessibility: () => boolean;
		private static isAccessibilityActivated: boolean = false;

		private static createLiveElement(kind: string) {
			const element = document.createElement("div");
			element.classList.add("uno-aria-live");
			element.setAttribute("aria-live", kind);
			return element;
		}

		/**
		 * Emits a diagnostic message to the console, but only when accessibility
		 * debug mode is enabled (see AccessibilityDebugger / enableDebugMode).
		 * Normal runs keep the browser console clean — these traces are only
		 * useful while developing the a11y layer and would otherwise be emitted
		 * on every focus change / DOM mutation, even when accessibility is off.
		 */
		public static debugLog(message: string) {
			if (Accessibility.debugModeEnabled) {
				console.debug(message);
			}
		}

		/**
		 * Same as debugLog, but uses console.warn for fallback/recovery paths
		 * (e.g. an element not yet flushed to the DOM). Gated behind debug mode
		 * so it does not spam the console during normal operation.
		 */
		public static debugWarn(message: string) {
			if (Accessibility.debugModeEnabled) {
				console.warn(message);
			}
		}

		public static setup() {
			Accessibility.debugLog('[A11y] Accessibility.setup() — initializing accessibility subsystem');
			const browserExports = WebAssemblyWindowWrapper.getAssemblyExports();

			// Wire up managed callbacks from WebAssemblyAccessibility.cs
			const accessibilityExports = browserExports.Uno.UI.Runtime.Skia.WebAssemblyAccessibility;
			this.managedEnableAccessibility = accessibilityExports.EnableAccessibility;
			this.managedIsAutoEnableAccessibility = accessibilityExports.IsAutoEnableAccessibility;
			this.managedOnScroll = accessibilityExports.OnScroll;
			this.managedOnInvoke = accessibilityExports.OnInvoke;
			this.managedOnToggle = accessibilityExports.OnToggle;
			this.managedOnRangeValueChange = accessibilityExports.OnRangeValueChange;
			this.managedOnTextInput = accessibilityExports.OnTextInput;
			this.managedOnExpandCollapse = accessibilityExports.OnExpandCollapse;
			this.managedOnSelection = accessibilityExports.OnSelection;
			this.managedOnFocus = accessibilityExports.OnFocus;
			this.managedOnBlur = accessibilityExports.OnBlur;
			this.managedOnSentinelFocus = accessibilityExports.OnFocusSentinel;

			this.containerElement = document.getElementById("uno-body");

			// Create live regions for screen reader announcements
			this.politeElement = Accessibility.createLiveElement("polite");
			this.assertiveElement = Accessibility.createLiveElement("assertive");
			this.containerElement.appendChild(this.politeElement);
			this.containerElement.appendChild(this.assertiveElement);

			const autoEnable = this.managedIsAutoEnableAccessibility();

			if (!autoEnable) {
				this.ensureEnableAccessibilityButton();
			}

			// Create semantic DOM root container (hidden but accessible).
			// Uses position:fixed to match the canvas coordinate system (which is also
			// position:fixed). Width/height:100% ensures the container covers the full
			// viewport so overflow:hidden doesn't clip semantic elements at 0×0.
			this.semanticsRoot = document.createElement("div");
			this.semanticsRoot.id = "uno-semantics-root";
			this.semanticsRoot.style.position = "fixed";
			this.semanticsRoot.style.top = "0";
			this.semanticsRoot.style.left = "0";
			this.semanticsRoot.style.width = "100%";
			this.semanticsRoot.style.height = "100%";
			this.semanticsRoot.style.overflow = "hidden";
			this.semanticsRoot.style.opacity = "0";
			this.semanticsRoot.style.pointerEvents = "none";
			this.semanticsRoot.setAttribute("aria-label", "Application content");
			this.containerElement.appendChild(this.semanticsRoot);

			if (autoEnable) {
				// Auto-enable accessibility without requiring user interaction.
				// The C# EnableAccessibility() has retry logic for when
				// Window/RootElement aren't ready yet.
				Accessibility.debugLog('[A11y] Auto-enabling accessibility (FeatureConfiguration.AutomationPeer.AutoEnableAccessibility = true)');
				this.managedEnableAccessibility();
			}
		}

		private static ensureEnableAccessibilityButton(): void {
			const existing = document.getElementById("uno-enable-accessibility") as HTMLDivElement | null;
			if (existing) {
				this.enableAccessibilityButton = existing;
				existing.setAttribute("tabindex", "0");
				existing.removeAttribute("aria-disabled");
				return;
			}

			this.enableAccessibilityButton = document.createElement("div");
			this.enableAccessibilityButton.id = "uno-enable-accessibility";
			this.enableAccessibilityButton.setAttribute("aria-live", "polite");
			this.enableAccessibilityButton.setAttribute("role", "button");
			this.enableAccessibilityButton.setAttribute("tabindex", "0");
			this.enableAccessibilityButton.setAttribute("aria-label", "Enable accessibility");
			this.enableAccessibilityButton.addEventListener("click", this.onEnableAccessibilityButtonClicked.bind(this));
			this.enableAccessibilityButton.addEventListener("keydown", (e) => {
				if (e.key === "Enter" || e.key === " ") {
					e.preventDefault();
					this.onEnableAccessibilityButtonClicked(e as any);
				}
			});
			this.containerElement.prepend(this.enableAccessibilityButton);
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
			const isCurrent = () => Accessibility.isCurrentSemanticElement(element);

			element.addEventListener('wheel', (e) => {
				if (!isCurrent()) {
					e.stopImmediatePropagation();
					return;
				}
				// When scrolling with wheel, we want to prevent scroll events.
				e.preventDefault();
			}, {passive:false});

			element.addEventListener('scroll', (e) => {
				if (!isCurrent()) {
					e.stopImmediatePropagation();
					return;
				}
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
			const owningListbox = element.getAttribute('role') === 'option'
				? element.parentElement?.closest('[role="listbox"]') as HTMLElement | null
				: null;
			if (owningListbox && element.parentElement === owningListbox) {
				element.dataset.unoOptionFocusable = String(isFocusable);
				if (!isFocusable) {
					element.tabIndex = -1;
				}
				Accessibility.synchronizeListboxTabStop(owningListbox);
				return;
			}
			if (element.getAttribute('role') === 'grid') {
				element.dataset.unoGridFocusable = String(isFocusable);
				if (!isFocusable || element.getAttribute('aria-disabled') === 'true') {
					Accessibility.suspendGridTabStops(element);
				} else {
					Accessibility.synchronizeGridTabStop(element);
				}
				return;
			}
			if (element.getAttribute('role') === 'row') {
				element.tabIndex = -1;
				return;
			}
			if (Accessibility.isGridItem(element)) {
				const grid = element.closest('[role="grid"]') as HTMLElement | null;
				if (grid) {
					Accessibility.synchronizeGridTabStop(grid);
				}
				return;
			}

			const desiredTabIndex = isFocusable ? 0 : -1;
			const owningGridItem = element.closest('[role="gridcell"], [role="columnheader"], [role="rowheader"]') as HTMLElement | null;
			if (owningGridItem && owningGridItem !== element) {
				element.dataset.unoGridTabIndex = String(desiredTabIndex);
				Accessibility.updateGridDescendantFocusability(element, owningGridItem, isFocusable);
			} else {
			// Focusable controls participate in the natural tab order (tabindex="0").
			// Non-focusable controls must NOT participate, but they may still need to be
			// programmatically focusable (for screen-reader navigation / focus recovery),
			// so they get tabindex="-1" rather than having the attribute removed —
			// native <button>/<input>/<a> default to tabbable when no tabindex is set.
				element.tabIndex = desiredTabIndex;
			}
			// Semantic elements must NEVER have pointer-events: all.
			// Mouse events must pass through to the canvas below.
			// Keyboard focus (Tab) and screen reader navigation work
			// independently of pointer-events.
			element.style.pointerEvents = "none";
			element.style.touchAction = "none";
		}

		public static getSemanticsRoot(): HTMLElement | null {
			return this.semanticsRoot ?? null;
		}

		public static getSemanticElementById(id: string): HTMLElement | null {
			const root = this.getSemanticsRoot();
			return root?.querySelector<HTMLElement>(`#${CSS.escape(id)}`) ?? null;
		}

		public static getSemanticElementByHandle(handle: number): HTMLElement | null {
			return this.getSemanticElementById(`uno-semantics-${handle}`);
		}

		public static isCurrentSemanticElement(element: HTMLElement): boolean {
			return element.isConnected && this.getSemanticElementById(element.id) === element;
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
			setTimeout(() => {
				if (child.parentNode === ariaLiveElement) {
					ariaLiveElement.removeChild(child);
				}
			}, 300);
		}

		/**
		 * Returns true if the "Enable Accessibility" button is still in the DOM
		 * (i.e. accessibility has not yet been activated by the user).
		 */
		public static isEnableAccessibilityButtonActive(): boolean {
			return document.getElementById("uno-enable-accessibility") !== null;
		}

		private static onEnableAccessibilityButtonClicked(evt: MouseEvent) {
			if (this.enableAccessibilityButton.getAttribute("aria-disabled") === "true") {
				return;
			}
			this.enableAccessibilityButton.setAttribute("aria-disabled", "true");
			this.enableAccessibilityButton.tabIndex = -1;
			this.managedEnableAccessibility();
		}

		public static onAccessibilityActivationSucceeded(): void {
			if (this.isAccessibilityActivated) {
				return;
			}
			this.isAccessibilityActivated = true;
			this.enableAccessibilityButton?.remove();
			LiveRegion.initialize();
			this.announceAssertive("Accessibility enabled successfully.");
		}

		public static onAccessibilityActivationFailed(): void {
			this.isAccessibilityActivated = false;
			this.ensureEnableAccessibilityButton();
			this.announceAssertive("Accessibility could not be enabled. Try again.");
		}

		/**
		 * Focuses a semantic element by handle.
		 * If the element isn't in the DOM yet (timing issue from batched mutations),
		 * retries once after a requestAnimationFrame. This handles the case where
		 * C# fires focus synchronously but the JS DOM mutation hasn't been flushed yet.
		 */
		public static focusSemanticElement(handle: number) {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.focus();
			} else {
				// Element might not be in DOM yet due to batched/deferred mutations.
				// Retry once after the next animation frame.
				requestAnimationFrame(() => {
					const retryElement = Accessibility.getSemanticElementByHandle(handle);
					if (retryElement) {
						retryElement.focus();
					} else {
						Accessibility.debugWarn(`[A11y] TS focusSemanticElement: element NOT FOUND handle=${handle} (after retry)`);
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

		public static installFocusSentinels() {
			if (!this.focusSentinelStart) {
				this.focusSentinelStart = Accessibility.createFocusSentinel("uno-focus-sentinel-start", true);
			}
			if (!this.focusSentinelEnd) {
				this.focusSentinelEnd = Accessibility.createFocusSentinel("uno-focus-sentinel-end", false);
			}

			document.body.insertBefore(this.focusSentinelStart, document.body.firstChild);
			document.body.appendChild(this.focusSentinelEnd);
		}

		private static createFocusSentinel(id: string, isStart: boolean): HTMLDivElement {
			const sentinel = document.createElement("div");
			sentinel.id = id;
			sentinel.tabIndex = 0;
			sentinel.setAttribute("aria-hidden", "true");
			sentinel.style.position = "fixed";
			sentinel.style.width = "1px";
			sentinel.style.height = "1px";
			sentinel.style.padding = "0";
			sentinel.style.margin = "-1px";
			sentinel.style.overflow = "hidden";
			sentinel.style.opacity = "0";
			sentinel.style.pointerEvents = "none";
			sentinel.style.top = "0";
			sentinel.style.left = "0";

			sentinel.addEventListener("focus", () => {
				if (this.isDepartingFocus) {
					return;
				}
				// Defer: browsers revert focus changes made synchronously inside a focus handler.
				setTimeout(() => {
					if (this.managedOnSentinelFocus) {
						this.managedOnSentinelFocus(isStart);
					}
				}, 0);
			});

			return sentinel;
		}

		public static focusDepartureSentinel(isForward: boolean) {
			const sentinel = isForward ? this.focusSentinelEnd : this.focusSentinelStart;
			if (!sentinel) {
				return;
			}

			this.isDepartingFocus = true;
			sentinel.focus();
			setTimeout(() => { this.isDepartingFocus = false; }, 0);
		}

		public static removeFocusSentinels() {
			this.focusSentinelStart?.remove();
			this.focusSentinelEnd?.remove();
			this.focusSentinelStart = null;
			this.focusSentinelEnd = null;
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
			if (activeElement.getAttribute('role') === 'grid') {
				if (!Accessibility.isGridHierarchyEnabled(activeElement)) {
					Accessibility.suspendGridTabStops(activeElement);
					return;
				}
				Accessibility.synchronizeGridTabStop(activeElement, true);
				return;
			}

			let nestedGrid = activeElement.closest('[role="grid"]') as HTMLElement | null;
			while (nestedGrid) {
				const containingGridItem = nestedGrid.parentElement?.closest('[role="gridcell"], [role="columnheader"], [role="rowheader"]') as HTMLElement | null;
				if (!containingGridItem) {
					break;
				}
				const containingGrid = containingGridItem.closest('[role="grid"]') as HTMLElement | null;
				if (!containingGrid || !Accessibility.isGridItemEligible(containingGridItem, containingGrid)) {
					activeElement.tabIndex = -1;
					Accessibility.synchronizeGridTabStop(nestedGrid, true);
					return;
				}

				Accessibility.enterGridInteractionMode(containingGridItem, activeElement);
				nestedGrid = containingGridItem.closest('[role="grid"]') as HTMLElement | null;
			}

			const owningGridItem = activeElement.closest('[role="gridcell"], [role="columnheader"], [role="rowheader"]') as HTMLElement | null;
			if (owningGridItem && owningGridItem !== activeElement) {
				Accessibility.enterGridInteractionMode(owningGridItem, activeElement);
				return;
			}
			if (owningGridItem === activeElement) {
				const grid = activeElement.closest('[role="grid"]') as HTMLElement | null;
				if (!grid || !Accessibility.isGridItemEligible(activeElement, grid)) {
					activeElement.tabIndex = -1;
					if (grid) {
						if (grid.dataset.unoGridActiveId === activeElement.id) {
							delete grid.dataset.unoGridActiveId;
						}
						Accessibility.synchronizeGridTabStop(grid, true);
					}
					return;
				}
				Accessibility.exitGridInteractionMode(activeElement);
			}

			// Promote the active element to the single tab stop (tabindex="0").
			// Sibling group members are demoted to tabindex="-1" below.
			activeElement.tabIndex = 0;

			// Determine the group scope. Only radio buttons (sharing the
			// same 'name') and tab-role children of a tablist are grouped.
			const parent = activeElement.parentElement;
			if (!parent) {
				return;
			}
			if (activeElement.getAttribute('role') === 'option' && parent.getAttribute('role') === 'listbox') {
				Accessibility.synchronizeListboxTabStop(parent, activeElement);
				return;
			}

			let groupSelector: string | null = null;
			let groupRoot: HTMLElement = parent;

			if (activeElement instanceof HTMLInputElement &&
				activeElement.type === 'radio' &&
				activeElement.name) {
				// Radio group: only affect radios with the same name
				groupSelector = `input[type="radio"][name="${CSS.escape(activeElement.name)}"]`;
			} else if (activeElement.getAttribute('role') === 'tab' &&
				parent.getAttribute('role') === 'tablist') {
				// Tablist group: only affect tab-role children
				groupSelector = '[role="tab"]';
			} else if (activeElement.getAttribute('role') === 'menuitem' &&
				parent.getAttribute('role') === 'menu') {
				// Menu group: only affect menuitem-role children
				groupSelector = '[role="menuitem"]';
			} else if (activeElement.getAttribute('role') === 'treeitem') {
				// Tree group: affect treeitem-role siblings at same level
				groupSelector = '[role="treeitem"]';
			} else if (['gridcell', 'columnheader', 'rowheader'].includes(activeElement.getAttribute('role') ?? '')) {
				const grid = activeElement.closest('[role="grid"]') as HTMLElement | null;
				if (grid) {
					grid.dataset.unoGridActiveId = activeElement.id;
					groupRoot = grid;
					groupSelector = '[role="gridcell"], [role="columnheader"], [role="rowheader"]';
				}
			}

			if (!groupSelector) {
				// No recognized ARIA group — do not touch sibling tabindexes.
				// General focus management relies on natural tab order.
				return;
			}

			// Only modify tabindex on elements within the same group
			const groupMembers = groupRoot.querySelectorAll(groupSelector);
			groupMembers.forEach((member: HTMLElement) => {
				if (groupRoot.getAttribute('role') === 'grid' && member.closest('[role="grid"]') !== groupRoot) {
					return;
				}
				if (member !== activeElement && member.tabIndex === 0) {
					member.tabIndex = -1;
				}
			});
		}

		public static synchronizeListboxTabStop(listbox: HTMLElement, preferred: HTMLElement | null = null): void {
			const options = Array.from(listbox.querySelectorAll<HTMLElement>('[role="option"]'))
				.filter(option => option.parentElement === listbox);
			if (listbox.dataset.unoUsesActiveDescendant === 'true') {
				options.forEach(option => option.tabIndex = -1);
				return;
			}

			const isEligible = (option: HTMLElement) =>
				option.dataset.unoOptionFocusable === 'true' &&
				option.getAttribute('aria-disabled') !== 'true';
			const active = preferred && isEligible(preferred)
				? preferred
				: options.find(option => option.tabIndex === 0 && isEligible(option))
					?? options.find(option => option.getAttribute('aria-selected') === 'true' && isEligible(option))
					?? options.find(isEligible)
					?? null;

			options.forEach(option => option.tabIndex = option === active ? 0 : -1);
		}

		public static initializeListboxOption(listbox: HTMLElement, option: HTMLElement, preferred: boolean): void {
			if (listbox.dataset.unoUsesActiveDescendant === 'true') {
				option.tabIndex = -1;
				return;
			}

			const eligible = option.dataset.unoOptionFocusable === 'true' &&
				option.getAttribute('aria-disabled') !== 'true';
			if (!eligible) {
				option.tabIndex = -1;
				return;
			}

			const current = listbox.querySelector<HTMLElement>(':scope > [role="option"][tabindex="0"]');
			if (preferred || !current) {
				if (current && current !== option) {
					current.tabIndex = -1;
				}
				option.tabIndex = 0;
			} else {
				option.tabIndex = -1;
			}
		}

		public static initializeGridTabStop(element: HTMLElement, preferDataCell: boolean): void {
			const grid = element.closest('[role="grid"]') as HTMLElement | null;
			if (!grid) {
				return;
			}

			const activeId = grid.dataset.unoGridActiveId;
			const activeItem = activeId
				? Accessibility.getDirectGridItems(grid).find(item => item.id === activeId) ?? null
				: null;
			const validActiveItem = activeItem && Accessibility.isGridItemEligible(activeItem, grid) ? activeItem : null;
			if (!Accessibility.isGridItemEligible(element, grid)) {
				element.tabIndex = -1;
				return;
			}
			if (!validActiveItem || preferDataCell && validActiveItem.getAttribute('role') !== 'gridcell') {
				if (validActiveItem) {
					validActiveItem.tabIndex = -1;
				}
				grid.dataset.unoGridActiveId = element.id;
				element.dataset.unoGridTabIndex = '0';
				if (grid.dataset.unoGridFocusable !== 'false' && grid.getAttribute('aria-disabled') !== 'true') {
					const owningGridItem = Accessibility.getOwningGridItem(grid);
					element.tabIndex = owningGridItem && owningGridItem.dataset.unoGridInteractionMode !== 'true' ? -1 : 0;
				}
				grid.tabIndex = -1;
				return;
			}

			element.tabIndex = -1;
		}

		public static updateGridDisabledState(grid: HTMLElement, disabled: boolean): void {
			if (disabled) {
				Accessibility.suspendGridTabStops(grid);
			} else {
				Accessibility.synchronizeGridTabStop(grid);
			}
		}

		public static synchronizeGridTabStop(grid: HTMLElement, focusActiveItem: boolean = false): void {
			grid.tabIndex = -1;
			if (!Accessibility.isGridHierarchyEnabled(grid)) {
				Accessibility.suspendGridTabStops(grid);
				return;
			}

			const owningGridItem = Accessibility.getOwningGridItem(grid);
			const items = Accessibility.getDirectGridItems(grid);
			let activeItem = Accessibility.getGridActiveItem(grid, items)
				?? items.find(item => item.getAttribute('role') === 'gridcell')
				?? items[0];
			items.forEach(item => {
				item.tabIndex = -1;
				if (item !== activeItem && item.dataset.unoGridInteractionMode === 'true') {
					Accessibility.exitGridInteractionMode(item);
				}
			});
			if (!activeItem) {
				delete grid.dataset.unoGridActiveId;
				grid.tabIndex = owningGridItem ? -1 : 0;
				if (focusActiveItem && grid.tabIndex === 0 && document.activeElement !== grid) {
					grid.focus();
				}
				return;
			}

			grid.dataset.unoGridActiveId = activeItem.id;
			activeItem.dataset.unoGridTabIndex = '0';
			if (activeItem.dataset.unoGridInteractionMode === 'true') {
				const focusedDescendant = activeItem.contains(document.activeElement) && document.activeElement !== activeItem
					? document.activeElement as HTMLElement
					: activeItem.querySelector('[tabindex="0"]') as HTMLElement | null;
				if (focusedDescendant) {
					activeItem.querySelectorAll('[tabindex="0"]').forEach((descendant: HTMLElement) => {
						if (descendant !== focusedDescendant) {
							descendant.tabIndex = -1;
						}
					});
					focusedDescendant.tabIndex = Number(focusedDescendant.dataset.unoGridTabIndex ?? '0');
					if (focusActiveItem && document.activeElement !== focusedDescendant) {
						focusedDescendant.focus();
					}
					return;
				}
				Accessibility.exitGridInteractionMode(activeItem);
			}
			activeItem.tabIndex = owningGridItem && owningGridItem.dataset.unoGridInteractionMode !== 'true' ? -1 : 0;
			if (focusActiveItem && activeItem.tabIndex === 0 && document.activeElement !== activeItem) {
				activeItem.focus();
			}
		}

		public static suspendGridTabStops(grid: HTMLElement): void {
			const currentItem = Accessibility.getGridActiveItem(grid);
			if (currentItem) {
				grid.dataset.unoGridActiveId = currentItem.id;
			}

			const interactionItems = (Array.from(grid.querySelectorAll('[data-uno-grid-interaction-mode="true"]')) as HTMLElement[])
				.sort((left, right) => Accessibility.getElementDepth(right) - Accessibility.getElementDepth(left));
			interactionItems.forEach(item => Accessibility.exitGridInteractionMode(item));
			grid.querySelectorAll('[role="gridcell"], [role="columnheader"], [role="rowheader"]').forEach((item: HTMLElement) => item.tabIndex = -1);
			grid.tabIndex = -1;

			if (grid.contains(document.activeElement)) {
				const owningGridItem = Accessibility.getOwningGridItem(grid);
				const owningGrid = owningGridItem?.closest('[role="grid"]') as HTMLElement | null;
				if (owningGridItem && owningGrid && Accessibility.isGridItemEligible(owningGridItem, owningGrid)) {
					Accessibility.exitGridInteractionMode(owningGridItem);
					owningGridItem.focus();
				} else if (document.activeElement instanceof HTMLElement) {
					document.activeElement.blur();
				}
			}
		}

		private static getDirectGridItems(grid: HTMLElement): HTMLElement[] {
			return (Array.from(grid.querySelectorAll('[role="gridcell"], [role="columnheader"], [role="rowheader"]')) as HTMLElement[])
				.filter(item => Accessibility.isGridItemEligible(item, grid));
		}

		private static isGridItem(element: HTMLElement): boolean {
			return ['gridcell', 'columnheader', 'rowheader'].includes(element.getAttribute('role') ?? '');
		}

		public static isGridItemEligible(item: HTMLElement, grid: HTMLElement): boolean {
			if (!Accessibility.isGridItem(item) || item.closest('[role="grid"]') !== grid ||
				item.hidden || item.getAttribute('aria-disabled') === 'true' || item.closest('[hidden]') ||
				!Accessibility.isGridHierarchyEnabled(grid)) {
				return false;
			}

			for (let ancestor = item.parentElement; ancestor && ancestor !== grid; ancestor = ancestor.parentElement) {
				if (ancestor.hidden || ancestor.getAttribute('aria-disabled') === 'true') {
					return false;
				}
			}

			return true;
		}

		private static isGridHierarchyEnabled(grid: HTMLElement): boolean {
			let currentGrid: HTMLElement | null = grid;
			while (currentGrid) {
				if (currentGrid.dataset.unoGridFocusable === 'false' || currentGrid.getAttribute('aria-disabled') === 'true' ||
					currentGrid.hidden || currentGrid.closest('[hidden]')) {
					return false;
				}

				const owningItem = Accessibility.getOwningGridItem(currentGrid);
				if (!owningItem) {
					return true;
				}
				const parentGrid = owningItem.closest('[role="grid"]') as HTMLElement | null;
				if (!parentGrid || owningItem.hidden || owningItem.getAttribute('aria-disabled') === 'true') {
					return false;
				}
				for (let ancestor = owningItem.parentElement; ancestor && ancestor !== parentGrid; ancestor = ancestor.parentElement) {
					if (ancestor.hidden || ancestor.getAttribute('aria-disabled') === 'true') {
						return false;
					}
				}
				currentGrid = parentGrid;
			}
			return true;
		}

		public static suspendGridSubtree(element: HTMLElement): void {
			const interactionItems = ([element, ...Array.from(element.querySelectorAll('[data-uno-grid-interaction-mode="true"]'))] as HTMLElement[])
				.filter(item => item.dataset.unoGridInteractionMode === 'true')
				.sort((left, right) => Accessibility.getElementDepth(right) - Accessibility.getElementDepth(left));
			interactionItems.forEach(item => Accessibility.exitGridInteractionMode(item));
			if (element.tabIndex === 0) {
				element.tabIndex = -1;
			}
			element.querySelectorAll('[tabindex="0"]').forEach((descendant: HTMLElement) => descendant.tabIndex = -1);
		}

		private static updateGridDescendantFocusability(element: HTMLElement, owningGridItem: HTMLElement, isFocusable: boolean): void {
			if (owningGridItem.dataset.unoGridInteractionMode !== 'true') {
				element.tabIndex = -1;
				return;
			}

			const current = owningGridItem.contains(document.activeElement) && document.activeElement !== owningGridItem
				? document.activeElement as HTMLElement
				: owningGridItem.querySelector('[tabindex="0"]') as HTMLElement | null;
			if (isFocusable) {
				if (current === element || !current) {
					owningGridItem.querySelectorAll('[tabindex="0"]').forEach((descendant: HTMLElement) => {
						if (descendant !== element) {
							descendant.tabIndex = -1;
						}
					});
					element.tabIndex = Number(element.dataset.unoGridTabIndex ?? '0');
				} else {
					element.tabIndex = -1;
				}
				return;
			}

			element.tabIndex = -1;
			if (current !== element) {
				return;
			}
			const replacement = (Array.from(owningGridItem.querySelectorAll('[data-uno-grid-tab-index]')) as HTMLElement[])
				.find(candidate => candidate !== element && Number(candidate.dataset.unoGridTabIndex ?? '-1') >= 0);
			if (replacement) {
				replacement.tabIndex = Number(replacement.dataset.unoGridTabIndex ?? '0');
				replacement.focus();
			} else {
				Accessibility.exitGridInteractionMode(owningGridItem);
				owningGridItem.focus();
			}
		}

		public static prepareGridItemFocus(item: HTMLElement): boolean {
			const grid = item.closest('[role="grid"]') as HTMLElement | null;
			if (grid && Accessibility.isGridItemEligible(item, grid)) {
				return true;
			}

			item.tabIndex = -1;
			if (grid) {
				if (grid.dataset.unoGridActiveId === item.id) {
					delete grid.dataset.unoGridActiveId;
				}
				Accessibility.synchronizeGridTabStop(grid, true);
			}
			return false;
		}

		private static getGridActiveItem(grid: HTMLElement, items: HTMLElement[] = Accessibility.getDirectGridItems(grid)): HTMLElement | null {
			const activeId = grid.dataset.unoGridActiveId;
			return items.find(item => item.id === activeId) ?? items.find(item => item.tabIndex === 0) ?? null;
		}

		private static getOwningGridItem(grid: HTMLElement): HTMLElement | null {
			return grid.parentElement?.closest('[role="gridcell"], [role="columnheader"], [role="rowheader"]') as HTMLElement | null;
		}

		private static getElementDepth(element: HTMLElement): number {
			let depth = 0;
			for (let current = element.parentElement; current; current = current.parentElement) {
				depth++;
			}
			return depth;
		}

		private static enterGridInteractionMode(gridItem: HTMLElement, activeDescendant: HTMLElement): void {
			const grid = gridItem.closest('[role="grid"]') as HTMLElement | null;
			if (!grid || !Accessibility.isGridItemEligible(gridItem, grid)) {
				return;
			}

			gridItem.dataset.unoGridInteractionMode = 'true';
			grid.dataset.unoGridActiveId = gridItem.id;
			gridItem.dataset.unoGridNavigationTabIndex = String(gridItem.tabIndex);
			gridItem.tabIndex = -1;
			grid.querySelectorAll('[role="gridcell"], [role="columnheader"], [role="rowheader"]').forEach((item: HTMLElement) => {
				if (item.closest('[role="grid"]') === grid && item !== gridItem) {
					item.tabIndex = -1;
				}
			});
			gridItem.querySelectorAll('[data-uno-grid-tab-index]').forEach((descendant: HTMLElement) => {
				descendant.tabIndex = descendant === activeDescendant
					? Number(descendant.dataset.unoGridTabIndex ?? '0')
					: -1;
			});
			activeDescendant.tabIndex = Number(activeDescendant.dataset.unoGridTabIndex ?? '0');
		}

		public static exitGridInteractionMode(gridItem: HTMLElement): void {
			if (gridItem.dataset.unoGridInteractionMode !== 'true') {
				return;
			}

			delete gridItem.dataset.unoGridInteractionMode;
			gridItem.querySelectorAll('[data-uno-grid-tab-index]').forEach((descendant: HTMLElement) => {
				descendant.tabIndex = -1;
			});
			const grid = gridItem.closest('[role="grid"]') as HTMLElement | null;
			gridItem.tabIndex = grid && grid.dataset.unoGridActiveId === gridItem.id && Accessibility.isGridItemEligible(gridItem, grid)
				? 0
				: -1;
			delete gridItem.dataset.unoGridNavigationTabIndex;
		}

		public static addRootElementToSemanticsRoot(rootHandle: number, width: number, height: number, x: number, y: number, isFocusable: boolean): void {
			Accessibility.debugLog(`[A11y] addRootElementToSemanticsRoot: handle=${rootHandle} size=${width}x${height} pos=(${x},${y}) focusable=${isFocusable}`);
			Accessibility.getSemanticElementByHandle(rootHandle)?.remove();
			let element = Accessibility.createSemanticElement(x, y, width, height, rootHandle, isFocusable);
			this.semanticsRoot.appendChild(element);
		}

		public static clearSemanticTree(): void {
			while (this.semanticsRoot?.firstChild) {
				this.semanticsRoot.removeChild(this.semanticsRoot.firstChild);
			}
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
			ariaLabel: string,
			isFocusable: boolean,
			ariaChecked: string,
			isVisible: boolean,
			horizontallyScrollable: boolean,
			verticallyScrollable: boolean,
			temporary: string,
			xamlAutomationId: string): boolean {

			// Remove any pre-existing element with this handle to prevent duplicates
			const existing = Accessibility.getSemanticElementByHandle(handle);
			if (existing) {
				existing.remove();
			}

			let parent: HTMLElement | null = Accessibility.getSemanticElementByHandle(parentHandle);
			if (!parent) {
				// Fall back to the semantics root instead of failing.
				// This matches the behavior of the SemanticElements factory path
				// and ensures elements still appear in the accessibility tree
				// even when their semantic parent was pruned.
				Accessibility.debugWarn(`[A11y] addSemanticElement: PARENT NOT FOUND — handle=${handle} parentHandle=${parentHandle} controlType='${temporary}' role='${role}' label='${ariaLabel}'. Falling back to semanticsRoot.`);
				parent = this.semanticsRoot;
				if (!parent) {
					Accessibility.debugWarn(`[A11y] addSemanticElement: semanticsRoot also null. Element will NOT appear in semantic tree.`);
					return false;
				}
			}

			Accessibility.debugLog(`[A11y] addSemanticElement: handle=${handle} parentHandle=${parentHandle} controlType='${temporary}' role='${role}' labelLength=${ariaLabel?.length ?? 0} size=${width}x${height} pos=(${x},${y}) focusable=${isFocusable} visible=${isVisible}`);

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

			// ariaLabel is the *aria-label* source (peer-resolved Name / AutomationProperties.Name)
			// and is intentionally NOT the AutomationId — keeping the parameter named for what it
			// becomes guards against a future regression where AutomationId leaks back into the
			// accessible name. xamlAutomationId below is the test/dev identifier and never an AT name.
			if (ariaLabel && ariaLabel.trim().length > 0) {
				element.setAttribute("aria-label", ariaLabel.trim());
			}

			if (xamlAutomationId && xamlAutomationId.trim().length > 0) {
				element.setAttribute("xamlautomationid", xamlAutomationId);
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
			Accessibility.updateElementFocusability(element, isFocusable);

			return true;
		}

		public static configureSemanticAction(handle: number, action: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (!element || !action) {
				return;
			}

			const invoke = (event: Event) => {
				if (!Accessibility.isCurrentSemanticElement(element)) {
					event.preventDefault();
					event.stopImmediatePropagation();
					return;
				}

				event.preventDefault();
				switch (action) {
					case "invoke":
						Accessibility.managedOnInvoke?.(handle);
						break;
					case "toggle":
						Accessibility.managedOnToggle?.(handle);
						break;
					case "expandCollapse":
						Accessibility.managedOnExpandCollapse?.(handle);
						break;
					case "selection":
						Accessibility.managedOnSelection?.(handle);
						break;
				}
			};

			element.addEventListener("click", invoke);
			element.addEventListener("keydown", event => {
				if (event.key === "Enter" || event.key === " ") {
					invoke(event);
				}
			});
		}

		public static removeSemanticElement(parentHandle: number, childHandle: number): void {
			const child = Accessibility.getSemanticElementByHandle(childHandle);
			if (!child) {
				Accessibility.debugWarn(`[A11y] removeSemanticElement: child handle=${childHandle} not found in DOM (parent=${parentHandle})`);
				return;
			}
			Accessibility.debugLog(`[A11y] removeSemanticElement: parent=${parentHandle} child=${childHandle}`);
			Accessibility.repairGridFocusBeforeRemoval(child);
			// Use child.remove() instead of parent.removeChild(child) to handle
			// cases where the child's actual DOM parent differs from the semantic parent
			// (e.g., after re-parenting or when duplicate IDs existed previously).
			child.remove();
		}

		public static updateIsFocusable(handle: number, isFocusable: boolean): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				Accessibility.debugLog(`[A11y] TS updateIsFocusable: handle=${handle} focusable=${isFocusable}`);
				if (element.getAttribute('aria-disabled') === 'true' && element.dataset.unoEnabledTabIndex !== undefined) {
					element.dataset.unoEnabledTabIndex = String(isFocusable ? 0 : -1);
					element.tabIndex = -1;
				} else {
					Accessibility.updateElementFocusability(element, isFocusable);
				}
			}
			// Silently skip if element doesn't exist in the semantic DOM.
			// Many controls get IsFocusable updates but aren't in the semantic
			// tree (pruned as non-semantic). This is expected.
		}

		public static setXamlAutomationId(handle: number, automationId: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				// Mirror updateAriaLabel/setAriaStringAttribute: normalize on write by setting the
				// trimmed value (and removing the attribute when empty) so live-sync matches the
				// creation-time path and we never persist leading/trailing whitespace.
				const trimmed = automationId ? automationId.trim() : "";
				if (trimmed.length > 0) {
					element.setAttribute("xamlautomationid", trimmed);
				} else {
					element.removeAttribute("xamlautomationid");
				}
			}
		}

		public static updateAriaLabel(handle: number, automationId: string): void {
			Accessibility.debugLog(`[A11y] TS updateAriaLabel: handle=${handle} labelLength=${automationId?.length ?? 0}`);
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				// Omit an empty/whitespace aria-label rather than emitting aria-label="" (which screen
				// readers announce as "blank"); a nameless control must carry no aria-label attribute.
				// Write the TRIMMED value so live-sync matches the creation-time path
				// (setAriaStringAttribute) and never persists leading/trailing whitespace.
				const trimmed = automationId ? automationId.trim() : "";
				if (trimmed.length > 0) {
					element.setAttribute("aria-label", trimmed);
				} else {
					element.removeAttribute("aria-label");
				}
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
				const trimmed = description ? description.trim() : "";
				if (trimmed.length > 0) {
					// Use aria-description (modern) with title fallback (wider support)
					element.setAttribute("aria-description", trimmed);
					element.title = trimmed;
				} else {
					element.removeAttribute("aria-description");
					element.removeAttribute("title");
				}
			}
		}

		/**
		 * Updates the ARIA landmark role on a semantic element.
		 * VoiceOver rotor uses landmarks (main, navigation, search, etc.) for quick navigation.
		 */
		public static updateLandmarkRole(handle: number, role: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				const snapshot = this.roleOverrideSnapshots.get(element);
				if (snapshot) {
					snapshot.role = role || null;
					return;
				}

				if (role) {
					element.setAttribute("role", role);
				} else {
					element.removeAttribute("role");
				}
			}
		}

		public static updateRoleOverride(handle: number, role: string, isOverride: boolean): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (!element) {
				return;
			}

			let snapshot = this.roleOverrideSnapshots.get(element);
			if (snapshot) {
				this.restoreRoleOverrideSnapshot(element, snapshot);
			}

			if (isOverride) {
				if (!snapshot) {
					snapshot = {
						role: element.getAttribute("role"),
						attributes: new Map(this.roleSpecificAriaAttributes.map(name => [name, element.getAttribute(name)]))
					};
					this.roleOverrideSnapshots.set(element, snapshot);
				}
				if (role) {
					element.setAttribute("role", role);
				} else {
					element.removeAttribute("role");
				}
				this.sanitizeRoleOverrideAttributes(element);
			} else {
				this.roleOverrideSnapshots.delete(element);
				if (role) {
					element.setAttribute("role", role);
				} else {
					element.removeAttribute("role");
				}
			}
		}

		public static updateRoleOverrideToggleState(handle: number, attribute: string, state: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (!element) {
				return;
			}

			element.removeAttribute("aria-checked");
			element.removeAttribute("aria-pressed");
			if (element instanceof HTMLInputElement && (element.type === "checkbox" || element.type === "radio")) {
				element.checked = state === "true";
				element.indeterminate = state === "mixed";
			}
			if (attribute && !(attribute === "aria-checked" && element instanceof HTMLInputElement &&
				(element.type === "checkbox" || element.type === "radio"))) {
				element.setAttribute(attribute, state);
			}
		}

		public static sanitizeActiveRoleOverride(element: HTMLElement): void {
			if (this.roleOverrideSnapshots.has(element)) {
				this.sanitizeRoleOverrideAttributes(element);
			}
		}

		public static updateIntrinsicRoleAttribute(element: HTMLElement, attribute: string, value: string | null): void {
			const snapshot = this.roleOverrideSnapshots.get(element);
			if (snapshot) {
				const intrinsicRole = snapshot.role?.trim().split(/\s+/, 1)[0] ?? "";
				const intrinsicValue = value !== null && (!intrinsicRole || this.roleSupportsAttribute(intrinsicRole, attribute))
					? value
					: null;
				snapshot.attributes.set(attribute, intrinsicValue);

				const presentedRole = element.getAttribute("role")?.trim().split(/\s+/, 1)[0] ?? "";
				if (value === null || presentedRole && !this.roleSupportsAttribute(presentedRole, attribute)) {
					element.removeAttribute(attribute);
				} else {
					element.setAttribute(attribute, value);
				}
				return;
			}

			if (value === null) {
				element.removeAttribute(attribute);
			} else {
				element.setAttribute(attribute, value);
			}
		}

		private static restoreRoleOverrideSnapshot(
			element: HTMLElement,
			snapshot: { role: string | null; attributes: Map<string, string | null> }
		): void {
			if (snapshot.role === null) {
				element.removeAttribute("role");
			} else {
				element.setAttribute("role", snapshot.role);
			}
			snapshot.attributes.forEach((value, name) => {
				if (value === null) {
					element.removeAttribute(name);
				} else {
					element.setAttribute(name, value);
				}
			});
		}

		private static sanitizeRoleOverrideAttributes(element: HTMLElement): void {
			const role = element.getAttribute("role")?.trim().split(/\s+/, 1)[0] ?? "";
			this.roleSpecificAriaAttributes.forEach(attribute => {
				if (element.hasAttribute(attribute) && !this.roleSupportsAttribute(role, attribute)) {
					element.removeAttribute(attribute);
				}
			});
		}

		private static roleSupportsAttribute(role: string, attribute: string): boolean {
			switch (attribute) {
				case "aria-checked":
					return ["checkbox", "menuitemcheckbox", "menuitemradio", "option", "radio", "switch", "treeitem"].includes(role);
				case "aria-pressed":
					return role === "button";
				case "aria-valuemax":
				case "aria-valuemin":
				case "aria-valuenow":
				case "aria-valuetext":
					return ["meter", "progressbar", "scrollbar", "separator", "slider", "spinbutton"].includes(role);
				case "aria-expanded":
					return ["application", "button", "checkbox", "combobox", "gridcell", "link", "listbox", "menuitem", "row", "rowheader", "tab", "treeitem"].includes(role);
				case "aria-selected":
					return ["gridcell", "option", "row", "tab", "treeitem"].includes(role);
				case "aria-readonly":
					return ["checkbox", "combobox", "grid", "gridcell", "listbox", "radiogroup", "slider", "spinbutton", "textbox"].includes(role);
				case "aria-level":
					return ["heading", "listitem", "row", "tab", "treeitem"].includes(role);
				case "aria-posinset":
				case "aria-setsize":
					return ["article", "listitem", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "radio", "row", "tab", "treeitem"].includes(role);
				case "aria-multiselectable":
					return ["grid", "listbox", "tablist", "tree"].includes(role);
				case "aria-colcount":
				case "aria-rowcount":
					return ["grid", "table", "treegrid"].includes(role);
				case "aria-colindex":
				case "aria-colspan":
				case "aria-rowindex":
				case "aria-rowspan":
					return ["cell", "columnheader", "gridcell", "row", "rowheader"].includes(role);
				case "aria-sort":
					return role === "columnheader" || role === "rowheader";
				case "aria-activedescendant":
					return ["application", "combobox", "grid", "group", "listbox", "menu", "menubar", "radiogroup", "row", "searchbox", "select", "spinbutton", "tablist", "textbox", "toolbar", "tree", "treegrid"].includes(role);
				case "aria-orientation":
					return ["listbox", "menu", "menubar", "radiogroup", "scrollbar", "separator", "slider", "tablist", "toolbar", "tree"].includes(role);
				case "aria-required":
					return ["checkbox", "combobox", "gridcell", "listbox", "radiogroup", "searchbox", "spinbutton", "textbox", "tree"].includes(role);
				case "aria-modal":
					return role === "dialog" || role === "alertdialog";
				default:
					return true;
			}
		}

		/**
		 * Updates aria-roledescription on a semantic element.
		 * Provides a human-readable description of the role for VoiceOver.
		 */
		public static updateAriaRoleDescription(handle: number, roleDescription: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				const trimmed = roleDescription?.trim() ?? "";
				if (trimmed) {
					element.setAttribute("aria-roledescription", trimmed);
				} else {
					element.removeAttribute("aria-roledescription");
				}
			}
		}

		public static updateAriaLevel(handle: number, level: number): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				const snapshot = this.roleOverrideSnapshots.get(element);
				if (snapshot) {
					this.updateIntrinsicRoleAttribute(element, "aria-level", level > 0 ? String(level) : null);
				} else if (level > 0 && this.supportsAriaLevel(element)) {
					element.setAttribute("aria-level", String(level));
				} else {
					element.removeAttribute("aria-level");
				}
			}
		}

		private static supportsAriaLevel(element: HTMLElement): boolean {
			const role = element.getAttribute("role")?.trim().split(/\s+/, 1)[0];
			if (role) {
				return role === "heading" || role === "listitem" || role === "row" || role === "tab" || role === "treeitem";
			}

			return this.supportsImplicitAriaLevel(element);
		}

		private static supportsImplicitAriaLevel(element: HTMLElement): boolean {
			return /^H[1-6]$/.test(element.tagName) || element.tagName === "LI" || element.tagName === "TR";
		}

		public static updatePositionInSet(handle: number, positionInSet: number, sizeOfSet: number): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				const valid = positionInSet > 0 && sizeOfSet > 0;
				this.updateIntrinsicRoleAttribute(element, "aria-posinset", valid ? String(positionInSet) : null);
				this.updateIntrinsicRoleAttribute(element, "aria-setsize", valid ? String(sizeOfSet) : null);
			}
		}

		/**
		 * Updates aria-required on a semantic element.
		 * Screen readers announce the field as "required".
		 */
		public static updateAriaRequired(handle: number, required: boolean): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				const nativeRequired = element instanceof HTMLTextAreaElement ||
					element instanceof HTMLInputElement &&
					["checkbox", "date", "datetime-local", "email", "file", "month", "number", "password", "radio", "search", "tel", "text", "time", "url", "week"].includes(element.type);
				if (nativeRequired) {
					(element as HTMLInputElement | HTMLTextAreaElement).required = required;
					element.removeAttribute("aria-required");
					return;
				}

				if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) {
					element.required = false;
				}
				const snapshot = this.roleOverrideSnapshots.get(element);
				const intrinsicRole = (snapshot?.role ?? element.getAttribute("role"))?.trim().split(/\s+/, 1)[0] ?? "";
				if (required && intrinsicRole && this.roleSupportsAttribute(intrinsicRole, "aria-required")) {
					this.updateIntrinsicRoleAttribute(element, "aria-required", "true");
				} else {
					this.updateIntrinsicRoleAttribute(element, "aria-required", null);
				}
			}
		}

		/**
		 * Updates aria-invalid on a semantic element.
		 * Screen readers announce the field as "invalid" when its value fails form validation.
		 */
		public static updateAriaInvalid(handle: number, invalid: boolean): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				if (invalid) {
					element.setAttribute("aria-invalid", "true");
				} else {
					element.removeAttribute("aria-invalid");
				}
			}
		}

		/**
		 * Updates aria-pressed on a toggle button semantic element.
		 */
		public static updateAriaPressed(handle: number, pressed: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				this.updateIntrinsicRoleAttribute(element, "aria-pressed", pressed);
			}
		}

		/**
		 * Updates aria-keyshortcuts on a semantic element.
		 * Screen readers announce the formatted shortcut (e.g. "Ctrl+S") alongside the name.
		 */
		public static updateAriaKeyShortcuts(handle: number, keyShortcuts: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				if (keyShortcuts) {
					element.setAttribute("aria-keyshortcuts", keyShortcuts);
				} else {
					element.removeAttribute("aria-keyshortcuts");
				}
			}
		}

		/**
		 * Updates aria-haspopup on a semantic element from the C# value (FR-028).
		 * The popup kind ("listbox", "menu", "dialog", …) is decided in C# from the control's
		 * ExpandCollapse pattern / control type, never hardcoded in TS.
		 */
		public static updateAriaHasPopup(handle: number, hasPopup: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				// Mirror the omit-when-empty contract: aria-haspopup=" " is a malformed token
				// that AT either rejects or treats as "true"; both are wrong. Trim and omit.
				const trimmed = hasPopup ? hasPopup.trim() : "";
				if (trimmed.length > 0) {
					element.setAttribute("aria-haspopup", trimmed);
				} else {
					element.removeAttribute("aria-haspopup");
				}
			}
		}

		/**
		 * Updates the HTML accesskey attribute on a semantic element (FR-028).
		 * Sourced from AutomationProperties.AccessKey (a mnemonic, e.g. "F"). This is distinct
		 * from aria-keyshortcuts (the AcceleratorKey activation shortcut).
		 */
		public static setAccessKey(handle: number, accessKey: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				// Mirror the omit-when-empty contract used by updateAriaLabel / setXamlAutomationId:
				// whitespace-only values would emit accesskey=" ", which is meaningless to AT and
				// can interfere with browser shortcut handling.
				const trimmed = accessKey ? accessKey.trim() : "";
				if (trimmed.length > 0) {
					element.setAttribute("accesskey", trimmed);
				} else {
					element.removeAttribute("accesskey");
				}
			}
		}

		/**
		 * Updates aria-modal on a semantic element.
		 * Used for dialogs that should scope screen reader announcements.
		 */
		public static updateAriaModal(handle: number, modal: boolean): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				this.updateIntrinsicRoleAttribute(element, "aria-modal", modal ? "true" : null);
			}
		}

		/**
		 * Updates aria-busy on a semantic element.
		 * Mapped from AutomationProperties.ItemStatus when the status indicates the
		 * element is busy/loading, so screen readers suppress reading transient content.
		 */
		public static updateAriaBusy(handle: number, busy: boolean): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				if (busy) {
					element.setAttribute("aria-busy", "true");
				} else {
					element.removeAttribute("aria-busy");
				}
			}
		}

		/**
		 * Updates the lang attribute on a semantic element.
		 * Mapped from AutomationProperties.Culture so screen readers pronounce the
		 * content using the correct locale.
		 */
		public static updateLang(handle: number, lang: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				// Mirror the omit-when-empty contract: lang=" " is invalid per the HTML/BCP-47
				// language-tag grammar and would make AT fall back unpredictably. Trim and omit.
				const trimmed = lang ? lang.trim() : "";
				if (trimmed.length > 0) {
					element.setAttribute("lang", trimmed);
				} else {
					element.removeAttribute("lang");
				}
			}
		}

		/**
		 * Updates aria-live on a semantic element for live region announcements.
		 * Screen readers monitor elements with aria-live for content changes.
		 */
		public static updateAriaLive(handle: number, ariaLive: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				// aria-atomic is intentionally NOT forced here (FR-028). Defaulting every live
				// region to aria-atomic="true" makes screen readers re-announce the entire region
				// on any change; the browser default (false — announce only changed nodes) is
				// correct for the common status/log case. A region whose WinUI semantics require
				// atomic announcement must opt in explicitly elsewhere.
				const trimmed = ariaLive?.trim() ?? "";
				if (trimmed) {
					element.setAttribute("aria-live", trimmed);
				} else {
					element.removeAttribute("aria-live");
				}
			}
		}

		/**
		 * Updates aria-describedby on a semantic element.
		 * References other semantic elements by their IDs (space-separated).
		 */
		/**
		 * Updates aria-labelledby on a semantic element.
		 * References the labeling element by its DOM ID.
		 */
		public static updateAriaLabelledBy(handle: number, idList: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				if (idList) {
					element.setAttribute("aria-labelledby", idList);
				} else {
					element.removeAttribute("aria-labelledby");
				}
			}
		}

		public static updateAriaDescribedBy(handle: number, idList: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				if (idList) {
					element.setAttribute("aria-describedby", idList);
				} else {
					element.removeAttribute("aria-describedby");
				}
			}
		}

		/**
		 * Updates aria-controls on a semantic element.
		 * References other semantic elements by their IDs (space-separated).
		 */
		public static updateAriaControls(handle: number, idList: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.dataset.unoAuthoredControls = idList.trim();
				Accessibility.applyAriaControls(element);
			}
		}

		public static updateRuntimeAriaControls(handle: number, idList: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.dataset.unoRuntimeControls = idList.trim();
				Accessibility.applyAriaControls(element);
			}
		}

		private static applyAriaControls(element: HTMLElement): void {
			const controls: string[] = [];
			[element.dataset.unoAuthoredControls, element.dataset.unoRuntimeControls].forEach(value => {
				if (value) {
					controls.push(...value.split(/\s+/).filter(Boolean));
				}
			});
			const idList = Array.from(new Set(controls)).join(' ');
			if (idList) {
				element.setAttribute('aria-controls', idList);
			} else {
				element.removeAttribute('aria-controls');
			}
		}

		/**
		 * Updates aria-flowto on a semantic element.
		 * Defines the next element(s) in an alternative reading order.
		 */
		public static updateAriaFlowTo(handle: number, idList: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.dataset.unoAuthoredFlowTo = idList.trim();
				this.applyAriaFlowTo(element);
			}
		}

		public static updateInverseAriaFlowTo(handle: number, idList: string): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				element.dataset.unoInverseFlowTo = idList.trim();
				this.applyAriaFlowTo(element);
			}
		}

		private static applyAriaFlowTo(element: HTMLElement): void {
			const ids: string[] = [];
			[element.dataset.unoAuthoredFlowTo, element.dataset.unoInverseFlowTo].forEach(value => {
				if (value) {
					ids.push(...value.split(/\s+/).filter(Boolean));
				}
			});
			const idList = Array.from(new Set(ids)).join(" ");
			if (idList) {
				element.setAttribute("aria-flowto", idList);
			} else {
				element.removeAttribute("aria-flowto");
			}
		}

		public static updateAriaChecked(handle: number, ariaChecked: string): void {
			Accessibility.debugLog(`[A11y] TS updateAriaChecked: handle=${handle} checked=${ariaChecked}`);
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
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
					element.removeAttribute("aria-checked");
				} else {
					this.updateIntrinsicRoleAttribute(element, "aria-checked", ariaChecked);
				}
			}
		}

		public static updateAriaAttribute(handle: number, attribute: string, value: string | null): void {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				this.updateIntrinsicRoleAttribute(element, attribute, value);
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
			Accessibility.debugLog(`[A11y] TS hideSemanticElement: handle=${handle}`);
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				Accessibility.repairGridFocusBeforeRemoval(element);
				element.hidden = true;
			}
		}

		public static repairGridFocusBeforeRemoval(element: HTMLElement): void {
			if (element.contains(document.activeElement)) {
				let nestedGrid = document.activeElement?.closest('[role="grid"]') as HTMLElement | null;
				while (nestedGrid && element.contains(nestedGrid)) {
					const containingGridItem = nestedGrid.parentElement?.closest('[role="gridcell"], [role="columnheader"], [role="rowheader"]') as HTMLElement | null;
					if (!containingGridItem) {
						break;
					}

					Accessibility.exitGridInteractionMode(containingGridItem);
					nestedGrid = containingGridItem.closest('[role="grid"]') as HTMLElement | null;
				}
			}

			const grid = element.closest('[role="grid"]') as HTMLElement | null;
			if (!grid || grid === element) {
				if (grid === element) {
					Accessibility.suspendGridTabStops(grid);
				}
				return;
			}
			const owningGridItem = element.closest('[role="gridcell"], [role="columnheader"], [role="rowheader"]') as HTMLElement | null;
			const removesOwningGridItem = !!owningGridItem &&
				(element === owningGridItem || element.contains(owningGridItem));
			const containedFocus = element.contains(document.activeElement);
			const activeId = grid.dataset.unoGridActiveId;
			const activeItem = activeId
				? Accessibility.getDirectGridItems(grid).find(item => item.id === activeId) ?? null
				: null;
			const removesActiveItem = element.id === activeId || !!(activeItem && element.contains(activeItem));
			if (!containedFocus && !removesActiveItem && element.tabIndex !== 0 && !element.querySelector('[tabindex="0"]')) {
				return;
			}
			if (owningGridItem && !removesOwningGridItem && containedFocus) {
				Accessibility.exitGridInteractionMode(owningGridItem);
				owningGridItem.focus();
				return;
			}

			const allItems = Accessibility.getDirectGridItems(grid);
			const removedIndex = allItems.findIndex(item => item === element || element.contains(item));
			const items = allItems.filter((item: HTMLElement) =>
				item !== element &&
				!element.contains(item));
			const replacement = items[Math.min(Math.max(removedIndex, 0), items.length - 1)] ?? null;
			const wasActiveStop = element.tabIndex === 0 || !!element.querySelector('[tabindex="0"]');
			const containingGridItem = Accessibility.getOwningGridItem(grid);
			const remainingDataCell = items.find(item => item.getAttribute('role') === 'gridcell');
			if (!remainingDataCell) {
				delete grid.dataset.unoGridHasDataCell;
			}
			if (wasActiveStop && replacement) {
				grid.dataset.unoGridActiveId = replacement.id;
			}
			if (containedFocus && replacement) {
				replacement.focus();
			} else if (containedFocus && !replacement) {
				if (containingGridItem) {
					Accessibility.exitGridInteractionMode(containingGridItem);
					containingGridItem.focus();
				}
			}
			queueMicrotask(() => {
				if (!grid.isConnected) {
					return;
				}
				Accessibility.synchronizeGridTabStop(grid);
				if (containedFocus && !replacement && !containingGridItem) {
					grid.focus();
				}
			});
		}

		public static updateSemanticElementPositioning(handle: number, width: number, height: number, x: number, y: number) {
			const element = Accessibility.getSemanticElementByHandle(handle);
			if (element) {
				const wasHidden = element.hidden;
				element.hidden = false;
				element.style.left = `${x}px`;
				element.style.top = `${y}px`;
				element.style.width = `${width}px`;
				element.style.height = `${height}px`;
				const grid = wasHidden ? element.closest('[role="grid"]') as HTMLElement | null : null;
				if (grid) {
					Accessibility.synchronizeGridTabStop(grid);
				}
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

			const overlay = this.debugOverlayElement;
			while (overlay.firstChild) {
				overlay.removeChild(overlay.firstChild);
			}

			const title = document.createElement("b");
			title.textContent = "A11y Debug";
			overlay.appendChild(title);

			const lines = [
				`Elements: ${semanticCount}`,
				`Avg frame: ${avgFrameOverheadMs.toFixed(2)}ms (${totalFrames} frames)`,
				`Virtualized containers: ${virtualizedContainers}`,
				`Focus: ${focusInfo}`,
				`Modal: ${modalState}`
			];

			// Text nodes rather than innerHTML: modalState and the focus id are runtime-supplied,
			// and the overlay must never turn them into markup.
			for (const line of lines) {
				overlay.appendChild(document.createElement("br"));
				overlay.appendChild(document.createTextNode(line));
			}
		}
	}
}
