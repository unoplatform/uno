# Running benchmarks locally

Two ways to run the suite locally:

1. **`dotnet test`** with `Category=Benchmark` filter — terminal, scriptable.
2. **SamplesApp** Performance / Benchmarks page — interactive, with in-app
   results table designed for side-by-side window comparison across two
   worktrees.

## Setup

You'll have already configured the Skia cross-target override per the main
[Building Uno UI guide](../articles/uno-development/building-uno-ui.md). With
that in place:

```bash
cd src
dotnet restore Uno.UI-Skia-only.slnf
dotnet build Uno.UI-Skia-only.slnf --no-restore
```

## Run from the terminal

Run all benchmarks against the Skia desktop runtime tests:

```bash
cd src
dotnet test Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Skia.csproj \
  --filter "TestCategory=Benchmark"
```

Run a specific benchmark class:

```bash
dotnet test Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Skia.csproj \
  --filter "FullyQualifiedName~BorderBenchmark"
```

By default the harness writes results to `%TEMP%/uno-bench-<guid>/`. The fixture
prints the artifacts directory at the start of each run.

## Run from SamplesApp (interactive, recommended for A/B)

1. Run the Skia samples app:
   ```bash
   cd src/SamplesApp/SamplesApp.Skia.Generic
   dotnet run -c Release
   ```
2. Navigate to **Performance / Benchmarks** in the sample browser.
3. Click **Run all** (or filter to a substring + **Run filtered**).
4. The in-app table populates with `Mean / StdDev / Allocated / N` per benchmark.

The page is designed for side-by-side window comparison: open the same
SamplesApp on two worktrees on the same machine, run benchmarks on each, and
eyeball the results table on each. Stable column widths and stable sort order
mean rows line up visually.

### Save results for later

The **Save zip** button writes a result archive in the same v1 schema CI
publishes. Default filename: `benchmarks-<yyyymmdd-hhmm>.zip`.

The zip's contents:
- `BenchmarkDotNet.Artifacts/` — BDN's full output (markdown report, CSV).
- `results.jsonl` — normalized v1 schema rows.
- `meta.json` — run metadata (timestamp, host info).

### Copy as markdown

The **Copy as markdown** button puts a markdown table on the clipboard. Useful
for pasting into PR descriptions, GitHub issues, or chat.

## Local A/B comparison: two branches

Two ways:

### Visual (one-shot, no diff math)

1. Worktree A on branch X. Run benchmarks in SamplesApp. Save zip A.
2. Worktree B on branch Y. Run benchmarks in SamplesApp. Save zip B.
3. Compare the in-app tables visually with windows side-by-side.

### Computed delta table (markdown / csv / json)

Use the publisher CLI:

```bash
cd src
dotnet run --project Uno.UI.Benchmarks.Publisher -- \
  compare benchmarks-A.zip benchmarks-B.zip --output md
```

Output options: `md` (default — markdown table), `csv`, `json`.

Sample output:

```
| Benchmark | Mean A | Mean B | Δ | Δ% | Alloc Δ |
| --- | ---: | ---: | ---: | ---: | ---: |
| BorderBenchmark.Toggle_BorderBrush | 132.4 ns | 145.7 ns | +13.3 ns | +10.0% | +0 B |
| BorderBenchmark.Toggle_Style | 1.42 µs | 1.35 µs | -70.0 ns | -4.9% | +0 B |
…
```

### Dashboard upload (planned)

The dashboard's `/?compare` page accepts two uploaded zips and renders the same
diff table. As of v1 the in-browser zip parser is not yet implemented; extract
`results.jsonl` from each zip and upload those directly, or use the CLI above.

## Tips for stable local runs

- Plug in the laptop. Battery throttling skews micro-benchmark numbers.
- Close Chrome / Slack / VS / anything else competing for CPU.
- Disable the OS's "balanced" power plan for the run; switch to "high
  performance" or equivalent.
- Don't trust a single run with `< 5%` deltas. Run twice. The dashboard's
  noise-band heuristic uses 2σ + 5% for a reason.
- For Render benchmarks, plug in to mains AND don't have any other GPU-using
  app running (Discord with hardware accel, browser with WebGL tabs, etc.).

## Environment variables

| Variable | Effect |
|----------|--------|
| `UITEST_RUNTIME_BENCHMARKS_ONLY=true` | The runtime-tests harness runs *only* `[Category=Benchmark]` tests. Set automatically by the CI benchmark stages. |
| `AGENT_NAME` | Stamped into the result JSONL's `host.agentName` field. CI sets this to `$(Agent.Name)`. Locally defaults to `local`. |
| `BENCHMARK_REPO_TOKEN` | PAT for pushing to the data branch. Local A/B never needs this; only the CI publish stage does. |
