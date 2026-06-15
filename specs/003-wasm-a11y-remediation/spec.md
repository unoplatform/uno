# Feature Specification: WASM Accessibility Remediation

**Feature Branch**: `003-wasm-a11y-remediation`
**Created**: 2026-06-06
**Status**: Draft
**Input**: "Improve the accessibility implementation: map XAML controls to the correct
semantic HTML elements (Button→`<button>`, TextBox/PasswordBox→`<input>`, TextBlock
headings→`<h1>`–`<h6>`, body text→`<p>`/`<span>`, ScrollViewer→`role=region`,
CheckBox→`<input type=checkbox>`), with correct fallbacks." — plus a follow-up:
"a lot of Uno elements have `tabindex` when they shouldn't."
**Depends On**: `001-wasm-accessibility`, `002-wasm-a11y-advanced`

**Terminology**: "AOM" = the Skia-WASM **accessibility object model**, i.e. the parallel
semantic DOM overlay (`#uno-semantics-root`). Used consistently throughout; "semantic DOM"
and "semantic overlay" are synonyms for the same structure.

## Context

A verification audit of the *existing* Skia-on-WebAssembly accessibility layer (see
[research.md](./research.md)) found that most requested mappings already exist, but with
real defects: one mapping is **broken** (RadioButton), several are **partial**
(PasswordBox/Heading live-sync, ScrollViewer region), one is a **gap** (standalone body
text), and a cross-cutting **tabindex** flaw puts non-interactive elements (headings,
composite containers) into the keyboard tab order. The subsystem is also **runtime-untested**
(its runtime tests are almost entirely `[Ignore]`d). This feature remediates those
defects and establishes DOM-level test coverage. It targets the **Skia WASM** AOM only;
the native WASM-DOM target is maintenance-only.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Radio buttons are usable by screen-reader & keyboard users (Priority: P1)

A screen-reader/keyboard user encounters a `RadioButton` group, hears the correct
checked state, can select an option, and the selection is announced.

**Why this priority**: Currently broken on three axes — radios render unchecked
regardless of `IsChecked`, clicking does nothing at the peer level, and external changes
write an invalid attribute. This is the most severe, fully user-blocking defect.

**Independent Test**: Build a `RadioButton` group with one `IsChecked=true`; enable the
AOM; assert the corresponding `<input type=radio>` has `checked=true`, that activating it
via the DOM invokes selection on the peer, and that changing `IsChecked` in code updates
the DOM `checked` state. Fails before, passes after.

**Acceptance Scenarios**:

1. **Given** a `RadioButton` with `IsChecked=true`, **When** the semantic element is
   created, **Then** the `<input type=radio>` is rendered `checked` and exactly one radio
   in the group is checked.
2. **Given** focus on an unchecked radio, **When** the user activates it (Space/click via
   AT), **Then** the peer selects it (`ISelectionItemProvider.Select` or equivalent) and
   the new state is announced.
3. **Given** `IsChecked` is changed in code, **When** the change propagates, **Then** the
   DOM radio's native `checked` state updates (not `aria-selected`).
4. **Given** a radio group, **When** rendered, **Then** exactly one radio is a keyboard
   tab stop (`tabindex=0`), others `-1` (roving), and arrowing changes the checked radio.

---

### User Story 2 - Non-interactive elements are not keyboard tab stops (Priority: P1)

A keyboard user tabs through a page and lands only on genuinely interactive controls —
not on headings, region landmarks, or composite-widget containers.

**Why this priority**: Headings and grouping containers are currently hardcoded
`tabindex=0`, injecting non-actionable stops into the tab order (WCAG 2.4.3). The heading
case is permanent and uncorrectable today.

**Independent Test**: Render a page with headings, a ListBox, a TabControl, and a Menu;
enable the AOM; assert that headings and the tablist/tree/menu containers are **not** in
the tab order, while interactive controls are. Fails before, passes after.

**Acceptance Scenarios**:

1. **Given** a `TextBlock` with `AutomationProperties.HeadingLevel` set, **When** its
   `<hN>` semantic element is created, **Then** it has **no** `tabindex` and is reachable
   only via screen-reader heading navigation.
2. **Given** a control with `IsTabStop=false` or `IsEnabled=false`, **When** its semantic
   element is created, **Then** it is **not** a keyboard tab stop, regardless of element
   type (factory or generic path).
3. **Given** a composite widget (tablist/tree/menu), **When** rendered, **Then** the
   single tab stop is the active item (roving), not the container.
4. **Given** a disabled `ComboBox`/`ListBox` (div-based), **When** created disabled,
   **Then** it is not a tab stop.

---

### User Story 3 - Dynamic state changes reach assistive technology (Priority: P2)

When a control's state changes at runtime, the change is reflected in the semantic DOM so
screen readers announce it.

**Why this priority**: Attributes are set once at creation and never updated. US3 owns the
**PasswordBox value** and **TextBox placeholder** live-sync; the other live-sync properties
are owned by their stories (heading `aria-level` → FR-011/US4; `aria-required`, landmark,
etc. → FR-027/US7), all built on the shared FR-010 mechanism. Live apps mutate state
constantly.

**Independent Test**: For each affected property, change it in code after creation and
assert the corresponding DOM attribute updates. Fails before, passes after.

**Acceptance Scenarios**:

1. **Given** a `PasswordBox`, **When** `Password` is set in code, **Then** the DOM
   `<input type=password>` value reflects the change (masked).
2. **Given** a `TextBox`, **When** `PlaceholderText` changes, **Then** the DOM placeholder
   updates.

*(Heading `aria-level` live-sync is validated under US4; `aria-required`/landmark live-sync
under US7 — all exercise the same FR-010 mechanism.)*

---

### User Story 4 - Headings expose the correct level (Priority: P2)

A screen-reader user navigating by heading hears the correct heading level, including
WinUI levels 7–9.

**Why this priority**: Levels 7–9 are silently clamped to `<h6>` and `aria-level=6`,
collapsing distinctions WinUI/UIA preserve.

**Independent Test**: Set `HeadingLevel=Level7`; assert the emitted `aria-level` is 7
(rendered on a clamped `<h6>`, per ARIA which permits `aria-level>6`). Fails before,
passes after.

**Acceptance Scenarios**:

1. **Given** `HeadingLevel=Level7..Level9`, **When** the heading is created, **Then** the
   `<hN>` tag is clamped to `<h6>` but `aria-level` carries the true level (7–9).
2. **Given** `HeadingLevel=Level1..Level6`, **When** created, **Then** tag and `aria-level`
   match exactly.

---

### User Story 5 - Scrollable regions are meaningful landmarks; static text is reachable (Priority: P3)

Screen-reader users get a `region` landmark only for actually-scrollable, named
ScrollViewers (no rotor noise), and standalone body text is reachable per the chosen
design.

**Why this priority**: Important for rotor/landmark cleanliness and text navigation, but
lower user impact than broken interaction and tab order. Body-text exposure is a design
decision (see research §6).

**Independent Test**: Render a non-scrollable ScrollViewer and assert it produces no
`region` landmark; render a scrollable, named one and assert a labeled `role=region`.

**Acceptance Scenarios**:

1. **Given** a non-scrollable ScrollViewer, **When** rendered, **Then** it does **not**
   become a `role=region` landmark.
2. **Given** a scrollable ScrollViewer with an accessible name, **When** rendered,
   **Then** it is a `role=region` with that `aria-label` (never a descendant-text dump).
3. **Given** standalone body text (per chosen design), **When** rendered, **Then** its
   text is exposed to AT via the agreed mechanism.

---

### User Story 6 - The mappings are covered by runtime tests (Priority: P2)

Each mapping has DOM-level runtime tests that enable the AOM and assert the produced
element type, ARIA attributes, live-sync, and DOM→Uno routing.

**Why this priority**: The current suite is near-zero and almost entirely `[Ignore]`d, so
every fix here is otherwise inspection-only. Tests are required by the constitution
(Principle III) and prevent regression.

**Independent Test**: The suite itself — it must run (not `[Ignore]`d) on Skia WASM and
assert real DOM state via `document.getElementById('uno-semantics-{handle}')`.

**Acceptance Scenarios**:

1. **Given** the AOM is enabled in a test, **When** each mapped control is added, **Then**
   a test asserts the correct element tag + ARIA attributes.
2. **Given** a DOM interaction is simulated, **When** it fires, **Then** a test asserts the
   automation-peer action ran.
3. **Given** the existing `[Ignore]`d a11y tests, **When** this feature lands, **Then**
   they are re-enabled or replaced with active, asserting tests.

---

### User Story 7 - ARIA attributes are correct, complete, and consistent across paths (Priority: P1/P2)

A screen-reader user gets correct roles, names, relationships, and states for every
in-scope control — not a developer test-id read aloud as the name, not an invalid `role`
token the browser ignores, not a relationship pointing at a non-existent element, and not a
control that loses half its attributes because it took the generic creation path.

**Why this priority**: The ARIA-attribute audit (research §8) found wrong-target mappings
(`AutomationId`→`aria-label`, `LabeledBy` flattened instead of `aria-labelledby`, invalid
`FindHtmlRole` tokens), a systemic factory-vs-generic attribute gap, dangling IDREFs, and a
dozen wholly-unmapped attributes. Several of these (the wrong-target name/role defects) are
Level-A `4.1.2 Name, Role, Value` failures — as severe as the RadioButton break.

**Independent Test**: For representative controls, enable the AOM and assert the emitted
`role`, `aria-label`, `aria-labelledby`, relationship IDREFs, and state attributes against
the live DOM — including a control routed through the generic path (e.g. Image/Group) and a
control with `AutomationId` + `LabeledBy` set. Fails before, passes after.

**Acceptance Scenarios**:

1. **Given** a control with `AutomationProperties.AutomationId` set (and no Name), **When**
   the semantic element is created, **Then** the `aria-label` is **not** the AutomationId;
   the AutomationId is exposed as a DOM id/`data-*` (e.g. `xamlautomationid`) instead.
2. **Given** a control with `AutomationProperties.LabeledBy` pointing at a labelling
   element, **When** rendered, **Then** it emits `aria-labelledby` referencing that
   element's semantic id (not a flattened `aria-label` string).
3. **Given** any in-scope control, **When** its `role` is emitted, **Then** the `role` value
   is a valid WAI-ARIA token (`img` not `image`, `textbox` not `edit`, etc.) or it relies on
   a native-implicit role — never an invalid UIA token.
4. **Given** a control that takes the generic creation path (Image, Group, ProgressBar),
   **When** created, **Then** it receives the same attribute set as the factory path
   (`aria-describedby`/`controls`/`required`/`posinset`/`setsize`/`selected`/`valuenow`/
   `modal`+`role=dialog` as applicable).
5. **Given** a relationship attribute (`aria-labelledby`/`describedby`/`controls`/`flowto`/
   `activedescendant`), **When** emitted, **Then** every referenced id exists in the AOM;
   references to pruned/absent elements are not emitted, and stale references are cleared.
6. **Given** a form field with `IsDataValidForForm=false` or a Slider/ScrollBar with an
   orientation, **When** rendered, **Then** `aria-invalid` / `aria-orientation` are emitted.

### Edge Cases

- Radio group where none is checked → first radio is the tab stop; none `checked`.
- Heading with `HeadingLevel` changed repeatedly at runtime → `aria-level` tracks; tag
  re-creation is best-effort.
- `IsTabStop=false` on an interactive control → not a tab stop even though it is the right
  element type.
- ScrollViewer that becomes scrollable at runtime → region landmark is **not** re-evaluated
  live (creation-time only, per the FR-013 decision); documented as a known limitation.
- PasswordBox value set before AOM activation → reflected when AOM builds.
- Disabled-at-creation div-composite → not tabbable from first render.
- `LabeledBy`/`ControlledPeers` target is a structural element (Grid/Border) or pruned
  TextBlock with no semantic node → reference is omitted, not dangled (FR-022).
- `AutomationId` set but no `Name` → element has a DOM id but **no** `aria-label` (it must
  not borrow the id as a name) (FR-018).
- Control type absent from the role map and not in the factory switch → it must still emit a
  valid role or rely on a native-implicit one, never a bare invalid token (FR-020).
- `Custom` landmark with no Name **and** no `LocalizedLandmarkType` → must not produce a bare
  unlabeled `role="region"`; resolve a name or omit the landmark (FR-014).
- `LocalizedLandmarkType` set on a `Main`/`Navigation`/`Search`/`Form` landmark → emitted as
  `aria-roledescription` (not dropped), provided the element has an accessible name (FR-025).
- `AutomationProperties.LandmarkType` changed after the element is mounted → the DOM role
  updates (today the live-sync branch is dead) (FR-027).

## Requirements *(mandatory)*

### Functional Requirements

**RadioButton (P1)**
- **FR-001**: System MUST render `<input type=radio>` with the correct initial `checked`
  state derived from `RadioButton.IsChecked` (not gated solely on the Toggle pattern).
- **FR-002**: System MUST route a DOM activation of a radio to a peer action that selects
  it (Select / toggle-on), so the user's click is not a no-op.
- **FR-003**: System MUST update the radio's native `checked` state (not `aria-selected`)
  when `IsChecked` changes externally.
- **FR-004**: System MUST make exactly one radio per group a tab stop (`tabindex=0`),
  others `-1`, at creation and as selection changes.

**tabindex / focusability (P1)**
- **FR-005**: System MUST gate the `tabindex` of every semantic element on the control's
  real focusability (`IsAccessibilityFocusable` / `Control.IsTabStop`/`IsEnabled`/
  visibility) across **both** the factory and generic creation paths.
- **FR-006**: System MUST NOT assign `tabindex` to non-interactive elements (headings,
  `region`/landmarks, `group` containers, static text).
- **FR-007**: System MUST present composite widgets (tablist, tree, menu, grid, listbox)
  with a single tab stop using the **roving active-item** model (**decided** — container is
  `tabindex=-1`/none, the active item is the single `0`), applied identically across creation
  and the virtualized fast-path. (Decision resolves prior open item; revisit only if a
  control needs the `aria-activedescendant` model instead.)
- **FR-008**: System MUST remove the tab stop from div/table-based composites when they are
  disabled, evaluated at creation and on change.
- **FR-012**: System MUST drive roving `tabindex` from focus movement (not only
  selection/toggle), and promote exactly one composite item at creation. *(A tabindex/roving
  requirement — grouped here rather than under Heading levels; ID retained for traceability.)*

**Live state-sync (P2)** — *single ownership per attribute to avoid overlap; FR-009, FR-011,
and FR-027 together constitute the live-sync coverage:*
- **FR-009**: System MUST live-update the DOM for the **US3-owned** properties:
  **PasswordBox value** and **TextBox placeholder**. (Heading `aria-level` is owned by
  FR-011; `aria-required` and the remaining attributes by FR-027; ScrollViewer region is
  creation-time per FR-013, not a live property here.)
- **FR-010**: System MUST generalize the property→attribute update mechanism (chain the WASM
  `NotifyPropertyChangedEventCore` override to base, or a property→attribute map) so new
  properties do not silently become creation-only. This is the shared substrate FR-009/011/027
  build on.

**Heading levels (P2)**
- **FR-011**: System MUST preserve WinUI heading levels 7–9 in `aria-level` (clamping only
  the `<hN>` tag to `<h6>`), and MUST live-update `aria-level` on `HeadingLevel` change
  (heading live-sync is owned here, not by FR-027).

**ScrollViewer / body text (P3)**
- **FR-013**: System MUST emit `role=region` for a ScrollViewer ONLY when it is actually
  scrollable AND has an accessible name; otherwise it MUST NOT be a landmark. **Decided**:
  this is evaluated at element creation; live scrollability-transition re-evaluation is
  **deferred** (out of scope for this iteration — see Assumptions).
- **FR-014**: Every landmark/`region` (ScrollViewer→region, `AutomationLandmarkType`
  landmarks, named groups) MUST have a meaningful accessible name (never a concatenated
  descendant-text dump); a landmark/region MUST NOT be emitted unlabeled, and
  `aria-roledescription` MUST NOT be emitted on an element that has no accessible name
  (per ARIA, roledescription is not a substitute for a name).
- **FR-015**: **Decided** — System MUST emit a non-interactive text element (`<p>` for
  block `TextBlock`, `<span>` for inline) for a **standalone** body `TextBlock` whose text is
  **not** already absorbed by a parent control's accessible name. Inner/label `TextBlock`s
  that a parent absorbs via `ResolveLabel` remain pruned (no element), preserving the
  DOM-bloat / nested-focusable mitigation. The emitted text element carries no `tabindex` and
  no interactive role. *(Revisit if profiling shows DOM-bloat regressions; this is the
  decision most worth reconfirming with the product owner.)*

**Testing (P2)**
- **FR-016**: System MUST add runtime tests that enable the AOM and assert, per mapping:
  element tag, key ARIA attributes, live-sync, and DOM→Uno routing.
- **FR-017**: System MUST re-enable or replace the `[Ignore]`d accessibility test methods
  so coverage is active on Skia WASM.

**ARIA attribute correctness (P1 — wrong-target/role; P2 — parity/unmapped; see research §8)**
- **FR-018**: System MUST NOT use `AutomationProperties.AutomationId` as the accessible
  name. `aria-label` MUST derive only from the resolved name (`ResolveLabel`); AutomationId
  MUST be surfaced as a DOM identifier (e.g. `xamlautomationid`/`data-*`), consistently on
  both creation paths.
- **FR-019**: System MUST emit `AutomationProperties.LabeledBy` as `aria-labelledby`
  referencing the labelling element's semantic id (populate the currently-dead
  `AriaAttributes.LabelledBy`), rather than flattening it into a static `aria-label`.
- **FR-020**: System MUST emit only valid WAI-ARIA `role` tokens. The `FindHtmlRole`
  fallback MUST be normalized (`img` not `image`, `textbox` not `edit`; drop/replace
  `pane`/`window`/`custom`/`datagrid`/`spinner`/`statusbar`/`titlebar`/…), and the
  Skia generic path MUST agree with `AriaMapper` (and reconcile `ToggleSwitch`→`switch`).
- **FR-021**: The generic creation path MUST apply the same `GetAriaAttributes`-derived
  attributes as the factory path (`aria-describedby`/`controls`/`flowto`/`required`/
  `description`/`posinset`/`setsize`/`selected`/`valuenow`/`modal`+`role=dialog`), closing
  the factory-vs-generic divergence.
- **FR-022**: System MUST guarantee relationship IDREF integrity: `aria-labelledby`/
  `describedby`/`controls`/`flowto`/`activedescendant` MUST reference only semantic
  elements present in the AOM; dangling references MUST NOT be emitted, and MUST be cleared
  when the target is removed or selection is cleared.
- **FR-023**: System MUST map `aria-invalid` from `IsDataValidForForm` (inverted polarity),
  with live-sync. (Invalid *state* only; associating the error-text element via
  `aria-errormessage` is **out of scope** — see Out of Scope.)
- **FR-024**: System MUST map `aria-orientation` for `Slider`/`ScrollBar` (replacing the
  non-standard `orient`/CSS approach).
- **FR-025**: System MUST source `aria-roledescription` completely: from
  `LocalizedControlType` (currently unmapped) AND from `LocalizedLandmarkType` on **all**
  landmark types — not only `Custom` (currently `LocalizedLandmarkType` is silently dropped
  for `Main`/`Navigation`/`Search`/`Form`, a parity gap vs WinUI/UIA). Emission remains
  gated on the element having an accessible name (FR-014).
- **FR-026**: System MUST map `aria-level` from `AutomationProperties.Level` (distinct from
  `HeadingLevel`) for hierarchical items such as `TreeViewItem`.
- **FR-027**: System MUST add live-sync for the currently creation-only/dead attributes
  (`FullDescription`, `IsRequiredForForm`, `IsDialog`/`aria-modal`,
  `LiveSetting`, `AcceleratorKey`/`AccessKey`, **`LandmarkType`/`LocalizedLandmarkType`**;
  heading `aria-level` is owned by FR-011, not duplicated here) —
  including chaining the WASM `NotifyPropertyChangedEventCore` override to base or a
  generalized property→attribute map (ties to FR-010) — and MUST preserve
  `FullDescription` > `HelpText` precedence on update. NOTE: several of these attached
  properties (e.g. `LandmarkType`, `LocalizedLandmarkType`, `HelpText`) register **no
  changed-callback** and raise **no** automation event today, so their existing live-sync
  branches are unreachable dead code — wiring the changed-callback/raise is part of this work.
- **FR-028**: System MUST correct value semantics: do not inject `posinset` "N of M" text
  into `aria-label` for roles that don't support it; drive `aria-haspopup` from the C#
  value (not TS hardcoding); map `AccessKey` to the HTML `accesskey` attribute rather than
  conflating it into `aria-keyshortcuts`; and **stop forcing `aria-atomic=true`** on every
  live region — omit it (browser default) unless a specific region's WinUI semantics require
  atomic announcement.
- **FR-029**: System SHOULD map the completeness gaps that have a concrete WinUI source:
  `aria-busy` (from `ItemStatus`) and `lang` (from `Culture`). `aria-owns`/`aria-current`/
  `aria-details` have **no current source** and are explicitly out of scope unless a source
  property is introduced.
- **FR-030**: System MUST add runtime tests asserting emitted ARIA output (roles, names,
  relationships, states) against the live DOM, including a generic-path control and an
  `AutomationId`+`LabeledBy` case (extends FR-016).

**Tree-walk completeness (P2 — runtime-confirmed via the A11y Inspector; see research §9)**
- **FR-031**: The initial AOM build MUST register virtualized containers (`ItemsRepeater`/
  `ListViewBase`) **and backfill items already realized at enable time** — not only via the
  live `OnChildAdded` path (which is gated off during `CreateAOM`). Otherwise destinations
  already on screen when accessibility is enabled (e.g. `NavigationView` menu items) are never
  surfaced. Builds on FR-021/T055 (the attribute-parity facet); FR-031 is the registration-
  timing facet.
- **FR-032**: The semantic-tree walker MUST NOT expose `Visibility=Collapsed` / hidden subtrees
  to AT. (Runtime-confirmed: `TopNavOverflowButton` is `rendered? hidden` yet `in AT tree?
  exposed`, producing a phantom "More" control.)

### Key Entities

- **Semantic element creation path**: the two routes (type-specific factory vs. generic)
  that produce DOM nodes; the gap is that only the generic path honors focusability.
- **Focusability gate**: `IsAccessibilityFocusable(element, IsFocusable)` — the authority
  that must feed `tabindex` on all paths.
- **Roving model**: the rule for which composite member is the single tab stop and what
  drives its movement.
- **Property→attribute update map**: the `NotifyPropertyChangedEventCore` switch that
  decides which runtime property changes reach the DOM.
- **AutomationProperties→ARIA map**: the source-property → ARIA-attribute mapping in
  `AriaMapper`/`SemanticElementFactory`/the generic path. Its defects are categorized as
  *wrong-target* (e.g. `AutomationId`→`aria-label`), *generic-path-gap* (factory applies it,
  generic doesn't), *creation-only* (no live-sync), *unmapped*, and *dangling-IDREF*.
- **Role source**: `AriaMapper.ControlTypeToRoleMap` (valid ARIA) vs the `FindHtmlRole`
  fallback (emits invalid UIA tokens) — these must be reconciled to valid ARIA roles.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A `RadioButton` group is fully operable by keyboard/AT — correct initial
  state, selectable, announced — verified by an active runtime test.
- **SC-002**: Zero non-interactive elements (headings, region/group containers) appear in
  the keyboard tab order, verified by tests across heading/listbox/tabcontrol/menu.
- **SC-003**: For every mapping in scope, a runtime test asserts the produced element type
  and ARIA attributes against the live DOM (no `[Ignore]`).
- **SC-004**: Runtime state changes (password value, heading level, placeholder,
  `aria-required`, landmark) are reflected in the DOM within one update cycle, verified by
  tests. (ScrollViewer scrollability transitions are excluded — creation-time only per FR-013.)
- **SC-005**: Heading levels 1–9 are preserved in `aria-level`.
- **SC-006**: axe-core (or equivalent) reports no "region must have an accessible name"
  and no "interactive controls must be focusable / non-interactive must not be" violations
  for the in-scope controls.
- **SC-007**: No regression to the currently-correct mappings (Button, CheckBox, TextBox,
  composite items) — guarded by tests.
- **SC-008**: No in-scope control exposes a developer test-id as its accessible name, and
  every emitted `role` is a valid WAI-ARIA token — verified by tests across the factory and
  generic paths.
- **SC-009**: `LabeledBy` produces `aria-labelledby` and every relationship IDREF resolves
  to an element present in the AOM (zero dangling references) — verified by tests.
- **SC-010**: Controls on the generic path expose the same ARIA attribute set as equivalent
  factory-path controls (no attribute is silently dropped by path choice).
- **SC-011**: All on-screen navigation destinations in a pinned-open `NavigationView` are
  reachable to AT when accessibility is enabled (no items dropped because they were realized
  in a repeater before enable), and no `Collapsed`/hidden control (e.g. an inactive "More"
  button) is exposed — verified by the A11y Inspector / a runtime test.

## Assumptions

- The existing 001/002 infrastructure (semantic DOM, `AriaMapper`, `SemanticElementFactory`,
  `FocusSynchronizer`, roving) remains the foundation; this feature corrects and extends it.
- Runtime tests can enable the AOM in-test via the existing `EnableAccessibilityThroughDom`
  helper used by the 2 active TextBox tests.
- WinUI C++ sources may be consulted for radio/heading/region semantics (Constitution VII).
- NVDA (Windows) and VoiceOver (macOS) remain the manual-verification screen readers.
- **Backward compatibility (Constitution VI)**: several fixes change observable a11y output
  (AutomationId no longer in `aria-label`, normalized `role` tokens, headings/containers
  leaving the tab order, dropped `aria-atomic`). These are bug-fixes, not public-API breaks
  (no `fix!`/`feat!`), but MUST be captured in release notes / changelog so downstream apps
  that scraped the old output are forewarned.
- **Deferred (this iteration)**: live re-evaluation of ScrollViewer scrollability (FR-013 is
  creation-time); `aria-owns`/`aria-current`/`aria-details` (no source); native WASM-DOM
  `tabindex`-noise cleanup (optional).

## Out of Scope

- Native WASM-DOM target behavior changes (maintenance-only). Optional: removing the
  blanket `tabindex="-1"` over-application and the dead `hasOwnProperty` branch — tracked
  but not required. Note: the `FindHtmlRole` role-token normalization (FR-020) lives in
  shared C# (`AutomationProperties.uno.cs`) and so also corrects the native path's invalid
  roles incidentally — that is acceptable (a bug fix), but no other native-target change is
  in scope and the native path must keep working.
- Native mobile accessibility (TalkBack / iOS VoiceOver).
- New control patterns not already mapped in 001/002.
- The debounce-queue activation (dead code) — note it; only address if it blocks a fix.
- **`aria-errormessage`** (associating an invalid field with its error-text element). Uno conveys
  invalid *state* via `aria-invalid` (FR-023) but not the error *message* association, because
  WinUI exposes no source automation property for it; emitting it would require a Uno-specific
  attached property (an `ErrorMessage` IDREF mirroring `LabeledBy`→`aria-labelledby`, gated on
  `aria-invalid`). Deferred — the validation sample demonstrates `aria-required`/`aria-invalid`
  only. (No `aria-errormessage` path exists in the codebase as of this writing.)
