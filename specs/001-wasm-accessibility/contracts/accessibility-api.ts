/**
 * TypeScript Interface Contracts for WebAssembly Accessibility
 *
 * These interfaces define the contract between C# (via JSImport/JSExport)
 * and the TypeScript accessibility layer.
 */

// =============================================================================
// Enums
// =============================================================================

/**
 * Type of semantic HTML element to create
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
 * Slider orientation
 */
export type SliderOrientation = 'horizontal' | 'vertical';

/**
 * Checkbox/Toggle state
 */
export type CheckedState = 'true' | 'false' | 'mixed';

// =============================================================================
// Element Creation Parameters
// =============================================================================

/**
 * Base parameters for all semantic elements
 */
export interface SemanticElementBase {
  handle: number;       // IntPtr from C#
  x: number;
  y: number;
  width: number;
  height: number;
  label?: string;
  disabled?: boolean;
}

/**
 * Parameters for creating a button element
 */
export interface ButtonElementParams extends SemanticElementBase {
  type: 'button';
}

/**
 * Parameters for creating a slider element
 */
export interface SliderElementParams extends SemanticElementBase {
  type: 'slider';
  value: number;
  min: number;
  max: number;
  step: number;
  orientation: SliderOrientation;
}

/**
 * Parameters for creating a text input element
 */
export interface TextBoxElementParams extends SemanticElementBase {
  type: 'textbox' | 'textarea' | 'password';
  value: string;
  multiline: boolean;
  readonly: boolean;
}

/**
 * Parameters for creating a checkbox element
 */
export interface CheckboxElementParams extends SemanticElementBase {
  type: 'checkbox' | 'radio';
  checked: CheckedState;
}

/**
 * Parameters for creating a combobox element
 */
export interface ComboBoxElementParams extends SemanticElementBase {
  type: 'combobox';
  expanded: boolean;
  selectedValue?: string;
}

/**
 * Parameters for creating a listbox element
 */
export interface ListBoxElementParams extends SemanticElementBase {
  type: 'listbox';
  multiselect: boolean;
}

/**
 * Parameters for creating a list item element
 */
export interface ListItemElementParams extends SemanticElementBase {
  type: 'listitem';
  selected: boolean;
  positionInSet: number;
  sizeOfSet: number;
}

// =============================================================================
// State Update Parameters
// =============================================================================

export interface SliderUpdateParams {
  handle: number;
  value: number;
  min: number;
  max: number;
}

export interface TextBoxUpdateParams {
  handle: number;
  value: string;
  selectionStart: number;
  selectionEnd: number;
}

export interface ExpandCollapseUpdateParams {
  handle: number;
  expanded: boolean;
}

export interface SelectionUpdateParams {
  handle: number;
  selected: boolean;
}

export interface PositioningUpdateParams {
  handle: number;
  x: number;
  y: number;
  width: number;
  height: number;
}

// =============================================================================
// Callback Signatures (C# JSExport methods)
// =============================================================================

/**
 * Callbacks from TypeScript to C# (via JSExport)
 */
export interface AccessibilityCallbacks {
  /**
   * Called when a button is invoked (click, Enter, Space)
   */
  onInvoke(handle: number): void;

  /**
   * Called when a checkbox/radio/toggle is toggled
   */
  onToggle(handle: number): void;

  /**
   * Called when a slider value changes
   */
  onRangeValueChange(handle: number, value: number): void;

  /**
   * Called when text is entered in a textbox
   */
  onTextInput(handle: number, value: string, selectionStart: number, selectionEnd: number): void;

  /**
   * Called when a combobox/expander is expanded/collapsed
   */
  onExpandCollapse(handle: number): void;

  /**
   * Called when a list item is selected
   */
  onSelection(handle: number): void;

  /**
   * Called when a semantic element receives focus
   */
  onFocus(handle: number): void;

  /**
   * Called when a semantic element loses focus
   */
  onBlur(handle: number): void;

  /**
   * Called to scroll a scroll container
   */
  onScroll(handle: number, horizontalOffset: number, verticalOffset: number): void;
}

// =============================================================================
// Main Accessibility API (C# JSImport methods)
// =============================================================================

/**
 * API exposed by TypeScript for C# to call (via JSImport)
 */
export interface AccessibilityAPI {
  // Setup
  setup(): void;

  // Element creation
  addRootElementToSemanticsRoot(
    handle: number,
    width: number,
    height: number,
    x: number,
    y: number,
    isFocusable: boolean
  ): void;

  addSemanticElement(
    parentHandle: number,
    handle: number,
    index: number | null,
    width: number,
    height: number,
    x: number,
    y: number,
    role: string,
    automationId: string,
    isFocusable: boolean,
    ariaChecked: string | null,
    isVisible: boolean,
    horizontallyScrollable: boolean,
    verticallyScrollable: boolean,
    elementType: string
  ): boolean;

  // Type-specific element creation (NEW)
  createButtonElement(params: ButtonElementParams): void;
  createSliderElement(params: SliderElementParams): void;
  createTextBoxElement(params: TextBoxElementParams): void;
  createCheckboxElement(params: CheckboxElementParams): void;
  createComboBoxElement(params: ComboBoxElementParams): void;
  createListBoxElement(params: ListBoxElementParams): void;
  createListItemElement(params: ListItemElementParams): void;

  // Element removal
  removeSemanticElement(parentHandle: number, childHandle: number): void;

  // State updates
  updateAriaLabel(handle: number, label: string): void;
  updateAriaChecked(handle: number, checked: string | null): void;
  updateSliderValue(params: SliderUpdateParams): void;
  updateTextBoxValue(params: TextBoxUpdateParams): void;
  updateExpandCollapseState(params: ExpandCollapseUpdateParams): void;
  updateSelectionState(params: SelectionUpdateParams): void;
  updateDisabledState(handle: number, disabled: boolean): void;
  updateSemanticElementPositioning(params: PositioningUpdateParams): void;
  updateNativeScrollOffsets(handle: number, horizontalOffset: number, verticalOffset: number): void;
  updateIsFocusable(handle: number, isFocusable: boolean): void;

  // Visibility
  hideSemanticElement(handle: number): void;
  showSemanticElement(handle: number): void;

  // Focus
  focusSemanticElement(handle: number): void;

  // Live regions
  announcePolite(text: string): void;
  announceAssertive(text: string): void;

  // Debug mode
  enableDebugMode(enabled: boolean): void;
}

// =============================================================================
// Debug Mode Types
// =============================================================================

export interface DebugOverlayStyles {
  outline: string;          // e.g., '2px solid green'
  backgroundColor: string;  // e.g., 'rgba(0, 255, 0, 0.1)'
  pointerEvents: string;    // 'none' to not interfere with canvas
}

export interface DebugConfig {
  enabled: boolean;
  overlayStyles: DebugOverlayStyles;
  showLabels: boolean;      // Show aria-label as visible text
  showHandles: boolean;     // Show handle IDs for debugging
}
