namespace Uno.UI.Runtime.Skia {

	/**
	 * Type of semantic HTML element to create.
	 */
	export type SemanticElementType =
		| 'generic'      // <div> with ARIA role
		| 'button'       // <button>
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
			handle: number,
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
			element.style.pointerEvents = 'all';

			if (label) {
				element.setAttribute('aria-label', label);
			}

			if (disabled) {
				element.disabled = true;
				element.setAttribute('aria-disabled', 'true');
			}

			const callbacks = this.getCallbacks();

			// Click handler for button activation
			element.addEventListener('click', (e) => {
				e.preventDefault();
				if (callbacks.onInvoke) {
					callbacks.onInvoke(handle);
				}
			});

			// Keyboard handlers for Enter/Space
			element.addEventListener('keydown', (e) => {
				if (e.key === 'Enter' || e.key === ' ') {
					e.preventDefault();
					if (callbacks.onInvoke) {
						callbacks.onInvoke(handle);
					}
				}
			});

			// Append to semantics root
			const root = this.getSemanticsRoot();
			if (root) {
				root.appendChild(element);
			}
		}

		/**
		 * Creates a slider (range input) semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createSliderElement(
			handle: number,
			x: number,
			y: number,
			width: number,
			height: number,
			value: number,
			min: number,
			max: number,
			step: number,
			orientation: string
		): void {
			const element = document.createElement('input');
			element.type = 'range';
			this.applyCommonStyles(element, x, y, width, height, handle);

			// Enable focus and interaction
			element.tabIndex = 0;
			element.style.pointerEvents = 'all';

			// Set range properties
			element.min = String(min);
			element.max = String(max);
			element.value = String(value);
			element.step = String(step);

			// Set ARIA value attributes for screen readers
			element.setAttribute('aria-valuenow', String(value));
			element.setAttribute('aria-valuemin', String(min));
			element.setAttribute('aria-valuemax', String(max));

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

			// Append to semantics root
			const root = this.getSemanticsRoot();
			if (root) {
				root.appendChild(element);
			}
		}

		/**
		 * Creates a checkbox semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createCheckboxElement(
			handle: number,
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
			element.style.pointerEvents = 'all';

			if (label) {
				element.setAttribute('aria-label', label);
			}

			// Set checked state
			if (checkedState === 'true') {
				element.checked = true;
			} else if (checkedState === 'mixed') {
				element.indeterminate = true;
				element.setAttribute('aria-checked', 'mixed');
			}

			const callbacks = this.getCallbacks();

			// Change event handler for toggle events (T040)
			element.addEventListener('change', () => {
				if (callbacks.onToggle) {
					callbacks.onToggle(handle);
				}
			});

			// Append to semantics root
			const root = this.getSemanticsRoot();
			if (root) {
				root.appendChild(element);
			}
		}

		/**
		 * Creates a radio button semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createRadioElement(
			handle: number,
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
			element.style.pointerEvents = 'all';

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

			// Append to semantics root
			const root = this.getSemanticsRoot();
			if (root) {
				root.appendChild(element);
			}
		}

		/**
		 * Creates a text input semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createTextBoxElement(
			handle: number,
			x: number,
			y: number,
			width: number,
			height: number,
			value: string,
			multiline: boolean,
			password: boolean,
			isReadOnly: boolean
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
			element.style.pointerEvents = 'all';

			element.value = value;

			if (isReadOnly) {
				element.readOnly = true;
			}

			const callbacks = this.getCallbacks();

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

			// Append to semantics root
			const root = this.getSemanticsRoot();
			if (root) {
				root.appendChild(element);
			}
		}

		/**
		 * Creates a combobox semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createComboBoxElement(
			handle: number,
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
			element.style.pointerEvents = 'all';

			if (selectedValue) {
				element.setAttribute('aria-label', selectedValue);
			}

			const callbacks = this.getCallbacks();

			// Keyboard handlers for expand/collapse (T061)
			element.addEventListener('keydown', (e) => {
				if (e.key === 'Enter' || e.key === ' ' || (e.key === 'ArrowDown' && e.altKey)) {
					e.preventDefault();
					if (callbacks.onExpandCollapse) {
						callbacks.onExpandCollapse(handle);
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

			// Append to semantics root
			const root = this.getSemanticsRoot();
			if (root) {
				root.appendChild(element);
			}
		}

		/**
		 * Creates a listbox semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createListBoxElement(
			handle: number,
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
			element.style.pointerEvents = 'all';

			if (multiselect) {
				element.setAttribute('aria-multiselectable', 'true');
			}

			// Append to semantics root
			const root = this.getSemanticsRoot();
			if (root) {
				root.appendChild(element);
			}
		}

		/**
		 * Creates a list item semantic element and appends it to the semantics root.
		 * Called from C# via JSImport.
		 */
		public static createListItemElement(
			handle: number,
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
			element.style.pointerEvents = 'all';

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

			// Append to semantics root
			const root = this.getSemanticsRoot();
			if (root) {
				root.appendChild(element);
			}
		}

		/**
		 * Updates the value of a slider element and its ARIA attributes.
		 */
		public static updateSliderValue(handle: number, value: number, min: number, max: number): void {
			const element = document.getElementById(`uno-semantics-${handle}`) as HTMLInputElement;
			if (element && element.type === 'range') {
				element.min = String(min);
				element.max = String(max);
				element.value = String(value);
				element.setAttribute('aria-valuenow', String(value));
				element.setAttribute('aria-valuemin', String(min));
				element.setAttribute('aria-valuemax', String(max));
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
				element.value = value;
				element.setSelectionRange(selectionStart, selectionEnd);
			}
		}

		/**
		 * Updates the expanded/collapsed state of a combobox element.
		 */
		public static updateExpandCollapseState(handle: number, expanded: boolean): void {
			const element = document.getElementById(`uno-semantics-${handle}`);
			if (element) {
				element.setAttribute('aria-expanded', String(expanded));
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
	}
}
