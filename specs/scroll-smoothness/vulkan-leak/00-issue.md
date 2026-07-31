# Android Skia Vulkan host exhausts per-process driver memory under sustained scrolling (SIGABRT in `vkCmdBlitImage`)

## Summary

On the Android Skia **Vulkan** render path (`UnoSKVulkanView`), every presented frame allocates a
`VkCommandBuffer` and a `VkFence` that are **never freed until the whole `VulkanDisplay` is torn
down**. `VulkanCommandBufferPool` has a reclaim path, but it is gated behind an `autoFree` flag that
defaults to `false` and is omitted at its only construction site, and `Submit()` unconditionally
files every buffer into a queue that has no other drain. At 60–120 fps that is thousands of live
driver command buffers per minute; after tens of seconds of sustained touch scrolling the Adreno
driver can no longer obtain GPU-mapped memory and aborts on the render thread inside the swapchain
blit — while the device itself still has ~1.4 GB free, because the exhaustion is **per-process**.

This is not a scroll-smoothness regression: the defect ships with the original Vulkan backend
(`a9a4027136`, 2026-03-28) and predates all work on this branch. Scrolling only exposes it, by
being the workload that sustains a high present rate for long enough.

## Environment

| | |
|---|---|
| Device | Samsung Galaxy Fold 7 (Adreno / Qualcomm), physical device |
| OS | Android, 64-bit (`/vendor/lib64/hw/vulkan.adreno.so`) |
| Render path | Uno Skia **Android Vulkan** — `UnoSKVulkanView` (`SurfaceView` + `ISurfaceHolderCallback`). **Not** the GL `UnoSKCanvasView` path. |
| App | SamplesApp |
| Repo | `unoplatform/uno`, branch `dev/mazi/smooth-scroll` (defect also present on `master`) |
| Reproducibility | Reproducible |
| Managed heap during repro | 39 MB → 109 MB over ~1 min of navigation — modest, and **not** the exhausted resource |
| Device memory during repro | lmkd reported ~1.4 GB available; the app was **not** LMK-killed |

## Repro steps

1. Build and deploy SamplesApp for Android with the **Vulkan** renderer active (`UnoSKVulkanView`,
   not `UnoSKCanvasView`).
2. Open any scrollable sample (a virtualized list or a long `ScrollViewer`).
3. Touch-scroll continuously — flings and drags, keep frames being produced — for **tens of
   seconds**. Do not let the app go idle.
4. Watch `adb logcat`. Signature A appears first as a warning storm; Signature B follows and kills
   the process.

## Evidence

Signature A (earlier, non-fatal warnings):

```
W Adreno-GSL: sharedmem_gpumem_alloc: mmap failed errno 12 Out of memory
E Adreno-GSL: kgsl_sharedmem_alloc() failed! Allocation size: (128 KB); Flags: (0x880c2500)
F libc    : mprotect failed on atexit array: Out of memory
```

Signature B (later, fatal):

```
F libc: Fatal signal 6 (SIGABRT) in tid 11192 (UnoVulkanRender), pid 11108
  #01 scudo::die()
  #03 scudo::reportMapError(unsigned long)
  #04 scudo::MemMapLinux::remapImpl(...)
  #05 scudo::MapAllocator<...>::allocate(...)
  #07 scudo_calloc
  #08 calloc
  #09..#11 /vendor/lib64/hw/vulkan.adreno.so
  #12 qglinternal::vkCmdBlitImage2(VkCommandBuffer_T*, VkBlitImageInfo2 const*)
  #13 qglinternal::vkCmdBlitImage(...)
```

What the signatures constrain:

- `kgsl_sharedmem_alloc` is the Adreno **GPU-mapped** shared-memory allocator. Its failures are
  per-process, and are consistent with running out of mappable address space or of per-process GPU
  allocations — not with device-wide memory pressure. lmkd reporting 1.4 GB free rules the latter
  out.
- `scudo::MapAllocator` is scudo's **secondary** allocator, used for large allocations that are
  serviced by their own `mmap`. `reportMapError` means the `mmap`/`mremap` failed. On a 64-bit
  process with a healthy system, `ENOMEM` from `mmap` most plausibly means the per-process mapping
  count (`vm.max_map_count`, default 65530) or the driver's per-process GPU allocation budget has
  been reached.
- The abort is on `UnoVulkanRender`, inside the driver recording `vkCmdBlitImage`. That is
  `VulkanDisplay.BlitImageToCurrentImage` recording into the frame's command buffer
  (`src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs:335`). **The crash is at the allocation site,
  not the leak site** — the driver simply happens to be the next thing that needs memory.

## Analysis

Every claim below is verified by inspection of the sources at
`D:/Work/uno-worktrees/scrollsmooth`. Anything not verified in source is labelled **UNVERIFIED**.

### 1. Per-frame `VkCommandBuffer` + `VkFence` leak — CONFIRMED, primary cause

The pool exposes a reclaim method that is called from exactly one place, and only when `_autoFree`
is set:

```csharp
// src/Uno.UI/Vulkan/Interop/VulkanCommandBufferPool.skia.cs:59-62
public unsafe VulkanCommandBuffer CreateCommandBuffer()
{
    if (_autoFree)
        FreeFinishedCommandBuffers();
```

`_autoFree` defaults to `false` (`VulkanCommandBufferPool.skia.cs:18`) and the **only construction
site in the repository** omits it:

```csharp
// src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs:37
CommandBufferPool = new VulkanCommandBufferPool(_context);
```

Meanwhile every submit files the buffer into a queue permanently:

```csharp
// src/Uno.UI/Vulkan/Interop/VulkanCommandBuffer.skia.cs:108
_pool.AddSubmittedCommandBuffer(this);      // -> VulkanCommandBufferPool.skia.cs:78 Enqueue
```

Repo-wide grep over `src/Uno.UI/Vulkan/`: `FreeFinishedCommandBuffers` appears only at its
declaration (`:38`) and the dead guarded call (`:62`); `FreeUsedCommandBuffers` only at its
declaration (`:32`) and in `Dispose()` (`:52`). There is no
`new VulkanCommandBufferPool(..., true)` anywhere.

Leak accounting:

- **What is allocated** — one `VkCommandBuffer` via `vkAllocateCommandBuffers`
  (`VulkanCommandBufferPool.skia.cs:72`), holding its recorded command stream (three pipeline
  barriers plus `vkCmdBlitImage`) in Adreno driver memory; and one `VkFence` via `vkCreateFence`
  (`VulkanFence.skia.cs:23`), created in the `VulkanCommandBuffer` constructor
  (`VulkanCommandBuffer.skia.cs:25`).
- **Cadence** — once per presented frame. `UnoSKVulkanView.RenderFrame` (`UnoSKVulkanView.cs:199`)
  → `VulkanContext.RenderFrame` (`VulkanContext.skia.cs:181`) → `_display.StartPresentation()`
  (`:207`) → `CommandBufferPool.CreateCommandBuffer()` (`VulkanDisplay.skia.cs:264`). The Win32/X11
  split-paint path leaks identically via `VulkanContext.skia.cs:327`.
- **Who should free it** — `FreeFinishedCommandBuffers()` (`VulkanCommandBufferPool.skia.cs:38-48`),
  which would dequeue every buffer whose fence is signalled and call `VulkanCommandBuffer.Dispose()`
  → `vkDestroyFence` + `vkFreeCommandBuffers` (`VulkanCommandBuffer.skia.cs:28-39`).
- **Why it does not happen** — `_autoFree == false`, so the drain is dead code. The only other
  drain is `VulkanCommandBufferPool.Dispose()` (`:50-52`), reached only from
  `VulkanDisplay.Dispose()` (`VulkanDisplay.skia.cs:387`) — i.e. Android surface destroy or
  `VulkanContext.Resize`.
- **Category** — (a) native/driver objects. `VulkanCommandBuffer` and `VulkanFence` have no
  finalizers, and the managed wrappers are strongly rooted by `_commandBuffers`, so the GC cannot
  rescue this. That is exactly consistent with the modest managed heap alongside native exhaustion.

Aggravating detail: the pool is created with `VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT`
(`VulkanCommandBufferPool.skia.cs:25`), which requires each buffer to be independently resettable
and therefore prevents a shared linear pool arena — each leaked buffer pins its own driver
allocation. The `Allocation size: (128 KB)` in Signature A is the granule you would expect for an
Adreno per-command-buffer command/indirect-buffer chunk. *(That specific attribution of the 128 KB
granule to command buffers is an inference from the log, **UNVERIFIED** against the driver.)*

Order of magnitude: at 120 Hz, ~7,200 command buffers and 7,200 fences per minute. If each pins even
one 128 KB kgsl allocation, that is ~15 MB/s of unreclaimable GPU-mapped memory and ~120 new
mappings/s — hundreds of MB and tens of thousands of mappings within the observed
tens-of-seconds window. Both the byte figure and the mapping figure land in the right range; the fix
is the same either way.

**Why this explains the specific evidence, point by point:**

| Evidence | Fit |
|---|---|
| Per-process, not device, exhaustion | kgsl allocations and their mappings are per-process; the device is untouched |
| Abort inside `vkCmdBlitImage` on the render thread | The driver allocates while recording the blit into the freshly allocated (never recycled) buffer — the next allocation after thousands have accumulated |
| Growth under sustained scrolling | Exactly one leaked buffer + fence per presented frame; scrolling is the workload that sustains presents |
| Modest managed heap | The managed `VulkanCommandBuffer` wrapper is tens of bytes; 7,200/min is ~0.4 MB/min |

### 2. Command buffers that are never submitted escape even a drain fix — CONFIRMED

A buffer only enters the queue after a successful `vkQueueSubmit`
(`VulkanCommandBuffer.skia.cs:105-108`). If anything throws between `CreateCommandBuffer()` and
`Submit()`, the buffer is orphaned with no reference anywhere and survives until
`vkDestroyCommandPool`. Two such paths exist and neither has a `try`/`finally`:

- `VulkanContext.BlitAndPresent` (`VulkanContext.skia.cs:325-336`) — `EndPresentation` throwing
  before `Submit`.
- `VulkanContext.RenderFrame` (`VulkanContext.skia.cs:217-222`) — the `OUT_OF_DATE`/`SUBOPTIMAL`
  catch unwinds past `_display.StartPresentation()` at `:207`.

Low cadence normally; per-frame if candidate 4 is active.

### 3. `VulkanContext.Dispose()` is one-shot — CONFIRMED, wrong cadence for this repro

```csharp
// src/Uno.UI/Vulkan/VulkanContext.skia.cs:358-361
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;
```

`_disposed` is never cleared and `Initialize()` (`:50`) never checks it, while `UnoSKVulkanView`
holds the context in a `readonly` field (`UnoSKVulkanView.cs:32`) and calls `Dispose()` on **every**
`SurfaceDestroyed` (`:121`) and `Initialize()` on every `SurfaceCreated` (via `:141` → `:190`).

From the second `SurfaceDestroyed` onward, `Dispose()` returns immediately and `Initialize()`
overwrites `_instance`, `_device`, `_display`, `_renderImage`, `_grContext`
(`VulkanContext.skia.cs:57, 67, 80, 83, 87`), orphaning an entire Vulkan device generation:
`VkInstance`, `VkDevice`, `VkSurfaceKHR`, `VkSwapchainKHR` + image views, the semaphore pair, the
`VkCommandPool` **with its whole accumulated command-buffer/fence queue**, a full-screen
`VulkanImage` (`VkImage` + `vkAllocateMemory`, ~15 MB device-local on a Fold inner display), and a
`GRContext` with its resource cache. `ANativeWindow_release` (`UnoSKVulkanView.cs:125`) still runs
while that generation's live `VkSurfaceKHR`/swapchain reference the window — the exact hazard the
in-code comment at `:176-179` warns about.

Cadence is per app background/foreground, rotation, or fold/unfold — **not** per scroll frame. It
cannot on its own explain a tens-of-seconds pure-scroll repro, but it will pile on if the tester
backgrounded the app at any point, and it must be fixed regardless.

### 4. Possible per-frame full swapchain recreation — UNVERIFIED, cheap to settle

```csharp
// src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs:232-240
public bool EnsureSwapchainAvailable()
{
    if (_surface == null || Size != _surface.Size)
    {
        RecreateSwapchain();
```

called every frame from `VulkanContext.RenderFrame` (`VulkanContext.skia.cs:190`). The two sides
come from different sources:

- `Size` is the swapchain extent, taken from `capabilities.currentExtent`
  (`VulkanDisplay.skia.cs:86-87`, stored at `:164`).
- `_surface.Size` is `VulkanKhrSurface.Size => _surfaceInfo.Size` (`VulkanKhrSurface.skia.cs:63`),
  which is the immutable `SKSizeI` baked into `DirectVulkanSurface` at construction
  (`VulkanContext.skia.cs:79, 148, 399-406`), sourced from `holder.SurfaceFrame`
  (`UnoSKVulkanView.cs:188-190`).

If those ever disagree — plausible with Android pre-rotation on a foldable — the condition is
permanently true and every frame performs `DeviceWaitIdle` + full swapchain teardown/recreate. The
handles balance (`VulkanDisplay.skia.cs:129-133` destroys the old swapchain right after creating the
new one; `CreateSwapchainImages` opens with `DestroyCurrentImageViews()` at `:163`), so this is
**not a handle leak** — but the driver-side BufferQueue/gralloc churn is enormous and Adreno reclaims
swapchain buffers lazily.

Related, same area: `CreateSwapchain` deliberately requests `preTransform = IDENTITY` when the
surface reports a rotated `currentTransform` (`VulkanDisplay.skia.cs:123-125`). Per spec that makes
`vkAcquireNextImageKHR` return `VK_SUBOPTIMAL_KHR`, which `StartPresentation` treats as
recreate-and-retry (`:253-254`) — and since the next `CreateSwapchain` makes the same choice, the
`while (true)` loop at `:245` would **never terminate**, recreating a swapchain per iteration.
`VK_SUBOPTIMAL_KHR` also means the acquire *succeeded*, so the loop re-enters
`AcquireNextImageKHR` with a semaphore that already has a pending signal (`:247-252`) — invalid
usage — and drops the acquired image without ever presenting it. If this were live from frame 1 the
app would never render, so it is not the steady state; it may still fire transiently.

**UNVERIFIED on-device.** One log line settles both (see *How to confirm*).

### 5. `ChoreographerFramePacer` is permanently dead after the first `SurfaceDestroyed` — CONFIRMED amplifier

`_pacer` is a `readonly` field created once (`UnoSKVulkanView.cs:34`) but `Dispose()`d on every
`SurfaceDestroyed` (`:118`) and never recreated. `WaitForNextFrame()` then returns immediately
(`ChoreographerFramePacer.cs:66-71`), so from the second surface onward the render loop
(`UnoSKVulkanView.cs:143-159`) is unpaced. MAILBOX present does not block
(`VulkanDisplay.skia.cs:104-105`), so the frame rate — and therefore the candidate-1 leak rate —
becomes whatever the compositor can request.

This is an amplifier, **not a cause**: the pacer only exists since `5dbe922dfb` on this branch, and
before that commit the loop had no pacing at all. It reduces the leak rate; its bug merely restores
the previous behaviour after a surface cycle.

Secondary (correctness, not memory): `_vsync.Dispose()` (`ChoreographerFramePacer.cs:93`) races the
posted `_vsync.Set()` on the Looper thread (`:54`, `:74`).

### 6. Unbounded render-thread lifetime; two render threads can coexist — CONFIRMED possible, medium likelihood here

`SurfaceCreated` overwrites `_renderThread` with no null check (`UnoSKVulkanView.cs:84-89`);
`SurfaceDestroyed` joins with a 2 s bound whose result is ignored and then nulls the field
unconditionally (`:116-117`). The render thread has several unbounded blocking points that make
exceeding 2 s realistic — the infinite `while (!surface.CanSurfacePresent()) Thread.Sleep(16)` spin
(`VulkanDisplay.skia.cs:54`, precisely the state once the Android surface is gone),
`AcquireNextImageKHR` with `ulong.MaxValue` (`:250`, `:283`), `vkWaitForFences` with `ulong.MaxValue`
(`VulkanFence.skia.cs:40`), and `DeviceWaitIdle`. Because `_surfaceReady` is a single shared flag
(`:143`), a stale thread that wakes after the next `SurfaceCreated` sees it true again and resumes —
doubling the candidate-1 allocation rate, on a context disposed at `:121` and a window released at
`:125`.

### 7. `Visual._picture` / `_childrenPicture` never released on visual destruction — CONFIRMED, wrong memory class

`Visual._picture` and `_childrenPicture` are raw `IntPtr` `SkPicture` refs
(`src/Uno.UI.Composition/Composition/Visual.skia.cs:53-54`). All four *overwrite* sites correctly
unref first (`:236-240`, `:250-253`, `:497-499`, `:577-589`) — but there is no release for "this
visual is gone":

- `Visual` has **no** `DisposeInternal` override (grep over `Visual*.cs`, `ContainerVisual*.cs`,
  `ShapeVisual*.cs`, `BorderVisual*.cs`, `SpriteVisual*.cs` returns none; the only override in
  `src/Uno.UI.Composition/Composition/` is `VisualInteractionSource.cs:78`).
- `~CompositionObject()` (`CompositionObject.cs:29-32`) → `Dispose()` (`:305`) →
  `DisposeInternal()` (`:307-318`), which only stops animations and never touches Skia handles.
- `Visual` has no finalizer of its own, and the handles are raw `IntPtr`, so the GC has no hook.

Real, permanent, and scales with scroll distance (recycled containers, torn-down item templates).
**But it is the wrong memory class for this crash**: an `SkPicture` and its `SkRecord` live on the
libc heap (scudo *primary*, small allocations), not in `kgsl`. It cannot produce Signature A, and it
would inflate RSS, which lmkd did not see. Fix it — just not as the answer to this bug. *(Pinned
GPU textures via referenced `SkImage`s are a plausible secondary GPU cost but **UNVERIFIED** for the
SamplesApp scroll tree.)*

### 8. Contributors that are real but not the cause

- **`GRContext` created with no options and never purged** — `VulkanContext.skia.cs:115-127`; no
  `SetResourceCacheLimit`, no `PurgeUnlockedResources`, and no Android `OnTrimMemory`/`LowMemory`
  hook anywhere in `src/Uno.UI.Runtime.Skia.Android/` or `src/Uno.UI/`. Not itself a leak, but it
  means nothing ever reclaims GPU-side pressure under duress.
- **Damage clip complexity (branch-introduced, already narrowed)** — the damage snapshot is applied
  as a GPU clip on the Vulkan canvas every frame
  (`src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:291`,
  `canvas.ClipPath(damage, antialias: false)`). See the branch-impact section below.
- **Finalizer-only native churn** — `visual._ownContentPath` overwritten without `Dispose`
  (`Visual.skia.cs:483`, `:495`; producers return a fresh `SKPath` from `builder.Detach()`, e.g.
  `BorderVisual.skia.cs:407`); undisposed `SKPictureRecorder` at `Visual.skia.cs:447` and `:552` and
  `CompositionEffectBrush.skia.cs:1488-1490`; `new SKPath()` at `SkiaRenderHelper.skia.cs:78` never
  disposed by any consumer. All real, all reclaimed by the SkiaSharp finalizer, all self-limiting.
- **`VulkanImage` error-path leak** — if `vkCreateImageView` at `VulkanImage.skia.cs:149` throws,
  the already-created `VkImage` (`:115`) and `VkDeviceMemory` (`:83`) are never destroyed and the
  exception escapes the constructor (`:199`), so no caller can call `Dispose`. Contrast `:119-127`,
  which does have cleanup. Real defect, error-path-only cadence.
- **`RecreateSurface` destroys the surface before the swapchain created from it** —
  `VulkanDisplay.skia.cs:210-217` (`_surface?.Dispose()` at `:212` precedes `DestroySwapchain()` at
  `:215`), violating VUID-vkDestroySurfaceKHR-surface-01266. Low cadence
  (`VK_ERROR_SURFACE_LOST_KHR` only). Should be reordered.

### 9. Explicitly ruled out (verified as correct — not padding the list)

- **Swapchain image views** — `CreateSwapchainImages` opens with `DestroyCurrentImageViews()`
  (`VulkanDisplay.skia.cs:163` → `:151-159`) before reallocating at `:172-174`, and all three entry
  points (`:38`, `:207`, `:229`) go through it. Not leaking.
- **`oldSwapchain`** — `:129` passes the old handle, `:133` destroys it immediately after a
  successful `vkCreateSwapchainKHR`, and both recreate paths pass `this` (`:205`, `:227`). Exactly
  one destroy per handle. Not leaking.
- **Swapchain `VkImage`s** — owned by the swapchain (`:166-171`); correctly *not* destroyed
  individually.
- **Semaphores** — one `VulkanSemaphorePair` per display (`:36`), reused every frame, disposed at
  `:384`. Two total.
- **`SKSurface` / `GRBackendRenderTarget`** — cached per size, early-return at
  `VulkanContext.skia.cs:228`, disposed together in `DisposeCachedSkiaSurface` (`:256-267`) from
  `Resize` (`:143`), `ResizeRenderImage` (`:168`) and `Dispose` (`:368`). Not per-frame.
- **Intermediate `VulkanImage`** — created only at `VulkanContext.skia.cs:83`, `:150`, `:171`, and
  disposed before each recreation (`:145`, `:170`); view + image + memory all freed in
  `VulkanImageBase.Dispose` (`VulkanImage.skia.cs:172-191`).
- **Descriptor pools** — this code creates none; grep of `DeviceApi.(Create|Allocate)` over
  `src/Uno.UI/Vulkan/` returns only swapchain, image view, command pool, command buffers, fence,
  semaphore, image and memory.
- **The command *pool* count** — one per display (`VulkanCommandBufferPool.skia.cs:28`), destroyed
  at `:55`. It is the buffers inside that grow.
- **Per-frame managed garbage that IS freed** — the three `new[]` arrays per submit
  (`VulkanDisplay.skia.cs:356-358`) and `new VkPresentModeKHR[]` (`:64`) are ordinary GC garbage.
  Wasteful, not a leak.
- **Damage-region and clip-path churn** — `DamageRegion` reuses its builder and path and resets
  rather than recreating (`DamageRegion.skia.cs:82-92`), the `Detach()` at `:104` is inside
  `using`; damage snapshots are pooled (`CompositionTarget.Rendering.skia.cs:69`, `:144`, `:153`,
  `:428`); per-visual clip paths are pooled (`Visual._pathPool`, `Visual.skia.cs:26`, e.g.
  allocate/free at `Visual.Damage.skia.cs:82-83` / `:176-184`).
- **`VulkanContext.Resize`** — fully balanced (`VulkanContext.skia.cs:133-154`), and in fact the
  *only* routine that ever drains the candidate-1 leak, via `VulkanDisplay.Dispose` → `:387`.
- **Managed GC pressure as the primary cause** — the retained managed wrappers are tiny; 39→109 MB
  is not the exhausted resource.

### Ranking against the specific evidence

| # | Candidate | Cadence | Fits per-process exhaustion | Fits abort in `vkCmdBlitImage` | Fits scroll growth | Fits modest managed heap | Verdict |
|---|---|---|---|---|---|---|---|
| 1 | `VkCommandBuffer` + `VkFence` never freed | **per frame** | yes (kgsl) | yes (same allocation class) | yes | yes | **root cause** |
| 2 | Non-submitted buffers on exception paths | per failed present | yes | yes | partial | yes | contributing defect |
| 3 | `VulkanContext.Dispose()` one-shot | per surface cycle ≥ 2 | yes | indirect | no | yes | must fix, wrong cadence |
| 4 | Per-frame swapchain recreation | per frame *if* extents disagree | yes | indirect | yes | yes | **UNVERIFIED**, multiplies 1 |
| 5 | Dead pacer → unpaced loop | after first surface cycle | — | — | multiplies 1 | — | amplifier |
| 6 | Concurrent render threads | per surface cycle | — | — | multiplies 1 | — | amplifier |
| 7 | `Visual._picture` leaked on destruction | per recycled visual | libc heap, not kgsl | no | yes | yes | real leak, wrong class |
| 8 | Unbudgeted `GRContext`, finalizer-only churn | various | partial | no | partial | yes | contributors |

## Suggested fix

**Primary (fixes the crash):**

1. Recycle command buffers. Minimal change — construct the pool with reclaim enabled at
   `src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs:37`:
   ```csharp
   CommandBufferPool = new VulkanCommandBufferPool(_context, autoFree: true);
   ```
   Better — remove the `autoFree` parameter entirely (`VulkanCommandBufferPool.skia.cs:13, 18, 21,
   61-62`) and always call `FreeFinishedCommandBuffers()`; there is no caller that wants the
   accumulating behaviour. `FreeFinishedCommandBuffers` peeks the queue head and stops at the first
   unsignalled fence, which is correct for a single queue where fences signal in submission order.
   Best — stop allocating per frame at all: keep a ring of N buffers (N = swapchain image count)
   with their fences and `vkResetCommandBuffer` the one whose fence is signalled. Note that
   `VulkanCommandBuffer._hasStarted` / `_hasEnded` (`VulkanCommandBuffer.skia.cs:16-17`) must be
   reset for a buffer to be reusable.

2. Guarantee the buffer is freed even when it is never submitted. Wrap its lifetime in
   `try`/`finally` at `VulkanContext.skia.cs:207-213` and `:325-336`, and free (not enqueue) it if
   `Submit()` was not reached.

**Secondary (same subsystem, should ride along):**

3. Make `VulkanContext` re-initialisable: clear `_disposed` in `Initialize()`, or drop the one-shot
   guard at `VulkanContext.skia.cs:360-361`, so the second and subsequent `SurfaceDestroyed` cycles
   actually tear down their generation.
4. Do not permanently kill the pacer: either recreate `ChoreographerFramePacer` in `SurfaceCreated`
   or stop disposing it in `SurfaceDestroyed` (`UnoSKVulkanView.cs:34`, `:118`), and stop disposing
   `_vsync`/`_ready` while a Looper-posted callback may still touch them
   (`ChoreographerFramePacer.cs:93-94`).
5. Bound the render thread's lifetime per thread rather than through the shared `_surfaceReady`
   flag: give each `RenderLoop` its own cancellation token, refuse to start a second thread while
   one is alive (`UnoSKVulkanView.cs:84-89`), and bound `CanSurfacePresent`'s spin
   (`VulkanDisplay.skia.cs:54`) and the `AcquireNextImageKHR` timeouts (`:250`, `:283`).
6. Handle `VK_SUBOPTIMAL_KHR` as "present this frame, recreate at the top of the next one" instead
   of recreate-and-retry with an already-signalled semaphore
   (`VulkanDisplay.skia.cs:245-262`), and bound that `while (true)`.
7. Reorder `RecreateSurface` (`VulkanDisplay.skia.cs:210-217`) to destroy the swapchain before the
   surface it was created from.
8. Add error-path cleanup around `vkCreateImageView` in `VulkanImage.skia.cs:149`.

**Separate issue (file independently — real leak, different subsystem):**

9. Release `Visual._picture` / `_childrenPicture` when a `Visual` dies — override
   `CompositionObject.DisposeInternal` in `Visual`. Needs care: the only path that currently runs is
   the finalizer, so the `sk_refcnt_safe_unref` must be marshalled to the render/UI thread rather
   than executed on the finalizer thread, and a deterministic `Dispose` from `UIElement` teardown
   would be better than relying on finalization at all.

## How to confirm

Cheapest decisive check, in order:

**1. One log line, in-process (settles candidate 1 outright).**
In `VulkanCommandBufferPool.CreateCommandBuffer()`
(`src/Uno.UI/Vulkan/Interop/VulkanCommandBufferPool.skia.cs:59`), log `_commandBuffers.Count` every
120th call. Scroll for 30 s. **Expected if the diagnosis is right: strictly monotonic, ~120/s, no
plateau.** After the fix it should oscillate around the swapchain image count (2–3).

**2. `adb`, no code change — count the driver's mappings (settles the memory class).**
```bash
PID=$(adb shell pidof -s <your.package.name>)
# total VMAs and kgsl-backed VMAs, sampled once a second
adb shell "while true; do echo \$(wc -l < /proc/$PID/maps) \$(grep -c kgsl /proc/$PID/maps); sleep 1; done"
# the per-process ceiling this is racing towards
adb shell cat /proc/sys/vm/max_map_count
```
Scroll continuously. A kgsl mapping count that climbs ~1 per presented frame and never falls
confirms a per-frame driver-object leak. Compare against GPU accounting:
```bash
adb shell dumpsys meminfo <your.package.name> | grep -Ei 'Gfx dev|GL mtrack|Native Heap|TOTAL'
```
Expect `Gfx dev` / `GL mtrack` to grow steadily while `Native Heap` and Dalvik stay flat — which
also discriminates candidate 1 (driver memory) from candidate 7 (libc heap).

**3. Two log lines, to settle candidate 4 (per-frame swapchain recreation).**
- In `VulkanDisplay.EnsureSwapchainAvailable()` (`VulkanDisplay.skia.cs:232`), log `Size` and
  `_surface.Size` once a second. If they differ, the swapchain is being rebuilt every frame.
- In `VulkanDisplay.StartPresentation()` (`:247`), log any `acquireResult` that is not
  `VK_SUCCESS`. A steady stream of `VK_SUBOPTIMAL_KHR` confirms the pre-rotation path.

**4. Surface-cycle count, to decide whether candidates 3/5/6 are live in this repro.**
Count `SurfaceCreated`/`SurfaceDestroyed` in logcat during the run (both already log at Debug,
`UnoSKVulkanView.cs:78`, `:96`, `:111`). If the count is 1/0 for a pure scroll, candidate 1 is
sufficient on its own — which is the expectation.

**5. Optional heavier confirmation.** Enable `VK_LAYER_KHRONOS_validation` with the object-lifetime
and best-practices layers; it will report the growing live `VkCommandBuffer`/`VkFence` populations
directly.

---

## Does recent scroll-smoothness work on this branch contribute? — honest answer

**No for the root cause; partially yes for two secondary vectors. Being explicit about both.**

**Not the cause.** The command-buffer/fence leak (candidate 1) was introduced with the Vulkan
backend itself in `a9a4027136` *"feat: Add Vulkan hardware acceleration for Android Skia backend"*
(2026-03-28), long before this branch. `git log <merge-base>..HEAD -- src/Uno.UI/Vulkan/` shows the
only branch commits touching the Vulkan sources are the pacer work
(`03bf236a9a`, reverted by `d5bda45e57`, then `5dbe922dfb`) and unrelated fixes — none of them
touched `VulkanCommandBufferPool`, `VulkanCommandBuffer` or `VulkanFence`. Candidates 2, 3, 4, 6, 7
and 8 are likewise all pre-existing.

**The framing "the damage rework is the only rendering-adjacent change on this branch" is not
accurate** — `ChoreographerFramePacer` (`5dbe922dfb`, added today) is also rendering-adjacent and
sits directly on the Android Vulkan render loop. Its effect, though, is *protective*: before it, the
render loop had no pacing at all, so it **reduces** the leak rate. Its own bug (permanently disposed
after the first `SurfaceDestroyed`, candidate 5) merely restores the pre-existing unpaced behaviour
after a surface cycle. If the crash log came from a build *before* `5dbe922dfb`, the loop was
unpaced throughout and the leak rate was correspondingly higher.

**Two branch changes could genuinely add driver-side pressure:**

1. **The damage clip, between `81a5f6467e` and `3fec98f3fd`.** The damage snapshot is applied as a
   GPU clip on the Vulkan canvas every frame
   (`src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:291`). `81a5f6467e` (11:50 today)
   introduced `DamageRegion` with `MaxPoints = 256` — up to 64 overlapping contours handed to
   `ClipPath` — which on a tiled GPU costs a clip-mask allocation per frame. `3fec98f3fd` (15:15
   today) recognised exactly this and narrowed it to `MaxPoints = 32` (8 rects), and also stopped
   disposing/recreating the native `SKPathBuilder` on every collapse
   (`DamageRegion.skia.cs:24-25`, `:45-54`, `:82-92`). **On a build between those two commits, this
   is a real additional per-frame GPU allocation vector on the Android Vulkan path.** On current
   HEAD it is bounded. It is a multiplier, not a leak — the clip masks come from the (unbudgeted)
   Skia resource cache and are recycled.
2. **`88e226ecb3` "Bound frame picture recording to frame size"** changed the root recording bounds
   from `Visual.InfiniteClipRect` to the frame rect (`SkiaRenderHelper.skia.cs:40`). This *reduces*
   recorded work. No plausible negative memory impact.

Everything else in the damage rework audits clean: pooled clip paths (`Visual.Damage.skia.cs:82-83`
allocate, `:176-184` free on every path via `finally`; `:192-193` uses a `DisposableStruct` to
guarantee the free), a reused static outset builder with the detached path in a `using`
(`:188`, `:195-197`), and pooled damage snapshots.

**What would settle the branch question definitively:** which commit the crashing APK was built
from. If it is at or after `3fec98f3fd`, vector (1) above was already bounded and candidate 1 stands
alone. Either way, candidate 1 is present on `master` and on every build of this branch, and fixing
it is not conditional on that answer.

---

*Evidence classification for this report: **code review by inspection** of the cited sources.
No compile validation and no runtime validation were performed as part of this analysis; the
on-device measurements under "How to confirm" have not been run.*
