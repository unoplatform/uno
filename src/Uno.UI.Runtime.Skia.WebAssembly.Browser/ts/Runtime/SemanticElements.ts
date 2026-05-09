namespace Uno.UI.Runtime.Skia {

	/**
	 * Type of semantic HTML element to create.
	 */
	export type SemanticElementType =
		| 'generic'      // <div> with ARIA role
		| 'button'       // <button>
		| 'togglebutton' // <button aria-pressed>
		| 'switch'       // <button role="switch" aria-checked>
		| 'heading'      // <h1>-<h6> (VoiceOver rotor heading navigation)
		| 'checkbox'     // <input type="checkbox">
		| 'radio'        // <input type="radio">
		| 'slider'       // <input type="range">
		| 'textbox'      // <input type="text">
		| 'textarea'     // <textarea>
		| 'password'     // <input type="password">
		| 'combobox'     // <div role="combobox">
		| 'listbox'      // <div role="listbox">
		| 'listitem'     // <div role="option">
		| 'link';        // <a>

	/**
	 * Slider orientation.
	 */
	export type SliderOrientation = 'horizontal' | 'vertical';

	/**
	 * Checkbox/Toggle state.
	 */
	export type CheckedState = 'true' | 'false' | 'mixed';

	/**
	 * Factory for creating semantic DOM elements.
	 * Each element type has specific event handlers that call back to managed code.
	 */
	export class SemanticElements {

		/**
		 * Gets the semantics root element.
		 */
		private static getSemanticsRoot(): HTMLElement | null {
			return document.getElementById("uno-semantics-root");
		}

		/**
		 * Gets callbacks from Accessibility class.
		 */
		private static getCallbacks() {
			return Accessibility.getCallbacks();
		}

		/**
		 * Removes any existing DOM element with the given id to prevent duplicates.
		 * Returns the removed element (for potential reuse) or null.
		 */
		private static removeExistingById(id: string): HTMLElement | null {
			const existing = document.getElementById(id);
			if (existing) {
				existing.remove();
			}
			return existing;
		}

		/**
		 * Appends a created semantic element to its proper parent in the tree,
		 * or falls back to the semantics root if the parent is not found.
		 * Automatically removes any pre-existing element with the same id.
		 */
		private static appendToParent(
			element: HTMLElement,
			parentHandle: number,
			index: number | null
		): void {
			// Remove any pre-existing element with this id to prevent duplicates
			SemanticElements.removeExistingById(element.id);

			let parent: HTMLElement | null = null;
			if (parentHandle !== 0) {
				parent = document.getElementById(`uno-semantics-${parentHandle}`);
			}
			if (!parent) {
				console.warn(`[A11y] TS appendToParent: parent NOT FOUND handle=${parentHandle} for element=${element.id} — falling back to semanticsRoot`);
				parent = SemanticElements.getSemanticsRoot();
			}
			if (parent) {
				if (index != null && index < parent.childElementCount) {
					parent.insertBefore(element, parent.children[index]);
				} else {
					parent.appendChild(element);
				}
			}
		}

		/**
		 * Applies common positioning and styling to all semantic elements.
		 */
		private static applyCommonStyles(
			element: HTMLElement,
			x: number,
			y: number,
			width: number,
			height: number,
			handle: number
		): void {
			element.style.position = 'absolute';
			element.style.left = `${x}px`;
			element.style.top = `${y}px`;
			element.style.width = `${width}px`;
			element.style.height = `${height}px`;
			element.id = `uno-semantics-${handle}`;

			const callbacks = this.getCallbacks();

			// Common event handlers
			element.addEventListener('focus', () => {
				if (callbacks.onFocus) {
					callbacks.onFocus(handle);
				}
			});

			element.addEventListener('blur', () => {
				if (callbacks.onBlur) {
					callbacks.onBlur(handle);
				}
			});

			// Apply debug styling if debug mode is enabled
			if (Accessibility.isDebugModeEnabled()) {
				element.style.outline = "2px solid rgba(0, 255, 0, 0.7)";
				element.style.backgroundColor = "rgba(0, 255, 0, 0.1)";
			}
		}

		/**
		 * Creates a button semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createButtonElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			disabled: boolean
		): void {
			const element = document.createElement('button');
			this.applyCommonStyles(element, x, y, width, height, handle);

			// Enable focus and interaction
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			if (label) {
				element.setAttribute('aria-label', label);
			}

			if (disabled) {
				element.disabled = true;
				element.setAttribute('aria-disabled', 'true');
			}

			const callbacks = this.getCallbacks();

			// Click handler for button activation.
			// <button> natively fires 'click' on both Enter and Space,
			// so no separate keydown handler is needed.
			element.addEventListener('click', (e) => {
				e.preventDefault();
				if (callbacks.onInvoke) {
					callbacks.onInvoke(handle);
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a toggle button semantic element (button with aria-pressed).
		 * Used for ToggleButton, AppBarToggleButton, etc.
		 * Called from C# via JSImport.
		 */
		public static createToggleButtonElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			pressed: string,
			disabled: boolean
		): void {
			const element = document.createElement('button');
			this.applyCommonStyles(element, x, y, width, height, handle);

			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			// aria-pressed for toggle button pattern (distinct from aria-checked for checkboxes)
			element.setAttribute('aria-pressed', pressed);

			if (label) {
				element.setAttribute('aria-label', label);
			}

			if (disabled) {
				element.disabled = true;
				element.setAttribute('aria-disabled', 'true');
			}

			const callbacks = this.getCallbacks();

			// <button> natively fires 'click' on both Enter and Space,
			// so no separate keydown handler is needed.
			element.addEventListener('click', (e) => {
				e.preventDefault();
				if (callbacks.onToggle) {
					callbacks.onToggle(handle);
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a switch semantic element (role="switch" with aria-checked).
		 * Used for ToggleSwitch which maps to the ARIA switch pattern.
		 * Called from C# via JSImport.
		 */
		public static createSwitchElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			isOn: string,
			disabled: boolean
		): void {
			const element = document.createElement('button');
			this.applyCommonStyles(element, x, y, width, height, handle);

			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			// role="switch" with aria-checked for ToggleSwitch (ARIA switch pattern)
			element.setAttribute('role', 'switch');
			element.setAttribute('aria-checked', isOn);

			if (label) {
				element.setAttribute('aria-label', label);
			}

			if (disabled) {
				element.disabled = true;
				element.setAttribute('aria-disabled', 'true');
			}

			const callbacks = this.getCallbacks();

			// <button> natively fires 'click' on both Enter and Space,
			// so no separate keydown handler is needed.
			element.addEventListener('click', (e) => {
				e.preventDefault();
				if (callbacks.onToggle) {
					callbacks.onToggle(handle);
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a slider (range input) semantic element.
		 * Called from C# via JSImport.
		 */
		public static createSliderElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			value: number,
			min: number,
			max: number,
			step: number,
			orientation: string,
			valueText: string | null
		): void {
			const element = document.createElement('input');
			element.type = 'range';
			this.applyCommonStyles(element, x, y, width, height, handle);

			// Enable focus and interaction
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			// Set range properties
			element.min = String(min);
			element.max = String(max);
			element.value = String(value);
			element.step = String(step);

			// Set ARIA value attributes for screen readers
			element.setAttribute('aria-valuenow', String(value));
			element.setAttribute('aria-valuemin', String(min));
			element.setAttribute('aria-valuemax', String(max));

			// aria-valuetext: VoiceOver reads this instead of the raw number
			// when a human-readable value description is available
			if (valueText) {
				element.setAttribute('aria-valuetext', valueText);
			}

			if (orientation === 'vertical') {
				// Some browsers support orient attribute, others need CSS
				element.setAttribute('orient', 'vertical');
				element.style.writingMode = 'bt-lr';
				element.style.webkitAppearance = 'slider-vertical';
			}

			const callbacks = this.getCallbacks();

			// Input event handler for value changes (T030)
			element.addEventListener('input', () => {
				if (callbacks.onRangeValueChange) {
					callbacks.onRangeValueChange(handle, parseFloat(element.value));
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a checkbox semantic element.
		 * Called from C# via JSImport.
		 */
		public static createCheckboxElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			checkedState: string | null,
			label: string | null
		): void {
			const element = document.createElement('input');
			element.type = 'checkbox';
			this.applyCommonStyles(element, x, y, width, height, handle);

			// Enable focus and interaction
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			if (label) {
				element.setAttribute('aria-label', label);
			}

			// Set checked state (WCAG 4.1.2: aria-checked must match visual state)
			if (checkedState === 'true') {
				element.checked = true;
				element.setAttribute('aria-checked', 'true');
			} else if (checkedState === 'mixed') {
				element.indeterminate = true;
				element.setAttribute('aria-checked', 'mixed');
			} else {
				element.setAttribute('aria-checked', 'false');
			}

			const callbacks = this.getCallbacks();

			// Change event handler for toggle events (T040)
			element.addEventListener('change', () => {
				if (callbacks.onToggle) {
					callbacks.onToggle(handle);
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a radio button semantic element.
		 * Called from C# via JSImport.
		 */
		public static createRadioElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			checked: boolean,
			label: string | null,
			groupName: string | null
		): void {
			const element = document.createElement('input');
			element.type = 'radio';
			this.applyCommonStyles(element, x, y, width, height, handle);

			// Enable focus and interaction
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			if (label) {
				element.setAttribute('aria-label', label);
			}

			if (groupName) {
				element.name = groupName;
			}

			element.checked = checked;

			const callbacks = this.getCallbacks();

			// Change event handler for toggle events
			element.addEventListener('change', () => {
				if (callbacks.onToggle) {
					callbacks.onToggle(handle);
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a heading semantic element (h1-h6).
		 * VoiceOver uses headings for rotor navigation (VO+U → Headings).
		 * Called from C# via JSImport.
		 */
		public static createHeadingElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			level: number,
			label: string | null
		): void {
			// Clamp heading level to valid h1-h6 range
			const clampedLevel = Math.max(1, Math.min(6, level));
			const element = document.createElement(`h${clampedLevel}`) as HTMLHeadingElement;
			this.applyCommonStyles(element, x, y, width, height, handle);

			// Enable focus for screen readers
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			// Reset default heading styles so they don't affect layout
			element.style.margin = '0';
			element.style.padding = '0';
			element.style.fontSize = 'inherit';
			element.style.fontWeight = 'inherit';

			if (label) {
				element.setAttribute('aria-label', label);
				element.textContent = label;
			}

			element.setAttribute('aria-level', String(clampedLevel));

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a text input semantic element.
		 * Called from C# via JSImport.
		 */
		public static createTextBoxElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			value: string,
			multiline: boolean,
			password: boolean,
			isReadOnly: boolean,
			selectionStart: number,
			selectionEnd: number
		): void {
			let element: HTMLInputElement | HTMLTextAreaElement;

			if (multiline) {
				element = document.createElement('textarea');
			} else {
				element = document.createElement('input');
				element.type = password ? 'password' : 'text';
			}

			this.applyCommonStyles(element, x, y, width, height, handle);

			// Enable focus and interaction
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			element.value = value;
			const maxLen = value.length;
			const initialSelectionStart = Math.max(0, Math.min(selectionStart, maxLen));
			const initialSelectionEnd = Math.max(initialSelectionStart, Math.min(selectionEnd, maxLen));
			try {
				element.setSelectionRange(initialSelectionStart, initialSelectionEnd);
			} catch {
				// Some browsers/input types may reject selection updates before focus.
			}

			if (isReadOnly) {
				element.readOnly = true;
			}

			const callbacks = this.getCallbacks();

			// Block native character insertion so the managed TextBox KeyDown path is the single
			// source of text edits. Without this, the key is inserted both by the browser (into
			// this <input>) and by managed OnKeyDownSkia, producing duplicated input once a11y
			// moves focus to the semantic element instead of the invisible TextBox <input>.
			BrowserInvisibleTextBoxViewExtension.attachTextInputKeyHandlers(element, multiline);

			// Input event handler for text changes (T050)
			element.addEventListener('input', () => {
				if (callbacks.onTextInput) {
					callbacks.onTextInput(
						handle,
						element.value,
						element.selectionStart ?? 0,
						element.selectionEnd ?? 0
					);
				}
			});

			// Handle IME composition events for international text input (T055)
			element.addEventListener('compositionend', () => {
				if (callbacks.onTextInput) {
					callbacks.onTextInput(
						handle,
						element.value,
						element.selectionStart ?? 0,
						element.selectionEnd ?? 0
					);
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a combobox semantic element.
		 * Called from C# via JSImport.
		 */
		public static createComboBoxElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			expanded: boolean,
			selectedValue: string | null
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);

			element.setAttribute('role', 'combobox');
			element.setAttribute('aria-expanded', String(expanded));
			element.setAttribute('aria-haspopup', 'listbox');
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			if (selectedValue) {
				element.setAttribute('aria-label', selectedValue);
			}

			const callbacks = this.getCallbacks();

			// Keyboard handlers for expand/collapse (WAI-ARIA combobox pattern)
			element.addEventListener('keydown', (e) => {
				if (e.key === 'Enter' || e.key === ' ' || (e.key === 'ArrowDown' && e.altKey)) {
					e.preventDefault();
					if (callbacks.onExpandCollapse) {
						callbacks.onExpandCollapse(handle);
					}
				} else if (e.key === 'Escape') {
					// Escape collapses an open popup (WAI-ARIA combobox pattern)
					if (element.getAttribute('aria-expanded') === 'true') {
						e.preventDefault();
						if (callbacks.onExpandCollapse) {
							callbacks.onExpandCollapse(handle);
						}
					}
				}
			});

			// Click handler for expand/collapse
			element.addEventListener('click', (e) => {
				e.preventDefault();
				if (callbacks.onExpandCollapse) {
					callbacks.onExpandCollapse(handle);
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a listbox semantic element.
		 * Called from C# via JSImport.
		 */
		public static createListBoxElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			multiselect: boolean
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);

			element.setAttribute('role', 'listbox');
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

			if (multiselect) {
				element.setAttribute('aria-multiselectable', 'true');
			}

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a list item semantic element.
		 * Called from C# via JSImport.
		 */
		public static createListItemElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			selected: boolean,
			positionInSet: number,
			sizeOfSet: number
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);

			element.setAttribute('role', 'option');
			element.setAttribute('aria-selected', String(selected));
			element.setAttribute('aria-posinset', String(positionInSet));
			element.setAttribute('aria-setsize', String(sizeOfSet));
			element.tabIndex = -1; // Focusable but not in tab order (parent listbox manages focus)
			element.style.pointerEvents = 'none';

			const callbacks = this.getCallbacks();

			// Click handler for selection (T073)
			element.addEventListener('click', () => {
				if (callbacks.onSelection) {
					callbacks.onSelection(handle);
				}
			});

			// Keyboard handler for Enter/Space selection
			element.addEventListener('keydown', (e) => {
				if (e.key === 'Enter' || e.key === ' ') {
					e.preventDefault();
					if (callbacks.onSelection) {
						callbacks.onSelection(handle);
					}
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Updates the value of a slider element and its ARIA attributes.
		 */
		public static updateSliderValue(handle: number, value: number, min: number, max: number, valueText: string | null): void {
			const element = document.getElementById(`uno-semantics-${handle}`) as HTMLInputElement;
			if (element && element.type === 'range') {
				element.min = String(min);
				element.max = String(max);
				element.value = String(value);
				element.setAttribute('aria-valuenow', String(value));
				element.setAttribute('aria-valuemin', String(min));
				element.setAttribute('aria-valuemax', String(max));
				if (valueText) {
					element.setAttribute('aria-valuetext', valueText);
				} else {
					element.removeAttribute('aria-valuetext');
				}
			}
		}

		/**
		 * Updates the value of a text input element.
		 */
		public static updateTextBoxValue(
			handle: number,
			value: string,
			selectionStart: number,
			selectionEnd: number
		): void {
			const element = document.getElementById(`uno-semantics-${handle}`) as HTMLInputElement | HTMLTextAreaElement;
			if (element && (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA')) {
				// Skip no-op writes to avoid caret and IME churn when the DOM value already matches.
				if (element.value !== value) {
					element.value = value;
				}

				// Negative sentinel from C# means "do not touch selection".
				// This preserves the browser-managed caret for browser-originated a11y text input.
				if (selectionStart >= 0 && selectionEnd >= 0) {
					// Validate selection range to prevent exceptions
					const maxLen = value.length;
					const start = Math.max(0, Math.min(selectionStart, maxLen));
					const end = Math.max(start, Math.min(selectionEnd, maxLen));
					try {
						element.setSelectionRange(start, end);
					} catch {
						// Some input types (e.g., password in some browsers) don't support setSelectionRange
					}
				}
			}
		}

		/**
		 * Updates the read-only state of a text input element.
		 */
		public static updateTextBoxReadOnly(handle: number, isReadOnly: boolean): void {
			const element = document.getElementById(`uno-semantics-${handle}`) as HTMLInputElement | HTMLTextAreaElement;
			if (element && (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA')) {
				element.readOnly = isReadOnly;
				if (isReadOnly) {
					element.setAttribute('aria-readonly', 'true');
				} else {
					element.removeAttribute('aria-readonly');
				}
			}
		}

		/**
		 * Updates the placeholder text of a text input element.
		 */
		public static updateTextBoxPlaceholder(handle: number, placeholder: string): void {
			const element = document.getElementById(`uno-semantics-${handle}`) as HTMLInputElement | HTMLTextAreaElement;
			if (element && (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA')) {
				element.placeholder = placeholder ?? '';
			}
		}

		/**
		 * Updates the expanded/collapsed state of a combobox element.
		 */
		public static updateExpandCollapseState(handle: number, expanded: boolean): void {
			const element = document.getElementById(`uno-semantics-${handle}`);
			if (element) {
				element.setAttribute('aria-expanded', String(expanded));
				// Clear activedescendant when collapsing
				if (!expanded) {
					element.removeAttribute('aria-activedescendant');
				}
			}
		}

		/**
		 * Updates aria-activedescendant on a combobox/listbox to point to the active option.
		 * Screen readers use this to announce the currently focused option without moving DOM focus.
		 */
		public static updateActiveDescendant(containerHandle: number, activeItemHandle: number): void {
			const container = document.getElementById(`uno-semantics-${containerHandle}`);
			if (container) {
				if (activeItemHandle !== 0) {
					const activeId = `uno-semantics-${activeItemHandle}`;
					container.setAttribute('aria-activedescendant', activeId);
				} else {
					container.removeAttribute('aria-activedescendant');
				}
			}
		}

		/**
		 * Updates the selected state of a list item element.
		 */
		public static updateSelectionState(handle: number, selected: boolean): void {
			const element = document.getElementById(`uno-semantics-${handle}`);
			if (element) {
				element.setAttribute('aria-selected', String(selected));
			}
		}

		/**
		 * Updates the disabled state of an element.
		 */
		public static updateDisabledState(handle: number, disabled: boolean): void {
			const element = document.getElementById(`uno-semantics-${handle}`) as HTMLButtonElement | HTMLInputElement;
			if (element) {
				if ('disabled' in element) {
					element.disabled = disabled;
				}
				element.setAttribute('aria-disabled', String(disabled));
			}
		}

		/**
		 * Creates a link (anchor) semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createLinkElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null
		): void {
			const element = document.createElement('a');
			this.applyCommonStyles(element, x, y, width, height, handle);

			element.tabIndex = 0;
			element.style.pointerEvents = 'none';
			// Native <a> has implicit role="link" — no need to set explicitly

			if (label) {
				element.setAttribute('aria-label', label);
			}

			const callbacks = this.getCallbacks();

			element.addEventListener('click', (e) => {
				e.preventDefault();
				if (callbacks.onInvoke) {
					callbacks.onInvoke(handle);
				}
			});

			element.addEventListener('keydown', (e) => {
				if (e.key === 'Enter' || e.key === ' ') {
					e.preventDefault();
					if (callbacks.onInvoke) {
						callbacks.onInvoke(handle);
					}
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		// ===== Tab/Tree/Grid/Menu Semantic Elements =====

		/**
		 * Creates a tablist container element.
		 */
		public static createTabListElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'tablist');
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a tab element with selection and keyboard support.
		 */
		public static createTabElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			selected: boolean,
			positionInSet: number,
			sizeOfSet: number
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'tab');
			element.setAttribute('aria-selected', String(selected));
			element.tabIndex = selected ? 0 : -1;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			if (positionInSet > 0 && sizeOfSet > 0) {
				element.setAttribute('aria-posinset', String(positionInSet));
				element.setAttribute('aria-setsize', String(sizeOfSet));
			}

			const callbacks = this.getCallbacks();
			element.addEventListener('click', () => {
				if (callbacks.onSelection) {
					callbacks.onSelection(handle);
				}
			});
			element.addEventListener('keydown', (e) => {
				if (e.key === 'Enter' || e.key === ' ') {
					e.preventDefault();
					if (callbacks.onSelection) {
						callbacks.onSelection(handle);
					}
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a tree container element.
		 */
		public static createTreeElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			multiselectable: boolean
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'tree');
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			if (multiselectable) {
				element.setAttribute('aria-multiselectable', 'true');
			}
			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a treeitem element with level, expanded state, and selection.
		 */
		public static createTreeItemElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			level: number,
			expanded: string,
			selected: boolean,
			positionInSet: number,
			sizeOfSet: number
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'treeitem');
			element.tabIndex = -1;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			if (level > 0) {
				element.setAttribute('aria-level', String(level));
			}
			// expanded: "true", "false", or "none" (leaf node)
			if (expanded !== 'none') {
				element.setAttribute('aria-expanded', expanded);
			}
			element.setAttribute('aria-selected', String(selected));
			if (positionInSet > 0 && sizeOfSet > 0) {
				element.setAttribute('aria-posinset', String(positionInSet));
				element.setAttribute('aria-setsize', String(sizeOfSet));
			}

			const callbacks = this.getCallbacks();
			element.addEventListener('click', () => {
				if (callbacks.onSelection) {
					callbacks.onSelection(handle);
				}
			});
			// WAI-ARIA tree item keyboard pattern
			element.addEventListener('keydown', (e) => {
				const currentExpanded = element.getAttribute('aria-expanded');
				if (e.key === 'Enter' || e.key === ' ') {
					e.preventDefault();
					if (callbacks.onSelection) {
						callbacks.onSelection(handle);
					}
				} else if (e.key === 'ArrowRight') {
					if (currentExpanded === 'false') {
						// Expand collapsed node
						e.preventDefault();
						if (callbacks.onExpandCollapse) {
							callbacks.onExpandCollapse(handle);
						}
					} else if (currentExpanded === 'true') {
						// Move to first child
						e.preventDefault();
						const firstChild = element.querySelector('[role="treeitem"]') as HTMLElement;
						if (firstChild) {
							firstChild.focus();
						}
					}
				} else if (e.key === 'ArrowLeft') {
					if (currentExpanded === 'true') {
						// Collapse expanded node
						e.preventDefault();
						if (callbacks.onExpandCollapse) {
							callbacks.onExpandCollapse(handle);
						}
					} else {
						// Move to parent tree item
						e.preventDefault();
						const parentItem = element.parentElement?.closest('[role="treeitem"]') as HTMLElement;
						if (parentItem) {
							parentItem.focus();
						}
					}
				} else if (e.key === 'ArrowDown') {
					// Move to next visible tree item
					e.preventDefault();
					const allItems = Array.from(element.closest('[role="tree"]')?.querySelectorAll('[role="treeitem"]') ?? []) as HTMLElement[];
					const currentIndex = allItems.indexOf(element);
					if (currentIndex >= 0 && currentIndex < allItems.length - 1) {
						allItems[currentIndex + 1].focus();
					}
				} else if (e.key === 'ArrowUp') {
					// Move to previous visible tree item
					e.preventDefault();
					const allItems = Array.from(element.closest('[role="tree"]')?.querySelectorAll('[role="treeitem"]') ?? []) as HTMLElement[];
					const currentIndex = allItems.indexOf(element);
					if (currentIndex > 0) {
						allItems[currentIndex - 1].focus();
					}
				} else if (e.key === 'Home') {
					// Move to first tree item
					e.preventDefault();
					const firstItem = element.closest('[role="tree"]')?.querySelector('[role="treeitem"]') as HTMLElement;
					if (firstItem) {
						firstItem.focus();
					}
				} else if (e.key === 'End') {
					// Move to last tree item
					e.preventDefault();
					const allItems = element.closest('[role="tree"]')?.querySelectorAll('[role="treeitem"]');
					if (allItems && allItems.length > 0) {
						(allItems[allItems.length - 1] as HTMLElement).focus();
					}
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a grid/table container element with row/column count.
		 */
		public static createGridElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			rowCount: number,
			colCount: number
		): void {
			const element = document.createElement('table');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'grid');
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			if (rowCount > 0) {
				element.setAttribute('aria-rowcount', String(rowCount));
			}
			if (colCount > 0) {
				element.setAttribute('aria-colcount', String(colCount));
			}
			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a grid row element.
		 */
		public static createGridRowElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			rowIndex: number
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'row');
			element.style.pointerEvents = 'none';
			if (rowIndex > 0) {
				element.setAttribute('aria-rowindex', String(rowIndex));
			}
			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a grid cell element.
		 */
		public static createGridCellElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			rowIndex: number,
			colIndex: number
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'gridcell');
			element.tabIndex = -1;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			if (rowIndex > 0) {
				element.setAttribute('aria-rowindex', String(rowIndex));
			}
			if (colIndex > 0) {
				element.setAttribute('aria-colindex', String(colIndex));
			}
			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a column header element.
		 */
		public static createColumnHeaderElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			colIndex: number
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'columnheader');
			element.tabIndex = -1;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			if (colIndex > 0) {
				element.setAttribute('aria-colindex', String(colIndex));
			}
			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a menu container element.
		 */
		public static createMenuElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'menu');
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			this.appendToParent(element, parentHandle, index);
		}

		/**
		 * Creates a menuitem element.
		 */
		public static createMenuItemElement(
			parentHandle: number,
			handle: number,
			index: number | null,
			x: number,
			y: number,
			width: number,
			height: number,
			label: string | null,
			disabled: boolean,
			hasSubmenu: boolean
		): void {
			const element = document.createElement('div');
			this.applyCommonStyles(element, x, y, width, height, handle);
			element.setAttribute('role', 'menuitem');
			element.tabIndex = -1;
			element.style.pointerEvents = 'none';
			if (label) {
				element.setAttribute('aria-label', label);
			}
			if (disabled) {
				element.setAttribute('aria-disabled', 'true');
			}
			if (hasSubmenu) {
				element.setAttribute('aria-haspopup', 'menu');
			}

			const callbacks = this.getCallbacks();
			element.addEventListener('click', () => {
				if (callbacks.onInvoke) {
					callbacks.onInvoke(handle);
				}
			});
			// WAI-ARIA menu item keyboard pattern
			element.addEventListener('keydown', (e) => {
				if (e.key === 'Enter' || e.key === ' ') {
					e.preventDefault();
					if (callbacks.onInvoke) {
						callbacks.onInvoke(handle);
					}
				} else if (e.key === 'ArrowDown') {
					// Move to next menu item
					e.preventDefault();
					const allItems = Array.from(element.parentElement?.querySelectorAll('[role="menuitem"]') ?? []) as HTMLElement[];
					const currentIndex = allItems.indexOf(element);
					if (currentIndex >= 0 && currentIndex < allItems.length - 1) {
						allItems[currentIndex + 1].focus();
					}
				} else if (e.key === 'ArrowUp') {
					// Move to previous menu item
					e.preventDefault();
					const allItems = Array.from(element.parentElement?.querySelectorAll('[role="menuitem"]') ?? []) as HTMLElement[];
					const currentIndex = allItems.indexOf(element);
					if (currentIndex > 0) {
						allItems[currentIndex - 1].focus();
					}
				} else if (e.key === 'ArrowRight' && hasSubmenu) {
					// Open submenu
					e.preventDefault();
					if (callbacks.onExpandCollapse) {
						callbacks.onExpandCollapse(handle);
					}
				} else if (e.key === 'Escape') {
					// Close menu (move focus to parent)
					e.preventDefault();
					const parentMenu = element.parentElement;
					if (parentMenu) {
						parentMenu.focus();
					}
				}
			});

			this.appendToParent(element, parentHandle, index);
		}

		// ===== Virtualized Container Functions =====

		/**
		 * Queue for batching virtualized item DOM mutations via requestAnimationFrame.
		 */
		private static virtualizedMutationQueue: (() => void)[] = [];
		private static virtualizedRafId: number = 0;

		/**
		 * Schedules a virtualized mutation to be flushed in the next animation frame.
		 */
		private static scheduleVirtualizedMutation(mutation: () => void): void {
			SemanticElements.virtualizedMutationQueue.push(mutation);
			if (SemanticElements.virtualizedRafId === 0) {
				SemanticElements.virtualizedRafId = requestAnimationFrame(() => {
					SemanticElements.flushVirtualizedMutations();
				});
			}
		}

		/**
		 * Flushes all queued virtualized mutations.
		 */
		private static flushVirtualizedMutations(): void {
			const queue = SemanticElements.virtualizedMutationQueue;
			SemanticElements.virtualizedMutationQueue = [];
			SemanticElements.virtualizedRafId = 0;
			for (const mutation of queue) {
				mutation();
			}
		}

		/**
		 * Registers a virtualized container (creates listbox/grid element).
		 */
		public static registerVirtualizedContainer(
			containerHandle: number,
			role: string,
			label: string,
			multiselectable: boolean
		): void {
			// If an element for this container already exists (created by CreateListBoxElement),
			// just update its attributes instead of creating a duplicate.
			const existing = document.getElementById(`uno-semantics-${containerHandle}`);
			if (existing) {
				if (label) {
					existing.setAttribute('aria-label', label);
				}
				if (multiselectable) {
					existing.setAttribute('aria-multiselectable', 'true');
				}
				return;
			}

			const root = SemanticElements.getSemanticsRoot();
			if (!root) {
				return;
			}

			const element = document.createElement('div');
			element.id = `uno-semantics-${containerHandle}`;
			element.setAttribute('role', role);
			element.style.position = 'absolute';

			if (label) {
				element.setAttribute('aria-label', label);
			}
			if (multiselectable) {
				element.setAttribute('aria-multiselectable', 'true');
			}

			root.appendChild(element);
		}

		/**
		 * Adds a semantic element for a realized virtualized item.
		 * Batched via requestAnimationFrame.
		 */
		public static addVirtualizedItem(
			containerHandle: number,
			itemHandle: number,
			index: number,
			totalCount: number,
			x: number,
			y: number,
			width: number,
			height: number,
			role: string,
			label: string
		): void {
			SemanticElements.scheduleVirtualizedMutation(() => {
				const container = document.getElementById(`uno-semantics-${containerHandle}`);
				if (!container) {
					return;
				}

				// If an element for this item already exists (created by OnChildAdded→CreateListItemElement),
				// just update it instead of creating a duplicate.
				const existingItem = document.getElementById(`uno-semantics-${itemHandle}`);
				if (existingItem) {
					existingItem.setAttribute('aria-posinset', String(index + 1));
					existingItem.setAttribute('aria-setsize', String(totalCount));
					if (label) {
						existingItem.setAttribute('aria-label', label);
					}
					// Ensure item is inside the correct container
					if (existingItem.parentElement !== container) {
						container.appendChild(existingItem);
					}
					return;
				}

				const element = document.createElement('div');
				SemanticElements.applyCommonStyles(element, x, y, width, height, itemHandle);
				element.setAttribute('role', role);
				// aria-posinset is 1-based, index is 0-based
				element.setAttribute('aria-posinset', String(index + 1));
				element.setAttribute('aria-setsize', String(totalCount));
				element.tabIndex = -1;
				element.style.pointerEvents = 'none';

				if (label) {
					element.setAttribute('aria-label', label);
				}

				const callbacks = SemanticElements.getCallbacks();
				element.addEventListener('click', (e) => {
					e.preventDefault();
					if (callbacks.onSelection) {
						callbacks.onSelection(itemHandle);
					}
				});

				container.appendChild(element);
			});
		}

		/**
		 * Removes a semantic element for an unrealized virtualized item.
		 * Batched via requestAnimationFrame.
		 */
		public static removeVirtualizedItem(itemHandle: number): void {
			SemanticElements.scheduleVirtualizedMutation(() => {
				const element = document.getElementById(`uno-semantics-${itemHandle}`);
				if (element && element.parentElement) {
					element.parentElement.removeChild(element);
				}
			});
		}

		/**
		 * Updates the total item count on all realized items in a container.
		 */
		public static updateVirtualizedItemCount(containerHandle: number, totalCount: number): void {
			const container = document.getElementById(`uno-semantics-${containerHandle}`);
			if (!container) {
				return;
			}
			const items = container.querySelectorAll<HTMLElement>('[aria-posinset]');
			items.forEach((el) => {
				el.setAttribute('aria-setsize', String(totalCount));
			});
		}

		/**
		 * Unregisters a virtualized container and removes all its semantic elements.
		 */
		public static unregisterVirtualizedContainer(containerHandle: number): void {
			const element = document.getElementById(`uno-semantics-${containerHandle}`);
			if (element && element.parentElement) {
				element.parentElement.removeChild(element);
			}
		}
	}
}
