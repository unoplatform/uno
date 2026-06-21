---
description: MSBuild/cross-targeting internals for the Uno solution (TFMs, output paths, package versions, runtime asset selection). Auto-loaded when editing project/props/targets files.
paths:
  - "src/**/*.csproj"
  - "src/**/*.props"
  - "src/**/*.targets"
---

# Build system internals (Uno)

Day-to-day build setup (slnf filters, `UnoTargetFrameworkOverride`, `UnoFastDevBuild`) is in AGENTS.md. This rule is the **MSBuild-internals** layer for when you edit project files.

- **Never edit or commit `Generated/` folders** — auto-generated WinUI stubs. Don't remove `[Uno.NotImplemented]` stubs unless removing the whole feature; when you implement an API, move it out of `Generated/` per AGENTS.md "Implementing New WinUI Features".
- **Never commit `src/crosstargeting_override.props`** (gitignored, per-developer). Close VS before changing `UnoTargetFrameworkOverride` — changing it with the solution open causes NuGet restore instability / VS hangs; delete `src/.vs/` if caching gets stuck.
- **TFMs come from `Directory.Build.props`** properties (`NetCurrent`=net10.0, `NetPrevious`=net9.0, and the `Net*PreviousAndCurrent` families). Reference those properties — don't hardcode `net10.0`/`net9.0` in a `.csproj`.
- **Platform symbols & suffix exclusion are owned by `Uno.CrossTargetting.targets`** — never set platform `DefineConstants` or `Compile Remove="**/*.skia.cs"` yourself (see `platform-targeting.md`).
- **Central Package Management is NOT enabled** in the main solution. Versions are pinned via `PackageReference … Update` in `src/Directory.Build.targets`. Don't add a `Directory.Packages.props` under `src/` — it would break the override-based versioning.
- **Output paths** for the ~43 multi-variant projects are set centrally via `_AdjustedOutputProjects` in `Directory.Build.props` (`BaseOutputPath=bin\$(MSBuildProjectName)` so `.Skia`/`.Wasm`/`.Reference`/`.Tests` coexist). Don't set `BaseOutputPath` per-project; a new multi-TFM project with a novel suffix must be **added to `_AdjustedOutputProjects`** or it hits bin/obj collisions.
- **`RuntimeAssetsSelectorTask`** swaps platform TFMs to the generic `netX.0` for non-WinRT assemblies that reference `Uno.UI` on Skia+mobile — don't override it manually.
- `ProjectReference Private=false` is set globally (`Directory.Build.targets`); override only with `DisablePrivateProjectReference=true` where copying is genuinely needed.
- Analyzer/code-style config is centralized (`Directory.Build.props`, `AnalysisLevel=9`); don't add StyleCop or per-project analyzer packages.
