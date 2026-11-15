# Porting WinUI interaction tests

WinUI's interaction tests (`controls/dev/*/InteractionTests`) drive a separate test app through UI Automation, using MITA (`Microsoft.Windows.Apps.Test`) and `MUXTestInfra`. This folder lets them run, nearly unchanged, as Uno runtime tests inside the app process.

| Folder | Namespaces | What it is |
|---|---|---|
| `Foundation/` | `Microsoft.Windows.Apps.Test.*` | Uno implementation of the MITA API: `UIObject` wraps an `AutomationPeer`, input goes through `InputInjector` |
| `Infra/` | `Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common` / `.Infra` | `MUXTestInfra`, ported from WinUI. Only `Wait`, `TestEnvironment` and `TestSetupHelper` are adapted |

## Porting a test

```bash
dotnet run build/winui-port/port-interaction-test.cs -- \
  <winui>/controls/dev/Foo/InteractionTests/FooTests.cs \
  src/Uno.UI.RuntimeTests/Microsoft/UI/Xaml/Controls/Foo/FooTests_InteractionTests.mux.cs \
  "controls/dev/Foo/InteractionTests/FooTests.cs, commit <sha>"
```

The script makes only mechanical edits:

- Calls that wait for the UI (`Wait.*`, `KeyboardHelper.*`, `InputHelper.*`, `FocusHelper.*`, `waiter.Wait()`, the `*AndWait` helpers) get `await`. Their callers become `async Task`, all the way up the call chain.
- `using (var setup = new TestSetupHelper("Foo Tests"))` becomes `await using (var setup = await TestSetupHelper.Create("Foo Tests"))`.
- The TAEF `WEX.*` usings are removed (`Common.Log`/`Common.Verify` and MSTest replace them), `[RunsOnUIThread]` is added, and indentation becomes tabs.

The script prints a warning for anything it can't rewrite mechanically, such as an override or a property that now awaits.

Queries stay synchronous (`FindElement.*`, `UIObject` properties, pattern calls such as `Invoke()` or `Toggle()`), so most lines are unchanged.

## Test pages

`TestSetupHelper.Create("Foo Tests")` looks for a type carrying `[TopLevelTestPage(Name = "Foo")]` (or `[AxeScanTestPage(Name = ...)]`) in SamplesApp. Port the page from `controls/dev/Foo/TestUI` and keep its attributes. Upstream tests find elements by `AutomationProperties.Name` and `AutomationId`, so keep those values.

## Differences from upstream

- Element lookups don't poll: a missing element fails right away, after a layout pass. Add `await Wait.ForIdle()` before looking up content that appears asynchronously.
- Waiters poll the automation surface instead of listening for UIA events.
- `BoundingRectangle` and pointer coordinates are window-relative DIPs.
- Axe scans are reported as inconclusive. Multi-touch gestures throw `NotSupportedException`.
