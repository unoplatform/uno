# Quickstart: WASM Accessibility Remediation

How to build the target, enable the semantic DOM in a test, inspect it, and run the suite.

## Build (Skia WASM)

```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props   # once
# set <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride> and UnoFastDevBuild=true
dotnet restore Uno.UI-Wasm-only.slnf
dotnet build Uno.UI-Wasm-only.slnf --no-restore     # NEVER CANCEL — 15+ min timeout
```

TypeScript in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/*` compiles to an
embedded JS resource as part of that project's build — edit the `.ts`, rebuild the project.

## The one thing to understand first: the AOM is inert until activated

The semantic DOM is **not built** until the sr-only "Enable accessibility" button is
activated (no screen-reader auto-detection; `AutoEnableAccessibility` defaults false,
[FeatureConfiguration.cs:80](../../src/Uno.UI/FeatureConfiguration.cs)). So any test or
manual check must enable it first, or you will see an empty `#uno-semantics-root` and
conclude (wrongly) that nothing works.

## Writing a DOM-level runtime test (the pattern this feature standardizes)

The 2 currently-active TextBox tests are the template
([Given_AccessibleTextBox.cs](../../src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleTextBox.cs),
the `When_SemanticTextBox_*` methods). Pattern:

1. `[TestMethod]` with the Skia-WASM platform condition; add the element to
   `WindowHelper.WindowContent`; `await WindowHelper.WaitForLoaded(element)`.
2. Enable the AOM in-test via the existing `EnableAccessibilityThroughDom` helper.
3. Locate the semantic node: `document.getElementById('uno-semantics-{handle}')` via JS
   interop (handle = the element's `ContainerVisual` handle).
4. Assert the **real DOM**: `tagName`, `getAttribute('aria-*')`, `tabIndex`, `.checked`,
   `.value`, etc. — not just `AriaMapper` return values in isolation.
5. For DOM→Uno routing: dispatch the DOM event (`click`/`change`/`input`) and assert the
   peer/control state changed.

> Do **not** write a test that only calls `AriaMapper.GetSemanticElementType(...)` and
> asserts the enum — that is the shallow pattern that let these defects ship. Assert the
> produced DOM.

## Run the tests

Use the `/runtime-tests` skill (handles build, base64 filter encoding, execution, parsing)
targeting Skia Desktop by default; for the WASM-specific DOM assertions run the WASM target.
Example focus while iterating on Phase A:

```
# via the /runtime-tests skill, filter to the radio/checkbox class
Given_AccessibleCheckBox
```

Re-enable `[Ignore]`d methods as you add real assertions (FR-017). Each fix must
fail-before / pass-after (Constitution III).

## Manual verification (per AGENTS.md validation checklist)

- Run SamplesApp on WASM; activate accessibility (Tab to the first sr-only control).
- NVDA (Windows) / VoiceOver (macOS Safari): confirm radio selection, heading navigation
  (rotor/H — headings must **not** be Tab stops), region landmarks only where meaningful.
- axe-core scan for SC-006: no "region must have an accessible name", no interactive/
  non-interactive focusability violations.

## Evidence discipline (per debugging-discipline rule)

Label findings explicitly: **Code review** (inspection) vs **Compile** (which project
built) vs **Runtime** (which test/app ran). The audits in [research.md](./research.md) are
**code-review** level; this feature's job is to convert each fix to **runtime**-validated
via the tests above.

## Key files (quick map)

- Decision: `src/Uno.UI/Accessibility/AriaMapper.cs`
- Focusability gate: `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs`
- Orchestration + live-sync switch: `…/WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs`
- Factory: `…/WebAssembly.Browser/Accessibility/SemanticElementFactory.cs`
- Focus + roving: `…/WebAssembly.Browser/Accessibility/FocusSynchronizer.cs`
- DOM creation: `…/WebAssembly.Browser/ts/Runtime/SemanticElements.ts`
- Generic path + focusability honoring: `…/WebAssembly.Browser/ts/Runtime/Accessibility.ts`
