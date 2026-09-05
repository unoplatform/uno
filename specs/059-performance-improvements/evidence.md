# Uno Platform performance improvements — measured evidence

**Status**: In progress
**Branch**: `dev/mazi/perf-impr` (based on `feature/breakingchanges`)
**Audience**: Internal engineering (Uno Platform maintainers)

Every entry below corresponds to **exactly one commit**. A fix is only listed once it has a
before/after measurement produced by the harnesses described in §2, and (where the change touches
observable behaviour) a runtime-test run. Numbers that were reasoned about rather than measured are
labelled as such — they are never presented as measurements.

---

## 1. Scope and rules

- **Skia-first.** Only code the Skia build compiles is in scope. Native Android-Views / native
  UIKit / WASM-DOM UI paths are maintenance-only and are not optimised here.
- **Behaviour-preserving.** No relaxed validation, no dropped edge cases, no changed public
  signatures. Where Uno and WinUI differ, the WinUI C++ source is cited and Uno is moved *towards*
  it, never away.
- **One fix per commit**, each with its own row in §4.
- **Two proofs per fix**: a measurement showing the win, and evidence that behaviour is unchanged
  (runtime tests, or an argument grounded in the WinUI source plus tests).

## 2. Harnesses

Both harnesses live outside the repo (they reference it) so they never ship. They are reproducible
from the commands below.

### 2.1 Integration harness — `D:\Throwaway\uno-perf-int`

A headless Uno app (`Uno.UI.Runtime.Skia.Headless`) that project-references the repo's own
`Uno.UI`, `Uno.UI.Composition`, `Uno.WinRT`, `Uno.Foundation` and `Uno.UI.Dispatching`, so it
measures **the real framework**, rebuilt from the working tree:

```bash
cd D:/Throwaway/uno-perf-int
dotnet build -c Release -p:UnoFastDevBuild=true -p:UnoTargetFrameworkOverride=net10.0
dotnet bin/Release/net10.0/UnoPerfInt.dll all
```

Workloads (tree = 4 levels deep, branching 6 → **3109 elements**: nested `Grid`/`StackPanel`,
`Border` with background+padding+margin at every level, `TextBlock` leaves):

| Workload | What it does |
|---|---|
| `layout.full-pass` | Dirties **every** element, then `Measure` + `Arrange` the whole tree |
| `layout.resize-pass` | Alternates the available width so no cached size can be reused |
| `tree.build` | Constructs the tree from scratch (template-materialisation cost) |
| `dp.set-get` | 1000 × (`Opacity` set, `Width` set, both read) on a real `Border` |

Reported per iteration: wall time, **managed bytes allocated on the UI thread**
(`GC.GetAllocatedBytesForCurrentThread`), and bytes per element.

> Allocation counts are deterministic and unaffected by machine load, so they are the primary
> metric. Wall-clock is only reported from a quiet machine, with a control workload
> (`tree.build` / `dp.set-get`, untouched by most fixes) to detect drift.

### 2.2 Allocation profiler — `D:\Throwaway\uno-perf-analyzer`

`dotnet-trace` + a TraceEvent-based analyzer that attributes sampled allocations **by type and by
call stack**, which is how each finding below was located rather than guessed:

```bash
cd D:/Throwaway/uno-perf-int
dotnet-trace collect --format nettrace -o alloc.nettrace \
  --providers "Microsoft-Windows-DotNETRuntime:0x200001:5,Microsoft-DotNETCore-SampleProfiler" \
  -- dotnet bin/Release/net10.0/UnoPerfInt.dll layout

cd D:/Throwaway/uno-perf-analyzer
dotnet bin/Release/net10.0/AllocAnalyzer.dll ../uno-perf-int/alloc.nettrace 25 15
```

### 2.3 Micro-benchmarks — `D:\Throwaway\uno-perf-bench`

BenchmarkDotNet 0.15.4, used for isolated `Old` vs `New` comparisons where a change can be
extracted from its surroundings.

### 2.4 Correctness

```bash
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj \
  -c Release -f net10.0 -p:UnoFastDevBuild=true -p:UnoTargetFrameworkOverride=net10.0
export UITEST_RUNTIME_TESTS_FILTER=$(echo -n "<filter>" | base64)
cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

---

## 3. Baseline

Measured on the merge-base of this branch, before any fix in §4.
Windows 11, .NET 10.0.301, Release, workstation GC, `UnoPerfInt` as described above.

| Workload | Time / iteration | Allocated / iteration | Allocated / element |
|---|---:|---:|---:|
| `layout.full-pass` | 33.34 ms | 8,196,595 B | 2,636.4 B |
| `layout.resize-pass` | 34.96 ms | 8,191,205 B | 2,634.7 B |
| `tree.build` | 35.68 ms | 21,830,618 B | 7,021.7 B |
| `dp.set-get` (×1000) | 0.258 ms | 48,001 B | 48.0 B |

**Where the layout allocations go** (top of the by-type profile for `layout.full-pass`, sampled
allocation events, 1.20 GB sampled total):

| Share | Type |
|---:|---|
| 5.0 % | `LinkedListNode<UnicodeText+Cluster>` |
| 4.1 % | `LinkedListNode<UnicodeText+Glyph>` |
| 3.7 % | `ValueTuple<float, UnicodeText+Cluster>[]` |
| 3.3 % | `Line[]` |
| 2.5 % | `List<int>` / `int[]` |
| 2.1 % | `UnicodeText` |

The by-stack profile attributes these to **two** call sites of equal weight —
`TextBlock.MeasureOverride → ParseText` (29.0 MB in the top stack) **and**
`TextBlock.ArrangeOverride → ParseText` (28.2 MB). Text is shaped twice per layout pass.

---

## 4. Fixes

<!-- one row per commit; newest last -->

### F1 — `TextBlock` re-shapes its text at arrange time when only the available height changed

**Commit**: `perf(textblock): Skip the arrange-time text re-shape`
**Files**: `src/Uno.UI/UI/Xaml/Controls/TextBlock/TextBlock.cs`, `src/Uno.UI/UI/Xaml/Documents/UnicodeText.cs`

**Symptom.** The allocation profile of a layout pass attributed two call sites of almost equal
weight to `TextBlock.ParseText`: one under `MeasureOverride` (29.0 MB in the top stack) and one
under `ArrangeOverride` (28.2 MB). Every `TextBlock` was running ICU boundary analysis, HarfBuzz
shaping and line breaking **twice** per layout pass.

**Root cause.** `ArrangeOverride` re-parsed whenever the arrange constraint differed from the
measure constraint by full `Size` equality:

```csharp
if (_lastParsedTextCreationValues.availableSize != availableSizeWithoutPadding || ...alignment...)
```

A panel virtually always measures a child with an unconstrained or generous height and then
arranges it at its desired height, so the `Height` component differs on essentially every pass —
even though the width, which is what drives line breaking, is identical.

**Why the height is not free to ignore.** `UnicodeText` reads `availableSize.Height` in exactly one
place (`UnicodeText.cs:520`) to decide `isEarlyLastLine`, which drops the lines that do not fit
(`UnicodeText.cs:610`). So height genuinely can change the result — but only when it actually
truncated something.

**WinUI does exactly this distinction.** `BlockNode::CanBypassMeasure`
(`dxaml/xcp/core/text/BlockLayout/BlockNode.cpp:395-425`) requires the *width* to match, and then:

```cpp
if (m_pBreak != NULL)
{
    // If there is a break for this paragraph, then height constraint needs to be
    // the same to break at the same place.
    bypass = IsCloseReal(availableSize.height, m_prevAvailableSize.height);
}
else
{
    // If there was no break, we can bypass as long as the desired height will fit
    // in the available space.
    bypass = (m_desiredSize.height <= availableSize.height);
}
```

Uno's unconditional full-`Size` comparison is strictly more conservative than WinUI's, at the cost
of a full re-shape.

**Fix.** `UnicodeText` now records whether any line was dropped because of the height constraint
(`IsHeightTruncated`, the analogue of WinUI's `m_pBreak != nullptr`), and `TextBlock` applies
WinUI's rule in `NeedsReparseForArrange`: width and alignment must match; height must match only if
the previous layout was height-truncated, otherwise it is enough that the text still fits.

This is sound because prefix sums make it exact: for every line *i*,
`totalHeight_i + nextLineHeight_i` is the prefix sum through *i+1*, which is `<= outSize.Height`.
So `arrangeHeight >= outSize.Height` implies no line can be dropped that was not dropped before,
and the boundary case (a panel arranging at exactly the desired height) lands on the `<=` side.

**Measurement** — `UnoPerfInt`, Release, quiet machine, 3 runs each, median reported. `tree.build`
and `dp.set-get` are **control workloads** (untouched by this change) and confirm no machine drift.

| Workload | Metric | Before | After | Δ |
|---|---|---:|---:|---:|
| `layout.full-pass` | time / pass | 32.41 ms | 19.03 ms | **−41.3 %** |
| `layout.full-pass` | alloc / pass | 8,194,446 B | 4,143,044 B | **−49.4 %** |
| `layout.full-pass` | alloc / element | 2,635.7 B | 1,332.6 B | **−49.4 %** |
| `layout.resize-pass` | time / pass | 32.93 ms | 18.57 ms | **−43.6 %** |
| `layout.resize-pass` | alloc / pass | 8,191,210 B | 4,137,379 B | **−49.5 %** |
| *control* `tree.build` | time | 33.00 ms | 33.32 ms | +1.0 % (noise) |
| *control* `dp.set-get` | time | 0.2413 ms | 0.2434 ms | +0.9 % (noise) |

Raw before/after runs — `layout.full-pass` time: 32.41 / 33.45 / 31.45 → 19.42 / 19.03 / 18.86 ms.

**Correctness.** Runtime tests, Skia Desktop, `Given_TextBlock*` + `Given_TextBox*`:
**363 run, 358 passed, 7 skipped, 5 failed.** All 5 failures reproduce **unchanged on the baseline**
with the fix stashed (verified by rebuilding and re-running them), so none is caused by this change:

| Test | Baseline | With fix |
|---|---|---|
| `Given_TextBlock.When_Inlines_Transitively_Change` | Failed | Failed |
| `Given_TextBlock.When_IsTextSelectionEnabled_CRLF` | Failed | Failed |
| `Given_TextBox.When_Caret_Line_Straddles_Viewport_Edge_Grippers_Are_Hidden` | Failed | Failed |
| `Given_TextBox.When_Copy_Paste` | Failed (False) | Failed (True) — clipboard, flaky either way |
| `Given_TextBox.When_Multiline_Pointer_TripleTap_With_Wrapping` | Failed | Failed |


### F2 — `Grid.MeasureCellsGroup` leaks its pooled span-store array on the empty-group path

**Commit**: `perf(grid): Stop leaking the pooled span store on the empty path`
**File**: `src/Uno.UI/UI/Xaml/Controls/Grid/Grid.cs`

**Root cause.** `SpanStoreStackVector` derives from `StackVector<T>`, which rents from
`ArrayPool<T>.Shared` **in its constructor** (`src/Uno.WinRT/Collections/StackVector.cs:49`) and
returns the array in `Dispose()` (`:62`). `MeasureCellsGroup` constructed it *before* its
`cellsHead >= cellCount` early return, so that path skipped the `Dispose()` at the end of the
method and the rented array was never given back.

`Grid.MeasureOverride` calls `MeasureCellsGroup` up to seven times per measure, once per cell
group, and a group head is `int.MaxValue` when the group is empty — so a typical grid takes the
leaking path two to three times per measure while only returning one array. The pool bucket drains,
after which every `Rent` allocates a fresh array.

**Fix.** Move the early return above the construction. The vector is not touched before that point,
so the change is inert other than not renting.

**Measurement** — `UnoPerfInt` `grid.full-pass`: a 341-`Grid` / 1024-`Border` tree (depth 5,
branching 4, `Auto` rows and `*` columns), 1365 elements, every element dirtied per pass. 3 runs
each, median.

| Metric | Before | After | Δ |
|---|---:|---:|---:|
| alloc / pass | 431,065 B | 120,073 B | **−72.1 %** |
| alloc / element | 315.8 B | 88.0 B | **−72.1 %** |
| time / pass | 3.67 ms | 3.11 ms | **−15.3 %** |

Allocation figures are byte-identical across all runs in each configuration.

> **Correction.** This entry originally reported **−64.6 %** on time (3.73 ms → 1.32 ms). That
> comparison was invalid: the "before" run executed the `grid` workload **alone**, while the "after"
> run executed it as part of the full `all` set, where the preceding workloads had already warmed up
> the JIT. The corrected figure above isolates this change alone — both runs execute `grid` alone,
> and the change is reverted/re-applied on top of the current tree — giving 3.67 ms → 3.11 ms
> (raw: 3.82 / 3.34 / 3.67 / 2.93 / 3.88 → 3.11 / 3.70 / 3.22 / 3.05 / 3.10, medians). The
> allocation figures were never affected: they are deterministic and independent of warm-up.
> Isolated the same way, this change accounts for **390,105 B → 79,113 B (−79.7 %)** on the current
> tree.
>
> **Lesson applied to every later entry:** before/after runs must use the *same* workload set in the
> *same* order.

**The arithmetic confirms the diagnosis.** 431,065 − 120,073 = 310,992 B saved per pass. The tree
holds 341 grids, each taking the leaking path ~3 times → ~1023 leaked rentals per pass →
**~304 B per rental**, which is exactly a 16-element `SpanStoreEntry[]` (16 B per entry) plus the
array header. The saving is not a side effect; it is the leaked rentals, one for one.

**Correctness.** Runtime tests, Skia Desktop, `Given_Grid` + `Given_GridLayouting` +
`Given_GridView_Items`: **85 run, 81 passed, 4 skipped, 4 failed.** All 4 failures reproduce on the
baseline *and* on the pre-F1 tree (verified by two separate rebuild-and-rerun cycles), so they
predate this work entirely:

| Test | Pre-F1 | With F1 only | With F1+F2 |
|---|---|---|---|
| `Given_Grid.When_Child_Added_Measure_And_Visible_Arrange` | Failed | Failed | Failed |
| `Given_Grid.When_ColumnDefinition_Width_Changed` | Failed | Failed | Failed |
| `Given_GridLayouting.When_Grid_ColumnCollection_Changes` | Failed | Failed | Failed |
| `Given_GridLayouting.When_Grid_RowCollection_Changes` | Failed | Failed | Failed |

### F3 — `RectangleClip` hands out a brand-new native `SKPath` on every frame

**Commit**: `perf(composition): Cache the rounded-clip path`
**File**: `src/Uno.UI.Composition/Composition/RectangleClip.skia.cs`

**Symptom.** In the paint profile of a rounded-`Border` tree, `SkiaSharp.SKPath` is the single
largest type at **14.8 %** of sampled allocations, with `SKPathBuilder` a further 5.6 %. The stack
attributes it to
`Visual.Render → BorderVisual.GetPostPaintingClipping → RectangleClip.GetClipPath → SKPathBuilder.Detach()`.

**Root cause.** `GetClipPath` reset a shared builder, added the round rect, and returned
`builder.Detach()` — and `Detach()` transfers ownership into a **new** `SKPath`. So every rounded
visual produced a fresh managed wrapper plus native path on every frame, left for the finaliser.
`Visual.Render` asks for the post-painting clip several times per visual per frame, multiplying it
further.

`RectangleClip` was the only clip doing this: `InsetClip.GetClipPath` already caches its path
keyed on bounds (`InsetClip.skia.cs:24-31`), and `CompositionGeometricClip` reuses a static spare
(`CompositionGeometricClip.skia.cs:31`).

**Fix.** Build the path once and keep it. The path depends **only on this clip's own properties** —
`RectangleClip.GetBoundsCore` reads `Left`/`Top`/`Right`/`Bottom` and ignores the `visual` argument,
and `GetBounds` then applies the clip's own `TransformMatrix` — so an override of
`OnPropertyChangedCore` drops the cached path whenever any property changes. That makes the hot path
a single null check, which is *less* work than before, not a trade.

**Why the invalidation is complete.** Every mutation of those fields goes through the public
setters → `CompositionObject.SetProperty` → `OnPropertyChanged` → `OnPropertyChangedCore`; the three
`Compositor.CreateRectangleClip` overloads use object-initialiser syntax, so they do too.
`RectangleClip` does not override `SetAnimatableProperty`, so composition animations write to the
`Properties` set and never touch these backing fields. And `BorderVisual.UpdatePathsAndCornerClip`
discards and recreates the clip rather than mutating it (`BorderVisual.skia.cs:204, 310`).

**Measurement** — `UnoPerfInt` `render.full-paint`: 780 rounded `Border`s + 625 rounded
`Rectangle`s (1561 elements), painted through `RenderTargetBitmap` (a real paint walk), **no layout
invalidation** so this is paint cost only. 5 runs each, median.

| Metric | Before | After | Δ |
|---|---:|---:|---:|
| alloc / frame | 519,110 B | 381,830 B | **−26.4 %** |
| alloc / element / frame | 332.5 B | 244.6 B | **−26.4 %** |
| Gen0 collections per 40 frames | 1 | **0** | — |
| time / frame | 70.40 ms | 70.08 ms | ±0 (noise) |

Raw time runs: 70.61 / 70.40 / 70.45 / 70.05 / 68.91 → 71.82 / 70.08 / 69.84 / 71.18 / 69.84 ms.

**Honest scope note.** This is a **GC-pressure** win, not a throughput win: the paint walk is
dominated by native rasterisation, and wall-clock is unchanged within noise. What it removes is
137 KB of managed garbage and ~780 finaliser-tracked native paths **per frame** — at 60 fps, 8.2 MB/s
of garbage that no longer has to be collected.

**Correctness.** Runtime tests, Skia Desktop, `Given_Border` + `Given_ContainerVisual` +
`Given_ShapeVisual` + `Given_Visual_Damage` + `Given_UIElement` + `Windows_UI_Xaml_Shapes.*`:
**671 run, 654 passed, 1 skipped, 16 failed — before and after.** The two runs were compared as
*sets* of failing test names: **0 new failures, 0 newly fixed.** The 16 include the clip tests that
would catch exactly this kind of bug (`Given_UIElement.When_Both_Layouting_Clip_And_Clip_DP`,
`When_TranslateTransform_And_Clip`), and they fail identically on the baseline.

### F4 — `ShapeVisual` re-runs three LINQ chains over its shapes on every frame

**Commit**: `perf(composition): Walk shapes by index on the frame path`
**File**: `src/Uno.UI.Composition/Composition/ShapeVisual.skia.cs`

**Symptom.** After F3, the paint profile still showed
`OfTypeIterator<CompositionSpriteShape>` at 1.7 % and `Enumerator[CompositionShape]` at 2.2 % of
sampled allocations.

**Root cause.** `RequiresRepaintOnEveryFrame`, `DamageRegionSamplingMargin` and `CanPaint()` are
queried once per shape element per frame and were each written as a LINQ chain
(`OfType` + `Any`, `OfType` + `Select` + `DefaultIfEmpty` + `Max`, `Any`), so each frame allocated a
collection enumerator, one or two iterators and a closure **per shape visual**. The sibling
`BorderVisual` implements the same two members with plain expressions
(`BorderVisual.skia.cs:385-387`).

**Fix.** Walk `_shapes` by index. `DamageRegionSamplingMargin` keeps a `seen` flag so the
`DefaultIfEmpty(0f)` semantics are preserved exactly — 0 is returned when there is no sprite shape
at all, rather than clamping a set of margins up to 0 (which a naive `Max` starting from 0 would do).

**Measurement** — `UnoPerfInt` `render.full-paint`, same workload as F3, 5 runs, median.
Measured against F3 (the commit before this one), and cumulatively against the pre-F3 baseline.

| Metric | Before (F3) | After (F4) | Δ vs F3 | Δ vs pre-F3 |
|---|---:|---:|---:|---:|
| alloc / frame | 381,830 B | 271,826 B | **−28.8 %** | **−47.6 %** |
| alloc / element / frame | 244.6 B | 174.1 B | −28.8 % | −47.6 % |
| time / frame | 70.08 ms | 69.28 ms | −1.1 % (noise) | −1.6 % (noise) |

Raw time runs: 70.25 / 68.68 / 69.11 / 69.28 / 69.27 ms. Allocation figures vary by <5 B across runs.

**Correctness.** Same suite as F3 (`Given_Border`, `Given_ContainerVisual`, `Given_ShapeVisual`,
`Given_Visual_Damage`, `Given_UIElement`, `Windows_UI_Xaml_Shapes.*`): **671 run, 654 passed,
16 failed**, compared as sets against the F3 run — **0 new failures, 0 newly passing.**

### F5 — `BorderVisual` rebuilds its pre-painting round-rect path on every frame

**Commit**: `perf(composition): Cache the border pre-painting round rect`
**File**: `src/Uno.UI.Composition/Composition/BorderVisual.skia.cs`

**Symptom.** After F3 and F4, `SKPath` was *still* the largest type in the paint profile (9.2 %),
with `SKPathBuilder` at 6.0 %. The stack:
`Visual.Render → BorderVisual.GetPrePaintingClipping → BuildRoundRectPath → SKPathBuilder.Detach()`.

**Root cause.** Same class of defect as F3, in a different place. `GetPrePaintingClipping` ran
`using var roundRectPath = BuildRoundRectPath(rect);` on every frame for every rounded border, and
`BuildRoundRectPath` allocates **both** a new `SKPathBuilder` and a new `SKPath`. The `using`
disposed them, so there was no native leak — but two managed wrappers plus their handle
registrations were created and thrown away per rounded border per frame.

**Fix.** Cache the path, keyed on the `SKRoundRect` instance it was built from.
`CreateBorderPath` assigns a **brand new** `SKRoundRect` to `_borderPathOuterRect` every time it
runs (`BorderVisual.skia.cs:362-363`: `var outerRect = new SKRoundRect(); … _borderPathOuterRect = outerRect;`),
so `ReferenceEquals` is an exact cache key — a changed border geometry always produces a new
instance and therefore a cache miss. The superseded path is disposed, keeping the deterministic
native cleanup the old `using` provided.

The returned path never escapes: `GetPrePaintingClipping` either intersects it into `dst` or
transforms it into `dst`, both within the same call.

**Measurement** — `UnoPerfInt` `render.full-paint`, same workload, 5 runs, median.

| Metric | Before (F4) | After (F5) | Δ vs F4 | Δ vs pre-F3 |
|---|---:|---:|---:|---:|
| alloc / frame | 271,826 B | **9,752 B** | **−96.4 %** | **−98.1 %** |
| alloc / element / frame | 174.1 B | **6.2 B** | −96.4 % | −98.1 % |
| time / frame | 69.28 ms | 69.40 ms | ±0 (noise) | ±0 (noise) |

Raw time runs: 70.64 / 69.40 / 69.25 / 70.58 / 68.28 ms. Allocation figures vary by ≤2 B across runs.

**The arithmetic checks out.** 271,826 − 9,752 = 262,074 B over 780 rounded borders = **336 B per
border per frame**, which is an `SKPathBuilder` plus an `SKPath` plus their two handle-table
registrations. Wall-clock is unchanged, confirming the same rasterisation work is still being done —
the saving is pure managed overhead, not skipped rendering.

**Correctness.** Same suite as F3/F4: **671 run, 654 passed, 16 failed**, set-compared against the
F4 run — **0 new failures, 0 newly passing.**

---

### Cumulative effect on the paint path (F3 + F4 + F5)

| Metric | Before F3 | After F5 | Δ |
|---|---:|---:|---:|
| alloc / frame | 519,110 B | 9,752 B | **−98.1 %** |
| alloc / element / frame | 332.5 B | 6.2 B | **−98.1 %** |
| Gen0 collections / 40 frames | 1 | 0 | — |

At 60 fps this is **30.6 MB/s of managed garbage** that a rounded-border-heavy UI no longer
produces, and ~1,560 fewer finaliser-tracked native Skia objects per frame. Frame *time* is
unchanged throughout — the paint walk is bound by native rasterisation, so all three fixes are
GC-pressure wins rather than throughput wins, and are reported as such.

### F6 — `VisualTreeHelper.GetChild`/`GetChildrenCount` box an enumerator on every call

**Commit**: `perf(uno-ui): Index the visual-tree child accessors`
**File**: `src/Uno.UI/UI/Xaml/Media/VisualTreeHelper.cs`

**Symptom.** In the **text-free** grid workload, `Enumerator[UIElement]` was 6.1 % of sampled
allocations, with a second unnamed enumerator type at 5.6 %. The stack:

```
MaterializableList<T>.IEnumerable<T>.GetEnumerator()
Enumerable.Count(IEnumerable<T>, Func<T,bool>)
VisualTreeHelper.GetChildrenCount(DependencyObject)
FrameworkElement.GetFirstChild()
FrameworkElement.HasTemplateChild()
FrameworkElement.ApplyTemplate(ref bool)
FrameworkElement.InnerMeasureCore(Size)
```

— i.e. **once per element per measure**.

**Root cause.** Both accessors were LINQ one-liners over `UIElement.GetChildren()`, which returns a
`MaterializableList<UIElement>`. Two costs follow:
1. LINQ calls `IEnumerable<T>.GetEnumerator()`, which **boxes** the `List<T>.Enumerator` struct.
2. `MaterializableList` enumerates through its `Materialized` property, which is
   `_innerList.ToList()` — a **fresh defensive copy** whenever the collection has been touched since
   the last enumeration (`MaterializableList.cs:144`).

`FrameworkElement.GetFirstChild()` reaches `GetChildrenCount` precisely for elements whose child
collection is *empty* (the `{ Count: > 0 }` pattern fails and it falls through), so every leaf
element paid this on every measure.

**Fix.** Walk the children by index. `MaterializableList`'s indexer reads `_innerList` directly
(`MaterializableList.cs:127-135`) and `Count` is `_innerList.Count` (`:109`), so this is exactly the
same sequence — the `Materialized` copy only exists to survive mutation *during* enumeration, and
neither accessor mutates. `GetChild` preserves `ElementAtOrDefault`'s out-of-range behaviour
(including negative indices) by decrementing a counter and returning `null` if it never reaches 0.

**Measurement** — `UnoPerfInt`, 3 runs of the full workload set in identical order before and after,
median. `render.full-paint`, `tree.build` and `dp.set-get` act as controls.

| Workload | Metric | Before | After | Δ |
|---|---|---:|---:|---:|
| `grid.full-pass` | alloc / pass | 120,073 B | 79,113 B | **−34.1 %** |
| `grid.full-pass` | alloc / element | 88.0 B | 58.0 B | −34.1 % |
| `grid.full-pass` | time / pass | 1.2960 ms | 1.2635 ms | −2.5 % |
| `layout.full-pass` | alloc / pass | 4,148,024 B | 4,095,678 B | −1.3 % |
| `layout.resize-pass` | alloc / pass | 4,137,265 B | 4,085,425 B | −1.3 % |
| *control* `render.full-paint` | alloc | 9,759 B | 9,774 B | ±0 |
| *control* `tree.build` | alloc | 21,923,269 B | 21,927,173 B | ±0 |
| *control* `dp.set-get` | alloc | 48,001 B | 48,001 B | ±0 |

**The arithmetic confirms the diagnosis in both trees independently.** The grid tree has **1024**
childless leaf `Border`s: 120,073 − 79,113 = 40,960 B ÷ 1024 = **exactly 40 B per leaf per pass**, the
size of a boxed `List<T>.Enumerator`. The text tree has **1296** childless leaf `TextBlock`s:
4,148,024 − 4,095,678 = 52,346 B ÷ 1296 = **40.4 B per leaf per pass**. Same constant, two different
trees.

**Correctness.** Runtime tests, Skia Desktop — `Given_VisualTreeHelper`, `Given_Border`,
`Given_ContainerVisual`, `Given_ShapeVisual`, `Given_Visual_Damage`, `Given_UIElement`,
`Windows_UI_Xaml_Shapes.*`, `Given_Grid`, `Given_GridLayouting`, `Given_FrameworkElement*`:
**937 run, 911 passed, 6 skipped, 25 failed**, set-compared against the baseline —
**0 new failures, 0 newly passing.**

### F7 — `Grid.ValidateDefinitions` boxes a collection enumerator twice per measure

**Commit**: `perf(grid): Index the definition collections`
**Files**: `src/Uno.UI/UI/Xaml/Controls/Grid/Grid.cs`, `DefinitionCollectionBase.cs`,
`RowDefinitionCollection.cs`, `ColumnDefinitionCollection.cs`

**Symptom.** With the text-free grid workload profiled inside the measured loop only (stacks
filtered to the benchmark's `Pass` delegate), `Enumerator[RowDefinition]` was the second-largest
entry, allocated at
`DependencyObjectCollection<T>.GetEnumerator() ← Grid.ValidateDefinitions ← Grid.MeasureCellsGroup`.

**Root cause.** `ValidateDefinitions` iterated `definitions.GetItems()`, whose implementations
return the backing `DependencyObjectCollection<T>` as `IEnumerable<DefinitionBase>`
(`RowDefinitionCollection.cs:37`). `DependencyObjectCollection<T>.GetEnumerator()` returns
`IEnumerator<T>` (`DependencyObjectCollection.cs:229`), so the `List<T>.Enumerator` struct is boxed.
`ValidateDefinitions` runs twice per `Grid` measure — once for rows, once for columns.

**Fix.** Loop by index. `DefinitionCollectionBase` already exposed both `Count` and
`GetItem(int)` — no new API was needed — and `GetItem` is `_inner[index]`, the same sequence the
enumerator walks. `GetItems()` had exactly one caller, so it is removed from the interface and its
two implementations rather than left dead.

**Measurement** — `UnoPerfInt` `grid.full-pass`, run **alone** in both configurations, change
reverted and re-applied on the current tree, 5 runs, median.

| Metric | Before | After | Δ |
|---|---:|---:|---:|
| alloc / pass | 79,113 B | 51,833 B | **−34.5 %** |
| alloc / element | 58.0 B | 38.0 B | −34.5 % |
| time / pass | 3.11 ms | 3.08 ms | ±0 (noise) |

Raw time runs: 3.11 / 3.70 / 3.22 / 3.05 / 3.10 → 3.02 / 3.19 / 2.83 / 3.08 / 3.84 ms. Allocation
figures are byte-identical across all runs in each configuration.

27,280 B saved over **341 grids** = **80 B per grid per pass** = two boxed `List<T>.Enumerator`s
(40 B each, the same constant F6 measured), one for rows and one for columns. Exactly as diagnosed.

**Correctness.** Same 937-test suite as F6, set-compared against the F6 run —
**0 new failures, 0 newly passing.**

---

### Cumulative effect on the text-free grid layout pass (F2 + F6 + F7)

Measured with `grid.full-pass` run alone throughout:

| Metric | Before F2 | After F7 | Δ |
|---|---:|---:|---:|
| alloc / pass | 431,065 B | 51,833 B | **−88.0 %** |
| alloc / element | 315.8 B | 38.0 B | **−88.0 %** |
| time / pass | 3.73 ms | 3.08 ms | **−17.4 %** |

### F8 — the resource-scope walk allocates an iterator per theme reference

**Commit**: `perf(uno-ui): Walk the resource scope without an iterator`
**Files**: `src/Uno.UI/UI/Xaml/DependencyObject.Store.cs`, `src/Uno.UI/UI/Xaml/ResourceResolver.cs`

**Symptom.** In the **list-scroll** profile, `<GetResourceDictionaries>d__219` — the compiler-generated
`yield return` state machine behind `DependencyObject.GetResourceDictionaries` — was the single
largest allocator at 6.3 %, reached through
`FrameworkElement.OnFwEltLoading → UpdateResourceBindings → UpdateAllThemeReferences → UpdateThemeReference`
as the list materialises containers.

**Root cause.** The method is a `yield return` iterator, so **every call allocates a state machine** —
and it is called once per theme reference per element entering the tree, on top of every implicit-style
lookup (`GetImplicitStyle`) and every `{StaticResource}` visual-tree retrieval
(`ResourceResolver.TryVisualTreeRetrieval`).

**Fix.** Return a `readonly struct` enumerable with a struct enumerator instead. The walk itself is
**unchanged** — still lazy, still yields the containing dictionary first, then non-empty ancestor
`Resources` nearest-first, then `Application.Resources`, and still short-circuits at the caller's first
match — so every consumer sees exactly the sequence the iterator produced. It still implements
`IEnumerable<ResourceDictionary>` so the two cold `.ToArray()` callers keep working; `foreach` binds to
the struct enumerator and allocates nothing. WinUI's equivalent walk
(`ScopedResources::TraverseVisualTreeResources`) is likewise a plain loop that allocates nothing, so
this moves Uno *towards* WinUI rather than away.

`ResourceResolver.TryVisualTreeRetrieval` was rewritten from `(x as DependencyObject)?.GetResource‑
Dictionaries(true)` + null check to `if (scope?.Target is DependencyObject owner)`, so the struct is
enumerated directly rather than through a nullable.

**Why this rather than the hoist in §5.6.** The rejected hoist changed *when* the scope is resolved,
which diverges from WinUI. This changes only *how the sequence is represented* — the resolution order,
laziness and short-circuiting are bit-for-bit the same — and it removes more allocation (−13.8 % vs
−6.8 %).

**Measurement** — `UnoPerfInt`, 3 runs of the full workload set in identical order before and after,
median. `layout.full-pass`, `grid.full-pass`, `tree.build` and `dp.set-get` are controls.

| Workload | Metric | Before | After | Δ |
|---|---|---:|---:|---:|
| `list.scroll-step` | alloc / step | 154,522 B | 133,264 B | **−13.8 %** |
| `list.scroll-step` | alloc / container | 6,718.4 B | 5,794.1 B | −13.8 % |
| `list.scroll-step` | time / step | 1.6573 ms | 1.6444 ms | −0.8 % (noise) |
| *control* `layout.full-pass` | alloc | 4,096,383 B | 4,095,809 B | ±0 |
| *control* `grid.full-pass` | alloc | 51,833 B | 51,833 B | ±0 |
| *control* `tree.build` | alloc | 21,929,730 B | 21,976,875 B | ±0 |
| *control* `dp.set-get` | alloc | 48,001 B | 48,001 B | ±0 |

21,258 B saved per scroll step. At roughly 64–80 bytes per state machine that is on the order of
**~270 iterator allocations per scroll step** — consistent with a few containers materialising per
step, each a templated control carrying dozens of theme references (WinUI's own comment notes
`ListViewItemPresenter` has 41+). *That last figure is an estimate from the allocation delta, not a
counted value.*

The layout and grid workloads are unmoved because their trees are dirtied in place rather than
re-entered — this cost is paid on **Enter**, which is what list virtualisation does constantly.

**Correctness.** Two runtime-test suites on Skia Desktop, each set-compared against a baseline build:
the 327-test resources/theming/list suite and the 937-test layout/rendering/framework suite —
**1,264 tests, 0 new failures, 0 newly passing** in both.

### L1 — a `{Binding}` source keeps every handler it was ever bound with (leak)

**Commit**: `fix(binding): Drop dead INotifyPropertyChanged subscriptions`
**File**: `src/Uno.UI/DataBinding/BindingPath.cs`

**Symptom.** A long-lived `INotifyPropertyChanged` source bound by short-lived elements accumulates
`PropertyChanged` subscriptions **forever**. Measured with a probe that creates a `TextBlock`, binds it
to a shared view model, adds it to the tree, removes it, and collects — the source's handler count
grew perfectly linearly and never dropped:

```
batch 1/5: subscribers=35    targets alive=1/35
batch 2/5: subscribers=60    targets alive=1/60
batch 3/5: subscribers=85    targets alive=1/85
batch 4/5: subscribers=110   targets alive=1/110
batch 5/5: subscribers=135   targets alive=1/135
```

`targets alive=1/135` is the important half: the bound elements themselves **are** collected (the 1 is
the most recent, still held by a local), so this is not element retention. What leaks is the
subscription — the delegate and its captured weak-reference wrapper — one per element ever bound, held
by the source for its entire lifetime.

This is measured by reading the source's own `PropertyChanged` invocation-list length, so it is
**deterministic** — no dependence on the GC actually collecting anything, unlike a `WeakReference`
leak test.

**Root cause.** `BindingPath.SubscribeToNotifyPropertyChanged` attaches a closure to the source and
returns an `IDisposable` that detaches it. The closure deliberately holds only a *weak* reference to
the value handler, which is why the target is collectable. But the subscription is removed **only** by
that disposable — and nothing disposes it when the target is simply garbage-collected. So the handler
stays attached, permanently, with a dead weak reference inside it.

**Fix.** Make it a self-purging weak event: when the handler is raised and finds its value handler has
gone, it detaches itself from the source. This is what WPF's `PropertyChangedEventManager` does when it
finds a dead listener. Detaching uses the same `dataContextReference` the disposable uses, so a source
that raises with a different `sender` is still handled correctly. The dead-check is hoisted above the
property-name filter so a raise for *any* property purges.

Unsubscribing during a raise is safe — delegate invocation lists are immutable, so the in-flight raise
completes over the list it started with.

**Measurement** — same probe, 135 bound-then-removed elements, before and after.

| Metric | Before | After |
|---|---:|---:|
| subscribers after 135 cycles | **135** (unbounded) | **1** (bounded) |
| subscribers between raises | 35 → 60 → 85 → 110 → 135 | 26, flat |
| subscribers after each raise | unchanged | **1** |
| cost per `PropertyChanged` raise | 2.873 µs | **0.455 µs** (**−84 %**) |

The raise cost is the second-order effect: every raise walks the whole invocation list, dead entries
included. −84 % is the figure *after only 135 cycles* — the gap grows without bound, because the
before-case list never stops growing while the after-case stays at the number of live bindings.

**Scope note.** Purging happens when the source next raises `PropertyChanged`, which is the standard
limitation of self-purging weak events (WPF has the same). A source that never raises still holds its
subscriptions — but such a source also never pays the raise cost, and the retained memory is a
delegate plus a wrapper, not the element.

**Correctness.** Runtime tests, Skia Desktop — `Given_BindingExpression`, `Given_BindingMemoryLeak`,
`Given_BindableFoundationStructs`, `Given_BindableNullableValueType`, `Given_NonFE_DataContextBinding`,
`Given_Frame_DataContext`, `Given_ResourceDictionary_DataContext`, `Given_xBind`,
`Given_XBind_NavigatedTo`, `Given_FrameworkElement_And_Leak`, `Given_ListViewBase`: **278 run, 270
passed, 11 skipped, 7 failed**, set-compared against a baseline build — **0 new failures, 0 newly
passing.**

**Harness note.** The `leakbind` probe is deliberately built on a *counted* signal rather than
`WeakReference` liveness, because GC-based leak assertions are unreliable on this machine. The generic
`leak` probe added alongside it snapshots the `Count` of every static collection in `Uno.UI` and
`Uno.UI.Composition` by reflection and reports any that grew across N add/remove cycles; run against
plain element trees it reports **no growth** (heap delta converges to 0 B/cycle by the third batch),
which is a useful negative result.

### F9 — every visual state materialises an empty trigger collection when read

**Commit**: `perf(vsm): Stop materialising empty state-trigger collections`
**Files**: `src/Uno.UI/UI/Xaml/VisualState.cs`, `src/Uno.UI/UI/Xaml/VisualStateGroup.cs`

**Symptom.** In the list-scroll profile, `Enumerator[StateTriggerBase]` and an
`ArrayPool<short>.Rent` were both near the top. The stack is unusual — a *getter* ending in a
`SetValue`:

```
ArrayPool<short>.Rent
DependencyPropertyDetailsCollection.TryGetPropertyDetails
DependencyObject.SetValue
VisualState.set_StateTriggers          <-- a setter, reached from...
VisualState.get_StateTriggers          <-- ...its own getter
VisualStateGroup.ExecuteOnTriggers
VisualStateGroup.OnOwnerElementChanged
```

**Root cause.** `VisualState.StateTriggers` is a lazily-materialising getter: if the DP holds no list it
creates a `DependencyObjectCollection<StateTriggerBase>`, subscribes to its `VectorChanged`, and writes
it back through `SetValue`. That is reasonable for a *caller that intends to add a trigger* — but all
four consumers in `VisualStateGroup` only ever **read**:

| Site | What it does |
|---|---|
| `ExecuteOnTriggers` | iterates the triggers (three call sites: owner changed / loaded / unloaded) |
| `HasStateTriggers` | checks `Count > 0` |
| `GetActiveTrigger` (×2) | iterates the triggers |

So every visual state of every templated control materialised a collection, a delegate subscription, a
`DependencyPropertyDetails` entry and a pooled-array rent — to represent *no triggers at all*. Only
adaptive/custom-trigger states ever have any, which in practice is almost none of them.

**Fix.** Add `VisualState.StateTriggersOrDefault`, which reads the DP without materialising, and use it
at all four read sites. Behaviour is identical: when no collection exists there are no triggers, so
each loop body would have executed zero times anyway, and `HasStateTriggers` would have returned false.
A later caller that genuinely wants to *add* a trigger still goes through `StateTriggers` and gets the
collection created lazily exactly as before.

**Measurement** — `UnoPerfInt`, 3 runs of the full workload set in identical order, median.

| Workload | Metric | Before | After | Δ |
|---|---|---:|---:|---:|
| `list.scroll-step` | alloc / step | 133,277 B | 121,814 B | **−8.6 %** |
| `list.scroll-step` | time / step | 1.6278 ms | 1.5791 ms | **−3.0 %** |
| *control* `layout.full-pass` | alloc | 4,096,259 B | 4,096,127 B | ±0 |
| *control* `grid.full-pass` | alloc | 51,833 B | 51,833 B | ±0 |
| *control* `tree.build` | alloc | 21,946,936 B | 21,942,484 B | ±0 |

**Correctness.** Runtime tests, Skia Desktop — `Given_VisualStateManager`, `Given_EventTrigger`,
`Given_Button`, `Given_CheckBox`, `Given_RadioButton`, `Given_ToggleSwitch`, `Given_HyperlinkButton`,
`Given_AppBarButton`, `Given_Style`, `Given_ListViewBase`: **221 run, 214 passed, 11 skipped, 7 failed**.
Every visual-state and control test passes; the only failures are the `Given_ListViewBase` set already
established as pre-existing in the F8 and L1 baselines.

---

### Cumulative effect on a virtualised list scroll (F8 + L1 + F9)

| Metric | Before F8 | After F9 | Δ |
|---|---:|---:|---:|
| alloc / scroll step | 154,522 B | 121,814 B | **−21.2 %** |
| alloc / realised container | 6,718 B | 5,296 B | **−21.2 %** |

### F10 — every `bool` DependencyProperty set boxes, despite a cached-box helper existing

**Commit**: `perf(uno-ui): Use the cached bool boxes in generated DP setters`
**Files**: `src/SourceGenerators/Uno.UI.SourceGenerators.Internal/DependencyObject/DependencyPropertyGenerator.cs`,
`.../Mixins/DependencyPropertyMixinGenerator.cs`

**Root cause.** `DependencyObject.SetValue` takes an `object`, so a `bool` is boxed at the call site —
24 bytes on **every set**. Uno.UI already keeps the two boxed instances (`Uno.UI.Helpers.Boxes`,
used by hand in a handful of places), but both DependencyProperty generators emitted the plain
`SetValue(XProperty, value)`, so every generated `bool` setter allocated.

**Fix.** Both generators now emit `SetValue(XProperty, Uno.UI.Helpers.Boxes.Box(value))` when the
property type is `bool`, and pass `value` through unchanged for every other type. This is automatic
for any `bool` DP added later.

**Why it cannot change behaviour.** `DependencyObject.AreDifferent` (`DependencyObject.Store.cs:2313`)
takes the `newValue is ValueType` branch for a boxed `bool` and compares with `object.Equals` — a
*value* comparison. Change detection therefore never depended on box identity, so sharing the boxed
instances is invisible to it.

**Measurement** — `UnoPerfInt` `dp.bool-genDP`: 1,000 × toggling `Control.IsEnabled` (a
`[GeneratedDependencyProperty]` bool) on a `Button`, 3 runs, median.

| Metric | Before | After | Δ |
|---|---:|---:|---:|
| alloc / set | 72.0 B | 48.0 B | **−33.3 %** |

Byte-identical across all runs in each configuration: exactly **24 B per set**, one box. The
remaining 48 B is the change-notification path, unrelated to this fix.

**Correctness.** `DependencyPropertyGenerator` golden tests: **6 run, 6 passed.** Runtime tests,
Skia Desktop — `Given_Control`, `Given_Button`, `Given_CheckBox`, `Given_ToggleSwitch`,
`Given_RadioButton`, `Given_VisualStateManager`, `Given_UIElement`, `Given_FrameworkElement`:
**684 run, 658 passed, 2 skipped, 26 failed**, set-compared against a baseline build —
**0 new failures, 0 newly passing.**

> ⚠️ The wider `Uno.UI.SourceGenerators.Tests` suite is **not** a usable signal in this environment:
> it fails 340/482 on the **unmodified** tree and its pass count varies run to run (110, 128, 129 on
> identical inputs), because those tests load `src/*/bin/**` directly rather than through project
> references. The 6 DependencyProperty golden tests above were checked individually instead.

---

## 5. Investigated and deliberately *not* done

Recording these so the next person does not re-derive them.

### 5.1 Caching the text layout across **measure** (the other half of F1)

**Tracked as [#24212](https://github.com/unoplatform/uno/issues/24212).** The prerequisite
invalidation gap #4 is filed separately as a live bug: [#24210](https://github.com/unoplatform/uno/issues/24210).

`TextBlock.MeasureOverride` still re-shapes unconditionally. WinUI does not: it keeps a *content*
dirty flag on the text layout node (`m_pPageNode->IsMeasureDirty()`, set by
`CTextBlock::InvalidateContentMeasure()`) that is separate from the element's own measure-dirty
flag, so a `TextBlock` re-measured for an unrelated reason reuses its layout
(`BlockNode::CanBypassMeasure`). Uno conflates the two, and this is the **single largest remaining
allocator in a layout pass**.

The obvious implementation — set a flag in `TextBlock.InvalidateTextBlock()` (the funnel ~17
property handlers already call) and clear it in `ParseText` — was audited against every input of the
`UnicodeText` constructor and found **unsafe**. Five invalidation sources bypass that funnel:

| # | Source | Consequence if cached |
|---|---|---|
| 1 | `TextBlock.cs:1745` — `IFontCacheUpdateListener.Invalidate() => InvalidateMeasure()`. The async **fallback**-font path removes its listener *before* firing. | A skipped re-parse is **permanent**. Highest risk: emoji / CJK / Arabic / symbol fallback on WASM and Linux. |
| 2 | `FrameworkElement.cs:157` — `FlowDirectionProperty` is `AffectsMeasure \| Inherits` with **no** changed callback, yet `FlowDirection` is a direct `UnicodeText` ctor argument. | Runtime RTL flips (including inherited from an ancestor, and `TextBoxView.SetFlowDirection`) go stale. |
| 3 | `CoreServices.cs:243` — the OS text-scale walk calls bare `element.InvalidateMeasure()`. | Accessibility text scaling stops taking effect. |
| 4 | `TextBlock.cs:1427` — `IsSpellCheckEnabled` is a plain property with **no invalidation at all** (written from `TextBoxCore.Input.cs:305`). | Spell-check underlines do not refresh. |
| 5 | `TextBlock.cs:478` — `IsTextScaleFactorEnabledProperty` has no callback; it is covered only transitively through inline inheritance, so an *empty* `TextBlock` is not covered. | Same as 3, for the empty case. |

Existing runtime tests cover only gaps 1 and 3
(`Given_TextBlock.Check_FontFallback_Shaping2`,
`Given_TextBlock_TextScaling.When_TextScaleFactorChanges_Nested_Inlines_Are_Invalidated`).
Gaps 2, 4 and 5 have **no** coverage — every `FlowDirection` test sets it before load.

The guard would also have to be the full `NeedsReparseForArrange` predicate added in F1, not just
width + alignment, because `availableSize.Height` drives `isEarlyLastLine`.

Two further inputs invalidate **nothing today either**, so they are pre-existing rather than
regressions — but caching makes recovery strictly harder, because today *any* layout pass repairs
them and afterwards only a content change would:

- `CultureInfo.CurrentUICulture` (`UnicodeText.cs:1680`) opens the ICU line- and word-break
  iterators, so changing it at runtime changes wrapping for Thai / Japanese / Khmer.
- `FeatureConfiguration.Font.SymbolsFont` / `DefaultTextFontFamily` / `IgnoreTextScaleFactor` /
  `TextScaleFactor` / `MaximumTextScaleFactor` (`FeatureConfiguration.cs:159-213`) are plain statics
  documented as startup configuration.

**Minimum set of invalidation fixes required before the flag is sound:**

| # | Change | Why |
|---|---|---|
| 1 | `TextBlock.cs:1745` — `IFontCacheUpdateListener.Invalidate()` must call `InvalidateTextBlock()`, not `InvalidateMeasure()` | **Non-negotiable.** The listener de-registers itself *before* firing (`UnicodeText.cs:1633, 1659`) and only re-registers inside the `UnicodeText` constructor, so a missed re-parse is permanent. |
| 2 | Register a changed callback for `FlowDirectionProperty` on `TextBlock` (ctor, `TextBlock.cs:1381-1409`) rather than adding one to `FrameworkElement` | Covers both the element's own flip and the inherited one. `RichTextBlock` needs the same. |
| 3 | `CoreServices.cs:243-246` — the `TextBlock or RichTextBlock` branch must route through content invalidation, not bare `InvalidateMeasure()` | The same walk already calls `InvalidateTextScaleFontInfo()` at `:263`, so it is one branch. |
| 4 | `TextBlock.cs:1427` — back `IsSpellCheckEnabled` with a setter that calls `InvalidateTextBlock()` on change (caller: `TextBoxCore.Input.cs:305`) | Also fixes the live bug below. |
| 5 | `TextBlock.cs:478-484` — give `IsTextScaleFactorEnabledProperty` a changed callback | Only needed for the *empty*-text case; with ≥1 inline, inheritance already rescues it via `Inline.cs:103-108`. |

**On the cached path** the implementation must still take `desiredSize` from
`_lastParsedTextCreationValues.outSize` (which already carries `+ CaretThickness` for TextBox-owned
blocks, `TextBlock.cs:1493`) and still run the layout-rounding block at `:1452-1468`.

**Tests to write first** (none of these exist — every current `FlowDirection` test assigns before
load): mutate `TextBlock.FlowDirection` after load; the same via an ancestor's `FlowDirection`
(inherited path); toggle `TextBox.IsSpellCheckEnabled` after load; measure at `(w, 1000)` then
`(w, small)` and assert truncation kicks in; toggle `IsTextScaleFactorEnabled` on an empty
`TextBlock` after load.

⚠️ **One existing test will need re-checking:** `Given_TextBlock.When_FontFamily_Changed`
(`Given_TextBlock.cs:788`) calls bare `SUT.InvalidateMeasure()` in its retry loop, which becomes a
no-op for re-parsing once the flag exists.

**Conclusion:** worth doing, but it is its own piece of work — land the five invalidation fixes
first, each with a test, then add the flag. Note that **#4 looks like a live bug today**: nothing
re-measures when `IsSpellCheckEnabled` changes, so squiggles only appear once some unrelated
invalidation happens to trigger a re-parse.

**Calibrate the expected win first.** `UIElement.DoMeasure` (`UIElement.Layout.crossruntime.cs:212-227`)
already short-circuits when the element is not measure-dirty *and* the available size is unchanged,
so `MeasureOverride` never runs in that case. The gain therefore comes only from TextBlocks made
measure-dirty for a **non-content** reason, plus available-size changes that do not affect the text
layout — which is the same case F1 already models for arrange.

### 5.2 `DependencyPropertyDetailsCollection`'s pooled arrays are rarely returned

`TryGetPropertyDetails` rents its `short[]` offset map and `DependencyPropertyDetails?[]` entries
from pools (`DependencyPropertyDetailsCollection.cs:151, 177`) and returns them from `Dispose()`
(`:217-227`). Elements are normally **garbage-collected, not disposed**, so in the element-creation
profile `ArrayPool<short>.Rent` is a top allocator — the pool drains and every rent allocates, while
still rounding up to a bucket size. Either elements need deterministic disposal on unload or the
pooling is counter-productive here. Left alone: it is an architectural call, not a local fix.

### 5.3 Micro-optimisations rejected for lack of a measurable win

- Hoisting `GetScaleFactorForLayoutRounding()` out of the `Grid.ValidateDefinitions` loop. The
  scale-resolution chain (`RootScale.GetRootScaleForElement` → `VisualTree.GetContentRootForElement`
  → `ManagedWeakReference` / `ConditionalWeakTable`) is ~2 % of CPU samples in the grid workload,
  but the hoist's share of that is below this harness's noise floor, so it cannot be honestly
  claimed. Recorded, not committed.
- The remaining grid arrange cost is diffuse (`InnerArrangeCore` 6.6 %, `UIElement.Arrange` 5.7 %,
  `Grid.ArrangeOverride` 5.4 % of samples) with no single dominant hot spot — the sign of an already
  reasonably tuned path.

### 5.4 Pre-existing runtime-test failures on this branch

Not caused by this work — verified by re-running each against the tree **before** the first fix.
Listed here because they make a clean baseline harder for the next person:
`Given_TextBlock.When_Inlines_Transitively_Change`, `Given_TextBlock.When_IsTextSelectionEnabled_CRLF`,
`Given_TextBox.When_Caret_Line_Straddles_Viewport_Edge_Grippers_Are_Hidden`,
`Given_TextBox.When_Copy_Paste` (clipboard, flaky in both directions),
`Given_TextBox.When_Multiline_Pointer_TripleTap_With_Wrapping`, `Given_Border.Border_AntiAlias`,
`Given_Grid.When_Child_Added_Measure_And_Visible_Arrange`,
`Given_Grid.When_ColumnDefinition_Width_Changed`,
`Given_GridLayouting.When_Grid_{Row,Column}Collection_Changes`,
9 × `Given_Rectangle.When_StrokeThickness_*`, 4 × `Given_UIElement.When_*Clip*` /
`When_TransformToVisual_*` / `When_*Nesting*`, 4 × `Given_FrameworkElement_EffectiveViewport.EVP_When_ConstrainedInNonScrollableSV`,
`Given_FrameworkElement_And_Leak.When_Add_Remove(CommandBarFlyout_Leak)`.
Some are plausibly environment-specific (clipboard access, screenshot capture) rather than genuine
product failures.

### 5.5 The **full** runtime-test suite cannot complete locally — and that predates this work

**Tracked as [#24211](https://github.com/unoplatform/uno/issues/24211).**

Attempting an unfiltered `--runtime-tests` run of `SamplesApp.Skia.Generic` on Windows crashes the
process:

```
Fatal error. 0xC0000005
   at SkiaSharp.SkiaApi.sk_refcnt_safe_unref(IntPtr)
   at SkiaSharp.SKObjectExtensions.SafeUnRef(SkiaSharp.ISKReferenceCounted)
   at SkiaSharp.SKObject.DisposeNative()
   at SkiaSharp.SKSurface.Dispose(Boolean)
   at SkiaSharp.SKNativeObject.Finalize()
   at System.GC.RunFinalizers()
```

— an access violation on the **finalizer thread**, unref-ing an `SKSurface` whose native peer is
already gone.

Because F3/F5 changed native-path lifetimes (F5 adds an `SKPath.Dispose()`), this had to be ruled
out rather than assumed. It was: the pre-fix tree (`ea3418a53ca`, checked out over
`src/Uno.UI` + `src/Uno.UI.Composition` and rebuilt) crashes **identically** — same
`0xC0000005`, same stack, same SIGSEGV exit 139 — after 24 tests, against 28 for the fixed tree.
The difference in count is finalizer timing, not behaviour. No `SKSurface` is created or disposed by
any code this work touches (`SKSurface` appears only in the platform renderers,
`RenderTargetBitmap`, `RetainedLayer`, `SvgImageSource` and `VulkanContext`).

**Consequence for this work's validation:** the evidence above rests on *filtered* suites — up to
**937 tests** per comparison, chosen to cover the subsystems each fix touches — and every one was
compared as a **set of failing test names** between a baseline build and a fixed build, not just by
pass count. A whole-suite before/after comparison was not achievable locally; CI remains the
verification for that.

This crash is worth its own investigation independently of this work.

### 5.6 Theme lookups during a layout pass — aligned with WinUI, but measurement-neutral

**Commit**: `fix(theming): Cache theme lookups across a layout pass, as WinUI does`
**Files**: `src/Uno.UI/UI/Xaml/Internal/CoreServices.cs`, `src/Uno.UI/UI/Xaml/DependencyObject.mux.cs`

Profiling a **virtualised `ListView` scroll** (2,000 items, 23 realised containers) put
`<GetResourceDictionaries>d__219` — the `yield return` iterator behind the ancestor resource walk — at
the top of the allocation profile, at 5.4 %, reached through
`FrameworkElement.OnFwEltLoading → UpdateResourceBindings → UpdateAllThemeReferences → UpdateThemeReference`
as containers materialise.

**What was tried first, and rejected.** Hoisting the ancestor walk out of the per-property loop in
`UpdateAllThemeReferences` measured **−6.8 % allocations and −4.1 % time** on the scroll workload.
It was reverted: WinUI's `CDependencyObject::UpdateAllThemeReferences` (`Theming.cpp:308-334`)
deliberately calls `UpdateThemeReference(propertyIndex)` per property, each re-resolving through
`FindNextResolvedValueNoRef`, so WinUI observes a resource scope that changes *during* the loop and a
hoisted snapshot would not. A modest win is not worth a semantic divergence in this subsystem.

**What WinUI does instead** is cache the *resolved values*, not the dictionary chain —
`ThemeWalkResourceCache`, keyed on `(dictionary, theme, key)`. Its header names three scenarios, and
scenario 2 is exactly this one:

> *"During UpdateLayout - a lot of elements can enter the tree due to applying templates, and they
> will do a theme resource lookup as part of entering the tree. This spares ~69k ResourceDictionary
> lookups."*

Uno already has a faithful port of that cache, including the eviction hook that makes it safe across
the layout callout (`ResourceDictionary.cs:249, :494` ↔ MUX `Resources.cpp:207, :335`). But only
**scenario 1** was wired: the theme-change walk. Scenario 2 was not — `CoreServices.OnTick` opened no
caching session, and the tree-Enter path passed `cache: null`. (Scenario 3, AppBarButton's VSM update,
is still `TODO:Uno` against issue #19381.)

**Fix.** Port the two missing pieces: open a caching session around the per-frame layout pass
(MUX `xcpcore.cpp:6422-6426`, which wraps `pLayoutManager->UpdateLayout` for exactly this reason), and
thread the core's cache through the Enter path so the pinned-dictionary refresh can consult it —
WinUI reads the same cache off the core in `CThemeResource::RefreshValue` (`ThemeResource.cpp:81, 96`).

**Measurement — honest result: no win here.** With the workload re-driven through the dispatcher tick
so the layout pass runs the way it does in a real app, 5 runs each, median:

| Metric | Before | After | Δ |
|---|---:|---:|---:|
| time / scroll step | 1.7219 ms | 1.6996 ms | −1.3 % (noise) |
| alloc / scroll step | 152,744 B | 152,748 B | ±0 |

Instrumenting the cache showed it **is** working — **224 sessions, 1,353 hits, 37 misses, 37 stores**
across a 200-step scroll, a 97 % hit rate — but that is only ~7 avoided dictionary lookups per frame,
nowhere near WinUI's 69k. The reason is structural: Uno resolves most theme references through the
ancestor walk (Phase A), which neither framework caches, and reaches the cached pinned-dictionary
refresh (Phase B) comparatively rarely because Uno pins the dictionary at parse time.

**So this is landed as a WinUI-alignment fix, not as a performance improvement**, and is deliberately
recorded here rather than in §4 — §4 lists only changes with a measured win. It closes a real gap
(Uno implemented one of WinUI's three caching scenarios), it is measurement-neutral today, and it
becomes worth more as resolution moves through `RefreshValue`.

**Correctness.** Runtime tests, Skia Desktop — `Given_ThemeResource`, `Given_ElementTheme`,
`Given_ElementTheme_Resolution_Regression`, `Given_FrameworkElement_ThemeResources`,
`Given_MergedAppResources_ThemeResource`, `Given_CheckBox_ThemeResource_Regression`,
`Given_Theme_Materialization`, `Given_ResourceDictionary`, `Given_ResourceResolver`, `Given_Style`,
`Given_ListViewBase`, `Given_ListViewBase_Items`, `Given_AccessibleListView`: **327 run, 319 passed,
13 skipped, 7 failed**, set-compared — **0 new failures, 0 newly passing**. All 7 failures reproduce on
the baseline (verified by a separate rebuild-and-rerun of those tests).

**Harness note.** The `list.scroll-step` workload added for this: a `ListView` over 2,000 items with a
two-`TextBlock` `DataTemplate`, scrolled 137 px per step (deliberately not a multiple of the item
height, so every step straddles items and forces recycling). It requires `XamlControlsResources`
merged into `Application.Resources` — without the Fluent dictionaries a templated control has no
default style and the list never virtualises — and it must be driven through the dispatcher tick,
because that is where the framework's per-frame work happens.

### 5.7 New angles opened, and what they did *not* yield

Two subsystems were profiled for the first time. Both are characterised below rather than fixed —
recording them so the next person starts from the numbers rather than from scratch.

**Pointer input** (`input.pointer-move`: mouse moves injected across a 3,109-element tree)

| Metric | Value |
|---|---:|
| time / pointer move | 0.203 ms |
| alloc / pointer move | 974 B |

A CPU profile puts roughly half the cost in hit-testing: `SearchDownForTopMostElementAt` 20.4 % of
samples, `Matrix3x2Extensions.Transform` 14.1 %, `CompositionSpriteShape.HitTest` 8.1 %,
`UIElement.GetTransform` 7.0 %. The descent *does* prune — a child outside the clipped bounds returns
before recursing — but each element visited still pays a transform and a bounds intersection first,
and every `Border` reaches a native `sk_path_contains`. No single fixable hot spot: the cost is spread
across matrix math and native path containment.

Two false leads worth recording. `Monitor.Enter_Slowpath` showed at 4.2 % and looked like lock
contention on a per-event path; attributing it by caller chain showed it is entirely workload
*setup* (theme bindings, `TextBlock.ParseText`), not pointer routing. And a large share of the 974 B
is `InjectedInputMouseInfo.ToEventArgs`, i.e. the *injector*, not the routing a real app performs —
so that figure overstates real-app cost.

**Template materialisation** (`template.build-page`: 25 `Button`s + 25 `CheckBox`es created,
templated, laid out and torn down — 301 elements)

| Metric | Value |
|---|---:|
| time / page | 63.3 ms |
| alloc / page | 16,785,906 B |
| alloc / element | 55,767 B |

This is navigation and startup cost, and it is large — but the profile is **flat**: nothing above
2.1 %, and the top entries (`ContainerVisual`, `DependencyPropertyDetailsCollection`, `Binding`,
`Dictionary<DependencyProperty, ManagedWeakReference>`, the `short[]` details pool) are the intrinsic
per-element cost of the property system. Reducing it is an architectural question, not a hot-spot fix.

### 5.8 `Enum.HasFlag` — measured, and *not* worth changing

An allocation profile appeared to attribute allocations to `InjectedInputMouseOptions` inside a method
full of `HasFlag` calls, which suggested the classic "HasFlag boxes" sweep across the 172 call sites in
`Uno.UI`, `Uno.UI.Composition` and `Uno.WinRT`.

A BenchmarkDotNet micro-benchmark settled it — eight flag tests per operation:

| Method | Mean | Allocated |
|---|---:|---:|
| `HasFlag` | 1.014 ns | **0 B** |
| bitwise `&` | 0.266 ns | 0 B |

`Enum.HasFlag` is intrinsified by the JIT on .NET 10 and **allocates nothing**. The absolute
difference is ~0.09 ns per test. The sweep would have touched 172 sites for no allocation win and an
unmeasurable time win, so it was not done. The profiler's type attribution was misleading here.

### 5.9 Hand-written `bool` DP setters still box — follow-up to F10

F10 fixed the two *generators*. **151 hand-written `bool` dependency-property setters** in `Uno.UI`
still call `SetValue(XProperty, value)` directly and box on every set, including hot ones on the base
types: `UIElement.IsHitTestVisible` and `UIElement.IsTabStop` are both hand-written.

Measured directly against a pre-boxed `SetValue` call, the cost is the same **24 B per set** F10
removes. It was left out because 151 mechanical edits is a large diff for a per-set win that is not
paid per frame — worth doing as its own sweep, with `Boxes.Box(value)`, if someone wants it.

| Property | Kind | alloc / set |
|---|---|---:|
| `Control.IsEnabled` | generated (fixed by F10) | 48 B |
| `UIElement.IsTabStop` | hand-written | 24 B |
| `UIElement.IsHitTestVisible` | hand-written | 72 B |
