# Pre-remediation accessibility baseline

This note records the historical Skia WebAssembly behavior that the remediation tests
must prevent from returning. It describes the state before the fixes in this feature.

## Reproduction

1. Publish and serve the Skia WebAssembly SamplesApp.
2. Open a sample containing RadioButtons, a heading, a control with an
   `AutomationId`, and a control whose name comes from `AutomationProperties.LabeledBy`.
3. Enable accessibility through `#uno-enable-accessibility`.
4. Inspect the corresponding nodes under `#uno-semantics-root`.

## Observed failures

| Scenario | Historical output | Required output |
|---|---|---|
| Initially selected RadioButton | The native radio remained unchecked because creation only read the Toggle pattern, which RadioButton does not expose. | Native `checked` reflects `RadioButton.IsChecked`, and one radio in the group has `tabindex="0"`. |
| Heading | The heading received `tabindex="0"` through the generic focusability path. | The semantic heading has no tab stop. |
| `AutomationId` | The generic path copied the test identifier into `aria-label`, so assistive technology announced values such as `SubmitButton_42`. | The identifier is exposed as `xamlautomationid`; the accessible name comes from authored naming content. |
| `LabeledBy` | The relationship was flattened into a static `aria-label`; no `aria-labelledby` ID reference was emitted. | `aria-labelledby` references the labeller's semantic node and remains live without dangling IDs. |

These scenarios are covered by the active regression tests in this feature.
