namespace Uno.UI.Runtime.Skia {

	interface FocusTrapState {
		modalHandle: number;
		triggerHandle: number;
		focusableHandles: number[];
		hiddenElements: { element: HTMLElement; originalAriaHidden: string | null; originalTabIndex: string | null }[];
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

			// Hide all semantic elements outside the modal
			const semanticsRoot = document.getElementById("uno-semantics-root");
			const modalElement = document.getElementById(`uno-semantics-${modalHandle}`);
			const hiddenElements: FocusTrapState["hiddenElements"] = [];

			if (semanticsRoot) {
				const allElements = semanticsRoot.querySelectorAll("[id^='uno-semantics-']");
				allElements.forEach((el: HTMLElement) => {
					if (el !== modalElement && !modalElement?.contains(el)) {
						hiddenElements.push({
							element: el,
							originalAriaHidden: el.getAttribute("aria-hidden"),
							originalTabIndex: el.getAttribute("tabindex")
						});
						el.setAttribute("aria-hidden", "true");
						el.setAttribute("tabindex", "-1");
					}
				});
			}

			// Set role="dialog" on modal element
			if (modalElement) {
				modalElement.setAttribute("role", "dialog");
				modalElement.setAttribute("aria-modal", "true");
			}

			// Create keydown handler for Tab wrapping
			const keydownHandler = (e: KeyboardEvent) => {
				if (e.key === "Tab") {
					const wrapped = FocusTrap.handleTrapTab(modalHandle, e.shiftKey);
					if (wrapped) {
						e.preventDefault();
					}
				}
			};

			document.addEventListener("keydown", keydownHandler);

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
				const firstElement = document.getElementById(`uno-semantics-${focusableHandles[0]}`);
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
			if (!trap || trap.modalHandle !== modalHandle) {
				return;
			}

			// Remove keydown handler
			document.removeEventListener("keydown", trap.keydownHandler);

			// Restore hidden elements
			for (const item of trap.hiddenElements) {
				if (item.originalAriaHidden !== null) {
					item.element.setAttribute("aria-hidden", item.originalAriaHidden);
				} else {
					item.element.removeAttribute("aria-hidden");
				}
				if (item.originalTabIndex !== null) {
					item.element.setAttribute("tabindex", item.originalTabIndex);
				} else {
					item.element.removeAttribute("tabindex");
				}
			}

			// Remove dialog role
			const modalElement = document.getElementById(`uno-semantics-${modalHandle}`);
			if (modalElement) {
				modalElement.removeAttribute("role");
				modalElement.removeAttribute("aria-modal");
			}

			// Reactivate parent trap or clear
			FocusTrap.activeTrap = trap.parentState;

			// Restore focus to trigger element
			if (trap.triggerHandle) {
				const triggerElement = document.getElementById(`uno-semantics-${trap.triggerHandle}`);
				if (triggerElement) {
					triggerElement.focus();
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
					const lastElement = document.getElementById(`uno-semantics-${handles[handles.length - 1]}`);
					if (lastElement) {
						lastElement.focus();
						return true;
					}
				}
			} else {
				// Tab: wrap from last to first
				if (currentIndex >= handles.length - 1) {
					const firstElement = document.getElementById(`uno-semantics-${handles[0]}`);
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
	}
}
