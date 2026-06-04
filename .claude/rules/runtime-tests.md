---
description: Conventions for writing Uno runtime tests (real visual tree, runs on Skia/WASM/native/WinUI). Auto-loaded in Uno.UI.RuntimeTests. Build/run via the /runtime-tests skill.
paths:
  - "src/Uno.UI.RuntimeTests/**/*.cs"
---

# Runtime tests (Uno.UI.RuntimeTests)

Build & run with the **`/runtime-tests`** skill. WinUI parity: `/winui-runtime-tests`.

- Test class needs **both** `[TestClass]` and `[RunsOnUIThread]`. Test methods are **`async Task`** (never `void`/sync) — `WaitForLoaded`/`WaitForIdle`/`ScreenShot` return `Task` and must be awaited, or the test races to a false pass.
- **Folder mirrors the namespace with underscores**: a `Button` test goes in `Tests/Windows_UI_Xaml_Controls/Given_Button.cs`, namespace `Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls`. Class `Given_<Control>`, method `When_<Scenario>`.
- **Add to the tree** with `await UITestHelper.Load(element)` (sets content, waits for load, returns bounds). Plain `WindowHelper.WindowContent = el` does **not** wait — follow with `await WindowHelper.WaitForLoaded(el)`.
- **Always reset shared state in `finally`**: `WindowHelper.WindowContent = null`, `popup.IsOpen = false`, or `VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot)`. `[TestCleanup]` may be skipped on exceptions — use try-finally for anything critical.
- **Skip per platform with attributes, not `#if` on the method.** Wrapping `[TestMethod]` in `#if` compiles but breaks test discovery. Use:
  - `[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]` (Exclude = "skip on these"; Include = "only these"). Flags: `SkiaWin32/X11/Wpf/MacOS/Android/IOS/Wasm`, `NativeWinUI/Wasm/Android/IOS`.
  - `[Ignore("reason")]` for a hard skip; `[GitHubWorkItem(url)]` documents an issue **without** skipping.
  - `#if` is fine *inside* a method body (e.g. type aliasing).
- **Parameterize** with `[DataRow(...)]` (multiple per method) or `[CombinatorialData]` with `bool`/enum params — not hand-rolled loops.
- **Screenshots**: `var bmp = await UITestHelper.ScreenShot(el);` then `ImageAssert.HasColorAt(bmp, x, y, color, tolerance)` or `await ImageAssert.AreEqualAsync(a, b)`. A raw `RawBitmap` needs `await bmp.Populate()` before `GetPixel`. Use a small `tolerance` (1–5) for hardware rasterization variance. `[RequiresFullWindow]` for tests needing the real window size.
- Don't reference Uno internals (`DirectUI`, `Uno.UI.Xaml.Input`) unguarded — gate with `#if HAS_UNO`. MSTest usings come from `GlobalUsings.cs`; don't re-import.

Known flaky tests are tracked in GitHub issue #9080.
