---
description: Conventions for Uno.UI unit tests (pure logic, no visual tree). Auto-loaded in Uno.UI.Tests. Distinct from runtime tests.
paths:
  - "src/Uno.UI.UnitTests/**/*.cs"
---

# Unit tests (Uno.UI.UnitTests)

Pure-logic tests with **no visual tree** — the opposite of runtime tests. Run: `dotnet test src/Uno.UI.UnitTests/Uno.UI.UnitTests.csproj`.

- **MSTest** (`[TestClass]`/`[TestMethod]`), not NUnit/xUnit. Class `Given_<Feature>`, method `When_<Scenario>`.
- **Link issue-covering tests to their issue** with `[GitHubWorkItem("https://github.com/unoplatform/uno/issues/<n>")]` (method- or class-level) when a test reproduces or guards a specific GitHub issue — MSTest metadata for traceability, not a skip.
- **Do not load XAML or build a visual tree** here. Test `DependencyObject`/binder/converter/foundation logic directly. (Visual/layout behavior belongs in `Uno.UI.RuntimeTests`.)
- **Test doubles are inline** in the test file as nested or same-file `partial` classes (e.g. `SimpleDependencyObject1`, `MyBindingSource : INotifyPropertyChanged`, inline `IValueConverter`s). Don't add them to production folders.
- **No Moq on UI types.** `Moq` exists but is used only for non-UI abstractions (e.g. service locators). Never mock `FrameworkElement`/`DependencyObject`.
- **Unique DP names per test**: when calling `DependencyProperty.Register` in a test, use `nameof(When_TheTestMethod)` as the property name — DP registration is global and collides across parallel runs otherwise.
- Assertions: `Assert.*` is the norm; `AwesomeAssertions` (`.Should()`) is available for complex cases — mixed styles are fine.
- Setup/teardown: `[TestInitialize]`/`[TestCleanup]` per method; global one-time setup (culture, logging) is `[AssemblyInitialize]` in `Global.cs`. Restore any mutated global state in `[TestCleanup]`.
