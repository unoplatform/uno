# Data Model: WASM Accessibility Remediation

This feature is a runtime accessibility layer — the "data model" is the **state and
decision model** that produces the semantic DOM, not persisted entities. The defects in
[research.md](./research.md) all trace to these structures.

## Entities & state

### 1. Semantic element creation paths

Two routes turn a `UIElement`/`AutomationPeer` into a DOM node. The defect is divergence.

| Path | Trigger | Focusability | Attribute coverage | Label source |
|------|---------|--------------|--------------------|--------------|
| **Factory** (`SemanticElementFactory.CreateElement` → `Create*Element` JSImport → `create*Element` TS) | peer exists (common case), tried first | **none** — `tabindex` hardcoded in TS | full (disabled, required, posinset, …) | `AriaMapper.ResolveLabel` |
| **Generic** (`AddSemanticElement` fallback → `addSemanticElement` TS) | factory returns false (`Generic`) | **gated** on `IsAccessibilityFocusable` | subset (no disabled/required/posinset/labelledby…) | `peer.GetName()` (AutomationId first) |

**Target state**: both paths honor the focusability gate; the factory gains an
`isFocusable` parameter; attribute coverage and label source converge (or the divergence
is documented and intentional).

### 2. Focusability gate (authority for `tabindex`)

`IsAccessibilityFocusable(DependencyObject element, bool isFocusable)`
([SkiaAccessibilityBase.cs:473-500](../../src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs)):

```
false if (!isFocusable && no RoleOverride)
false if AccessibilityView == Raw
true  if RoleOverride
false if no automation peer
true  otherwise
```

`isFocusable` = `UIElement.IsFocusable` = `IsVisible && (IsEnabled || AllowFocusWhenDisabled)
&& (IsTabStop || IsFocusableForFocusEngagement()) && AreAllAncestorsVisible()`.

**Target state**: this is the single authority for whether any semantic element receives
`tabindex=0`. Non-interactive element types (heading, region, group, static text) never
receive a tab stop regardless of this gate (they are not interactive even when focusable
for other reasons).

### 3. tabindex decision matrix (target)

| Element category | Examples | tabindex (target) |
|---|---|---|
| Interactive control | button, checkbox, slider, textbox, combobox, link, switch | `0` **iff** focusability gate true; else none |
| Non-interactive | heading `<hN>`, static text, image | **none** (never a tab stop) |
| Landmark / group container | region (ScrollViewer), group | **none** (navigated via landmarks/rotor) |
| Composite container | listbox, tablist, tree, menu, grid | per chosen model — roving: `-1`/none; activedescendant: `0` (gated) — **one model, consistently** |
| Composite item | option, tab, treeitem, gridcell, menuitem, virtualized item | `-1`, with exactly one promoted to `0` (roving active item) |

### 4. Roving model

- **Group scope**: radio-name group, or `role` in {tab, option, menuitem, treeitem}
  ([Accessibility.ts:336-356](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts)).
- **Current driver**: only `IsSelected→true` and radio `ToggleState→On`
  ([WebAssemblyAccessibility.cs:1482-1485,1562-1565](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)).
- **Defects**: (a) no creation-time promotion → "multiple-zero" window (radios) or
  "container-only" entry; (b) not driven by focus movement → stale on arrow nav.
- **Target state**: promote exactly one item at creation; drive `UpdateRovingTabindex`
  from `FocusSynchronizer` focus changes; add gridcell/row/columnheader to recognized
  groups if grid cell-nav is in scope.

### 5. Property → attribute update map

`NotifyPropertyChangedEventCore` ([WebAssemblyAccessibility.cs:1459-1705](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs))
is a hand-maintained switch; properties without a branch are **creation-only**.

| Property | Currently live? | Target |
|---|---|---|
| ToggleState, Name, HelpText, Landmark, IsEnabled, ExpandCollapse, IsSelected, Value(TextBox), IsReadOnly, RangeValue, ScrollPercent, LabeledBy, Position/SizeOfSet | ✅ live | keep |
| **PasswordBox value** | ❌ (peer never raises) | live (raise event + branch) |
| **HeadingLevel** (`aria-level`) | ❌ (no branch) | live |
| **PlaceholderText** | ❌ | live |
| **IsRequiredForForm** (`aria-required`) | ❌ | live |
| **Horizontally/VerticallyScrollable** (region) | ❌ | live or documented-deferred |
| AcceleratorKey/AccessKey, MultiSelectable, RoleDescription | ❌ | best-effort / documented |

**Target state**: add the missing P1–P2 branches; FR-010 — consider a generalized,
data-driven property→attribute table so future properties are not silently creation-only.

### 6. RadioButton state (the broken mapping)

| Aspect | Current | Target |
|---|---|---|
| Initial `checked` | always false (`Checked` only from Toggle pattern; radio peer has only SelectionItem) | derived from `RadioButton.IsChecked` |
| DOM activation | `change → onToggle → Toggle()` = null = no-op | routes to a real selection action on the peer |
| External change | `IsSelected → aria-selected` (invalid on radio) | native `checked` (+ correct ARIA) |
| Group tab stop | every radio `0` at creation | one `0`, rest `-1` (roving), at creation |

## State transitions

**Radio activation (target)**: user activates DOM radio → JSExport selection callback →
peer selects → `IsChecked` flips → automation raise → DOM native `checked` updates + roving
moves the single `tabindex=0` to the checked radio + announcement.

**Heading level change (target)**: `AutomationProperties.HeadingLevel` set → automation
raise → `NotifyPropertyChangedEventCore` HeadingLevel branch → update `aria-level` (true
level 1–9), re-tag `<hN>` best-effort (tag clamped to `<h6>`).

**Focus movement in a composite (target)**: focus moves to item → `FocusSynchronizer`
focus handler → `UpdateRovingTabindex(item)` → item becomes `tabindex=0`, siblings `-1`.

## 7. AutomationProperties → ARIA attribute map (audit categories)

The source-property → ARIA-attribute mapping (`AriaMapper.GetAriaAttributes` + factory
appliers + generic path). Each mapping has a **status** (research §8); the target state is
all `correct`:

| Status | Meaning | Examples (current) |
|---|---|---|
| `correct` | right attribute, both paths, live where applicable | `LandmarkType`→role, `ExpandCollapse`→`aria-expanded`, `AccessibilityView.Raw`→pruned |
| `wrong-target` | mapped to the **wrong** attribute | `AutomationId`→`aria-label`; `LabeledBy`→flattened `aria-label`; `FindHtmlRole`→invalid `role`; `AccessKey`→`aria-keyshortcuts` |
| `generic-path-gap` | factory applies it, generic path drops it | `aria-describedby`/`controls`/`flowto`/`required`/`posinset`/`setsize`/`selected`/`valuenow`/`modal` |
| `creation-only` | set once, no live-sync branch | `HeadingLevel`/`aria-level`, `aria-required`, `aria-keyshortcuts`, `aria-multiselectable`, `aria-haspopup`, `aria-valuetext`, `FullDescription` |
| `unmapped` | no mapping at all | `aria-invalid`(`IsDataValidForForm`), `aria-orientation`, `aria-roledescription`(`LocalizedControlType`), `aria-level`(`Level`), `aria-busy`(`ItemStatus`), `lang`(`Culture`), `aria-owns`/`current`/`details` |
| `dangling-IDREF` | references an id not present in the AOM | `aria-controls`/`flowto`/`describedby`/`activedescendant` for structural/pruned peers |

**Two creation paths diverge** (see §1): the factory path uses `ResolveLabel` + the full
`GetAriaAttributes` set; the generic path uses `GetAutomationId()??GetName()` (hence the
`AutomationId`→`aria-label` defect) and a reduced attribute set (hence `generic-path-gap`).
Target: both paths converge on `ResolveLabel` for the name and the full attribute set.

**Role source**: `AriaMapper.ControlTypeToRoleMap` (valid ARIA) vs `FindHtmlRole` (UIA
tokens). Target: a single normalization so only valid WAI-ARIA roles (or native-implicit
roles) are emitted, identical on the Skia generic path and the native WASM-DOM path.

**IDREF integrity entity**: a relationship attribute's value is a list of
`uno-semantics-{handle}` ids. Invariant (target): every id in the list corresponds to a
semantic element currently in the AOM; on target removal/deselect the attribute is rewritten
or cleared.
