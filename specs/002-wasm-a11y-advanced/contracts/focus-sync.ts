/**
 * Focus Synchronization API Contract
 * Bidirectional focus bridge between XAML and browser semantic elements.
 */

/**
 * Focuses a semantic element in the DOM.
 * Called from C# when FocusManager.GotFocus fires for a XAML control.
 * Debounced to one call per animation frame.
 * Sets tabindex="0" on the target, tabindex="-1" on the previous.
 *
 * @param handle - Handle of the semantic element to focus
 */
declare function focusSemanticElement(handle: number): void;

/**
 * Blurs the currently focused semantic element.
 * Called from C# when FocusManager.LostFocus fires and no new focus target.
 *
 * @param handle - Handle of the semantic element losing focus
 */
declare function blurSemanticElement(handle: number): void;

/**
 * Callback: Semantic element received browser focus.
 * Called from TypeScript when a semantic element's focus event fires.
 * The C# side should call Control.Focus(FocusState.Keyboard) on the corresponding control.
 * Must check IsSyncing guard to prevent infinite loops.
 *
 * @param handle - Handle of the semantic element that received focus
 */
declare function onSemanticElementFocused(handle: number): void;

/**
 * Returns the handle of the currently focused semantic element, or 0 if none.
 */
declare function getCurrentFocusedHandle(): number;

/**
 * Updates tabindex for roving tabindex pattern within a group.
 * Sets tabindex="0" on the active element, tabindex="-1" on all others in the group.
 *
 * @param groupHandle - Handle of the parent group element
 * @param activeHandle - Handle of the active (focused) element in the group
 */
declare function updateRovingTabindex(groupHandle: number, activeHandle: number): void;
