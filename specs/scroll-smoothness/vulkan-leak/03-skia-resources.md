# 03 — Skia-side resource audit, Android Vulkan path

Scope: Skia/composition resources that grow under sustained rendering on the **Android Vulkan**
host (`UnoSKVulkanView` → `VulkanContext` → `CompositionTarget.OnNativePlatformFrameRequested`).
GL (`UnoSKCanvasView`), swapchain/command-buffer internals and driver-side objects are out of scope
here (covered by the other audits).

Every claim below cites `file:line` from the worktree at
`D:/Work/uno-worktrees/scrollsmooth`. Anything I could not confirm from source is marked
**UNVERIFIED**.

Evidence class: **code review by inspection only**. No build, no device run, no heap dump.

---

## Headline

The Skia layer has exactly **one unbounded, permanent native leak**:
`Visual._picture` / `Visual._childrenPicture` are raw `IntPtr` `SkPicture` references with **no
`Dispose`, no finalizer and no unref on visual destruction**. Every `Visual` that dies (list item
recycled, page navigated away, template torn down) permanently leaks its recorded `SkPicture` and
everything that picture retains — including `SkImage`s, which pin **GPU textures that the Skia
resource cache can then never purge**.

Everything else I found is either (a) freed correctly, or (b) freed only by the GC finalizer, which
is the classic "native memory invisible to the GC" pattern — real pressure, but self-limiting once a
GC runs. Given the reported managed heap stayed modest (39→109 MB), finalizer-backed churn alone is
unlikely to be the whole story; the permanent leak plus an **unbounded, never-purged GRContext
resource cache** is the combination that matches "per-process mappable memory exhausted while the
device had 1.4 GB free".

---

## 1. Is the GrContext told to purge? Where is the budget set?

**Nowhere on this path. No limit, no purge, no low-memory hook.**

- The context is created with no options:
  `src/Uno.UI/Vulkan/VulkanContext.skia.cs:115-127` builds a `GRVkBackendContext` and calls
  `GRContext.CreateVulkan(ctx)` (line 125). No `GRContextOptions` overload, no
  `SetResourceCacheLimit`, no `SetResourceCacheLimits`.
- Repo-wide, the **only** call to any cache-limit or purge API is
  `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Rendering/WebGlBrowserRenderer.cs:32`
  (`_context.SetResourceCacheLimit(ResourceCacheBytes)`, const `256 * 1024 * 1024` at line 16).
  Grep for `ResourceCacheLimit|PurgeResources|PurgeUnusedResources|PurgeUnlockedResources` over
  `src/**/*.cs` returns that single hit.
- No `OnTrimMemory` / `LowMemory` handler exists anywhere in
  `src/Uno.UI.Runtime.Skia.Android/` or `src/Uno.UI/` (grep returns nothing), so Android's
  memory-pressure callbacks never reach Skia.

Per-frame the Vulkan path does only:

```
src/Uno.UI/Vulkan/VulkanContext.skia.cs:191   _grContext.ResetContext();
src/Uno.UI/Vulkan/VulkanContext.skia.cs:203   _cachedSkSurface.Canvas.Flush();
src/Uno.UI/Vulkan/VulkanContext.skia.cs:204   _grContext.Flush();
```

**Ruled out — `Flush()` does submit.** I disassembled `SkiaSharp.dll` 4.148.0
(`src/Directory.Build.targets:77` pins `SkiaSharpVersion` = 4.148.0;
package at `D:\Packages\NuGet\skiasharp\4.148.0\lib\net10.0-android36.0\SkiaSharp.dll`).
`GRContext.Flush()` IL is `ldarg.0; ldc.i4.1; ldc.i4.0; call Flush(bool,bool); ret`, and
`Flush(bool submit, bool sync)` branches to `SkiaApi.gr_direct_context_flush_and_submit` when
`submit` is true. So the parameterless `Flush()` used here **is** flush-and-submit — unsubmitted
command buffers accumulating in the GrVkResourceProvider is **not** the mechanism.

**Consequence.** The GPU resource cache runs at Skia's compiled-in default budget for the whole
session and is only ever trimmed by Skia's own LRU when that budget is hit. Two problems on Adreno:

1. The exact default budget value is **UNVERIFIED** (it lives in Skia's C++, not in this repo).
   Whatever it is, it is a *byte* budget, not a *mapping-count* budget — and the failing allocation
   (`sharedmem_gpumem_alloc: mmap failed errno 12` for a 128 KB allocation) is consistent with the
   per-process mapping limit (`vm.max_map_count`) rather than with byte exhaustion, since lmkd
   reported ~1.4 GB free. A cache full of many small kgsl allocations reaches that limit long
   before it reaches a byte budget.
2. LRU purging **cannot evict a resource that is still referenced**. A GPU texture referenced by a
   leaked `SkPicture` (finding #2) is not "unlocked" and will never be purged, however tight the
   budget is set.

---

## 2. FramePicture: retain / release / OnPipelineReleased across a frame

**Verdict: the ref-counting is balanced. Producing frames faster than they are presented does *not*
leak a picture.** There is exactly one unbounded path, and it is the deliberate `_publicized`
escape hatch.

File: `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs`.

Protocol (`FramePicture`, lines 501-554):

- `Retain()` (511) / `Release(bool pictureAccessed)` (519) move `_retainCount`;
  `OnPipelineReleased()` (536) latches `_releasedByPipeline`.
- `ShouldDispose()` (553) = `!_disposed && !_publicized && _releasedByPipeline && _retainCount == 0`.

Per frame, UI thread — `Render()` (110-198):

1. line 131 `new FramePicture(picture)` — count 0, pipeline-released false.
2. lines 135-155 under `_frameGate`: read `previousFrame` from `_lastRenderedFrame`, install the new
   frame at 147.
3. lines 159-162: if the slot held a previous frame, `prev.picture.OnPipelineReleased()`.
4. line 195 `OnFramePictureRecorded(this, framePicture)` → 436-450: `Retain()` the new picture (count
   1) and `Release(pictureAccessed: false)` the picture it evicts from `_latestFrames` (441).

Per present, render thread — `Draw()` (221-330):

5. lines 232-241 under `_frameGate`: **borrow** the frame and set `_lastRenderedFrame = null` (238).
6. line 310 `ReturnFrame(...)` → 412-434: if the slot is still null, put the frame back (421);
   otherwise a newer frame won the slot, so `frame.picture.OnPipelineReleased()` (433) and the
   damage snapshot is recycled (428).

Race analysis — production outrunning presentation:

- **Draw borrows A, then Render produces B.** Render sees `previousFrame == null` (the slot was
  emptied at 238), so it does **not** pipeline-release anything; `OnFramePictureRecorded` releases A
  from `_latestFrames` (count → 0) but `A._releasedByPipeline` is still false, so A is **not**
  disposed. Draw then calls `ReturnFrame(A)`; the slot now holds B, so A gets
  `OnPipelineReleased()` → count 0 + pipeline-released → **disposed**. Correct.
- **Draw borrows A, Render produces B then C.** At C's `Render`, `previousFrame == B`, so line 161
  pipeline-releases B while B still carries its `_latestFrames` retain (not disposed); then line 195
  releases B from `_latestFrames` → **disposed**. A is disposed by `ReturnFrame` as above. Correct.
- The ordering (`OnPipelineReleased` at 161 *before* `Retain` of the new picture at 438) never
  double-disposes because `ShouldDispose` is guarded by `_disposed` and evaluated under `_gate`.

`RaiseRendering` (455-484) retains every picture into a local array (464) and releases all of them
in a `finally` (479-483). Balanced.

`OnTargetUnregistered` (486-493) releases the target's cached picture. Bounded: `_latestFrames`
holds at most one picture per live target.

### The one unbounded path: `_publicized`

`Release(pictureAccessed: true)` latches `_publicized` (525), and `ShouldDispose` (553) then returns
false **forever** — the `SKPicture` is never `Dispose()`d and is reclaimed only by SkiaSharp's
finalizer. `pictureAccessed` comes from `RenderingEventArgs.FrameDataAccessed`
(`src/Uno.UI/UI/Xaml/Media/RenderingEventArgs.cs:24-31`), which latches the moment **any**
`CompositionTarget.Rendering` subscriber reads the `FrameData` property.

This matters under scrolling specifically:

- Any live `Rendering` subscriber sets `_isRenderingActive` (89-97), which makes `Render` call
  `RequestNewFrame()` **every frame** (164-167) — continuous frame production for the duration.
- Uno itself subscribes during a scroll: `GestureRecognizer.Manipulation.InertiaProcessor.cs:381`
  (inertia/fling) and `Repeater/BuildTreeScheduler.cs:90` (incremental container realisation).
  Neither of those handlers reads `FrameData`, so they do **not** publicize — but an app or sample
  handler that does will convert every frame's root picture into finalizer-only garbage.

Also per raise: `RaiseRendering` allocates a `FramePicture[]` (459) and a `List<...>` (460) — managed
only, trivial.

---

## 3. `_picture` / `_childrenPicture`: any overwrite without unref?

**No.** All four mutation sites in `src/Uno.UI.Composition/Composition/Visual.skia.cs` unref first:

| Site | Lines | Behaviour |
|---|---|---|
| `InvalidatePaint` | 236-240 | unref then zero `_picture` |
| `InvalidateParentChildrenPicture` | 250-253 | unref then zero `parent._childrenPicture` |
| `PaintStep` repaint | 497-499 | `sk_refcnt_safe_unref(visual._picture)` **before** `visual._picture = picture` |
| `RenderChildrenStep` cache install | 577-589 | unrefs the old `_childrenPicture` (581) before assigning (584); unrefs the *new* picture instead (588) when the subtree was invalidated mid-render |

The short-circuit in `InvalidateParentChildrenPicture` (248, "stop at the first ancestor that already
has `ChildrenSKPictureInvalid`") is safe: setting that flag always zeroes `_childrenPicture` in the
same loop body, and `_childrenPicture` is only ever assigned when the flag is *clear* (577). So
`flag set ⇒ _childrenPicture == IntPtr.Zero` is an invariant and nothing is skipped.

### But: nothing releases them when the Visual dies — this is the leak

`_picture` and `_childrenPicture` are declared as raw handles at
`src/Uno.UI.Composition/Composition/Visual.skia.cs:53-54`. They have:

- **no finalizer** on `Visual` (there is none in `Visual.cs` / `Visual.skia.cs`);
- **no `DisposeInternal` override** — grep for `DisposeInternal` across
  `src/Uno.UI.Composition/` and `src/Uno.UI/` returns overrides only in
  `CompositionEffectBrush`, the four geometry types, `SkiaAcrylicBrush` and
  `VisualInteractionSource`. **`Visual` is not among them.**
- The base implementation `CompositionObject.DisposeInternal`
  (`src/Uno.UI.Composition/Composition/CompositionObject.cs:307-318`) only calls
  `StopAllAnimations()`. It never touches Skia handles.
- `~CompositionObject()` (29-32) → `Dispose()` (305) → that same animation-only path.
- Nothing in `Uno.UI` ever calls `Dispose()` on a tree `Visual`: grep for
  `Visual.Dispose()|_visual.Dispose|.Visual.Dispose` over `src/Uno.UI/` +
  `src/Uno.UI.Composition/` finds only `AnimatedVisualPlayer.mux.cs:275` (an `IAnimatedVisual`,
  unrelated).

So: **when a `Visual` becomes garbage, its recorded `SkPicture` reference count never drops.** That
is a permanent per-process native leak of the picture, its `SkRecord` op buffer, and every object
the picture holds a ref to — `SkPaint`, `SkShader`, `SkTextBlob`, and `SkImage`. A pinned `SkImage`
in turn pins its GPU texture in the (unbudgeted, never-purged — see §1) resource cache.

**Cadence:** per destroyed `Visual`. Under sustained scrolling in SamplesApp with virtualized lists,
that is every recycled container and every torn-down item template — a rate proportional to scroll
distance, which is exactly "tens of seconds of sustained touch scrolling".

**Side note (managed, secondary).** `CompositionObject.DisposeInternal` (309-317) is reached from the
finalizer thread, where `DispatcherQueue.Main.HasThreadAccess` is false, so it does
`TryEnqueue(StopAllAnimations)` — an instance-method delegate that **resurrects the object** and
parks it in the main dispatcher queue until the UI thread drains it. Every finalized Visual takes
this path. It does not free the native pictures either way, but it does mean Visual garbage
survives an extra collection cycle and piles work onto an already-saturated UI thread during a
scroll.

---

## 4. Per-frame `SKSurface` / `SKImage` / `SKPicture` created without disposal

### Correctly freed — not leaks (stating so explicitly rather than padding the list)

- **Swapchain `SKSurface`.** Created once per size in `EnsureCachedSkiaSurface`
  (`src/Uno.UI/Vulkan/VulkanContext.skia.cs:226-254`, early-returns at 228 when already present) and
  disposed together with its `GRBackendRenderTarget` in `DisposeCachedSkiaSurface` (256-267), which
  is called from `Resize` (143), `ResizeRenderImage` (168) and `Dispose` (368). No per-frame surface.
- **`RetainedLayer`** (`src/Uno.UI/Helpers/RetainedLayer.skia.cs`) recreates its surface only on a
  size change (20-29) and disposes the old one (22). It is **not used on this path at all** — the
  only references are `Win32WindowWrapper.Rendering.OpenGl.cs:218` and
  `WebGlBrowserRenderer.cs:24`. Irrelevant to Android Vulkan.
- **Root frame `SKPicture`.** `SkiaRenderHelper.RecordPictureAndReturnPath`
  (`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:36-53`) reuses a static `SKPictureRecorder` (21) and
  returns one picture per frame from `EndRecording()` (50). Ownership passes to `FramePicture`, which
  disposes it deterministically (§2) unless publicized.
- **Per-visual `_picture` churn.** `PaintStep` records into the shared static `_recorder`
  (`Visual.skia.cs:47`, used at 492/497) and unrefs the superseded picture (498). Net zero for a
  living visual.
- **Damage snapshot paths.** Pooled in `_damageSnapshotPool`
  (`CompositionTarget.Rendering.skia.cs:69`, popped at 144, pushed back at 153 and 428). No
  allocation after warm-up.
- **Clip scratch paths.** `Visual._pathPool` (`Visual.skia.cs:26`) allocate/free pairs at 407-410,
  432-436, 601-602, 650-651, 806-807. Pooled.
- **`DamageRegion`** (`src/Uno.UI.Composition/Composition/DamageRegion.skia.cs`) explicitly reuses its
  builder and path and resets rather than recreating (comment at 84-85, `Reset` 82-92). The
  `Detach()` at 104 is wrapped in `using`.
- **`SkiaAcrylicBrush._filter`** is cached by bounds and the previous one disposed
  (`SkiaAcrylicBrush.skia.cs:182-189`, `_cachedBounds` assigned at 244).
- **`ShadowPathAccumulator`** disposes all of its paths (`ShadowPathAccumulator.skia.cs:35-47`) and is
  used with `using var` (`Visual.skia.cs:692`).
- **`ClippedRelativeLayout.Path` setter** (`ApplicationActivity.cs:502-523`), invoked *every frame*
  from `UnoSKVulkanView.cs:217`, calls `value.ToSvgPathData()`. IL check of SkiaSharp 4.148.0 shows
  `SKPath.ToSvgPathData` wraps its native `SKString` in try/finally + `Dispose`, so the native string
  is freed. Cost is one native alloc/free + one managed string per frame — churn, not a leak. With
  no native elements present the SVG text is `""` and the Android `Path` is never rebuilt.

### Not freed — finalizer-only (native memory the GC cannot see)

| # | What | Where | Cadence | Why not freed |
|---|---|---|---|---|
| 4a | `SKPictureRecorder` for the non-analytic drop-shadow fallback | `Visual.skia.cs:447` (`var recorder = new SKPictureRecorder();`) — the recorded picture *is* unref'd at 466, the recorder is not | per frame, per shadow-casting visual whose analytic silhouette fails (`TryRenderAnalyticShadow` returns false at 439) | no `using`, no `Dispose()`; reclaimed only by SkiaSharp's finalizer |
| 4b | `SKPictureRecorder` for the picture-collapsing cache | `Visual.skia.cs:552` | per `_childrenPicture` (re)build, **not** per frame — line 533 short-circuits on a live cache, and during an active scroll `_framesSinceSubtreeNotChanged` is reset to 0 every frame (396) so the 50-frame threshold (541) is never met for the scrolling subtree | same |
| 4c | `SKPictureRecorder` + the `SKPicture` handed to `SKImageFilter.CreatePicture` | `CompositionEffectBrush.skia.cs:1488-1490` | per effect-filter rebuild | same; the in-source comment at 1487 says a shared static recorder segfaults, so this was deliberate |
| 4d | `visual._ownContentPath` — the old `SKPath` is dropped on assignment | `Visual.skia.cs:483` and `Visual.skia.cs:495`; producers are `BorderVisual.BuildOwnContentPath` (`BorderVisual.skia.cs:393-408`, `builder.Detach()` at 407) and `ShapeVisual` (`ShapeVisual.skia.cs:129-158`, `Detach()` at 157) | per **repaint** of a Border/Shape visual — i.e. per container realisation during virtualized scrolling, not per frame (a pure translate sets `MatrixDirty` only, `Visual.skia.cs:140-146`, never `PaintDirty`) | `Detach()` returns a fresh `SKPath`; the previous value is overwritten with no `Dispose()` |
| 4e | `SKPathBuilder` in `TryAddShadowPaths` | `SpriteVisual.skia.cs:45-47` (`new SKPathBuilder()` … `builder.Detach()`) | per shadow-silhouette walk of a colour-brush SpriteVisual | builder never disposed (the detached path *is* disposed by the walker at `Visual.skia.cs:877`) |
| 4f | `new SKPath()` for the native-element clip | `SkiaRenderHelper.skia.cs:78` (and the inverted copy at 93 / `clipPath.Dispose()` at 96) | **per frame**, but only when `ContentPresenter.HasNativeElements()` is true (46) | the path is stored as `_lastRenderedFrame.nativeElementClipPath` and neither `Render` (159-162), `ReturnFrame` (412-434) nor `Draw` ever disposes it. With no native elements the static `_emptyClipPath` (26) is returned instead and nothing is allocated. |
| 4g | `new SKPath()` when there is no frame to present | `CompositionTarget.Rendering.skia.cs:245` | rare — the slot is repopulated by `ReturnFrame` | never disposed by the caller (`UnoSKVulkanView.cs:217` just assigns it to `NativeLayerHost.Path`) |
| 4h | `SKImage.FromPixels` per paint | `CompositionNineGridBrush.skia.cs:52` — method ends at 73 with no `Dispose` | per paint of a nine-grid brush | **off this path**: grep finds no `CompositionNineGridBrush` usage anywhere under `src/Uno.UI/`, so it is public-API-only. Listed for completeness. |

Note on `Visual.skia.cs:854` — if any `TryAddShadowPaths` override ever adds to the shared static
scratch list (`_spareShadowContributions`, line 34) *and then* returns false, those `SKPath`s are
left in the static list undisposed and the "always empty on entry" invariant asserted in the comment
at 31-33 is broken. I checked both overrides (`SpriteVisual.skia.cs:28-49`,
`BorderVisual.skia.cs:416-...`) and **both return false before adding anything**, so today this is
latent, not live.

---

## 5. What a scroll frame actually allocates natively, and what releases it

Cadence definitions: *per frame* = every `CompositionTarget.Render` on the UI thread;
*per repaint* = only for visuals with `PaintDirty` set; *per recycle* = per container realised or
recycled by virtualization.

| Allocation | Cadence | Released by |
|---|---|---|
| 1 root `SkPicture` (`SkiaRenderHelper.skia.cs:50`) | per frame | `FramePicture.OnPipelineReleased`/`Release` → `Picture.Dispose()` (`CompositionTarget.Rendering.skia.cs:531`, 548). **Never**, if a `Rendering` subscriber read `FrameData` (`_publicized`, 525/553) — then GC finalizer only. |
| Damage snapshot `SKPath` | per frame | pooled, zero allocation (144/153/428) |
| 2 clip `SKPath`s per visual visited | per frame × visuals | `_pathPool`, zero allocation (407-410) |
| `ownClip`/`childClip` boolean ops (`SKPath.Op`) | per frame × visuals | in-place into pooled paths; Skia scratch is internal |
| 1 native `SkString` + 1 managed string from `ToSvgPathData` | per frame | `using` inside SkiaSharp (IL-verified) |
| `_childrenPicture` unref along the ancestor chain of everything that moved | per frame | `InvalidateParentChildrenPicture` (250-253) — **freed**, this is the scroll's dominant native *churn* and it is correct |
| 1 `SkPicture` per repainted visual | per repaint | previous unref'd at 498 — net zero **while the visual lives** |
| 1 `SKPath` per repainted Border/Shape visual (`_ownContentPath`) | per repaint | **finalizer only** (4d) |
| 1 `SKPictureRecorder` per non-analytic shadowed visual | per frame (if shadows present) | **finalizer only** (4a) |
| GPU offscreen for every `SaveLayer` with a backdrop filter (`SkiaAcrylicBrush.skia.cs:115`, `CompositionEffectBrush.skia.cs:1610`) | per frame per acrylic/backdrop brush — these are `RequiresRepaintOnEveryFrame` (`SkiaAcrylicBrush.skia.cs:66`, `CompositionEffectBrush.skia.cs:33`) | Skia's scratch-resource recycling inside the **unbudgeted, never-purged** GrResourceCache (§1) |
| `_picture` + `_childrenPicture` of every destroyed `Visual` | per recycle | **nothing — permanent native leak** (§3) |

**The asymmetry that matters.** A scroll frame's *transient* native work is well managed: pooled
paths, pooled damage snapshots, reused recorders, unref-before-overwrite everywhere. What is *not*
managed is the boundary where a visual leaves the tree: the pipeline has an unref for "this picture
was replaced" and none for "this visual is gone". Scrolling a virtualized list is precisely the
workload that maximises the second event while producing no signal in the managed heap (the leaked
bytes are native `SkRecord`/`SkPaint`/texture memory, not GC memory) — which is consistent with the
observed 39→109 MB managed heap alongside per-process mapping exhaustion.

---

## Ranked candidates

1. **`Visual._picture` / `_childrenPicture` leaked on visual destruction** — permanent, unbounded,
   scales with scroll distance, and pins GPU textures the cache cannot purge. *high confidence.*
2. **GRContext resource cache never budgeted and never purged on Android Vulkan** — not itself a
   leak, but it is why (1) is unrecoverable and why per-frame backdrop/SaveLayer offscreens keep
   accumulating distinct kgsl mappings. *high confidence that it is unset; medium that it is
   causal.*
3. **`_ownContentPath` overwritten without `Dispose`** — per repaint, finalizer-only. *high
   confidence it is undisposed; medium that the volume matters.*
4. **Undisposed `SKPictureRecorder`s** (4a/4b/4c) — finalizer-only; 4a is genuinely per-frame when
   drop shadows fall back to the non-analytic path. *high / medium.*
5. **`_publicized` FramePicture** — one root `SKPicture` per frame reclaimed by GC only, but only
   when an app handler reads `RenderingEventArgs.FrameData`. *high confidence in the mechanism, low
   that it fires in the reported repro.*
6. **Per-frame `new SKPath()` in `CalculateClippingPath`** — only with native elements present.
   *high mechanism / low applicability.*

## Ruled out

- `GRContext.Flush()` leaving Vulkan command buffers unsubmitted — IL-verified `flush_and_submit`.
- `FramePicture` retain/release imbalance under production-faster-than-presentation — traced, balanced.
- Overwriting `_picture` / `_childrenPicture` without unref — all four sites unref first.
- Per-frame `SKSurface` creation — the swapchain surface is cached and only rebuilt on resize.
- `RetainedLayer` — not referenced by the Android host at all.
- Damage-snapshot and clip `SKPath` churn — pooled.
- `SkiaAcrylicBrush._filter` rebuild churn — cached by bounds, previous disposed.
- `ToSvgPathData` per frame — native string freed via `using`.

## Open questions

- Skia's compiled-in default `GrResourceCache` budget for 4.148.0, and whether Adreno's kgsl
  allocations are one mmap each (i.e. whether `vm.max_map_count`, not bytes, is the wall).
  Not answerable from this repo.
- Whether the SamplesApp scroll repro has drop shadows and/or acrylic in the visual tree — that
  decides whether 4a and the per-frame backdrop offscreens are live or dormant.
- Whether `ContentPresenter.HasNativeElements()` is true during the repro (decides 4f).
- Whether the finalizer thread actually keeps up: the resurrection in
  `CompositionObject.DisposeInternal` (309-317) means every finalized Visual round-trips through the
  main dispatcher queue, which is saturated during a scroll. Needs a runtime measurement.
