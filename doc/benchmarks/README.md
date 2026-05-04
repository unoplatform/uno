# Uno Platform benchmarks

Benchmarks for Skia targets, hosted inside `Uno.UI.RuntimeTests`. Run as part of
CI on every push to `master` and nightly via a scheduled trigger; results are
published to the trend dashboard at <https://unoplatform.github.io/uno.benchmarks/>.

This page is the index. Pick the doc that matches what you want to do:

| You want to… | Read |
|--------------|------|
| Add a new benchmark | [adding-benchmarks.md](./adding-benchmarks.md) |
| Run benchmarks locally / compare two branches A/B | [local-workflow.md](./local-workflow.md) |
| Provision the data repo + PAT (admin task, once) | [provisioning.md](./provisioning.md) |
| Bring up a gated platform (Wasm/iOS/CoreCLR/NativeAOT) | [provisioning.md#bringing-up-a-new-target](./provisioning.md#bringing-up-a-new-target) |
| Understand a regression on the dashboard | [the dashboard repo's README](https://github.com/unoplatform/uno.benchmarks#how-to-read-a-regression) |

## What's here

The suite lives at `src/Uno.UI.RuntimeTests/Tests/Benchmarks/`. It uses
[BenchmarkDotNet](https://benchmarkdotnet.org) 0.14 with the
`InProcessNoEmitToolchain` so it works on every Skia target including AOT
platforms (iOS, Android NativeAOT, Wasm AOT) without `Reflection.Emit`.

Three categories:

- **Micro** (Category A) — hot-loop API surfaces (DP get/set, layout fast paths,
  brush attach/detach). Stable, deterministic, ns-scale.
- **Scenario** (Category B) — bounded end-to-end framework operations (theme
  resource lookup, ListView measure, page first-load).
- **Render** (Category C) — driving compositor frames through
  `CompositionTarget.Rendering`. Skia-specific. Gated off Linux Framebuffer (no
  GPU) and off Wasm/Android/iOS in v1 (separate noise-budget per target).

## CI shape (v1)

| Target | Status | Notes |
|--------|--------|-------|
| Skia Windows | ✅ live | All categories |
| Skia Linux X11 | ✅ live | All categories |
| Skia Linux Framebuffer | ✅ live | A+B only (no GPU presenter) |
| Skia macOS | ✅ live | All categories |
| Skia Android (Mono) | ✅ live | A+B (Render gated off mobile in v1) |
| WinAppSDK | ✅ live | Native WinUI parity baseline |
| Skia Wasm | 🔒 gated | Enable in follow-up PR after BDN 0.14 verified |
| Skia iOS | 🔒 gated | Enable after AOT linker preserve verified |
| Skia Android (CoreCLR) | 🔒 gated | Enable after BDN+CoreCLR-on-Android verified |
| Skia Android (NativeAOT) | 🔒 gated | Enable after NativeAOT trim preserve verified |

Trigger: `master` + nightly schedule. Never on PRs. Always-green collector — no
threshold-based pass/fail in v1. Results land on the dashboard's data branch via
a downstream publish job.

## Component map

```
src/
  Uno.UI.RuntimeTests/Tests/Benchmarks/
    BenchmarkRunner_Fixture.cs          # MSTest fixture, auto-discovery
    BenchmarkRunnerHost.cs              # in-process runner, soft timeouts, JSONL emission
    CoreConfig.cs                       # per-category BDN Job
    BenchmarkCategoryAttribute.cs       # [BenchmarkCategory(Micro|Scenario|Render)]
    RenderBenchmarkHost.cs              # scene setup + RenderFrameAsync helper
    Suites/{Micro,Scenario,Render}/...  # 11 benchmark classes, ~15 methods total
  Uno.UI.Benchmarks.Publisher/          # dotnet console: normalize / merge / compare
  SamplesApp/UITests.Shared/Windows_UI_Xaml/Performance/BenchmarksPage.xaml{,.cs}
                                        # in-app runner, save zip, copy as markdown
build/ci/tests/.azure-devops-benchmarks-*.yml
                                        # 11 per-target stages + publish stage
build/ci/tests/.azure-devops-tests-skia-benchmarks-stages.yml
                                        # top-level fan-out
```

External (not in this repo):

- `unoplatform/uno.benchmarks` — dashboard + data branch. Created and provisioned
  separately. See [provisioning.md](./provisioning.md).
