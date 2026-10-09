# Rendering budget tests

**Status**: Draft (report-only)
**Audience**: Internal engineering (Uno Platform maintainers)
**Related**: [#23526](https://github.com/unoplatform/uno/pull/23526), [#23984](https://github.com/unoplatform/uno/issues/23984), [#24791](https://github.com/unoplatform/uno/issues/24791), [#24788](https://github.com/unoplatform/uno/pull/24788), [#24792](https://github.com/unoplatform/uno/pull/24792), [#25054](https://github.com/unoplatform/uno/issues/25054), [#25114](https://github.com/unoplatform/uno/issues/25114)

Runtime tests that **count** the work the renderer does and report the counts on every pull request, so a
rendering or memory regression shows up in the PR that introduces it instead of in a stable release.

## 1. Why

Damage-region rendering ([#23526](https://github.com/unoplatform/uno/pull/23526)) made 6.7 repaint only what
changed, and two side effects shipped with it. Users found them in 6.7 stable
([#23984](https://github.com/unoplatform/uno/issues/23984), [#24791](https://github.com/unoplatform/uno/issues/24791)):

- Scrolling ran a path boolean (`SKPath.Op`) per clipped visual on every frame. Before
  [#24788](https://github.com/unoplatform/uno/pull/24788), 60 scroll steps finalized about 850 `SKPath` objects.
  After it, they finalized none.
- An active `ProgressRing` damaged the whole window on every frame until
  [#24792](https://github.com/unoplatform/uno/pull/24792).

Both are visible as **counts**: path operations per scroll step, and damaged area per frame. Timings are too
noisy to gate pull requests: in a local comparison of published builds, CPU time for the same build varied by up
to 3× between runs, while the counts were identical run after run.

## 2. What is measured

| Metric | Source | Why it is stable |
|---|---|---|
| Frames rendered | `CompositionTarget.FrameRendered` (internal event, raised once per recorded frame) | Driven by what changed, not by machine speed. Subscribing does not force frames, unlike the public `CompositionTarget.Rendering`. |
| Damaged area per frame | `CompositionTarget.LastRecordedDamage` and `LastRecordedFrameRect` (new, internal) | The damage a frame records is decided by the visual tree, not by the host, so it is the same on every desktop lane. |
| Path booleans | `SkiaPathOpCounter.Count` (new, internal) | Counts `SKPath.Op` calls. Only exists in builds with the Skia drawing backend (`UNO_DRAWING_SKIA`). |
| Measures | `UIElement.LayoutMeasureCoreCount` (existing, internal) | Deterministic for a given tree and input. |
| Objects left alive | `WeakReference` after a GC loop | Same loop as the binding leak tests. Each object still alive is reported by type. |

The two new seams cost nothing per frame: one keeps a reference to an array the frame already allocates, the
other is an `Interlocked.Increment` next to an `SKPath.Op`.

## 3. Scenarios and budgets

`src/Uno.UI.RuntimeTests/Microsoft/UI/Xaml/Media/Given_RenderingBudget.cs`, Skia only. A budget is an upper
bound. The priorities come from the performance issues and fix PRs of the last 18 months: memory retention and
rendering/damage lead, then scrolling, images, text and animations.

| Test | Metric | Budget | Catches |
|---|---|---|---|
| `When_Idle_Then_No_Frames` | `idle.frames-per-second` | ≤ 2 | Redraw loops on a static page |
| | `idle.full-window-frames` | 0 | |
| `When_Scrolling_List_Then_One_Frame_Per_Step` (`ListView`, rounded item borders, 30 steps of 37 px) | `scroll.frames-per-step` | ≤ 1.5 | Extra frames per scroll step |
| | `scroll.damage-per-viewport` | ≤ 1.25× | Scrolling repainting more than the list |
| | `scroll.full-window-frames` | 0 | |
| | `scroll.path-ops-per-step` | ≤ 1 | Per-frame path booleans (#23984) |
| | `scroll.measures-per-step` | ≤ 40 | Layout work per step |
| `When_ProgressRing_Active_Then_Damage_Stays_Within_Ring` | `progressring.damage-per-ring-area` | ≤ 2× | Small animations repainting the window (#24792) |
| | `progressring.full-window-frames` | 0 | |
| | `progressring.path-ops-per-frame` | ≤ 1 | |
| `When_Animated_Subtree_Removed_Then_Rendering_Stops` | `removed-animation.frames-per-second` | ≤ 2 | Detached animations driving frames (#25054) |
| | `removed-animation.objects-alive` | 0 | The detached subtree leaking |
| `When_Page_Removed_Then_Released` (image, list, ring, text box, toggle switch; measured on the second page) | `page-removal.objects-alive` | 0 | Controls retained after their page is gone |

Budgets leave room above today's values. The goal is to catch the step changes regressions cause (a whole
window instead of a ring, hundreds of path operations instead of none), not to pin exact numbers.

### Values on master (Skia desktop, Windows)

| Metric | Value | |
|---|---|---|
| `idle.frames-per-second` | 0 | |
| `scroll.frames-per-step` | 1 | |
| `scroll.damage-per-viewport` | 0.93× or 1.05× | Varies between runs; the only count that did |
| `scroll.path-ops-per-step` | 0.4 | |
| `scroll.measures-per-step` | 11.4 | |
| `progressring.damage-per-ring-area` | 1.21× | The ring plus its antialiasing outset |
| `progressring.path-ops-per-frame` | 0 | |
| `removed-animation.frames-per-second` | 11 (display rate) | **Over budget**: [#25054](https://github.com/unoplatform/uno/issues/25054), not fixed on master yet |
| `removed-animation.objects-alive` | 1 of 3 (the `Visual`) | **Over budget**: [#25054](https://github.com/unoplatform/uno/issues/25054) |
| `page-removal.objects-alive` | 0 of 7 | After a warm-up page (see below) |

Across four local runs, every other count was identical. Frame rates follow the display rate, which was about
11 Hz on the test machine. A frame rate therefore has a budget only where the expected value is zero (idle,
removed animation). Everything else is measured per frame or per step.

**First `ToggleSwitch` retained.** The first `ToggleSwitch` created in a process stays alive after its page is
removed, whatever its `IsOn`. Later instances are collected. It is a one-instance retention, not a growing leak,
and it only shows when no earlier test created a `ToggleSwitch`. That would explain why the add/remove leak tests
don't flag it. The page test therefore removes a warm-up page first and measures the second one. The retention itself is
worth its own issue.

The removed-animation test stops its animation when it ends. Otherwise the leaked animation from
[#25054](https://github.com/unoplatform/uno/issues/25054) would keep every later test in the run rendering at
display rate. Before this was added, it raised the idle test to 10 frames per second.

### Fails before the fixes, passes after (servicing/6.8)

The same tests, ported to `servicing/6.8` with equivalent hooks, run at three commits. 6.8 still has the
`SKPath`-based compositor, so the path counter there counts every `SKPath.Op` in the composition layer. Skia
desktop, Windows:

| Metric (budget) | Before [#24788](https://github.com/unoplatform/uno/pull/24788) | After #24788 | After [#24792](https://github.com/unoplatform/uno/pull/24792) | master |
|---|---:|---:|---:|---:|
| `scroll.path-ops-per-step` (≤ 1) | **57.5** | **6.4** | **6.4** | 0.4 |
| `progressring.damage-per-ring-area` (≤ 2×) | **122.75×** | **122.75×** | 1.21× | 1.21× |
| `progressring.path-ops-per-frame` (≤ 1) | **25** | **5** | **5** | 0 |
| `scroll.frames-per-step` (≤ 1.5) | 1 | 1 | 1 | 1 |
| `scroll.damage-per-viewport` (≤ 1.25×) | 0.97× | 0.97× | 0.93× | 1.05× |

Each fix moves the metric it targets, and nothing else moves:
- #24788 cuts the scroll path booleans by 9×.
- #24792 brings the ring's damage back to its own bounds.

The 6.4 path booleans per step left on 6.8 are most likely the clip-path work that master caches since
[#24230](https://github.com/unoplatform/uno/pull/24230) (not checked op by op). That cache was measured at under
3% of a 6.8 scroll frame and deliberately not backported, so the master budget is stricter than 6.8 needs to be.

## 4. How the numbers reach the pull request

1. A test calls `RuntimeTestMetrics.Record(name, value, budget, unit)`
   (`src/Uno.UI.RuntimeTests/Helpers/RuntimeTestMetrics.cs`).
2. The runtime-test runner (`UnitTestsControl`) clears the metrics before each attempt and writes them into the
   NUnit results as test-case properties: `metric:<name>`, `budget:<name>`, `unit:<name>`. Every lane already
   produces that file, including WASM and mobile, so adding a lane needs no new transport.
3. Each desktop lane runs `build/ci/templates/publish-runtime-tests-metrics.yml` after publishing its results.
   It calls `build/ci/scripts/runtime-tests-metrics.ps1 -Mode Extract`, which writes the lane's metrics as JSON
   into the shared `runtime-tests-metrics` artifact, one folder per lane and one file per job attempt.
4. The `runtime_tests_metrics` stage ("Tests - Rendering budgets") runs once the desktop runtime-test stages
   finish, whether they passed or failed. `-Mode Report` builds the report (§4.1), publishes it as the
   `runtime-tests-metrics-report` artifact and, on a pull request, creates or updates **one** comment. It finds
   the comment through the hidden marker `<!-- runtime-tests-metrics -->` and the account that posted it, so a
   push or a stage retry edits it in place. It posts with `CommentsGitHubPAT`, the same pipeline secret as the
   screenshot comparison, which should be a `unodevops` token. Fork pull requests get no secrets, so for them the
   report is only published as an artifact.

### 4.1 Compared with the latest published versions

The comment starts with one line per version (this PR, latest dev, latest stable), then the main comparison
table, Metric | Budget | This PR | Latest dev | Latest stable, on the first lane (Windows). Under it, **Every lane**
repeats that comparison for each lane, collapsed, with the lane's status in its heading (over budget, and what
this PR changed against the latest dev). "What the numbers mean" is collapsed last: the symbols, and one line per
metric taken from the tests' descriptions.

- The versions are read from nuget.org (`Uno.WinUI`) on every report, so the comparison is always with what users
  can install. The dev version is the newest one of this build's line: 7.0 for master, 6.8 for `servicing/6.8`.
  The stable version is the newest stable release, whichever release branch it came from, so the column moves
  from one stable line to the next without any change here.
- Each version is the build number of the CI build that produced it: `7.0.0-dev.1656` comes from build
  `7.0-dev.1656`, and `6.7.135` from build `6.7.135`. The report reads that build's own `runtime-tests-metrics`
  artifact, so all three columns come from the same tests, on the same kind of agent.
- A version built before these tests existed shows no numbers, with a note saying why. The dev column fills in
  with the first dev version published after this change. The stable column fills in with the first stable
  release whose branch carries the tests, so the tests need to be on the release branch before that release.
- Above the table, one line per column says how many of its metrics are over budget, and how many this PR moved
  against the latest dev, so "within budget" is never read as being about another version.
- ▲ / ▼ mark a change of more than 20% from the latest dev. The damaged area alone varies by about 13% between
  runs. Frame rates follow the agent's display rate, so they count as changed only when they cross their budget
  (for example a removed animation going from 33 frames per second to 0).

Every step is `continueOnError`. Nothing depends on the report stage, so package publishing never waits for it.

To preview a report locally:

```pwsh
build/ci/scripts/runtime-tests-metrics.ps1 -Mode Extract -ResultsFile results.xml -Lane skia-windows -OutputDirectory out
build/ci/scripts/runtime-tests-metrics.ps1 -Mode Report -InputDirectory out -ReportFile report.md `
    -BuildNumber 7.0-dev.1 -CollectionUri https://dev.azure.com/uno-platform/ `
    -ProjectId 1dd81cbd-cb35-41de-a570-b0df3571a196 -DefinitionId 5
```

`-DevBuildNumber` and `-StableBuildNumber` compare with specific builds instead of the latest published versions.

## 5. Report-only first, enforcing later

A value over its budget is flagged in the comment (⚠️). It does not fail the test or the build. A test fails only
when its scenario did not run, for example when nothing scrolled or the ring rendered no frame.

A budget starts failing PRs once it has been stable on every desktop lane for a couple of weeks of master
builds. The test then asserts on the value it records. Budgets that are over today (#25054, the `ToggleSwitch`)
are enforced once their fix lands, and the fix PR shows the value dropping in its own comment.

## 6. Adding a budget

- Measure counts, never durations.
- Record with `RuntimeTestMetrics.Record("<scenario>.<metric>", value, budget, unit, description)`. Normalize by
  the scenario size (per step, per frame, per area) so the value doesn't depend on window size or frame rate.
- Give it a one-sentence description: what it counts, in which scenario, and what a good value is. The report's
  "What the numbers mean" table is built from these descriptions.
- Keep the measured objects out of the async state machine (`[MethodImpl(NoInlining)]` helpers) when counting
  what stays alive.
- Undo anything a regression could leave running, so it can't affect later tests.

## 7. Next steps

- **More lanes.** WASM, Android and iOS already have the properties in their results files. They need the
  extract step, plus a path to the file on each agent.
- **Stable baseline.** The stable column stays empty until a stable release is built with these tests. That
  needs the tests and their counters on the release branches (the `servicing/6.8` port in §3 shows what the 6.x
  counters look like).
- **More scenarios.** Lottie playback damage, decoded image memory after the image leaves the screen
  ([#25114](https://github.com/unoplatform/uno/issues/25114)), text updates (measures and damage per edit).
- **Timing benchmarks** belong on merge builds, in parallel and never blocking publishing, with real devices
  before releases. They are out of scope here.

## 8. Deliberately not done

- **No timings**, for the reasons in §1.
- **No enforcement yet.** A noisy budget that fails PRs would be disabled within a week.
- **No present-path counters.** Whether a host repaints the whole target (for example when it doesn't preserve
  its contents) depends on the host. The recorded damage doesn't, and it is what regressed.
