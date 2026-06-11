---
description: Conventions for writing Uno runtime tests (real visual tree, runs on Skia/WASM/native/WinUI). Auto-loaded in Uno.UI.RuntimeTests. Build/run via the /runtime-tests skill.
paths:
  - "src/Uno.UI.RuntimeTests/**/*.cs"
---

# Runtime tests (Uno.UI.RuntimeTests)

Build & run with the **`/runtime-tests`** skill. WinUI parity: `/winui-runtime-tests`.

- Test class needs `[TestClass]`; add `[RunsOnUIThread]` to run on the UI thread — at the **class level** (applies to every method) or on individual **`[TestMethod]`s** (the more common pattern). A test that `await`s anything — `WaitForLoaded`/`WaitForIdle`/`ScreenShot`/`UITestHelper.Load` all return `Task` — **must** be `async Task` so the awaits are observed; otherwise it races to a false pass. A purely synchronous test (no `await` in its body — e.g. it only constructs objects and `Assert`s) should stay plain **`void`**; don't add `async Task` you never await.
- **Folder mirrors the namespace with underscores**: a `Button` test goes in `Tests/Windows_UI_Xaml_Controls/Given_Button.cs`, namespace `Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls`. Class `Given_<Control>`, method `When_<Scenario>`.
- **Add to the tree** with `await UITestHelper.Load(element)` (sets content, waits for load, returns bounds). Plain `WindowHelper.WindowContent = el` does **not** wait — follow with `await WindowHelper.WaitForLoaded(el)`.
- **`WaitForLoaded` requires the target to settle to a non-zero `ActualWidth`/`ActualHeight`.** Its default is-loaded check polls for non-zero size (and also fails on an empty templated `Control` or an unpopulated `ListView`), so it **times out on a `Collapsed`, empty, or zero-size element** — and `UITestHelper.Load` inherits this since it calls `WaitForLoaded`. For those cases pass a custom predicate, e.g. `await UITestHelper.Load(el, x => x.IsLoaded)` or `x => x.GetTemplateRoot() != null`.
- **Always reset shared state in `finally`**: `WindowHelper.WindowContent = null`, `popup.IsOpen = false`, or `VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot)`. `[TestCleanup]` may be skipped on exceptions — use try-finally for anything critical.
- **Skip per platform with attributes, not `#if` on the method.** Wrapping `[TestMethod]` in `#if` compiles but breaks test discovery. Use:
  - `[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]` (Exclude = "skip on these"; Include = "only these"). Flags: `SkiaWin32/X11/Wpf/MacOS/Android/IOS/Wasm`, `NativeWinUI/Wasm/Android/IOS`.
  - `[Ignore("reason")]` for a hard skip; `[GitHubWorkItem(url)]` documents an issue **without** skipping.
  - `#if` is fine *inside* a method body (e.g. type aliasing).
- **Link issue-covering tests to their issue**: when a test reproduces or guards a specific GitHub issue, annotate it (method- or class-level) with `[GitHubWorkItem("https://github.com/unoplatform/uno/issues/<n>")]`. It's traceability metadata only — it does **not** skip the test (unlike `[Ignore]`).
- **Parameterize** with `[DataRow(...)]` (multiple per method) or `[CombinatorialData]` with `bool`/enum params — not hand-rolled loops.
- **Screenshots**: `var bmp = await UITestHelper.ScreenShot(el);` then `ImageAssert.HasColorAt(bmp, x, y, color, tolerance)` or `await ImageAssert.AreEqualAsync(a, b)`. A raw `RawBitmap` needs `await bmp.Populate()` before `GetPixel`. Use a small `tolerance` (1–5) for hardware rasterization variance. `[RequiresFullWindow]` for tests needing the real window size.
- Don't reference Uno internals (`DirectUI`, `Uno.UI.Xaml.Input`) unguarded — gate with `#if HAS_UNO`. MSTest usings come from `GlobalUsings.cs`; don't re-import.

Known flaky tests are tracked in GitHub issue #9080.
