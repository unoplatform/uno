# ListViewItemPresenter / GridViewItemPresenter Porting Plan

## Overview

This document outlines the comprehensive plan for porting `ListViewItemPresenter` and `GridViewItemPresenter` from WinUI C++ to Uno Platform C#, as specified in [Issue #1444](https://github.com/unoplatform/uno/issues/1444).

The WinUI implementation consists of a sophisticated chrome-based rendering system with multiple layers, visual state management, animation commands, and extensive brush/property handling.

## Architecture Analysis

### WinUI Class Hierarchy

```
CContentPresenter
    └── CListViewBaseItemChrome (Core rendering engine - ~5200 lines)
            ├── CListViewItemChrome (ListView-specific)
            └── CGridViewItemChrome (GridView-specific, minimal specialization)

ContentPresenter (DirectUI/DXAML layer)
    └── ListViewBaseItemPresenter (Animation processing)
            ├── ListViewItemPresenter (ListView API surface)
            └── GridViewItemPresenter (GridView API surface)

CFrameworkElement
    └── CListViewBaseItemSecondaryChrome (Earmark geometry rendering)
```

### Key Components

1. **CListViewBaseItemChrome** (`ListViewBaseItemChrome.cpp/.h` - ~5200 lines)
   - Core rendering engine for all chrome layers
   - Visual state management (11 state groups)
   - Animation command queue management
   - Brush management (40+ brushes)
   - Layout (Measure/Arrange) with chrome elements
   - Secondary chrome integration

2. **CListViewBaseItemSecondaryChrome** (~200 lines)
   - Renders earmark geometry for selection visuals
   - Owned by primary chrome

3. **ListViewBaseItemPresenter** (`ListViewBaseItemPresenter_Partial.cpp/.h` - ~1100 lines)
   - Animation command visitor implementation
   - Processes 6 animation command types
   - Creates and manages storyboards

4. **ListViewItemPresenter** (`ListViewItemPresenter_Partial.cpp/.h` - ~130 lines)
   - `GoToElementStateCoreImpl` - delegates to chrome
   - Property accessors for padding/alignment

5. **GridViewItemPresenter** (`GridViewItemPresenter_Partial.cpp/.h` - ~70 lines)
   - Same structure as ListViewItemPresenter
   - Fewer properties (subset of ListView properties)

### Visual State Groups

The chrome manages 11 visual state groups:

| State Group | States | Description |
|-------------|--------|-------------|
| CommonStates | Normal, PointerOver, Pressed, PointerOverPressed, Disabled | Legacy hover/press states |
| FocusStates | Focused, Unfocused, PointerFocused | Focus visualization |
| SelectionHintStates | HorizontalSelectionHint, VerticalSelectionHint, NoSelectionHint | Swipe selection hints |
| SelectionStates | Unselecting, Unselected, UnselectedPointerOver, UnselectedSwiping, Selecting, Selected, SelectedSwiping, SelectedUnfocused | Selection states |
| DragStates | NotDragging, Dragging, DraggingTarget, MultipleDraggingPrimary, MultipleDraggingSecondary, DraggedPlaceholder, Reordering, ReorderingTarget, MultipleReorderingPrimary, ReorderedPlaceholder, DragOver | Drag/drop states |
| ReorderHintStates | NoReorderHint, BottomReorderHint, TopReorderHint, RightReorderHint, LeftReorderHint | Reorder hint animations |
| DataVirtualizationStates | DataAvailable, DataPlaceholder | Data virtualization |
| CommonStates2 | Normal, PointerOver, Pressed, Selected, PointerOverSelected, PressedSelected | New style combined states |
| DisabledStates | Enabled, Disabled | New style disabled state |
| MultiSelectStates | MultiSelectDisabled, MultiSelectEnabled | Multi-select checkbox mode |
| SelectionIndicatorStates | SelectionIndicatorDisabled, SelectionIndicatorEnabled | Selection indicator mode |

### Animation Commands

Six animation command types with visitor pattern:

1. **ListViewBaseItemAnimationCommand_Pressed** - Pointer down/up animations
2. **ListViewBaseItemAnimationCommand_ReorderHint** - Reorder hint offset animations
3. **ListViewBaseItemAnimationCommand_DragDrop** - Drag/drop visual state animations
4. **ListViewBaseItemAnimationCommand_MultiSelect** - Multi-select checkbox entrance/exit
5. **ListViewBaseItemAnimationCommand_IndicatorSelect** - Selection indicator slide animations
6. **ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility** - Selection indicator scale/fade

### Rendering Layers

Chrome renders in 6 ordered layers:
1. `ListViewBaseItemChromeLayerPosition_Base_Pre`
2. `ListViewBaseItemChromeLayerPosition_PrimaryChrome_Pre`
3. `ListViewBaseItemChromeLayerPosition_SecondaryChrome_Pre`
4. `ListViewBaseItemChromeLayerPosition_SecondaryChrome_Post`
5. `ListViewBaseItemChromeLayerPosition_PrimaryChrome_Post`
6. `ListViewBaseItemChromeLayerPosition_Base_Post`

## Porting Strategy

### Phase 1: Foundation Types and Enums

**Files to create:**
- `ListViewBaseItemChromeLayerPosition.cs` - Layer position enum
- `ListViewItemPresenterCheckMode.cs` - Already exists in Generated (just update `#if`)
- `ListViewItemPresenterSelectionIndicatorMode.cs` - Already exists in Generated (just update `#if`)

**Work items:**
1. Review and update generated enum files to enable them

### Phase 2: Animation Command Infrastructure

**Files to create:**
- `ListViewBaseItemAnimationCommand.cs` - Base command class
- `ListViewBaseItemAnimationCommand.Pressed.cs` - Pressed animation command
- `ListViewBaseItemAnimationCommand.ReorderHint.cs` - Reorder hint command
- `ListViewBaseItemAnimationCommand.DragDrop.cs` - Drag/drop command
- `ListViewBaseItemAnimationCommand.MultiSelect.cs` - Multi-select command
- `ListViewBaseItemAnimationCommand.IndicatorSelect.cs` - Indicator select command
- `ListViewBaseItemAnimationCommand.SelectionIndicatorVisibility.cs` - Selection indicator visibility command
- `IListViewBaseItemAnimationCommandVisitor.cs` - Visitor interface

### Phase 3: Secondary Chrome

**Files to create:**
- `ListViewBaseItemSecondaryChrome.cs` - Main class declaration
- `ListViewBaseItemSecondaryChrome.mux.cs` - Implementation (ported from .cpp)
- `ListViewBaseItemSecondaryChrome.h.mux.cs` - Header fields (ported from .h)

### Phase 4: Primary Chrome (Core Implementation)

**Files to create:**
- `ListViewBaseItemChrome.cs` - Main class declaration
- `ListViewBaseItemChrome.mux.cs` - Main implementation
- `ListViewBaseItemChrome.h.mux.cs` - Header fields/constants
- `ListViewBaseItemChrome.VisualStates.cs` - Visual state enums and structs
- `ListViewBaseItemChrome.Rendering.cs` - Draw methods
- `ListViewBaseItemChrome.Layout.cs` - Measure/Arrange
- `ListViewBaseItemChrome.Properties.cs` - Property accessors

**Key implementation considerations:**
- Replace `IContentRenderer` rendering with Uno's `Visual` layer or element-based rendering
- Replace `HWRenderParams` with Uno equivalents
- Implement brush management with weak references
- Port layout rounding helpers

### Phase 5: Base Presenter

**Files to create:**
- `ListViewBaseItemPresenter.cs` - Main class declaration
- `ListViewBaseItemPresenter.mux.cs` - Animation command processing
- `ListViewBaseItemPresenter.h.mux.cs` - Animation state fields

**Key implementation:**
- Port `ProcessAnimationCommands()` method
- Port all 6 `VisitAnimationCommand` overloads
- Create storyboard/animation infrastructure

### Phase 6: ListViewItemPresenter

**Files to update:**
- `ListViewItemPresenter.cs` - Update existing
- `ListViewItemPresenter.mux.cs` - Create implementation
- `ListViewItemPresenter.h.mux.cs` - Create header fields
- `ListViewItemPresenter.Properties.cs` - Move from Generated, implement

**Files in Generated to update:**
- Update `#if` directives to enable the type
- Remove `[NotImplemented]` attributes from implemented members

### Phase 7: GridViewItemPresenter

**Files to update:**
- `GridViewItemPresenter.cs` - Update existing
- `GridViewItemPresenter.mux.cs` - Create implementation
- `GridViewItemPresenter.h.mux.cs` - Create header fields
- `GridViewItemPresenter.Properties.cs` - Move from Generated, implement

### Phase 8: Integration and Testing

1. Update `ListViewBaseItem` to use the new presenter
2. Update `GridViewBaseItem` to use the new presenter
3. Create unit tests for visual state transitions
4. Create UI tests for rendering verification
5. Verify animations work correctly

## File Mapping

### WinUI to Uno File Mapping

| WinUI Source | Uno Target |
|-------------|-----------|
| `ListViewBaseItemChrome.h` | `ListViewBaseItemChrome.h.mux.cs` + `ListViewBaseItemChrome.VisualStates.cs` |
| `ListViewBaseItemChrome.cpp` | `ListViewBaseItemChrome.mux.cs` + `ListViewBaseItemChrome.Rendering.cs` + `ListViewBaseItemChrome.Layout.cs` |
| `ListViewBaseItemSecondaryChrome.h` | `ListViewBaseItemSecondaryChrome.h.mux.cs` |
| `ListViewBaseItemSecondaryChrome.cpp` | `ListViewBaseItemSecondaryChrome.mux.cs` |
| `ListViewBaseItemPresenter_Partial.h` | `ListViewBaseItemPresenter.h.mux.cs` |
| `ListViewBaseItemPresenter_Partial.cpp` | `ListViewBaseItemPresenter.mux.cs` |
| `ListViewItemPresenter_Partial.h` | `ListViewItemPresenter.h.mux.cs` |
| `ListViewItemPresenter_Partial.cpp` | `ListViewItemPresenter.mux.cs` |
| `ListViewItemPresenter.g.h` | `ListViewItemPresenter.Properties.cs` (properties only) |
| `GridViewItemPresenter_Partial.h` | `GridViewItemPresenter.h.mux.cs` |
| `GridViewItemPresenter_Partial.cpp` | `GridViewItemPresenter.mux.cs` |
| `GridViewItemPresenter.g.h` | `GridViewItemPresenter.Properties.cs` (properties only) |

## Dependencies and Blockers

### Required Infrastructure
- `SerialDisposable` and `CompositeDisposable` (available in Uno.Disposables)
- Theme animations (`PointerDownThemeAnimation`, `PointerUpThemeAnimation`, etc.)
- `ThemeGenerator`/`ThemeGeneratorHelper` for animation creation

### Potential Issues
1. **Rendering**: WinUI uses low-level `IContentRenderer`; Uno may need element-based approach
2. **Composition**: WinUI uses Composition APIs for some visuals
3. **Hit testing**: Custom hit test logic needs adaptation
4. **Focus visuals**: `FocusRectangleOptions` integration

## Constants Reference

Key constants from `ListViewBaseItemChrome.cpp`:

```csharp
// Size of check mark visual (old style)
static readonly Size s_selectionCheckMarkVisualSize = new(40.0f, 40.0f);

// Focus rectangle thickness
static readonly Thickness s_focusBorderThickness = new(2.0f, 2.0f, 2.0f, 2.0f);

// Offset from top-right corner of control border bounds
static readonly Point s_checkmarkOffset = new(-20.0f, 6.0f);

// Offsets for swipe hint animations
static readonly Point s_swipeHintOffset = new(-23.0f, 15.0f);

// Opacity values
const float s_swipingCheckSteadyStateOpacity = 0.5f;
const float s_selectingSwipingCheckSteadyStateOpacity = 1.0f;
const float s_cOpacityUnset = -1.0f;

// Corner radii
const float s_generalCornerRadius = 4.0f;
const float s_innerBorderCornerRadius = 3.0f;

// Selection indicator
static readonly Size s_selectionIndicatorSize = new(3.0f, 16.0f);
const float s_selectionIndicatorHeightShrinkage = 6.0f;
static readonly Thickness s_selectionIndicatorMargin = new(4.0f, 20.0f, 0.0f, 20.0f);

// Multi-select checkbox
static readonly Size s_multiSelectSquareSize = new(20.0f, 20.0f);
static readonly Thickness s_multiSelectSquareThickness = new(2.0f);
static readonly Thickness s_multiSelectRoundedSquareThickness = new(1.0f);
static readonly Thickness s_multiSelectSquareInlineMargin = new(12.0f, 0.0f, 0.0f, 0.0f);
static readonly Thickness s_multiSelectRoundedSquareInlineMargin = new(14.0f, 0.0f, 0.0f, 0.0f);
static readonly Thickness s_multiSelectSquareOverlayMargin = new(0.0f, 2.0f, 2.0f, 0.0f);

// Backplate/border
static readonly Thickness s_backplateMargin = new(4.0f, 2.0f, 4.0f, 2.0f);
static readonly Thickness s_borderThickness = new(1.0f);
static readonly Thickness s_innerSelectionBorderThickness = new(1.0f);

// Content offsets
const float s_listViewItemMultiSelectContentOffset = 32.0f;
const float s_multiSelectRoundedContentOffset = 28.0f;

// CheckMark path points
static readonly Point[] s_checkMarkPoints = new[]
{
    new Point(0.0f, 7.0f),
    new Point(2.2f, 4.3f),
    new Point(6.1f, 7.9f),
    new Point(12.4f, 0.0f),
    new Point(15.0f, 2.4f),
    new Point(6.6f, 13.0f)
};

// Focus border thickness
const float s_listViewItemFocusBorderThickness = 1.0f;
const float s_gridViewItemFocusBorderThickness = 2.0f;

// CheckMark glyph
const float s_checkMarkGlyphFontSize = 16.0f;
const string c_strCheckMarkGlyph = "\uE73E";

// Default values for rounded chrome
static readonly CornerRadius s_defaultSelectionIndicatorCornerRadius = new(1.5f);
static readonly CornerRadius s_defaultCheckBoxCornerRadius = new(3.0f);
static readonly Thickness s_selectedBorderThicknessRounded = new(2.0f);
static readonly Thickness s_selectedBorderThickness = new(0.0f);
```

## Brush Properties (40+)

The presenter exposes 40+ brush properties. Here's the complete list:

### Old Style (Legacy)
- CheckHintBrush
- CheckSelectingBrush
- CheckBrush
- CheckPressedBrush
- CheckDisabledBrush

### Common
- FocusBorderBrush
- FocusSecondaryBorderBrush
- PlaceholderBackground
- DragBackground
- DragForeground
- ContentMargin (Thickness)
- DisabledOpacity
- DragOpacity
- ReorderHintOffset

### Pointer Over
- PointerOverBackground
- PointerOverBorderBrush
- PointerOverForeground
- PointerOverBackgroundMargin (Thickness)

### Selected
- SelectedBackground
- SelectedForeground
- SelectedBorderBrush
- SelectedInnerBorderBrush
- SelectedBorderThickness (Thickness)

### Selected + Pointer Over
- SelectedPointerOverBackground
- SelectedPointerOverBorderBrush

### Selected + Pressed
- SelectedPressedBackground
- SelectedPressedBorderBrush

### Selected + Disabled
- SelectedDisabledBackground
- SelectedDisabledBorderBrush

### Pressed
- PressedBackground

### CheckBox (Rounded Style)
- CheckBoxBrush
- CheckBoxPointerOverBrush
- CheckBoxPressedBrush
- CheckBoxDisabledBrush
- CheckBoxSelectedBrush
- CheckBoxSelectedPointerOverBrush
- CheckBoxSelectedPressedBrush
- CheckBoxSelectedDisabledBrush
- CheckBoxBorderBrush
- CheckBoxPointerOverBorderBrush
- CheckBoxPressedBorderBrush
- CheckBoxDisabledBorderBrush
- CheckBoxCornerRadius (CornerRadius)

### Selection Indicator
- SelectionIndicatorBrush
- SelectionIndicatorPointerOverBrush
- SelectionIndicatorPressedBrush
- SelectionIndicatorDisabledBrush
- SelectionIndicatorCornerRadius (CornerRadius)
- SelectionIndicatorMode (enum)
- SelectionIndicatorVisualEnabled (bool)

### Reveal (Legacy)
- RevealBackground
- RevealBorderBrush
- RevealBorderThickness (Thickness)
- RevealBackgroundShowsAboveContent (bool)

### Other
- CheckMode (enum)
- SelectionCheckMarkVisualEnabled (bool)

## Estimated Effort

| Phase | Estimated Lines | Complexity |
|-------|----------------|------------|
| Phase 1: Foundation | ~100 | Low |
| Phase 2: Animation Commands | ~400 | Medium |
| Phase 3: Secondary Chrome | ~200 | Medium |
| Phase 4: Primary Chrome | ~3500 | High |
| Phase 5: Base Presenter | ~800 | High |
| Phase 6: ListViewItemPresenter | ~200 | Medium |
| Phase 7: GridViewItemPresenter | ~100 | Low |
| Phase 8: Integration | ~500 | Medium |

**Total estimated: ~5800 lines of C# code**

## Implementation Summary (Completed)

### Files Created

#### Core Infrastructure
| File | Description |
|------|-------------|
| `ListViewBaseItemChromeLayerPosition.cs` | Layer position enum for chrome rendering order |
| `ListViewBaseItemChromeVisualStates.cs` | 11 visual state enums + tracking struct |
| `ListViewBaseItemAnimationCommand.cs` | 6 animation command types + visitor interface |
| `ListViewBaseItemChromeConstants.cs` | Sizing/positioning constants |
| `ListViewBaseItemSecondaryChrome.cs` | Earmark geometry class |

#### ListViewBaseItemChrome (Abstract Base)
| File | Description |
|------|-------------|
| `ListViewBaseItemChrome.cs` | Main chrome class with state tracking, animation queue |
| `ListViewBaseItemChrome.VisualStates.cs` | State name to enum mappings |
| `ListViewBaseItemChrome.mux.cs` | GoToChromedState implementation |
| `ListViewBaseItemChrome.Layout.cs` | MeasureNewStyle/ArrangeNewStyle implementations |
| `ListViewBaseItemChrome.Elements.cs` | Element management (backplate, checkbox, etc.) |
| `ListViewBaseItemChrome.Properties.cs` | Brush setters and property accessors |

#### Presenters (Composition Pattern)
| File | Description |
|------|-------------|
| `ListViewItemPresenter.cs` | Main presenter using composition with chrome helper |
| `ListViewItemPresenterChrome.cs` | Chrome helper for ListViewItemPresenter |
| `GridViewItemPresenter.cs` | Main presenter using composition with chrome helper |
| `GridViewItemPresenterChrome.cs` | Chrome helper for GridViewItemPresenter |

### Key Architectural Decisions

1. **Composition over Inheritance**: The generated partial classes inherit from `ContentPresenter`, which cannot be changed. Therefore, we use a composition pattern where each presenter owns a chrome helper class that handles visual state management and animations.

2. **Lazy Initialization**: Chrome helpers are lazily initialized to avoid constructor conflicts with generated code.

3. **Rounded Chrome Style**: The implementation focuses on the modern "rounded" chrome style used in WinUI 3, while maintaining compatibility hooks for the legacy style.

4. **Animation Command Visitor Pattern**: Follows WinUI's design where animation commands are enqueued by the chrome and processed by the presenter using the visitor pattern.

### Visual State Groups Supported

1. **CommonStates2** - Normal, PointerOver, Pressed, Selected, PointerOverSelected, PressedSelected
2. **DisabledStates** - Enabled, Disabled
3. **FocusStates** - Focused, Unfocused, PointerFocused
4. **MultiSelectStates** - MultiSelectDisabled, MultiSelectEnabled
5. **SelectionIndicatorStates** - SelectionIndicatorDisabled, SelectionIndicatorEnabled
6. **ReorderHintStates** - NoReorderHint, BottomReorderHint, TopReorderHint, RightReorderHint, LeftReorderHint
7. **DragStates** - NotDragging, Dragging, DraggingTarget, etc.
8. **DataVirtualizationStates** - DataAvailable, DataPlaceholder

### Animation Commands Implemented

1. **Pressed** - Pointer down/up theme animations
2. **ReorderHint** - Item translation for reorder hints
3. **DragDrop** - Drag/drop visual feedback
4. **MultiSelect** - Checkbox entrance/exit animations
5. **IndicatorSelect** - Selection indicator slide animations
6. **SelectionIndicatorVisibility** - Scale/fade animations

### Known Limitations

1. **Visual Tree Integration**: Chrome elements (backplate, checkbox, etc.) are created but not fully integrated into the visual tree. This will require additional work to properly render.

2. **Some Animation Stubs**: Certain animation commands have placeholder implementations that clear state but don't run full animations yet.

3. **Unused Fields**: Some fields are declared for future use and have pragma warnings suppressed.

### Build Status

✅ **Uno.UI.Skia.csproj** - Builds successfully (0 errors)
