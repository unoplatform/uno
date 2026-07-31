# Vulkan leak audit — 01: Android view & surface/swapchain lifecycle

Scope: `UnoSKVulkanView` (Android SurfaceView host) and everything it drives per frame and per
surface-lifecycle event. All claims below are from source inspection at the cited file:line.
No runtime instrumentation was performed — evidence label is **Code review** unless stated.

Files read:

- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI.Runtime.Skia.Android/Platform/Vulkan/AndroidVulkanSurfaceFactory.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI.Runtime.Skia.Android/Platform/Vulkan/AndroidVulkanNativeInterop.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/VulkanContext.skia.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/Interop/VulkanCommandBufferPool.skia.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/Interop/VulkanCommandBuffer.skia.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/Interop/VulkanFence.skia.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/Interop/VulkanSemaphore.skia.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/Interop/VulkanImage.skia.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/Interop/VulkanKhrSurface.skia.cs`
- `D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/Vulkan/Interop/VulkanMemoryHelper.skia.cs`

---

## Headline

**Every presented frame permanently leaks one `VkCommandBuffer` and one `VkFence`.** The command
buffer pool has a recycling mechanism that is switched off at its only construction site, and no
code path other than pool disposal ever drains the submitted-buffer queue. Under sustained scrolling
at 60–120 Hz this is 3 600–14 400 live command buffers and fences after one minute — command-buffer
memory on Adreno comes from `kgsl_sharedmem_alloc` in ~128 KB granules, which is verbatim the failing
allocation in Signature A, and Signature B is the driver `calloc`-ing while recording
`vkCmdBlitImage` into yet another never-freed command buffer.

Two lifecycle defects act as amplifiers rather than independent leaks:
the frame pacer is permanently dead after the first `SurfaceDestroyed` (so the render loop free-runs
and multiplies the per-frame leak rate), and `VulkanContext.Dispose()` is one-shot, so from the
second `SurfaceDestroyed` onward an **entire Vulkan device generation** (instance, device, swapchain,
GRContext, full-screen render image, command pool + its whole accumulated queue) is orphaned.

---

## 1. Every allocation made per `RenderFrame()`

Call chain: `UnoSKVulkanView.RenderFrame()` (`UnoSKVulkanView.cs:199`) →
`VulkanContext.RenderFrame(Action<SKSurface>)` (`VulkanContext.skia.cs:181`).

| # | Allocated | Where | Freed? |
|---|-----------|-------|--------|
| 1 | `VkCommandBuffer` (`vkAllocateCommandBuffers`) | `VulkanDisplay.skia.cs:264` → `VulkanCommandBufferPool.skia.cs:72` | **NO — leaked** (see below) |
| 2 | `VkFence` (`vkCreateFence`) | `VulkanCommandBuffer.skia.cs:25` → `VulkanFence.skia.cs:23` | **NO — leaked** (destroyed only in `VulkanCommandBuffer.Dispose`, `:32`) |
| 3 | managed `VulkanCommandBuffer` object, enqueued into `Queue<VulkanCommandBuffer>` | `VulkanCommandBufferPool.skia.cs:14`, enqueued at `:78` from `VulkanCommandBuffer.skia.cs:108` | **NO — rooted by the queue** |
| 4 | 3 × managed `T[]` for the submit semaphore/stage spans | `VulkanDisplay.skia.cs:356-358` | Yes — plain GC garbage, **not a leak** |
| 5 | `stackalloc VkSemaphore[]` ×2 | `VulkanCommandBuffer.skia.cs:83,87` | Stack — **not a leak** |
| 6 | `VkImageBlit` / `VkImageMemoryBarrier` structs | `VulkanDisplay.skia.cs:309`, `VulkanMemoryHelper.skia.cs:43` | Stack locals — **not a leak** |
| 7 | `SKSurface` + `GRBackendRenderTarget` | `VulkanContext.skia.cs:250-253` | **Cached and reused** — created once per size (`EnsureCachedSkiaSurface` early-returns at `:228`). **Not a leak.** |
| 8 | `GRVkImageInfo` / `GRVkAlloc` | `VulkanContext.skia.cs:232-248` | Only on cache miss, managed. **Not a leak.** |

### The leak, precisely

`VulkanCommandBufferPool` supports recycling via `FreeFinishedCommandBuffers()`
(`VulkanCommandBufferPool.skia.cs:38-48`), but that method is called from exactly one place —
inside `CreateCommandBuffer()` and only when `_autoFree` is true:

```csharp
// VulkanCommandBufferPool.skia.cs:59-62
public unsafe VulkanCommandBuffer CreateCommandBuffer()
{
    if (_autoFree)
        FreeFinishedCommandBuffers();
```

`_autoFree` defaults to `false` (`VulkanCommandBufferPool.skia.cs:18`) and the **only construction
site in the repository** does not pass it:

```csharp
// VulkanDisplay.skia.cs:37
CommandBufferPool = new VulkanCommandBufferPool(_context);
```

(verified by grep: `new VulkanCommandBufferPool` matches only `VulkanDisplay.skia.cs:37`;
`FreeFinishedCommandBuffers` is referenced only at its declaration `:38` and the guarded call `:62`;
`FreeUsedCommandBuffers` only at `:32` and from `Dispose` `:52`.)

Meanwhile `Submit()` unconditionally files the buffer into the queue:

```csharp
// VulkanCommandBuffer.skia.cs:108
_pool.AddSubmittedCommandBuffer(this);
```

- **What is allocated:** one `VkCommandBuffer` (driver-side command/IB memory, allocated by the
  Adreno KGSL shared-memory allocator) and one `VkFence` (a kernel sync object) per presented frame.
- **On what cadence:** once per `RenderFrame()`, i.e. per frame — `StartPresentation()` at
  `VulkanContext.skia.cs:207` → `VulkanDisplay.skia.cs:264`.
- **Who should free it:** `VulkanCommandBufferPool.FreeFinishedCommandBuffers()`, which would
  dequeue every buffer whose fence is signalled and call `VulkanCommandBuffer.Dispose()`
  (`vkWaitForFences` + `vkDestroyFence` + `vkFreeCommandBuffers`, `:28-39`).
- **Why it does not happen:** `_autoFree == false`, so the drain is never invoked. The only other
  drain is `VulkanCommandBufferPool.Dispose()` (`:50-52`), which runs only when the whole
  `VulkanDisplay` is torn down (`VulkanDisplay.skia.cs:387`).

Note the pool *is* created with `VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT`
(`VulkanCommandBufferPool.skia.cs:25`), so the buffers are individually resettable — but they are
never reset or reused; a brand-new one is allocated every frame.

**Consistency with the crash signatures:** Signature A's `kgsl_sharedmem_alloc() failed! Allocation
size: (128 KB)` is the classic granule for Adreno command-buffer/indirect-buffer backing store.
Signature B aborts in `scudo_calloc` reached from `qglinternal::vkCmdBlitImage2` — i.e. the driver
allocating host bookkeeping while *recording* the blit at `VulkanDisplay.skia.cs:335`, into the
freshly allocated (never-recycled) command buffer, once `MapAllocator` can no longer `mmap`. Both
are per-process, not device-wide, which matches lmkd reporting ~1.4 GB free.

This leak is **not Android-specific** — `VulkanContext.BlitAndPresent()` (used by the Win32 split
paint path) allocates from the same non-autofree pool at `VulkanContext.skia.cs:327`. Android just
hits the per-process GPU-mappable ceiling first.

---

## 2. `ANativeWindow_fromSurface` / `ANativeWindow_release` pairing

`ANativeWindow_fromSurface` is called in exactly one place:

```csharp
// UnoSKVulkanView.cs:180-183
_nativeWindow = ANativeWindow_fromSurface(JNIEnv.Handle, surface.Handle);
GC.KeepAlive(surface);
```

reached only from `InitializeVulkan(holder)` (`:170`), which is called only from `RenderLoop`
(`:141`), which is started only from `SurfaceCreated` (`:84-89`).

`ANativeWindow_release` is called in two places:

- `SurfaceDestroyed` — `UnoSKVulkanView.cs:123-127`
- `Dispose(bool)` — `UnoSKVulkanView.cs:310-314`

**`SurfaceChanged` never releases and never re-acquires** (`UnoSKVulkanView.cs:92-105`). For a pure
resize that is *correct*: the `ANativeWindow` identity survives a geometry change, and
`VulkanContext.Resize` reuses the stored `_nativeWindowHandle` (`VulkanContext.skia.cs:148`). So a
resize/fold that produces only `SurfaceChanged` does **not** leak an `ANativeWindow` reference.

The refcount is nominally balanced across create/destroy pairs. It is **not** balanced under the
race described in §3/§4: `SurfaceDestroyed` releases and zeroes `_nativeWindow` (`:123-127`) *after*
a bounded 2-second join (`:116`). If a stale render thread is still executing and reaches line 180
after that zeroing, it stores a fresh reference into `_nativeWindow`; the next `SurfaceCreated`'s
thread then overwrites `_nativeWindow` again at the same line, and the intermediate reference is
lost forever. An `ANativeWindow` reference pins the surface's whole gralloc buffer set (several
full-screen buffers), so each lost reference is tens of MB. **UNVERIFIED at runtime** — race-window
dependent, but a foldable's rapid create/destroy sequences are exactly the trigger.

Additionally, the release at `:123-127` happens *unconditionally* even when a stale render thread is
still using the window (join timed out), and — per §3 — from the second cycle onward it happens
while a **live** `VkSurfaceKHR`/`VkSwapchainKHR` still references that `ANativeWindow`, because
`_vulkanContext.Dispose()` at `:121` no-ops. The in-code comment at `:176-179` states exactly why
that is illegal ("Releasing it while Vulkan surfaces are active causes a destroyed-mutex crash").

---

## 3. What leaks when `SurfaceCreated`/`Changed`/`Destroyed` fire N times

### 3a. `SurfaceChanged` × N (fold/unfold resize, no destroy)

`SurfaceChanged` → `VulkanContext.Resize(width, height)` (`UnoSKVulkanView.cs:101` →
`VulkanContext.skia.cs:133-154`). This path is **clean and, ironically, therapeutic**: it disposes
the cached Skia surface (`:143`), the `_renderImage` (`:145`) and the whole `_display` (`:146`) —
and `VulkanDisplay.Dispose()` (`:381-391`) calls `CommandBufferPool.Dispose()` → `FreeUsedCommandBuffers()`,
which is the *only* thing in the system that ever drains the leaked per-frame command buffers.

Costs per `SurfaceChanged`, all correctly balanced: 1 × `VkSwapchainKHR`, N × `VkImageView`,
1 × `VulkanSemaphorePair`, 1 × `VkCommandPool`, 1 × `VkSurfaceKHR`, 1 full-screen `VulkanImage`
(`vkCreateImage` + `vkAllocateMemory`, `VulkanImage.skia.cs:115,121`). **No leak found on this path.**

One hazard, not a leak: `Resize` runs on the **UI thread** while the render thread may be inside
`RenderFrame`. Both take `_device.Lock()` (`VulkanContext.skia.cs:138` and `:186`), but the render
thread reads `_display`/`_renderImage` *before* acquiring the lock (`:183`), so it can capture a
reference that `Resize` then disposes. Use-after-free, not a leak.

### 3b. `SurfaceCreated` × N

```csharp
// UnoSKVulkanView.cs:81-89
_surfaceReady = true;
_renderThread = new Thread(RenderLoop) { ... };
_renderThread.Start(holder);
```

No check that `_renderThread` is null; the previous thread reference is simply dropped. Per extra
`SurfaceCreated` that overlaps a surviving thread: one leaked OS thread (default 1 MB stack reserve)
plus that thread's continuing per-frame command-buffer/fence allocations. See §4.

### 3c. `SurfaceDestroyed` × N — the big one

```csharp
// UnoSKVulkanView.cs:114-127
_surfaceReady = false;
_renderEvent.Set();
_renderThread?.Join(TimeSpan.FromSeconds(2));
_renderThread = null;
_pacer.Dispose();
_vulkanContext.Dispose();
if (_nativeWindow != IntPtr.Zero) { ANativeWindow_release(_nativeWindow); _nativeWindow = IntPtr.Zero; }
```

Both `_vulkanContext` (`:32`) and `_pacer` (`:34`) are **`readonly` fields initialised once at
construction**, but both are disposed here on *every* surface destruction.

**(i) `VulkanContext.Dispose()` is one-shot.**

```csharp
// VulkanContext.skia.cs:358-361
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;
```

`Initialize()` (`:50`) never clears `_disposed` and never checks it — it happily rebuilds instance,
device, display, render image and `GRContext` into the same object. So:

| Cycle | Effect |
|-------|--------|
| Destroy #1 | `Dispose()` runs fully. Generation 1 is correctly torn down. `_disposed = true` forever. |
| Create #2 | `Initialize()` builds generation 2 into the same `VulkanContext`. Rendering resumes. |
| Destroy #2 | `Dispose()` returns at `:360`. **Generation 2 is orphaned in full.** `ANativeWindow_release` at `UnoSKVulkanView.cs:125` nevertheless runs — while gen-2's `VkSurfaceKHR`/swapchain still reference that window. |
| Create #3 | `Initialize()` overwrites `_instance`, `_device`, `_display`, `_renderImage`, `_grContext` (`:57,67,80,83,87`), making generation 2 permanently unreachable and undestroyable. |

**Per cycle from N = 2 onward this leaks:** a `VkInstance`, a `VkDevice` (with its driver-side
KGSL context and ring buffers), a `VkSurfaceKHR`, a `VkSwapchainKHR` plus its image views, a
`VulkanSemaphorePair`, a `VkCommandPool` **together with every command buffer and fence accumulated
in its queue since that generation started**, a full-screen `VulkanImage` (`VkImage` +
`vkAllocateMemory`; ~2176×1812×4 ≈ 15 MB of device-local memory on a Fold inner display), and a
`GRContext` whose resource cache holds arbitrarily many further `VkImage`/`VkBuffer` allocations
(`AbandonContext`/`Dispose` at `:372-373` never run).

`SurfaceDestroyed`/`SurfaceCreated` fire on every app background→foreground transition, every
rotation, and on a Fold 7 on fold/unfold when the activity is recreated — so N grows fast in normal
use.

**(ii) `ChoreographerFramePacer` is dead after the first destroy — the leak-rate amplifier.**

`_pacer.Dispose()` (`UnoSKVulkanView.cs:118`) sets `_disposed = true`
(`ChoreographerFramePacer.cs:85`) and disposes `_vsync`/`_ready` (`:93-94`). The field is `readonly`
and is never re-created. After that, every call returns immediately:

```csharp
// ChoreographerFramePacer.cs:66-71
public void WaitForNextFrame()
{
    if (_disposed || !_ready.Wait(MaxWait) || _handler is null)
    {
        return;
    }
```

So from the second surface onward the render loop at `UnoSKVulkanView.cs:143-159` has **no pacing at
all**. It spins as fast as `_renderEvent` is set and `RenderFrame` returns — MAILBOX present does not
block (`VulkanDisplay.skia.cs:104-105`, and the pacer's own doc comment at `:16-19` says exactly
this). Frame rate — and therefore the per-frame command-buffer/fence leak rate from §1 — becomes
unbounded rather than ~120/s. This turns a slow leak into a tens-of-seconds crash, matching the
observed repro.

Secondary, bounded: `_vsync.Dispose()` at `:93` races the posted `_vsync.Set()` lambda at `:54`/`:74`
running on the Looper thread → `ObjectDisposedException` there; and the pacer's Looper thread and its
`FrameCallback` (`:97`, a `Java.Lang.Object` = JNI global ref) are never joined/collected. Only one
pacer instance is ever created, so this is O(1), not O(N).

**(iii) `_renderEvent`** is disposed in `Dispose(bool)` (`UnoSKVulkanView.cs:315`) while a surviving
render thread may be blocked in `_renderEvent.Wait(...)` (`:146`) → `ObjectDisposedException`.
Correctness hazard, not a leak.

---

## 4. Is the render thread's lifetime bounded by the surface lifetime? Can two exist?

**No, and yes — two (or more) render threads can coexist and both will render.**

The intended bound is `_surfaceReady` (`UnoSKVulkanView.cs:143`) plus the join at `:116`. Both fail:

1. **The join is bounded to 2 seconds** (`:116`) and its result is ignored; `_renderThread = null`
   follows unconditionally at `:117`. Teardown then proceeds regardless.

2. **The render thread has several unbounded blocking points**, so exceeding 2 s is realistic:
   - `CreateSwapchain`'s spin: `while (!surface.CanSurfacePresent()) Thread.Sleep(16);`
     (`VulkanDisplay.skia.cs:54`) — **infinite** if the surface can never present, which is exactly
     the state after the Android surface has gone away.
   - `AcquireNextImageKHR` with `ulong.MaxValue` timeout (`VulkanDisplay.skia.cs:250`, `:283`).
   - `vkWaitForFences` with `ulong.MaxValue` in `VulkanCommandBuffer.Dispose` (`VulkanFence.skia.cs:40`).
   - `DeviceWaitIdle` (`VulkanContext.skia.cs:140,167,261,311,367`).
   - The device lock itself (`VulkanContext.skia.cs:186`).

3. **`_surfaceReady` is a single shared flag, not per-thread.** `SurfaceDestroyed` sets it false
   (`:114`); a stale thread blocked past the join wakes later, but by then `SurfaceCreated` may have
   set it back to true (`:81`). The stale thread's loop condition
   `while (_surfaceReady && !_disposed)` is satisfied again and it resumes rendering — now alongside
   the new thread started at `:84-89`, against the *same* `VulkanContext` and the *same*
   `_renderEvent`.

Consequences: the two threads serialise on `_device.Lock()` so they will not corrupt each other's
submissions, but they **double the per-frame command-buffer/fence allocation rate** from §1 and
double the present rate. Worse, the stale thread operates on a `VulkanContext` that
`SurfaceDestroyed` disposed at `:121` and on an `ANativeWindow` released at `:125` — the
use-after-free the comment at `:176-179` warns about.

There is also no guard against the *first* thread never having reached `InitializeVulkan` when
`SurfaceDestroyed` arrives; see the `_nativeWindow` overwrite race in §2.

---

## 5. Per-frame command buffer / fence / semaphore / image without recycling

| Object | Per frame? | Recycled? |
|--------|-----------|-----------|
| `VkCommandBuffer` | **Yes** — `VulkanDisplay.skia.cs:264` | **No.** Never reset, never freed until pool disposal. See §1. |
| `VkFence` | **Yes** — one per `VulkanCommandBuffer`, `VulkanCommandBuffer.skia.cs:25` | **No.** Destroyed only in `VulkanCommandBuffer.Dispose` (`:32`), which is only reached from the pool drain that never runs. |
| `VkSemaphore` | No — `VulkanSemaphorePair` created once per `VulkanDisplay` (`VulkanDisplay.skia.cs:36`) | Correctly reused; disposed at `:384`. **Not a leak.** But it *is* reused unsafely: `StartPresentation`'s retry loop (`:245-262`) re-calls `AcquireNextImageKHR` with the same `ImageAvailableSemaphore` after a `VK_SUBOPTIMAL_KHR`, even though the spec says the semaphore was already signalled on that result. Driver-state hazard, not memory. |
| `VkImage` (intermediate render target) | No — created in `Initialize`/`Resize`/`ResizeRenderImage` only (`VulkanContext.skia.cs:83,150,171`) | Correctly disposed before recreation (`:145,170`). **Not a leak.** |
| Swapchain `VkImage`/`VkImageView` | No — per swapchain creation (`VulkanDisplay.skia.cs:161-175`) | `DestroyCurrentImageViews()` runs first (`:163`) and in `RecreateSwapchainSafe` (`:228`). **Not a leak.** |
| `SKSurface` / `GRBackendRenderTarget` | No — cached (`VulkanContext.skia.cs:29-31, 226-254`) | Correctly reused. **Not a leak.** |
| `VkSurfaceKHR` | No | `VulkanDisplay.Dispose` `:389`; also correctly destroyed after device selection at `VulkanContext.skia.cs:70`. **Not a leak.** |

One caveat on the swapchain row: `RecreateSwapchain()` (`VulkanDisplay.skia.cs:197-208`) does **not**
call `DestroyCurrentImageViews()` itself — it relies on `CreateSwapchainImages()` doing so at `:163`,
which it does. Handles are balanced. However, `RecreateSwapchain` is reachable *per frame* through
`EnsureSwapchainAvailable()` (`VulkanContext.skia.cs:190` → `VulkanDisplay.skia.cs:232-240`), whose
condition is `Size != _surface.Size`, comparing the swapchain extent from
`capabilities.currentExtent` (`:87`, stored into `Size` at `:164`) against the fixed size baked into
`DirectVulkanSurface` at construction (`VulkanContext.skia.cs:79,148,403`). On Android with
pre-rotation, `currentExtent` is reported in the panel's native orientation, while the value passed
in comes from `holder.SurfaceFrame` (`UnoSKVulkanView.cs:188-190`) in the app's orientation. If those
disagree — plausible on a rotated or folded Fold 7 — `EnsureSwapchainAvailable` recreates the entire
swapchain (plus a full `DeviceWaitIdle`) **on every single frame**. Handles would still be balanced,
but the driver-side allocation churn is enormous and Adreno defers swapchain image memory release.
**UNVERIFIED** — needs one log line of `Size` vs `_surface.Size` on the device to confirm or rule out.

---

## Not leaks (stated explicitly, per the rules)

- The three `new[]` arrays per submit (`VulkanDisplay.skia.cs:356-358`) — ordinary short-lived GC
  garbage. Wasteful, not a leak.
- `SKSurface`/`GRBackendRenderTarget` — genuinely cached across frames.
- The intermediate `VulkanImage` — per size, disposed before recreation.
- `ANativeWindow` on the `SurfaceChanged`-only path — correctly *not* re-acquired.
- `VulkanContext.Resize` — fully balanced; it is in fact the only routine drain of the §1 leak.
- Semaphores — one pair per display, reused and disposed.

## Ranked candidates

1. **Per-frame `VkCommandBuffer` + `VkFence` leak** (`VulkanCommandBufferPool.skia.cs:62` guarded by
   `_autoFree == false` from `VulkanDisplay.skia.cs:37`). Per frame. High confidence. Direct match to
   both crash signatures.
2. **`VulkanContext.Dispose()` one-shot** (`VulkanContext.skia.cs:360`) → whole Vulkan generation
   orphaned per surface-destroy cycle from N = 2. High confidence.
3. **`ChoreographerFramePacer` permanently disposed after the first `SurfaceDestroyed`**
   (`UnoSKVulkanView.cs:118`, `ChoreographerFramePacer.cs:68`) → unpaced free-running render loop →
   multiplies (1). High confidence.
4. **Unbounded render-thread lifetime / two concurrent render threads**
   (`UnoSKVulkanView.cs:84-89`, `:116-117`, `:143`). High confidence that it is possible; medium that
   it occurs in this repro.
5. **Per-frame swapchain recreation via extent mismatch** (`VulkanDisplay.skia.cs:234`). Medium
   confidence, UNVERIFIED — cheap to check with one log line.
6. **`ANativeWindow` reference lost via the `InitializeVulkan`/`SurfaceDestroyed` race**
   (`UnoSKVulkanView.cs:180` vs `:123-127`). Low-medium confidence, race-dependent.

## Open questions for the next pass

- Instrument `VulkanCommandBufferPool._commandBuffers.Count` — it should be provably monotonic during
  a scroll. This is the one-line confirmation of candidate 1.
- Log `VulkanDisplay.Size` vs `_surface.Size` once per second to settle candidate 5.
- Count `SurfaceCreated`/`SurfaceDestroyed` invocations during the repro — does the Fold 7 actually
  cycle the surface during pure scrolling, or is candidate 1 sufficient on its own?
- Does `GRContext`'s Skia resource cache grow during scroll (`GetResourceCacheUsage`)? Nothing in
  this path ever purges it, and it is leaked wholesale by candidate 2.
