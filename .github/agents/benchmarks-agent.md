# Benchmarks Agent

Reference for AI agents working on Uno Platform benchmarks: the in-process
harness, the per-category BDN configs, the publisher tool, the dashboard, and
the CI pipeline gating.

## Architecture at a glance

- **Harness location:** `src/Uno.UI.RuntimeTests/Tests/Benchmarks/`. The
  benchmark fixtures live alongside regular runtime tests but are gated by
  `[TestCategory("Benchmark")]` so they only run when
  `UITEST_RUNTIME_BENCHMARKS_ONLY=true`.
- **BDN version:** 0.14.0, pinned in `src/Directory.Build.targets` via
  `$(BenchmarkDotNetVersion)`. Toolchain: `InProcessNoEmitToolchain.DontLogOutput`
  (works on every Skia target including iOS / Android NativeAOT / Wasm AOT
  because no `Reflection.Emit` is required).
- **Discovery:** `BenchmarkRunner_Fixture` uses MSTest `[DynamicData]` to
  reflect over the assembly and produce one MSTest row per `[Benchmark]`-bearing
  type under `Suites/`. Adding a new benchmark requires no fixture-file edit.
- **Categories:** `Micro` / `Scenario` / `Render`, declared via
  `[BenchmarkCategory(...)]`. Each gets a different BDN `Job` config from
  `CoreConfig.For()`.
- **Soft per-benchmark timeout:** 90s/120s/180s (Micro/Scenario/Render). 25-min
  suite-level cap. Implemented via `Task.WhenAny` + `CancellationTokenSource`
  in `BenchmarkRunnerHost.RunOneCoreAsync`.
- **Result emission:** `BenchmarkRunnerHost` writes `results.jsonl` matching
  the v1 schema (see `doc/benchmarks/Schema.md` mirror in `unoplatform/uno.benchmarks`)
  plus a zip of BDN's full artifact directory. Both consumed by the publisher
  and by SamplesApp's `BenchmarksPage`.
- **Publisher:** `src/Uno.UI.Benchmarks.Publisher` — three subcommands:
  `normalize` (per-platform stamping in CI), `merge --push` (downstream merge
  + git push to the data branch), `compare` (local A/B CLI).
- **Dashboard:** `unoplatform/uno.benchmarks` (separate repo). Vanilla HTML +
  ESM + uPlot. No build step. Reads JSONL from the data branch via raw
  GitHub URLs.

## Adding a benchmark

See `doc/benchmarks/adding-benchmarks.md` for the full guide. The short version:

1. Pick a category folder under
   `src/Uno.UI.RuntimeTests/Tests/Benchmarks/Suites/{Micro,Scenario,Render}/`.
2. Create a public class with `[BenchmarkCategory(BenchmarkCategory.X)]` and
   `[MemoryDiagnoser]`.
3. Add at least one `[Benchmark]` method. Allocate state in `[GlobalSetup]`,
   not in the benchmark body.
4. For Scenario/Render, dispatcher work goes through
   `TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass`.
5. For Render, use `RenderBenchmarkHost.SetupSceneAsync` /
   `RenderFrameAsync` / `ResetSceneAsync`. Drive 60 frames per iteration.
6. Verify locally: `dotnet test Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Skia.csproj
   --filter "FullyQualifiedName~YourBenchmarkClass"`.

No fixture-file edit, no yaml edit, no doc edit needed. Auto-discovery picks it
up.

## Common pitfalls

- **Doing setup inside `[Benchmark]`** — the setup time is averaged into the
  measurement. Always use `[GlobalSetup]`.
- **Returning `void` when the JIT could elide the work** — return a value
  the caller would meaningfully consume so the JIT can't dead-code it.
- **Leaking state across iterations** — Render benchmarks especially: any
  scene state that grows unboundedly will compound through warmup +
  iterations. Use `[GlobalCleanup]` + `RenderBenchmarkHost.ResetSceneAsync`.
- **Using `Reflection.Emit`-dependent toolchains** — never. Stick to
  `InProcessNoEmitToolchain.DontLogOutput`. The whole reason the suite runs
  on every Skia target is that we don't need JIT codegen at runtime.
- **Forgetting `await` on async benchmarks** — BDN measures the time-to-task-
  creation rather than actual work. Always `async Task` benchmark methods,
  always `await`.
- **`[GlobalSetup]` returning `Task` but not actually awaited internally** —
  if Setup needs dispatcher work, use `Task` return type and return the
  dispatcher's task directly so BDN waits for it.
- **`MemoryDiagnoser` interpretation on Render** — Skia's pool churn dominates
  the alloc number, not Uno's logic. Read it as a *trend* per platform, not as
  an absolute "Uno allocates X bytes per frame."
- **Adding a benchmark whose runtime exceeds the per-category timeout** —
  90s/120s/180s caps for Micro/Scenario/Render. If your benchmark legitimately
  needs more, lower the iteration count in `CoreConfig` for that category
  rather than the timeout. (Don't bump the timeout silently.)

## Bringing up a gated target (Skia Wasm / iOS / Android CoreCLR / NativeAOT)

See `doc/benchmarks/provisioning.md#bringing-up-a-new-target`. One PR per
target. The recipe per target:

1. Verify locally that BDN 0.14 InProcessNoEmit runs and produces results.
2. Fix any platform-specific preserve / linker issues. iOS and Android NativeAOT
   are most likely to need `[DynamicallyAccessedMembers(All)]` on
   `BenchmarkRunnerHost` or a linker preserve XML entry.
3. Replace the `condition: false` placeholder in
   `build/ci/tests/.azure-devops-benchmarks-<target>-skia.yml` with a real
   stage body modeled on a live target.
4. Add the target's artifact to the publish job's
   `DownloadPipelineArtifact@2` list in `.azure-devops-benchmarks-publish.yml`.
5. Open a PR scoped to the single target.

The dashboard auto-discovers new platforms from `index.json` — no dashboard
change needed.

## Modifying the result schema

The schema is `schemaVersion: 1` and the dashboard's data layer rejects
incompatible records. Rules of thumb:

- **Adding optional fields to `extra`:** no schema bump needed. Anyone who
  reads them must tolerate `undefined`.
- **Adding a top-level field that the dashboard needs to use:** bump
  `schemaVersion`. Update Schema.md (in `unoplatform/uno.benchmarks`),
  update `BenchmarkRunnerHost.SchemaVersion`, update the publisher's
  `Schema.cs`, and update the dashboard's `data.js` to handle both versions
  during a transition window.
- **Removing or renaming a field:** breaking. Same procedure but coordinate
  the cutover with the dashboard so old data isn't silently dropped.

The dashboard's per-commit aggregation is implemented in
`unoplatform/uno.benchmarks/js/data.js` (`aggregateByCommit`). Multiple runs of
the same commit collapse to one chart point at render time. Do not aggregate at
publisher time — the data branch is append-only.

## Pipeline gating

The benchmark stages are gated:

```yaml
- ${{ if or(eq(variables['Build.SourceBranch'], 'refs/heads/master'),
            eq(variables['Build.Reason'], 'Schedule')) }}:
  - template: tests/.azure-devops-tests-skia-benchmarks-stages.yml
```

This means:

- Never on PRs (no PR override variable in v1).
- Every push to master.
- Every nightly schedule trigger (currently 03:00 UTC).

To trigger an ad-hoc benchmark run on a non-master branch, queue the pipeline
manually in Azure DevOps with `Build.SourceBranch` overridden — the gate is
evaluated at queue time.

## Always-green collector

In v1, benchmark stages never fail on numbers. The MSTest fixture only fails if
`BenchmarkRunnerHost.RunOneAsync` returns no rows at all (harness crash, total
timeout). A benchmark that produces an error row (timeout, exception during
[GlobalSetup], etc.) still counts as "rows produced" and the stage goes green
with that row marked `error: "<reason>"` in the JSONL.

Threshold-based pass/fail is a v2 follow-up. It needs ~30 days of master data
to derive an empirical noise band per (platform, benchmark). Do not introduce
hard thresholds until the dashboard's regression-badge widget has been
trustworthy for at least a month.

## Files map

| File | Role |
|------|------|
| `src/Uno.UI.RuntimeTests/Tests/Benchmarks/BenchmarkRunner_Fixture.cs` | Single MSTest fixture, auto-discovery |
| `src/Uno.UI.RuntimeTests/Tests/Benchmarks/BenchmarkRunnerHost.cs` | RunOne / RunAll, soft timeouts, JSONL emission |
| `src/Uno.UI.RuntimeTests/Tests/Benchmarks/CoreConfig.cs` | Per-category BDN Job config |
| `src/Uno.UI.RuntimeTests/Tests/Benchmarks/RenderBenchmarkHost.cs` | Scene setup + RenderFrameAsync |
| `src/Uno.UI.RuntimeTests/Tests/Benchmarks/Suites/...` | Benchmark classes |
| `src/Uno.UI.Benchmarks.Publisher/...` | normalize / merge / compare CLI |
| `src/SamplesApp/UITests.Shared/Windows_UI_Xaml/Performance/BenchmarksPage.xaml{,.cs}` | In-app runner |
| `build/ci/tests/.azure-devops-benchmarks-*.yml` | 11 per-target stages + publish |
| `build/ci/tests/.azure-devops-tests-skia-benchmarks-stages.yml` | Top-level stage tree |
| `doc/benchmarks/{README,adding-benchmarks,local-workflow,provisioning}.md` | Public docs |
| `unoplatform/uno.benchmarks` (separate repo) | Dashboard + data branch |

## When to reach for what

| Task | Where |
|------|-------|
| Add a new benchmark | `Suites/<category>/` — that's it |
| Tune iteration count for a category | `CoreConfig.For()` |
| Change timeout for a category | `CoreConfig.PerBenchmarkTimeout()` |
| Change per-platform CI runtime | `.azure-devops-benchmarks-<target>-skia.yml` |
| Change platform enum | `Schema.md` + `BenchmarkRunnerHost` (host slug) + dashboard's URL handling |
| Bump BDN version | `Directory.Build.targets` `$(BenchmarkDotNetVersion)` |
| Change result schema | All four: `BenchmarkRunnerHost.cs` + publisher's `Schema.cs` + `Schema.md` + dashboard's `data.js` |
