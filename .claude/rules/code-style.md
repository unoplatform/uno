---
description: Non-obvious C# style/idiom conventions for Uno.UI / Uno.UWP / Uno.Foundation source. Auto-loaded when editing src C# files.
paths:
  - "src/**/*.cs"
---

# C# code style (Uno)

Indentation is **tabs** (see `.editorconfig`), braces always, `new_line_before_open_brace = all` (Allman). These are enforced by analyzers on CI — `UnoFastDevBuild=true` skips them locally but they still gate the PR.

## File headers
- **Ported WinUI/MUX code** carries, in this order:
  ```csharp
  // Copyright (c) Microsoft Corporation. All rights reserved.
  // Licensed under the MIT License.

  // MUX Reference AnimatedIcon.properties.cpp, tag winui3/release/1.7.1, commit f4d781d
  ```
  Keep the `// MUX Reference <file>, tag <tag>, commit <hash>` line accurate — it pins the upstream source. **Uno-native** code omits both the MS copyright and the MUX line.
- Don't add a `_Mux` suffix to ported members — use the natural WinUI name. (See `.github/agents/winui-porting-agent.md`.)

## Idioms
- **`#nullable enable`** is per-file (top of file), not global. New/refactored files should enable it; large legacy files (e.g. `FrameworkElement.cs`) often don't — don't bulk-flip them.
- **File-scoped namespaces** (`namespace X;`) for new files; don't force-convert existing braced-namespace partials unless rewriting the whole file.
- **Extension methods are `internal`** and live in `[TypeName]Extensions.cs` in the same namespace. Public cross-assembly extensions are the rare exception.
- **Logging**: guard with `this.Log().IsEnabled(LogLevel.X)` before building the message, then `this.Log().Debug(...)`/`.Error(...)`. `LogLevel` is `Uno.Foundation.Logging.LogLevel`.
- **Unimplemented members** are marked `[Uno.NotImplemented]` — never `throw new NotImplementedException()`. Generated stubs in `Generated/` folders already use the attribute; never edit those files.
- Suppress analyzer warnings narrowly with `#pragma warning disable <CODE>` / `restore <CODE>` (e.g. `CS0618` obsolete, `CA1422` platform-compat), not project-wide.

## Conditional symbols
`#if IS_UNIT_TESTS` and `#if !UNO_REFERENCE_API` gate code per build flavor; platform symbols are covered in `platform-targeting.md`. Global usings (`GlobalUsings.cs`) only define **Android** aliases (e.g. `AView`) and only under `__ANDROID__` — don't assume other platforms have global aliases.
