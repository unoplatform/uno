# ListViewItemPresenter Chrome System Port

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the complete ListViewItemPresenter/GridViewItemPresenter chrome rendering system from WinUI C++ to Uno Platform C#, enabling code-based visual state management for ListView/GridView items.

**Architecture:** In WinUI, `CListViewBaseItemChrome` (inheriting `CContentPresenter`) manages visual states and child elements (borders, checkboxes, selection indicators) using native composition rendering. In Uno, we embed this chrome logic directly into `ListViewItemPresenter`/`GridViewItemPresenter` (which already inherit from `ContentPresenter`), using XAML element manipulation instead of composition rendering. The `GoToElementStateCore` method intercepts VisualStateManager calls and applies visual changes through the chrome's state machine.

**Tech Stack:** C# 12, .NET 10, Uno Platform, XAML, Microsoft.UI.Xaml.Controls.Primitives

---

## File Structure

### New Files
- `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Chrome.cs` - State machine enums, VisualStates struct, state name mappings, GoToChromedState
- `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Chrome.Constants.cs` - Static constants (sizes, thicknesses, margins, offsets)
- `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Chrome.Elements.cs` - Child element creation/management (Ensure*, Remove*, Set*Properties)
- `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Chrome.Layout.cs` - MeasureOverride/ArrangeOverride for chrome layout
- `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Properties.cs` - 60 DependencyProperties implementation (replaces Generated stubs)

### Modified Files
- `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.cs` - GoToElementStateCore implementation
- `src/Uno.UI/UI/Xaml/Controls/Primitives/GridViewItemPresenter.cs` - GoToElementStateCore delegation
- `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls.Primitives/ListViewItemPresenter.cs` - Remove NotImplemented from implemented properties
- `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls.Primitives/GridViewItemPresenter.cs` - Remove NotImplemented from implemented properties

---

## Task 1: State Machine Foundation (Enums, VisualStates, State Mappings)

**Files:**
- Create: `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Chrome.cs`

Port the visual state enums, VisualStates struct, UpdateVisualStateGroup method, and all state name-to-enum mappings from CListViewBaseItemChrome.

- [ ] **Step 1:** Create `ListViewItemPresenter.Chrome.cs` with all 11 visual state enums (CommonStates, FocusStates, SelectionHintStates, SelectionStates, DragStates, ReorderHintStates, DataVirtualizationStates, CommonStates2, DisabledStates, MultiSelectStates, SelectionIndicatorStates) and the VisualStates struct.

- [ ] **Step 2:** Add the state name-to-enum dictionary mappings and `UpdateVisualStateGroup` method.

- [ ] **Step 3:** Add the `GoToChromedState` method that parses state names and updates the VisualStates struct, triggers visual changes and animations.

- [ ] **Step 4:** Build to verify compilation.

- [ ] **Step 5:** Commit.

## Task 2: Static Constants

**Files:**
- Create: `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Chrome.Constants.cs`

Port all static constants from CListViewBaseItemChrome (sizes, thicknesses, margins, offsets, opacity values).

- [ ] **Step 1:** Port all const fields and static default values.
- [ ] **Step 2:** Build to verify.
- [ ] **Step 3:** Commit.

## Task 3: GoToElementStateCore Integration

**Files:**
- Modify: `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.cs`
- Modify: `src/Uno.UI/UI/Xaml/Controls/Primitives/GridViewItemPresenter.cs`

Update GoToElementStateCore to delegate to the chrome state machine instead of returning false.

- [ ] **Step 1:** Update ListViewItemPresenter.GoToElementStateCore to call GoToChromedState.
- [ ] **Step 2:** Update GridViewItemPresenter.GoToElementStateCore similarly.
- [ ] **Step 3:** Build to verify.
- [ ] **Step 4:** Commit.

## Task 4: DependencyProperties Implementation

**Files:**
- Create: `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Properties.cs`
- Modify: `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls.Primitives/ListViewItemPresenter.cs`

Implement all 60 ListViewItemPresenter DependencyProperties by moving them from Generated stubs to real implementations. Properties include brush properties for all visual states, thickness/margin properties, opacity values, check mode, content alignment, etc.

- [ ] **Step 1:** Create Properties.cs with all brush, thickness, opacity, and mode DependencyProperties.
- [ ] **Step 2:** Update Generated file to skip implemented properties.
- [ ] **Step 3:** Add OnPropertyChanged handlers for properties that need to trigger visual updates.
- [ ] **Step 4:** Build to verify.
- [ ] **Step 5:** Commit.

## Task 5: Chrome Visual Elements (Ensure/Remove/Set methods)

**Files:**
- Create: `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Chrome.Elements.cs`

Port the child element management: EnsureMultiSelectCheckBox, EnsureSelectionIndicator, EnsureBackplate, EnsureInnerSelectionBorder, EnsureOuterBorder, and all corresponding Remove* and Set*Properties methods.

- [ ] **Step 1:** Port EnsureMultiSelectCheckBox and SetMultiSelectCheckBoxProperties.
- [ ] **Step 2:** Port EnsureSelectionIndicator and SetSelectionIndicatorBackground/CornerRadius.
- [ ] **Step 3:** Port EnsureBackplate and SetBackplateBackground/CornerRadius/Margin.
- [ ] **Step 4:** Port EnsureInnerSelectionBorder and EnsureOuterBorder with their Set*Properties methods.
- [ ] **Step 5:** Port all Remove* methods.
- [ ] **Step 6:** Port SetForegroundBrush for content foreground management.
- [ ] **Step 7:** Build to verify.
- [ ] **Step 8:** Commit.

## Task 6: Chrome Layout (Measure/Arrange)

**Files:**
- Create: `src/Uno.UI/UI/Xaml/Controls/Primitives/ListViewItemPresenter.Chrome.Layout.cs`

Port MeasureNewStyle and ArrangeNewStyle for chrome-managed layout, including positioning of multi-select checkbox, selection indicator, backplate, borders, and content.

- [ ] **Step 1:** Port MeasureOverride with multi-select and selection indicator awareness.
- [ ] **Step 2:** Port ArrangeOverride with child element positioning.
- [ ] **Step 3:** Build to verify.
- [ ] **Step 4:** Commit.

## Task 7: GridViewItemPresenter Parity

**Files:**
- Modify: `src/Uno.UI/UI/Xaml/Controls/Primitives/GridViewItemPresenter.cs`
- Modify: `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls.Primitives/GridViewItemPresenter.cs`

GridViewItemPresenter shares most chrome logic with ListViewItemPresenter (via CListViewBaseItemChrome base class). In Uno, GridViewItemPresenter delegates to ListViewItemPresenter's chrome methods but with GridView-specific defaults and behavior (IsChromeForGridViewItem=true).

- [ ] **Step 1:** Implement GridViewItemPresenter properties (subset of ListViewItemPresenter properties).
- [ ] **Step 2:** Update Generated file to skip implemented properties.
- [ ] **Step 3:** Build to verify.
- [ ] **Step 4:** Commit.

## Task 8: Full Build and Runtime Validation

- [ ] **Step 1:** Build the full Uno.UI.Skia project.
- [ ] **Step 2:** Build runtime tests.
- [ ] **Step 3:** Verify SamplesApp builds and runs with ListView/GridView samples.
- [ ] **Step 4:** Final commit.
