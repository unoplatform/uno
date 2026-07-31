# Vulkan swapchain / display / image audit — leak analysis

Scope: shared Vulkan code under `src/Uno.UI/Vulkan/**`, plus the Android driver
(`src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs`) needed to establish cadence.

All line numbers are from the worktree `D:/Work/uno-worktrees/scrollsmooth` at the time of writing.

---

## Headline

**`VulkanCommandBufferPool` is not a pool. It is an unbounded, never-drained accumulator.**
Every presented frame allocates one `VkCommandBuffer` + one `VkFence`, records the swapchain
blit into it, submits it, and enqueues it into a `Queue<VulkanCommandBuffer>` that has **zero
drain call sites in the entire repository**. Nothing is freed until the whole `VulkanDisplay`
is disposed (app surface destroy / resize). Under sustained scrolling at 60–120 fps that is
60–120 leaked command buffers *per second*, each holding its recorded command stream in
driver memory — which is exactly the memory the crashing frame
(`qglinternal::vkCmdBlitImage2` → `calloc` → `scudo` map failure) is trying to allocate.

---

## 1. Every `vkCreate*` / `vkAllocate*` in this code, and its matching destroy

| # | Create / Allocate | Site | Matching destroy | All paths? |
|---|---|---|---|---|
| 1 | `vkCreateSwapchainKHR` | `VulkanDisplay.skia.cs:131` | `DestroySwapchain()` → `vkDestroySwapchainKHR` `VulkanDisplay.skia.cs:140`, invoked at `:133` (recreate), `:215`, `:386` | **Yes** for recreate/dispose. Ordering defect in `RecreateSurface` — see §4b |
| 2 | `vkCreateImageView` (swapchain views) | `VulkanDisplay.skia.cs:192` | `DestroyCurrentImageViews()` `:151-159`, invoked at `:163`, `:226`, `:385` | **Yes** — see §2 |
| 3 | `vkCreateCommandPool` | `VulkanCommandBufferPool.skia.cs:28` | `vkDestroyCommandPool` `:55` (pool `Dispose`) | Yes (1 per display, low cadence) |
| 4 | **`vkAllocateCommandBuffers`** | **`VulkanCommandBufferPool.skia.cs:72`** | `vkFreeCommandBuffers` `VulkanCommandBuffer.skia.cs:36`, reachable only from `FreeUsedCommandBuffers`/`FreeFinishedCommandBuffers` — **never called** | **NO — LEAK, per frame.** See §3 |
| 5 | **`vkCreateFence`** | **`VulkanFence.skia.cs:23`** (one per `VulkanCommandBuffer`, `VulkanCommandBuffer.skia.cs:25`) | `vkDestroyFence` `VulkanFence.skia.cs:31`, only from `VulkanCommandBuffer.Dispose` `:32` | **NO — LEAK, per frame** (same root cause as #4) |
| 6 | `vkCreateSemaphore` ×2 | `VulkanSemaphore.skia.cs:23` via `VulkanSemaphorePair` `:47-48` | `vkDestroySemaphore` `:37`, from `VulkanDisplay.Dispose` `:384` | Yes — created **once** per display (`VulkanDisplay.skia.cs:36`), not per frame |
| 7 | `vkCreateImage` (render image) | `VulkanImage.skia.cs:115` | `vkDestroyImage` `:183` in `Dispose`; error-path destroy at `:125` if memory alloc fails | Mostly — gap at `:149`, see §1b |
| 8 | `vkAllocateMemory` (render image) | `VulkanImage.skia.cs:83` | `vkFreeMemory` `:188` in `Dispose` | Mostly — gap at `:149`, see §1b |
| 9 | `vkCreateImageView` (render image) | `VulkanImage.skia.cs:149` | `vkDestroyImageView` `:178` in `Dispose` | Yes |
| 10 | `VkSurfaceKHR` (platform `CreateSurface`) | `VulkanKhrSurface.skia.cs:21` | `vkDestroySurfaceKHR` `:68` in `Dispose`, from `VulkanDisplay.skia.cs:212`, `:389` | Yes, but ordering defect — §4b |
| 11 | temp `VkSurfaceKHR` for device selection | `VulkanContext.skia.cs:63` | `vkDestroySurfaceKHR` `VulkanContext.skia.cs:70` | Yes |

**Swapchain images themselves** (`vkGetSwapchainImagesKHR`, `VulkanDisplay.skia.cs:166-171`) are
owned by the swapchain and must *not* be individually destroyed. The code correctly does not
destroy them. **Not a leak.**

### 1b. Error-path gap in `VulkanImageBase.Initialize`

`VulkanImage.skia.cs:115-150`: if `vkCreateImageView` at `:149` fails (`ThrowOnError`), the
already-created `VkImage` (`:115`) and the already-allocated `VkDeviceMemory` (`:83`) are **not**
destroyed — the exception escapes the constructor (`VulkanImage.skia.cs:199`), so no caller holds
a reference to call `Dispose()`. Contrast with `:119-127`, which *does* have a try/catch that
destroys the image if `CreateMemory` throws. This is a real leak of a full-screen image + its
device memory, but its cadence is **once per render-image creation** (init / `Resize` /
`ResizeRenderImage`), not per frame, and only on an error path. **Not the crash driver.**

---

## 2. `CreateSwapchainImages` — are old views destroyed before new ones are created?

**Yes.** `VulkanDisplay.skia.cs:161-175` opens with `DestroyCurrentImageViews()` at `:163`, which
loops `vkDestroyImageView` over `_swapchainImageViews` and resets the array to
`Array.Empty<VkImageView>()` (`:155-158`). The array is then reallocated at `:172` and refilled at
`:173-174`. Every entry point into `CreateSwapchainImages` (`:38` ctor, `:207` `RecreateSwapchain`,
`:229` `RecreateSwapchainSafe`) is therefore covered. `RecreateSwapchainSafe` additionally calls
`DestroyCurrentImageViews()` itself at `:226` — redundant but harmless (the second call short-
circuits on `Length <= 0` at `:153`).

**Image views are not leaking.** Say so plainly rather than padding the list.

One caveat: `DestroyCurrentImageViews` destroys the views *before* `DeviceWaitIdle` in the
`RecreateSwapchainSafe` ordering is... actually fine (`:225` waits idle, then `:226` destroys).
In `RecreateSwapchain` the wait is at `:204` before `CreateSwapchain`/`CreateSwapchainImages`. OK.

---

## 3. Command buffers, fences, semaphores — pooled? bounded? **[PRIMARY FINDING]**

**Semaphores: fine.** One `VulkanSemaphorePair` per display, created at `VulkanDisplay.skia.cs:36`,
disposed at `:384`. Two semaphores total, reused every frame. Not a leak.

**Command buffers and fences: unbounded per-frame leak. This is the bug.**

The chain, verified end to end:

1. `VulkanDisplay` constructs its pool with the **default `autoFree: false`**:
   `VulkanDisplay.skia.cs:37` — `CommandBufferPool = new VulkanCommandBufferPool(_context);`
   against the signature `VulkanCommandBufferPool(IVulkanPlatformGraphicsContext context, bool autoFree = false)`
   (`VulkanCommandBufferPool.skia.cs:18`).

2. `CreateCommandBuffer` only reclaims when `_autoFree` is set:
   ```csharp
   // VulkanCommandBufferPool.skia.cs:59-63
   public unsafe VulkanCommandBuffer CreateCommandBuffer()
   {
       if (_autoFree)
           FreeFinishedCommandBuffers();
   ```
   With `autoFree == false` this branch is dead. It then does
   `vkAllocateCommandBuffers` (`:72-73`) and returns a **new** `VulkanCommandBuffer` (`:75`),
   whose constructor creates a **new `VkFence`** (`VulkanCommandBuffer.skia.cs:25`).

3. Every submit permanently enqueues the buffer:
   `VulkanCommandBuffer.skia.cs:108` — `_pool.AddSubmittedCommandBuffer(this);`
   → `VulkanCommandBufferPool.skia.cs:78` — `_commandBuffers.Enqueue(buffer);`

4. **The queue is never drained.** Repository-wide grep for `FreeUsedCommandBuffers` /
   `FreeFinishedCommandBuffers` returns only:
   - the definitions (`VulkanCommandBufferPool.skia.cs:32`, `:38`),
   - the dead `_autoFree` call at `:62`,
   - `Dispose()` at `:52`.

   There is **no** `new VulkanCommandBufferPool(..., true)` anywhere in `src/`.
   So the only reclaim is `VulkanDisplay.Dispose()` → `CommandBufferPool?.Dispose()`
   (`VulkanDisplay.skia.cs:387`).

5. Cadence, Android: `UnoSKVulkanView.RenderLoop` (`UnoSKVulkanView.cs:143-159`) calls
   `RenderFrame()` on every `_renderRequested`, i.e. every composition frame while scrolling.
   That reaches `VulkanContext.RenderFrame` (`VulkanContext.skia.cs:181`) →
   `_display.StartPresentation()` (`:207`) → `CommandBufferPool.CreateCommandBuffer()`
   (`VulkanDisplay.skia.cs:264`) → `EndPresentation` → `commandBuffer.Submit(...)`
   (`VulkanDisplay.skia.cs:356`) → enqueue.
   The Win32/X11 split path leaks identically via `VulkanContext.skia.cs:327`.

**Net effect per frame:** 1 leaked `VkCommandBuffer` (with its recorded blit + 3 pipeline
barriers still resident in driver command memory), 1 leaked `VkFence`, 1 managed
`VulkanCommandBuffer` rooted by the queue. At 120 Hz on a Fold 7, **~7,200 command buffers and
7,200 fences per minute**, none reclaimed until the Android surface is destroyed or resized.

**Aggravating factor:** the pool is created with
`VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT` (`VulkanCommandBufferPool.skia.cs:25`). That
flag tells the driver each command buffer must be independently resettable, which on Qualcomm/
Adreno means **per-buffer** command-stream allocation blocks rather than a shared linear pool
arena. Every leaked buffer therefore pins its own driver allocation. The GSL log line
`Allocation size: (128 KB)` is consistent with per-command-buffer command-stream chunks.

**Classification:** (a) native/driver objects. There is no finalizer on `VulkanCommandBuffer`,
`VulkanFence`, or `VulkanCommandBufferPool`, and the managed wrappers are strongly rooted by
`_commandBuffers` anyway — so the GC can never rescue this. That matches the observed modest
managed heap (39→109 MB) alongside native exhaustion.

**Secondary, same area:** a command buffer created but *not* submitted is never enqueued, so it
escapes even a fixed drain and survives until `vkDestroyCommandPool`. Reachable when
`EndPresentation` throws before `Submit` in the `VulkanContext.BlitAndPresent` try block
(`VulkanContext.skia.cs:327-336`) and when `RenderFrame`'s OUT_OF_DATE/SUBOPTIMAL catch
(`VulkanContext.skia.cs:217-222`) unwinds past `StartPresentation`. Low cadence normally, but
see §5b — under a persistent SUBOPTIMAL condition it becomes per-frame too.

---

## 4. `oldSwapchain` handling — one swapchain leaked per recreate?

**No, the normal path is correct.** `VulkanDisplay.skia.cs:129` sets
`oldSwapchain = oldDisplay?._swapchain ?? default`, and immediately after a successful
`vkCreateSwapchainKHR` line `:133` calls `oldDisplay?.DestroySwapchain()`, which issues
`vkDestroySwapchainKHR` and nulls the handle (`:137-142`). Both recreate entry points pass
`this` as `oldDisplay` (`:205`, `:227`), so the old swapchain is destroyed exactly once before
`_swapchain` is overwritten. **Not a leak.**

### 4a. Error path — the one hole
If `vkCreateSwapchainKHR` at `:131` fails, `ThrowOnError` throws *before* `:133`. The old
swapchain handle survives in `oldDisplay._swapchain` (good, it can still be destroyed later by
`Dispose`), so nothing leaks — but `RecreateSwapchainSafe` at `:227` has already destroyed the
image views at `:226`, leaving the display in a half-torn state. Correctness bug, not a leak.

### 4b. `RecreateSurface` destroys the surface *before* the swapchain that owns it
```csharp
// VulkanDisplay.skia.cs:210-217
private void RecreateSurface()
{
    _surface?.Dispose();      // :212  vkDestroySurfaceKHR
    _surface = null;
    _surface = new VulkanKhrSurface(_context, _platformSurface);
    DestroySwapchain();        // :215  vkDestroySwapchainKHR — surface already gone
    RecreateSwapchain();
}
```
This violates the Vulkan valid-usage rule that a `VkSurfaceKHR` must outlive every swapchain
created from it (VUID-vkDestroySurfaceKHR-surface-01266). On Adreno the swapchain's
`ANativeWindow` buffer references are released by the surface teardown path; destroying the
swapchain afterwards is undefined and can strand driver-side buffer handles. **Marked as a
plausible secondary driver-side leak — UNVERIFIED against driver behaviour**, and its cadence is
low (only `VK_ERROR_SURFACE_LOST_KHR`, `VulkanDisplay.skia.cs:255-256`, or `_surface == null`
at `:199-203`, which cannot happen after construction). Not the crash driver, but it should be
reordered.

---

## 5. The crash is inside `vkCmdBlitImage` — what grows per blit?

### 5a. Direct answer: the recording itself, because the command buffer is never freed or reset

`BlitImageToCurrentImage` (`VulkanDisplay.skia.cs:301-345`) records, into the frame's command
buffer: a pipeline barrier (`:303`), `vkCmdBlitImage` (`:335`), and a second barrier (`:341`).
`EndPresentation` adds a third barrier (`:349`) then submits (`:356`).

`vkCmdBlitImage` is one of the heaviest recording commands on Adreno — the driver must patch in a
blit/resolve program and its state, which is why frame #07/#08 of the crash stack is
`scudo_calloc`/`calloc` reached from `qglinternal::vkCmdBlitImage2`. That heap block belongs to
the *command buffer's* recording arena. Because the buffer is neither `vkResetCommandBuffer`'d,
nor `vkFreeCommandBuffers`'d, nor reclaimed via `vkResetCommandPool`, **each frame's blit
recording arena is retained forever.** The allocation that finally fails is the same class of
allocation that has been accumulating since the app started scrolling — the crash is at the
allocation site, not at the leak site.

This also explains Signature A preceding Signature B: `sharedmem_gpumem_alloc: mmap failed
errno 12` with the device still holding ~1.4 GB free is a **per-process** mapping/address-space
exhaustion, precisely what thousands of small independent driver mappings produce.

### 5b. Amplifier: the swapchain may be recreated *every frame* on this device

`CreateSwapchain` deliberately requests `preTransform = IDENTITY` when the surface reports a
rotated `currentTransform` and identity is supported:
```csharp
// VulkanDisplay.skia.cs:123-125
preTransform = supportsIdentityTransform && isRotated
    ? VkSurfaceTransformFlagsKHR.VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR
    : capabilities.currentTransform,
```
When `preTransform != currentTransform`, Android/Adreno returns **`VK_SUBOPTIMAL_KHR` from every
`vkAcquireNextImageKHR`**. `StartPresentation` treats SUBOPTIMAL as "recreate and retry"
(`VulkanDisplay.skia.cs:253-254`), so on a device in that state the loop performs
`DeviceWaitIdle` + full swapchain teardown/recreate on *every single frame*. Two consequences:

- **Semaphore misuse.** `VK_SUBOPTIMAL_KHR` means the acquire **succeeded** — `_nextImage` is
  valid and `_semaphorePair.ImageAvailableSemaphore` (`:251`) has a pending signal. The loop
  then re-enters `AcquireNextImageKHR` with the *same* semaphore (`:247-252`), which is invalid
  usage (a semaphore passed to acquire must have no pending signal operation), and the
  previously acquired image is never presented — its swapchain buffer is dropped from the
  acquire/present cycle. `DeviceWaitIdle` at `:204` does not drain a presentation-engine signal.
- **Recreate churn.** Per-frame `vkCreateSwapchainKHR` / `vkDestroySwapchainKHR` on the Adreno
  BufferQueue is not free even though the handles balance; released buffers are reclaimed lazily.

Cadence of 5b is device-state dependent (a Fold 7 is exactly the kind of device that reports a
rotated `currentTransform`), so I mark it **medium confidence, UNVERIFIED on-device**. It is not
required to explain the crash — §3 alone does — but if it is active it multiplies §3's leak rate
and adds its own pressure. Logging whether `AcquireNextImageKHR` returns SUBOPTIMAL, and how
often `RecreateSwapchain` runs, would settle it in one run.

### 5c. What is *not* growing
- **Descriptor pools:** this code creates none. No `vkCreateDescriptorPool`/
  `vkAllocateDescriptorSets` exists anywhere under `src/Uno.UI/Vulkan/` (grep of
  `DeviceApi.(Create|Allocate)` returns only swapchain, image view, command pool, command
  buffers, fence, semaphore, image, memory). Skia's own descriptor pools live inside
  `GRContext` and are recycled by Skia — out of scope here.
- **The command *pool* itself** is created once (`VulkanCommandBufferPool.skia.cs:28`). It is the
  buffers inside it that grow, not the pool count.
- **`GRBackendRenderTarget` / `SKSurface`** are cached and reused across frames
  (`VulkanContext.skia.cs:29-30`, created lazily at `:250-253`, disposed only on resize at
  `:256-267`). **Not a leak.**
- **The intermediate `VulkanImage`** is created once per size (`VulkanContext.skia.cs:83`, `:150`,
  `:171`) and disposed before each recreation (`:145`, `:170`). **Not a leak.**
- **Managed per-frame garbage** — the three `new[]{…}` arrays at `VulkanDisplay.skia.cs:356-358`
  and `new VkPresentModeKHR[…]` at `:64` — is ordinary GC garbage, collected normally, and is
  consistent with the modest 39→109 MB managed growth. **Not a leak.**

---

## Fix direction (not applied — audit only)

1. **Drain the pool every frame.** Either construct it with `autoFree: true`
   (`VulkanDisplay.skia.cs:37`) so `CreateCommandBuffer` reclaims finished buffers
   (`VulkanCommandBufferPool.skia.cs:61-62`), or better, stop allocating per frame at all: keep a
   small ring of N command buffers (N = swapchain image count) with their fences, and
   `vkResetCommandBuffer` + `BeginRecording` the one whose fence is signalled. `autoFree: true`
   alone still allocates/frees every frame — cheap relative to the leak, but a ring is the real fix.
   Note `VulkanCommandBuffer` also needs `_hasStarted`/`_hasEnded` (`:16-17`) reset to be reusable.
2. **Free non-submitted command buffers** on the exception paths in
   `VulkanContext.skia.cs:325-336` and `:217-222` (try/finally around the buffer's lifetime).
3. **Reorder `RecreateSurface`** (`VulkanDisplay.skia.cs:210-217`) to `DestroySwapchain()` →
   `_surface.Dispose()` → create new surface → recreate swapchain.
4. **Do not re-acquire with a pending semaphore.** Handle `VK_SUBOPTIMAL_KHR` by *presenting the
   frame anyway* and recreating at the top of the next frame, or use a per-frame semaphore ring.
5. **Add error-path cleanup** for the `vkCreateImageView` failure in `VulkanImage.skia.cs:149`.

## Validation status

- **Code review (by inspection): complete.** Every claim above cites a file+line in this worktree.
- **Compile validation: not performed** (audit only, no files changed outside
  `specs/scroll-smoothness/vulkan-leak/`).
- **Runtime validation: not performed.** The decisive on-device evidence would be
  `vkAllocateCommandBuffers` call count vs `vkFreeCommandBuffers` call count over a 30 s scroll
  (expect thousands vs zero), plus a log of `AcquireNextImageKHR` returning `VK_SUBOPTIMAL_KHR`
  to confirm or dismiss §5b.
