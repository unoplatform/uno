/**
 * Modal Focus Trap API Contract
 * Manages focus trapping for ContentDialog and modal overlays.
 */

/**
 * Activates a modal focus trap.
 * Sets aria-hidden="true" on all semantic elements outside the modal.
 * Saves the trigger element handle for focus restoration.
 *
 * @param modalHandle - Handle of the modal container element
 * @param triggerHandle - Handle of the element that opened the modal (for focus restore)
 * @param focusableHandles - Ordered array of focusable element handles within the modal
 */
declare function activateFocusTrap(
    modalHandle: number,
    triggerHandle: number,
    focusableHandles: number[]
): void;

/**
 * Deactivates the current modal focus trap.
 * Removes aria-hidden from background elements.
 * Restores focus to the trigger element.
 * If nested, reactivates the parent modal's trap.
 *
 * @param modalHandle - Handle of the modal being closed
 */
declare function deactivateFocusTrap(modalHandle: number): void;

/**
 * Updates the list of focusable elements within the active modal.
 * Called when modal content changes (e.g., buttons enabled/disabled).
 *
 * @param modalHandle - Handle of the modal container
 * @param focusableHandles - Updated ordered array of focusable handles
 */
declare function updateFocusTrapChildren(
    modalHandle: number,
    focusableHandles: number[]
): void;

/**
 * Handles Tab key within a focus trap.
 * Wraps focus from last → first (Tab) or first → last (Shift+Tab).
 * Called from the keydown handler on the modal container.
 *
 * @param modalHandle - Handle of the active modal
 * @param shiftKey - Whether Shift is pressed (reverse direction)
 * @returns true if focus was wrapped, false if normal Tab behavior should proceed
 */
declare function handleTrapTab(modalHandle: number, shiftKey: boolean): boolean;

/**
 * Returns whether a focus trap is currently active.
 */
declare function isFocusTrapActive(): boolean;

/**
 * Returns the handle of the active modal, or 0 if no trap is active.
 */
declare function getActiveTrapHandle(): number;
