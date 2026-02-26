namespace Uno.UI.Runtime.Skia {

	/**
	 * Type of semantic HTML element to create.
	 */
	export type SemanticElementType =
		| 'generic'      // <div> with ARIA role
		| 'button'       // <button>
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
		 * Appends a created semantic element to its proper parent in the tree,
		 * or falls back to the semantics root if the parent is not found.
		 */
		private static appendToParent(
			element: HTMLElement,
			parentHandle: number,
			index: number | null
		): void {
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
			console.log(`[A11y] TS createButtonElement: handle=${handle} parent=${parentHandle} label='${label}' disabled=${disabled}`);
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
			console.log(`[A11y] TS createSliderElement: handle=${handle} parent=${parentHandle} value=${value} min=${min} max=${max} step=${step} orient=${orientation}`);
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
			console.log(`[A11y] TS createCheckboxElement: handle=${handle} parent=${parentHandle} checked='${checkedState}' label='${label}'`);
			const element = document.createElement('input');
			element.type = 'checkbox';
			this.applyCommonStyles(element, x, y, width, height, handle);

			// Enable focus and interaction
			element.tabIndex = 0;
			element.style.pointerEvents = 'none';

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
			console.log(`[A11y] TS createRadioElement: handle=${handle} parent=${parentHandle} checked=${checked} label='${label}' group='${groupName}'`);
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
			console.log(`[A11y] TS createHeadingElement: handle=${handle} parent=${parentHandle} level=h${level} label='${label}'`);
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
			isReadOnly: boolean
		): void {
			console.log(`[A11y] TS createTextBoxElement: handle=${handle} parent=${parentHandle} multiline=${multiline} password=${password} readOnly=${isReadOnly} valueLen=${value?.length ?? 0}`);
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
			console.log(`[A11y] TS createComboBoxElement: handle=${handle} parent=${parentHandle} expanded=${expanded} selectedValue='${selectedValue}'`);
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
			console.log(`[A11y] TS createListBoxElement: handle=${handle} parent=${parentHandle} multiselect=${multiselect}`);
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
			console.log(`[A11y] TS createListItemElement: handle=${handle} parent=${parentHandle} selected=${selected} pos=${positionInSet}/${sizeOfSet}`);
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
		public static updateSliderValue(handle: number, value: number, min: number, max: number): void {
			console.log(`[A11y] TS updateSliderValue: handle=${handle} value=${value} min=${min} max=${max}`);
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
			console.log(`[A11y] TS updateTextBoxValue: handle=${handle} valueLen=${value?.length ?? 0} sel=${selectionStart}-${selectionEnd}`);
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
			console.log(`[A11y] TS updateExpandCollapseState: handle=${handle} expanded=${expanded}`);
			const element = document.getElementById(`uno-semantics-${handle}`);
			if (element) {
				element.setAttribute('aria-expanded', String(expanded));
			}
		}

		/**
		 * Updates the selected state of a list item element.
		 */
		public static updateSelectionState(handle: number, selected: boolean): void {
			console.log(`[A11y] TS updateSelectionState: handle=${handle} selected=${selected}`);
			const element = document.getElementById(`uno-semantics-${handle}`);
			if (element) {
				element.setAttribute('aria-selected', String(selected));
			}
		}

		/**
		 * Updates the disabled state of an element.
		 */
		public static updateDisabledState(handle: number, disabled: boolean): void {
			console.log(`[A11y] TS updateDisabledState: handle=${handle} disabled=${disabled}`);
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
			console.log(`[A11y] TS createLinkElement: handle=${handle} parent=${parentHandle} label='${label}'`);
			const element = document.createElement('a');
			this.applyCommonStyles(element, x, y, width, height, handle);

			element.tabIndex = 0;
			element.style.pointerEvents = 'none';
			element.setAttribute('role', 'link');

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
				if (e.key === 'Enter') {
					e.preventDefault();
					if (callbacks.onInvoke) {
						callbacks.onInvoke(handle);
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
				element.addEventListener('click', () => {
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
			const items = container.querySelectorAll('[aria-posinset]');
			items.forEach((el: HTMLElement) => {
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
