# Contracts: C# ↔ TS interop changes

The accessibility layer's contract is the JSImport (C#→JS) / JSExport (JS→C#) boundary
between `WebAssemblyAccessibility.cs` / `SemanticElementFactory.cs` and
`Accessibility.ts` / `SemanticElements.ts`. These signatures **must stay in sync** —
argument order is positional. Below are the changes this feature requires. Exact names are
illustrative; match existing conventions and confirm against the live source before coding.

## 1. `Create*Element` JSImports gain a focusability signal (FR-005)

**Today** none of the factory create functions accept `isFocusable`
([SemanticElementFactory.cs:1001-1103](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/SemanticElementFactory.cs)).
Each TS `create*Element` hardcodes `tabIndex`.

**Change**: add an `isFocusable` (bool) parameter to every `Create*Element` JSImport, pass
`IsAccessibilityFocusable(peer.Owner, peer.Owner.IsFocusable)` from the factory, and have
each TS `create*Element` route its tab-stop decision through the existing
`updateElementFocusability(element, isFocusable)`
([Accessibility.ts:217-229](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts)) instead of a literal.

```
// C# (illustrative)
CreateButtonElement(parentHandle, handle, index, x, y, w, h, label, disabled, isFocusable)
CreateCheckboxElement(..., checked, label, isRadio, isFocusable)   // + initial checked for radio (FR-001)
CreateTextBoxElement(..., multiline, password, isReadOnly, selStart, selEnd, isFocusable)
// ... and every other Create*Element
```

**Non-interactive element types** (`CreateHeadingElement`) must **not** set `tabindex` at
all, regardless of `isFocusable` (FR-006) — remove the hardcoded `element.tabIndex = 0` at
[SemanticElements.ts:461](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts).

## 2. RadioButton initial checked + selection routing (FR-001..003)

**Initial checked** — supply it from `RadioButton.IsChecked` rather than the absent Toggle
pattern. Either populate `AriaAttributes.Checked` for radios in `AriaMapper`, or pass an
explicit `checked` into `CreateRadioElement` derived from the owner.

**DOM activation routing** — the radio's DOM `change` handler currently calls
`onToggle` ([SemanticElements.ts:430-434](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts)),
and `OnToggle` no-ops for radio. Route radio activation to a selection callback instead:

```
// JSExport (JS→C#): radio activation must reach a real peer action
OnSelect(handle)        // → ISelectionItemProvider.Select() (or AutomationRadioButtonOnToggle)
// SemanticElements.ts: radio 'change' listener → callbacks.onSelect(handle), NOT onToggle
```

**External-change sync** — the `IsSelectedProperty` branch for a radio must update the
native `checked` property (via the existing `updateAriaChecked` path which already sets
native `checked`/`indeterminate`, [Accessibility.ts:674-693](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts)),
not `aria-selected` ([WebAssemblyAccessibility.cs:1550-1558](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)).

## 3. New / activated live-update JSImports (FR-009)

Add `NotifyPropertyChangedEventCore` branches and the corresponding update functions where
missing:

```
UpdateHeadingLevel(handle, ariaLevel)        // exists (UpdateAriaLevel) but unreachable on WASM — wire a branch; aria-level carries true 1..9 (FR-011)
UpdateTextBoxPlaceholder(handle, placeholder) // exists for creation — add a PlaceholderTextProperty branch
UpdateAriaRequired(handle, required)          // JSImport exists — call it on IsRequiredForForm change, not only creation
// PasswordBox value: browser-local value synchronization — see §4
```

## 4. PasswordBox value live-sync (FR-009) — browser projection

The browser's existing `TextBoxCore` notifications synchronize value, focus, and
selection changes for both text controls. `UpdateTextBoxValueKeepingSelection`
must use `PasswordBoxAutomationPeer.MaskPasswordValue` for password cores, not
`core.Text` verbatim. Initial realization and subsequent updates use the same
masked value, including when the visual control is revealed. No duplicate control
subscriptions or public UIA events are needed.

The text-input export also receives an edit range:

```text
OnTextInput(handle, value, selectionStart, selectionEnd, replacementStart, replacementLength)
```

For ordinary text and full-value replacement, the last two arguments are `-1`.
For a password edit, `value` contains only the inserted text; the edit range comes
from the browser's pre-input selection and the retained masked prefix/suffix.
The managed side preserves the real password outside that range. Composition is
committed once rather than feeding masked bullets back into the password.

Mask offsets count UTF-16 units. The managed splice expands a nonempty edit to
whole surrogate pairs and moves a collapsed insertion out of a pair before
changing text. It then remaps the browser's post-edit selection to the resulting
text; a one-bullet Backspace/Delete must not leave an unpaired half or a stale
caret. This follows the valid-position rule in pinned
`TextBoxHelpers.cpp::IsNotInSurrogateCRLF`; native RichEdit keyboard probes also
remove a supplementary character atomically. Raw programmatic password selection
APIs can accept half-pair offsets, so the projection must not treat those mask
positions as valid underlying edit boundaries.

The two selection coordinates at this internal boundary are directed anchor/caret
positions, not necessarily increasing. They use the existing signed
`TextBoxCore.SetPendingSelection` convention and round-trip as a backward DOM
selection when the anchor follows the caret. Ordinary TextBox input keeps its
full-value path. As in `BrowserInvisibleTextBoxViewExtension.OnInputTextChanged`,
the directed selection is applied after updating text, including no-op replacements.
The semantic value is re-synchronized even when the password did not change, so
replacing a character with itself cannot leave the inserted character in the DOM.

This does **not** add a public UIA Value change event. The source policy is pinned to
WinUI `winui3/release/1.8.4`, commit `dc46907e92b9965bb48d6001773777335b41aa24`:
`src/dxaml/xcp/core/native/text/Controls/PasswordBox.cpp` does not raise that event
from `OnContentChanged`. Likewise,
`src/dxaml/xcp/dxaml/lib/PasswordBoxAutomationPeer_Partial.cpp` supplies placeholder
hints through `GetDescribedByCoreImpl`, not a HelpText override. Header/placeholder
changes must not fabricate HelpText notifications.

## 5. Roving driver from focus (FR-012)

No interop signature change — add a C# call site: `FocusSynchronizer.OnXamlGotFocus` /
`OnBrowserFocus` ([FocusSynchronizer.cs:115-224](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/FocusSynchronizer.cs))
should call `UpdateRovingTabindex(focusedHandle)` alongside `FocusSemanticElement`. The TS
`updateRovingTabindex` ([Accessibility.ts:318-371](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts))
is reused as-is (extend recognized groups if grid cell-nav is in scope).

## 6. ScrollViewer region gating (FR-013..014)

No new JSImport — change the C# decision: only compute `role=region` for a ScrollViewer
that is actually scrollable (`IScrollProvider` Horizontally/VerticallyScrollable) **and**
has a real accessible name; resolve the label via `AriaMapper.ResolveLabel` (not raw
`GetName()` which dumps descendant text). Otherwise emit the generic element with no
landmark role.

## 7. ARIA attribute correctness (FR-018..030)

These are mostly **C#-side value/source corrections** plus a few new attribute setters; the
interop boundary changes are limited.

**Name & identity (FR-018/019)** — the generic path must stop using `AutomationId` as the
label. Split the single `automationId` arg into two concepts:

```
// today: AddSemanticElement(..., automationId, ...) where automationId = GetAutomationId() ?? GetName()
//        and TS does setAttribute('aria-label', automationId)
// target:
AddSemanticElement(..., ariaLabel, domAutomationId, ariaLabelledById, ...)
// TS: if (ariaLabel) setAttribute('aria-label', ariaLabel)         // from ResolveLabel only
//     if (domAutomationId) setAttribute('xamlautomationid', domAutomationId)  // NOT a name
//     if (ariaLabelledById) setAttribute('aria-labelledby', ariaLabelledById) // IDREF, populate AriaAttributes.LabelledBy
```

**Role normalization (FR-020)** — no signature change; a **value** fix in shared C#. Map
`FindHtmlRole`'s UIA tokens to valid ARIA (`image`→`img`, `edit`→`textbox`, drop
`pane`/`window`/`custom`/…) in `AutomationProperties.uno.cs` so both the Skia generic path
and the native WASM-DOM path emit valid roles; reconcile `ToggleSwitch`→`switch`.

**Generic-path parity (FR-021)** — the generic `AddSemanticElement` must apply the same
`GetAriaAttributes`-derived attributes the factory applies (describedby/controls/flowto/
required/description/posinset/setsize/selected/valuenow/modal+role=dialog). Either route the
generic path through the factory's attribute appliers, or call the same `UpdateAria*`
JSImports post-create.

**IDREF integrity (FR-022)** — `ResolvePeerCollectionToIdList` (and `activedescendant`) must
only emit a `uno-semantics-{handle}` id when that handle has a live semantic node
(`HasSemanticElement(handle)`); skip absent ones; clear the attribute when the collection is
cleared/deselected. TS appliers should defensively drop ids with no `getElementById` match.

**New attribute setters (FR-023..026)** — small additions (value + JSImport/setter):

```
UpdateAriaInvalid(handle, invalid)        // from IsDataValidForForm (inverted)
// aria-orientation: set on the <input type=range>/scrollbar element at creation + on change
UpdateAriaOrientation(handle, orientation)  // replace non-standard orient/CSS
// aria-roledescription from LocalizedControlType (reuse existing updateAriaRoleDescription)
// aria-level from AutomationProperties.Level (distinct from HeadingLevel) → existing UpdateAriaLevel
```

**Live-sync branches (FR-027)** — add `NotifyPropertyChangedEventCore` branches (or chain to
base / a generalized map) for `FullDescription`, `IsRequiredForForm`, `HeadingLevel`,
`IsDialog`, `LiveSetting`, `AcceleratorKey`/`AccessKey`; preserve `FullDescription`>`HelpText`
precedence so the `HelpText` branch doesn't clobber a `FullDescription`-derived description.

**Value-semantics (FR-028)** — drive `aria-haspopup` from the C# `HasPopup` value (not TS
hardcoding); map `AccessKey` to the HTML `accesskey` attribute; stop injecting `posinset`
"N of M" into `aria-label`.

## 8. ComboBox value, option state, and popup relationships

The virtualized-item import and its TypeScript implementation have matching final
arguments:

```text
AddVirtualizedItem(containerHandle, itemHandle, index, totalCount, x, y, width, height,
                  role, label, selected, automationId)
```

`selected` is `bool?` / `boolean | null`; `null` omits `aria-selected`.
`automationId` is a string mapped to `xamlautomationid`, never the name. Both
attributes are refreshed or removed on reused nodes. Selection updates arriving
before a queued realization must take precedence over its older state.
Live `IsSelected` notifications originate from the owner's item **data peer**, not
a cast of the container's `FrameworkElementAutomationPeer` to
`SelectorItemAutomationPeer`. This follows
`Selector_Partial.cpp::RaiseIsSelectedChangedAutomationEvent` at the same WinUI
`dc46907e92b9965bb48d6001773777335b41aa24` pin; null/placeholder items are skipped.
These owner-created peers are cached by item reference. Collection remove, replace,
reset, and source replacement must remove absent items from all managed peer stores
without replacing peers for surviving references or duplicates in another group.
Recycled containers release their item-peer `EventsSource` link. Direct
`ItemCollection[index]` assignment raises `ItemChanged`, matching the pinned
`ItemCollection_Partial.cpp::SetAt`, so that replacement follows the same lifecycle.

`SemanticElements.updateComboBoxValue(handle, selectedValue)` updates a dedicated
text node without replacing the accessible name or semantic child controls.
Select-only ComboBoxes still do not advertise WinUI's editable Value pattern.
Their browser value uses the selection's displayed content, including
`DisplayMemberPath`, rather than the data object's type name.

`Accessibility.updateAriaControls(handle, idList)` owns authored `ControlledPeers`;
`Accessibility.updateRuntimeAriaControls(handle, idList)` owns generated popup
relationships. The emitted `aria-controls` is their de-duplicated union. Updating
or clearing either source must leave the other intact.

## Contract sync rule

Every signature change here touches **both** the C# JSImport declaration and the TS
`globalThis.Uno.UI.Runtime.Skia.*` function. A mismatch fails silently at runtime (wrong
arg lands in the wrong parameter). Each contract change must land with a runtime test that
exercises the live DOM (FR-016), which is the only thing that catches an arg-order drift.
