# Finding: aria-label emitted on a name-prohibited role (role=generic etc.)

**Date**: 2026-06-08
**Branch**: `003-wasm-a11y-remediation`
**How found**: Chrome Lighthouse accessibility run on the A11yInspector sample — "aria-label and
aria-labelledby attributes are prohibited on presentation and none roles, as well as text-like roles
such as code, insertion, strong, etc." Confirmed in the live semantic DOM via the A11y Inspector.

> **Evidence level**: runtime-observed (live DOM over CDP) + Lighthouse.

## Symptom (runtime-observed)

The NavigationView pane emits:

```html
<div role="generic" aria-label="Sample sections"> … </div>
```

`role="generic"` is in ARIA's **name-prohibited** set, so `aria-label` is invalid there — browsers and
axe/Lighthouse ignore or flag it, and the name does **not** reach assistive technology. The name comes
from `AutomationProperties.Name="Sample sections"` on the `NavigationView` (its pane container peer maps
to `AutomationControlType.Custom → role=generic`, but the resolved name is still applied as `aria-label`).

## Root cause

`AriaMapper` applies the resolved accessible name as `aria-label` regardless of whether the element's
role permits naming. When a *named* element maps to a name-prohibited role (notably
`Custom → "generic"`, but also the text-level roles), the emitted `aria-label` is invalid.

ARIA name-prohibited roles (ARIA 1.2 "name from author" prohibited): `presentation`/`none`, `generic`,
`paragraph`, `caption`, `code`, `deletion`, `emphasis`, `insertion`, `strong`, `subscript`,
`superscript`, `term`, `time`.

## Fix options (Uno)

1. **Suppress** — don't emit `aria-label`/`aria-labelledby` when the role is name-prohibited (drop the
   attribute in the setter / `AriaMapper`). Simplest; the name is silently invalid anyway.
2. **Promote the role** — when a name-prohibited element carries an authored Name, map it to a nameable
   role instead of `generic` (e.g. `group`/`region`), so the name is valid. Best for containers like the
   NavigationView pane that legitimately have a name.

Either way, also confirm the NavigationView pane's intended role: a named pane should be a nameable
container, not `generic`.

## Cross-reference

A11yInspector now flags this class itself: `GapDetector` emits `FindingKind.NameOnProhibitedRole`
(Warning) when an emitted node has an `aria-label` on a name-prohibited role — so the inspector surfaces
exactly what Lighthouse caught. Related to [[findings-navigationview-overflow]] (same NavigationView).
