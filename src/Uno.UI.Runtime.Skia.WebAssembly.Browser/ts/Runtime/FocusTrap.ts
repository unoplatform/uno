namespace Uno.UI.Runtime.Skia {

	interface FocusTrapState {
		modalHandle: number;
		triggerHandle: number;
		focusableHandles: number[];
		hiddenElements: { element: HTMLElement; originalAriaHidden: string | null; originalInert: string | null }[];
		keydownHandler: (e: KeyboardEvent) => void;
		parentState: FocusTrapState | null;
	}

	/**
	 * Modal focus trap for ContentDialog and modal overlays.
	 * Manages aria-hidden on background, Tab/Shift+Tab wrapping,
	 * nested modal support, and focus restoration on close.
	 */
	export class FocusTrap {
		private static activeTrap: FocusTrapState | null = null;

		/**
		 * Activates a focus trap for a modal dialog.
		 * Hides background elements and starts Tab wrapping.
		 */
		public static activateFocusTrap(modalHandle: number, triggerHandle: number, focusableHandles: number[]): void {
			const parentState = FocusTrap.activeTrap;

			// Hide sibling subtree roots along the modal-to-root path. aria-hidden + inert
			// removes each whole subtree from AT and keyboard navigation without walking and
			// rewriting every semantic descendant.
			const semanticsRoot = Accessibility.getSemanticsRoot();
			const modalElement = Accessibility.getSemanticElementByHandle(modalHandle);
			const hiddenElements: FocusTrapState["hiddenElements"] = [];

			if (semanticsRoot && modalElement) {
				let current: HTMLElement = modalElement;
				while (current !== semanticsRoot) {
					const parent: HTMLElement | null = current.parentElement;
					if (!parent) {
						break;
					}

					for (const sibling of Array.from(parent.children) as HTMLElement[]) {
						if (sibling === current) {
							continue;
						}

						hiddenElements.push({
							element: sibling,
							originalAriaHidden: sibling.getAttribute("aria-hidden"),
							originalInert: sibling.getAttribute("inert")
						});
						sibling.setAttribute("aria-hidden", "true");
						sibling.setAttribute("inert", "");
					}

					current = parent;
				}
			}

			// Create keydown handler for Tab wrapping.
			// Use capture phase so user code cannot stopPropagation() to escape the trap.
			const keydownHandler = (e: KeyboardEvent) => {
				if (e.key === "Tab") {
					const wrapped = FocusTrap.handleTrapTab(modalHandle, e.shiftKey);
					if (wrapped) {
						e.preventDefault();
					}
				}
			};

			document.addEventListener("keydown", keydownHandler, true);

			FocusTrap.activeTrap = {
				modalHandle,
				triggerHandle,
				focusableHandles,
				hiddenElements,
				keydownHandler,
				parentState
			};

			// Focus the first focusable element in the modal
			if (focusableHandles.length > 0) {
				const firstElement = Accessibility.getSemanticElementByHandle(focusableHandles[0]);
				if (firstElement) {
					firstElement.focus();
				}
			}
		}

		/**
		 * Deactivates the focus trap for a modal dialog.
		 * Restores background elements and focus.
		 */
		public static deactivateFocusTrap(modalHandle: number): void {
			const trap = FocusTrap.activeTrap;
			if (!trap) {
				return;
			}

			// Handle out-of-order deactivation: if the requested modal is not
			// the topmost, walk the parent chain to find and remove it.
			if (trap.modalHandle !== modalHandle) {
				let current: FocusTrapState | null = trap;
				while (current) {
					if (current.parentState?.modalHandle === modalHandle) {
						const target = current.parentState;
						document.removeEventListener("keydown", target.keydownHandler, true);
						FocusTrap.restoreHiddenElements(target);
						// Splice out of linked list
						current.parentState = target.parentState;
						return;
					}
					current = current.parentState;
				}
				return;
			}

			// Remove keydown handler (must match capture phase used in activate)
			document.removeEventListener("keydown", trap.keydownHandler, true);

			// Restore hidden elements
			FocusTrap.restoreHiddenElements(trap);

			// Reactivate parent trap or clear
			FocusTrap.activeTrap = trap.parentState;

			// Restore focus to trigger element, with fallback to parent trap or body
			if (trap.triggerHandle) {
				const triggerElement = Accessibility.getSemanticElementByHandle(trap.triggerHandle);
				if (triggerElement) {
					triggerElement.focus();
				} else if (trap.parentState && trap.parentState.focusableHandles.length > 0) {
					const fallback = Accessibility.getSemanticElementByHandle(trap.parentState.focusableHandles[0]);
					if (fallback) {
						fallback.focus();
					}
				}
			}
		}

		/**
		 * Updates the focusable children within a modal.
		 */
		public static updateFocusTrapChildren(modalHandle: number, focusableHandles: number[]): void {
			if (FocusTrap.activeTrap && FocusTrap.activeTrap.modalHandle === modalHandle) {
				FocusTrap.activeTrap.focusableHandles = focusableHandles;
			}
		}

		/**
		 * Handles Tab/Shift+Tab within a focus trap.
		 * Returns true if focus was wrapped.
		 */
		public static handleTrapTab(modalHandle: number, shiftKey: boolean): boolean {
			const trap = FocusTrap.activeTrap;
			if (!trap || trap.modalHandle !== modalHandle || trap.focusableHandles.length === 0) {
				return false;
			}

			const activeElement = document.activeElement;
			const handles = trap.focusableHandles;

			// Find current position in focusable list
			let currentIndex = -1;
			for (let i = 0; i < handles.length; i++) {
				if (activeElement?.id === `uno-semantics-${handles[i]}`) {
					currentIndex = i;
					break;
				}
			}

			if (shiftKey) {
				// Shift+Tab: wrap from first to last
				if (currentIndex <= 0) {
					const lastElement = Accessibility.getSemanticElementByHandle(handles[handles.length - 1]);
					if (lastElement) {
						lastElement.focus();
						return true;
					}
				}
			} else {
				// Tab: wrap from last to first
				if (currentIndex >= handles.length - 1) {
					const firstElement = Accessibility.getSemanticElementByHandle(handles[0]);
					if (firstElement) {
						firstElement.focus();
						return true;
					}
				}
			}

			return false;
		}

		/**
		 * Returns whether a focus trap is currently active.
		 */
		public static isFocusTrapActive(): boolean {
			return FocusTrap.activeTrap !== null;
		}

		/**
		 * Returns the handle of the active modal, or 0 if no trap is active.
		 */
		public static getActiveTrapHandle(): number {
			return FocusTrap.activeTrap?.modalHandle ?? 0;
		}

		private static restoreHiddenElements(trap: FocusTrapState): void {
			for (const item of trap.hiddenElements) {
				if (item.originalAriaHidden !== null) {
					item.element.setAttribute("aria-hidden", item.originalAriaHidden);
				} else {
					item.element.removeAttribute("aria-hidden");
				}
				if (item.originalInert !== null) {
					item.element.setAttribute("inert", item.originalInert);
				} else {
					item.element.removeAttribute("inert");
				}
			}
		}
	}
}
