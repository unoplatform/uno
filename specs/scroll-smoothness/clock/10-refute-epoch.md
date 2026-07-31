# Epoch audit — adversarial re-verification of every platform note's `Stopwatch` claim

Scope: notes `01-win32.md` … `08-avalonia.md` in this folder each assert something about how a
platform frame timestamp relates to .NET `Stopwatch.GetTimestamp()`. This document re-derives every
one of those relationships **from primary source**, independently, and records where the notes were
right, where they were right for the wrong reason, and where they were wrong.

**Rule applied throughout:** default to *needs-offset* unless equality is proven. A fixed offset
measured once at startup is acceptable; a **drifting** relationship is not, and is called out
explicitly where it can occur.

---

## 0. Verdict table

| Target | `Stopwatch.GetTimestamp()` is | `Stopwatch.Frequency` | Platform frame timestamp | Relationship | Can it drift? |
|---|---|---|---|---|---|
| **Win32 / WinUI** | `QueryPerformanceCounter` — direct P/Invoke, no minipal | `QueryPerformanceFrequency()`, **read at runtime** (10 000 000 measured here) | `DWM_TIMING_INFO.qpcVBlank`, `DCOMPOSITION_FRAME_STATS.targetTime` — QPC | **Identity.** Zero offset, zero scale (`s_tickFrequency == 1.0` when QPF == 10 MHz) | **No** — literally the same counter read |
| **Android** | `clock_gettime(CLOCK_MONOTONIC)` → ns | **hard-coded** `1_000_000_000` | `Choreographer` `frameTimeNanos`, `ExpectedPresentationTimeNanos` — `System.nanoTime()` = `CLOCK_MONOTONIC` ns | **Identity after unit scale** (`ns / 100`) | **No** — same clock, same kernel, same process |
| **Apple (iOS / tvOS / Mac Catalyst / macOS)** | `clock_gettime_nsec_np(CLOCK_UPTIME_RAW)` = `mach_absolute_time() * numer / denom` → ns | **hard-coded** `1_000_000_000` | `CADisplayLink.timestamp` / `.targetTimestamp`, `CACurrentMediaTime()` — mach-absolute seconds | **Identity after unit scale** (`s * 1e9`) | **No** — same mach counter |
| **Browser WASM — single-threaded pack (what Uno ships)** | `__clock_gettime(CLOCK_MONOTONIC)` → `emscripten_get_now()` → **`performance.now()`** | **hard-coded** `1_000_000_000` | `requestAnimationFrame(t)` — `DOMHighResTimeStamp` ms on the document's `timeOrigin` | **Identity after unit scale** (`ms * 10 000`). Offset is exactly **0** | **No** — same JS call, same Window |
| **Browser WASM — `WasmEnableThreads` pack** | same chain, but emscripten emits `performance.timeOrigin + performance.now()` | 1e9 | same rAF value | **Fixed offset** = the document's `performance.timeOrigin` (~1.8e12 ms) | **No** — `timeOrigin` is a per-document constant |
| **Linux — DRM/KMS FrameBuffer host** | `clock_gettime(CLOCK_MONOTONIC)` → ns | 1e9 | `drm_event_vblank` `tv_sec`/`tv_usec` — `CLOCK_MONOTONIC` (kernel ≥ 4.15 always) | **Identity after unit scale** (`µs * 1000`) | **No** — same kernel clock, same process |
| **Linux — X11 Present / GLX `ust`** | `clock_gettime(CLOCK_MONOTONIC)` → ns | 1e9 | X server `GetTimeInMicros()` — `CLOCK_MONOTONIC` µs **on the X server's machine, in the X server's time namespace** | **Identity after unit scale — CONDITIONALLY.** Needs a runtime discriminator (§6) | **YES, in three failure modes.** This is the only target where the relationship can drift |

**Bottom line:** four of the six targets are a proven identity. One (threaded WASM) is a genuine
fixed offset. One (X11) is an identity *given the common configuration* but has three ways to become
a foreign — and in two of them **drifting** — epoch, and must therefore be validated, not assumed.

---

## 1. Method — what "verified" means in this document

Three evidence tiers are used and always labelled:

| Label | Meaning |
|---|---|
| **Source** | The actual file was fetched at a pinned ref and read. Ref and line numbers cited. |
| **Binary** | The *shipped* artifact on this machine was inspected — the thing that actually runs. |
| **Runtime** | Code was executed here and the output is reproduced. |

Everything else is **UNVERIFIED** and marked as such inline.

`dotnet/runtime` was read at tag **`v10.0.8`** (not `main`) — the notes cite `main`, which is not a
reproducible ref. `v10.0.8` matches the shared runtime and the runtime packs installed on this
machine (`C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.8`).

The Uno code under audit:

| File:line | Code |
|---|---|
| `src/Uno.UI.Composition/Composition/Compositor.cs:33` | `private static readonly double s_tickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;` |
| `src/Uno.UI.Composition/Composition/Compositor.cs:38` | `public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));` |
| `src/Uno.UI.Composition/Composition/Compositor.skia.cs:244-290` | `GetFrameTimestamp(long raw)` — the estimator |
| `src/Uno.UI.Composition/Composition/Compositor.skia.cs:312-313` | `var frameTimestamp = GetFrameTimestamp(TimestampInTicks);` — the record-time clock read |
| `src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.InertiaProcessor.cs:355-357` | `_startTimestamp = compositor.TimestampInTicks;` then differenced against the `FrameStarting` value |
| `src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs:669-677` | `var now = compositor.TimestampInTicks;` → `_wheelDecayH.Start(…, now)`, then ticked from `FrameStarting` |
| `src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs:621-628` | fling anchors `_flingStartTimestamp` on the **frame** timestamp, so it is epoch-agnostic |

The two `TimestampInTicks`-anchored drivers are why the epoch question is load-bearing: they mix a
`Stopwatch`-epoch start value with a `FrameStarting`-epoch tick value. If a platform frame timestamp
on a foreign epoch were fed to `FrameStarting`, both would compute a garbage first `elapsed`.

---

## 2. The .NET side, per platform — read from `dotnet/runtime` v10.0.8

### 2.1 The dispatch is a *compile-time file swap*, not a runtime branch

`src/libraries/System.Private.CoreLib/src/System.Private.CoreLib.Shared.projitems`:

| Line | Content |
|---|---|
| `:1630` | `<ItemGroup Condition="'$(TargetsWindows)' == 'true'">` |
| `:2240` | `<Compile Include="…System\Diagnostics\Stopwatch.Windows.cs" />` |
| `:2340` | `<ItemGroup Condition="'$(TargetsUnix)' == 'true' or '$(TargetsBrowser)' == 'true' or '$(TargetsWasi)' == 'true'">` |
| `:2567` | `<Compile Include="…System\Diagnostics\Stopwatch.Unix.cs" />` |

**Source.** So there are exactly **two** implementations, and **browser-wasm is in the Unix bucket** —
there is no `Stopwatch.Browser.cs` (fetch returns 404), no `Stopwatch.Wasi.cs`, no `Stopwatch.Mono.cs`.

All three runtime flavours compile the same file:

- `src/coreclr/nativeaot/System.Private.CoreLib/src/System.Private.CoreLib.csproj:554` —
  `<Import Project="$(LibrariesProjectRoot)\System.Private.CoreLib\src\System.Private.CoreLib.Shared.projitems" Label="Shared" />`
- `src/mono/System.Private.CoreLib/System.Private.CoreLib.csproj:283` — same import.

**Source.** This closes note `02-android.md` §3.3's *"UNVERIFIED at the binary level for
CoreCLR/NativeAOT-on-Android"* caveat at the **source** level: Mono, CoreCLR and NativeAOT cannot
diverge here, because they share the file. (Binary confirmation still only exists for the Mono
flavour — see §3.2.)

### 2.2 Windows — QPC, directly, **not** through minipal

`src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Windows.cs:8-28`:

```csharp
private static unsafe long GetFrequency()
{
    long resolution;
    Interop.BOOL result = Interop.Kernel32.QueryPerformanceFrequency(&resolution);
    Debug.Assert(result != Interop.BOOL.FALSE);
    return resolution;
}

public static unsafe long GetTimestamp()
{
    long timestamp;
    Interop.BOOL result = Interop.Kernel32.QueryPerformanceCounter(&timestamp);
    Debug.Assert(result != Interop.BOOL.FALSE);
    return timestamp;
}
```

**Source.** `Stopwatch.GetTimestamp()` on Windows is a **direct `kernel32!QueryPerformanceCounter`
P/Invoke**. `Stopwatch.Frequency` is a **direct `QueryPerformanceFrequency`** read, evaluated once in
the static initialiser (`Stopwatch.cs:13`).

### 2.3 Unix / Android / Apple / Browser — `Interop.Sys.GetTimestamp` → minipal

`Stopwatch.Unix.cs:8-14`:

```csharp
private static long GetFrequency()
{
    const long SecondsToNanoSeconds = 1000000000;
    return SecondsToNanoSeconds;
}

public static long GetTimestamp() => Interop.Sys.GetTimestamp();
```

`Common/src/Interop/Unix/System.Native/Interop.GetTimestamp.cs`:

```csharp
[LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetTimestamp")]
[SuppressGCTransition]
internal static partial long GetTimestamp();
```

`src/native/libs/System.Native/pal_time.c:82-85`:

```c
int64_t SystemNative_GetTimestamp(void)
{
    return minipal_hires_ticks();
}
```

`src/native/minipal/time.c:76-92` (non-Windows branch):

```c
int64_t minipal_hires_ticks(void)
{
#if HAVE_CLOCK_GETTIME_NSEC_NP
    return (int64_t)clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
#elif HAVE_CLOCK_MONOTONIC
    struct timespec ts;
    int result = clock_gettime(CLOCK_MONOTONIC, &ts);
    if (result != 0)
    {
        assert(!"clock_gettime(CLOCK_MONOTONIC) failed");
    }

    return ((int64_t)(ts.tv_sec) * (int64_t)(tccSecondsToNanoSeconds)) + (int64_t)(ts.tv_nsec);
#else
    #error "minipal_hires_ticks requires clock_gettime_nsec_np or clock_gettime to be supported."
#endif
}
```

**Source.** Two corrections to what the notes reproduce:

1. The `#else` is an `#elif HAVE_CLOCK_MONOTONIC` with a hard `#error` fallback, not a bare `#else`.
   The notes' paraphrase is harmless but the `#error` matters: there is **no third clock** any
   platform can silently fall into.
2. The branch is selected by a CMake feature probe at **runtime-pack build time**, per-RID —
   `src/native/minipal/configure.cmake:14-16`:
   ```cmake
   check_symbol_exists(CLOCK_MONOTONIC time.h HAVE_CLOCK_MONOTONIC)
   check_symbol_exists(CLOCK_MONOTONIC_COARSE time.h HAVE_CLOCK_MONOTONIC_COARSE)
   check_symbol_exists(clock_gettime_nsec_np time.h HAVE_CLOCK_GETTIME_NSEC_NP)
   ```
   **This means the correct verification is not "read the `#if`", it is "inspect the shipped
   binary for that RID".** §3 does that.

### 2.4 Hazard the notes did not raise: `Stopwatch.Frequency` on Unix is a *hard-coded constant*

`Stopwatch.Unix.cs:10-11` returns a literal `1000000000`. It does **not** call
`minipal_hires_tick_frequency()`. That function separately returns `tccSecondsToNanoSeconds`
(`minipal/time.c:71-74`) — a second, independent constant, in a different language, in a different
file. They agree today. Nothing in the build enforces that they keep agreeing.

Consequence for Uno: `s_tickFrequency` (`Compositor.cs:33`) is `1e7 / 1e9 = 0.01` **by declaration,
not by measurement**, on every non-Windows target. That is fine — but it means a runtime assertion
comparing `Compositor.TimestampInTicks` against the platform frame clock is worth having on every
platform, not just the ones flagged as risky.

### 2.5 Hazard the notes did not raise: the double multiply diverges from integer `/ 100` at high uptime

`TimestampInTicks` is `(long)(rawNs * 0.01)` — a `double` round-trip. A platform frame timestamp
would naturally be converted with integer `frameTimeNanos / 100` (as `02-android.md` §3.4
recommends). Those two are **not** the same function once the ns count exceeds 2^53.

**Runtime** (`scratchpad/epochcheck/convcheck.cs`, 200 000 samples per row):

```
s_tickFrequency = 0.01  exact? True
uptime ~    0.00 d : mismatches       0/200000  maxDelta=0 ticks (0 us)
uptime ~    0.04 d : mismatches       0/200000  maxDelta=0 ticks (0 us)
uptime ~    1.00 d : mismatches       0/200000  maxDelta=0 ticks (0 us)
uptime ~   10.00 d : mismatches       0/200000  maxDelta=0 ticks (0 us)
uptime ~  104.25 d : mismatches    1891/200000  maxDelta=1 ticks (0.1 us)
uptime ~  365.00 d : mismatches    2971/200000  maxDelta=1 ticks (0.1 us)
```

2^53 ns = 104.25 days of uptime. Below that the two conversions are bit-identical; above it they
differ by at most 1 tick (100 ns). **Severity: negligible** (100 ns against a 8 333 µs frame), but
it is a real discontinuity, and if a future assertion checks for exact equality it will fire on
long-uptime Linux boxes. Use a tolerance, not equality.

---

## 3. Per-platform re-verification

### 3.1 Windows — claim **CONFIRMED**, and the notes' *mechanism* was wrong in one place

Claim under audit (`01-win32.md` §2, `06-winui.md` §6): *`Stopwatch.GetTimestamp()` on Windows **is**
`QueryPerformanceCounter`; `Stopwatch.Frequency` **is** `QueryPerformanceFrequency`; no conversion.*

**CONFIRMED at three tiers.**

- **Source:** `Stopwatch.Windows.cs:8-28`, quoted in §2.2. The notes verified this empirically and
  explicitly disclaimed source reading (`01-win32.md:110-111`: *"Not read from `dotnet/runtime`
  source — no local clone"*). It is now read. They were right.
- **Runtime, independent re-measurement** (`scratchpad/epochcheck/qpccheck.cs`, this machine,
  Windows 11 Pro 10.0.29595 x64, 20 000 interleaved `QPC(a) / Stopwatch(s) / QPC(b)` triples):

  ```
  QueryPerformanceFrequency = 10000000
  Stopwatch.Frequency       = 10000000
  QPF == Stopwatch.Frequency: True
  s_tickFrequency (1e7/F)   = 1
  interleaved QPC(a)/SW(s)/QPC(b) over 20000: out-of-bracket=0, s==a in 16426 trials, maxSkew=0 ticks
  QPC now = 441251241725  => 12.257 h since boot-ish epoch
  GetTickCount64-equivalent uptime for cross-check: 12.257 h
  ```

  Zero out-of-bracket over 20 000 trials, and 82 % of samples were *bit-identical* to the QPC read
  taken immediately before. The QPC epoch also matches `Environment.TickCount64` (boot) to three
  decimals in hours — i.e. it is the machine-global counter DWM publishes on, not a per-process one.

- **Runtime, direct epoch cross-check against the platform frame timestamp**
  (`scratchpad/epochcheck/dwmepoch.cs` — `DwmFlush()`, then `Stopwatch.GetTimestamp()`, then
  `DwmGetCompositionTimingInfo(NULL, …)` with `cbSize = 292`):

  ```
  iter |   sw_before   |   qpcVBlank   |  vblank-sw(ms) | refreshPeriod | cRefresh
     0 |  441722671221 |  441722750587 |        7.9366 |         83325 | 3
     1 |  441722837756 |  441722917222 |        7.9466 |         83325 | 4
     2 |  441722920718 |  441723000551 |        7.9833 |         83325 | 5
     3 |  441723004476 |  441723083871 |        7.9395 |         83325 | 6
     4 |  441723087877 |  441723167189 |        7.9312 |         83325 | 7
     5 |  441723170670 |  441723250519 |        7.9849 |         83325 | 8
     6 |  441723253182 |  441723333837 |        8.0655 |         83325 | 9
     7 |  441723336950 |  441723417154 |        8.0204 |         83325 | 10
  ```

  `qpcVBlank − Stopwatch.GetTimestamp()` is **7.93–8.07 ms** against an 8.3325 ms refresh period —
  i.e. 0.95 ± 0.01 refresh periods, eight times in a row, with `cRefresh` advancing by exactly 1.
  A foreign epoch would show a difference of hours or of 1.7e15. This is a direct, independent
  confirmation that the DWM vblank timestamp and `Stopwatch` are on the same counter with **zero
  offset**. It also independently reproduces `01-win32.md`'s "~0.93 periods ahead" figure.

**Refutation — `08-avalonia.md:286`.** That note says:

> `Stopwatch.GetTimestamp()` bottoms out in `minipal_hires_ticks()`. Verified from dotnet/runtime `main`

**This is false on Windows.** `Stopwatch.Windows.cs` P/Invokes `Interop.Kernel32.QueryPerformanceCounter`
directly; it never touches `minipal`. `minipal_hires_ticks` exists and *also* calls
`QueryPerformanceCounter` (`minipal/time.c:12-19`), so the note reaches the right **value** — but the
stated chain is wrong, and it is wrong in the direction that matters: it implies a shared code path
between Windows and Unix that does not exist, and it implies `Stopwatch.Frequency` comes from
`minipal_hires_tick_frequency()` when in fact it is `QueryPerformanceFrequency` on Windows and a
**hard-coded 1e9** on Unix (§2.4). Correct chain: `Stopwatch.GetTimestamp()` → `Interop.Kernel32.QueryPerformanceCounter`.

**Residual caveat the notes state correctly.** `QueryPerformanceFrequency` is *read*, not assumed —
it is 10 MHz here and by OS convention on Windows 8+, but `s_tickFrequency` must keep being applied
rather than hard-coded to 1.0, because DWM's `qpcVBlank` / `qpcRefreshPeriod` are in **QPC units,
whatever QPF is**. `01-win32.md:105-106` already says exactly this. Keep it.

**`DCOMPOSITION_FRAME_STATS` (`06-winui.md`).** The QPC-units claim for `startTime` / `targetTime` /
`framePeriod` is **documentation + that note's own runtime measurement**; I did not re-run it. Since
§3.1 establishes `Stopwatch == QPC` from source, that note's measured `framePeriod` = 83 333–83 334
counts = 8.3333 ms on a 120 Hz panel is itself proof of QPC units. **Consistent, not independently
re-measured.**

### 3.2 Android — claim **CONFIRMED**, on the strength of AOSP source rather than inference

Claim under audit (`02-android.md` §3): *`frameTimeNanos` and `Stopwatch.GetTimestamp()` are the same
clock, `CLOCK_MONOTONIC`; conversion is `frameTimeNanos / 100`, no offset.*

**CONFIRMED.**

**Android side — Source, AOSP `main`:**

- `platform/libcore` `ojluni/src/main/native/System.c:254-258`:
  ```c
  static jlong System_nanoTime() {
    struct timespec now;
    clock_gettime(CLOCK_MONOTONIC, &now);
    return now.tv_sec * 1000000000LL + now.tv_nsec;
  }
  ```
- `platform/frameworks/base` `core/java/android/view/Choreographer.java`:
  - `:1269-1271` (`FrameCallback.doFrame` javadoc) — *"The time in nanoseconds when the frame started
    being rendered, in the `System.nanoTime()` timebase."*
  - `:1313-1314` (`FrameTimeline.getExpectedPresentationTimeNanos`) — *"The time in `System.nanoTime()`
    timebase which this frame is expected to be presented."*
  - `:1322-1323` (`getDeadlineNanos`) — same timebase.
- Corroborating, for anyone tempted by the other Android clock:
  `platform/system/core` `libutils/SystemClock.cpp:48-50` — `uptimeNanos()` = `systemTime(SYSTEM_TIME_MONOTONIC)`;
  `:64-75` — `elapsedRealtimeNano()` = `clock_gettime(CLOCK_BOOTTIME)`.
  `libutils/include/utils/Timers.h:75-81` gives `SYSTEM_TIME_MONOTONIC = 1`, and
  `libutils/Timers.cpp:33-41` indexes `{CLOCK_REALTIME, CLOCK_MONOTONIC, CLOCK_PROCESS_CPUTIME_ID,
  CLOCK_THREAD_CPUTIME_ID, CLOCK_BOOTTIME}` — so `SYSTEM_TIME_MONOTONIC` → `CLOCK_MONOTONIC`.
  **`SystemClock.elapsedRealtime*` is `CLOCK_BOOTTIME` and is a different epoch from everything in
  this document.** Do not use it.

**.NET side — Binary, the shipped artifact:**
`C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.android-arm64\10.0.8\runtimes\android-arm64\native\libSystem.Native.so`

```
clock_gettime_nsec_np -> 0
clock_gettime         -> 1
SystemNative_GetTimestamp -> 1
minipal_hires_ticks   -> 0
```

No `clock_gettime_nsec_np` anywhere in the binary → the CMake probe (§2.3) resolved
`HAVE_CLOCK_GETTIME_NSEC_NP = 0` for bionic → the `CLOCK_MONOTONIC` branch was compiled in.
`minipal_hires_ticks` has no symbol because it is inlined into `SystemNative_GetTimestamp`, which
matches `02-android.md` §3.3's disassembly (a single tail-call `b`). Independent method, same
conclusion.

**Both sides are in the same process on the same kernel**, and `CLOCK_MONOTONIC` on Linux is
system-wide. **Cannot drift.** Conversion is `frameTimeNanos / 100`.

The one thing `02-android.md` flagged as unverified — CoreCLR-on-Android and NativeAOT-on-Android —
is now closed at the **source** level by §2.1 (all three flavours import the same
`Stopwatch.Unix.cs`) and by the fact that all three link `libSystem.Native` built from the same
`pal_time.c` / `minipal/time.c`. Still **UNVERIFIED at the binary level** for those two flavours —
their runtime packs are not installed on this machine.

### 3.3 Apple — claim **CONFIRMED**, and the note's remaining "UNVERIFIED" hop is now closed

Claims under audit (`03-apple.md` §3, `08-avalonia.md` row 3):
1. *`Stopwatch.GetTimestamp()` on Apple = `clock_gettime_nsec_np(CLOCK_UPTIME_RAW)`* — the note
   verified this from source but marked runtime-flavour routing UNVERIFIED.
2. *`CLOCK_UPTIME_RAW` **is** `mach_absolute_time()`* — the note read Apple's Libc; `08-avalonia.md`
   marked the same claim *"Man-page-level, **UNVERIFIED at code level**"*.
3. *`CACurrentMediaTime()` / `CADisplayLink.timestamp` are in that base* — the note marked this
   *"**UNVERIFIED at Apple source level** — CoreAnimation is closed source"*.

**(1) CONFIRMED — Binary, four shipped runtime packs:**

| Pack (10.0.8) | `clock_gettime_nsec_np` in `libSystem.Native.dylib` | `mach_absolute_time` |
|---|---|---|
| `Microsoft.NETCore.App.Runtime.Mono.ios-arm64` | present | absent |
| `Microsoft.NETCore.App.Runtime.Mono.tvos-arm64` | present | absent |
| `Microsoft.NETCore.App.Runtime.Mono.maccatalyst-arm64` | present | absent |

The Apple branch is what actually shipped, on the actual RIDs Uno targets. (`osx-arm64` — the plain
Skia-macOS host — is **UNVERIFIED at the binary level**; that pack is not installed on this Windows
machine. It is the same CMake probe against the same Darwin headers, so the same branch is
overwhelmingly likely, but I did not read the bits.)

**(2) CONFIRMED — Source, `apple-oss-distributions/Libc` `main`, `gen/clock_gettime.c:77-135`:**

```c
clock_gettime_nsec_np(clockid_t clock_id)
{
    switch(clock_id){
    case CLOCK_REALTIME:   { … gettimeofday … }
    case CLOCK_MONOTONIC:  { … timeval2nsec(tv) - boottime … }
    case CLOCK_PROCESS_CPUTIME_ID: { … getrusage … }
    default:
        // calls that use mach_absolute_time units fall through into a common path
        break;
    }

    mach_timebase_info_data_t tb_info;
    kern_return_t kr = mach_timebase_info(&tb_info);
    …
    switch(clock_id){
    case CLOCK_MONOTONIC_RAW:        mach_time = mach_continuous_time();             break;
    case CLOCK_MONOTONIC_RAW_APPROX: mach_time = mach_continuous_approximate_time(); break;
    case CLOCK_UPTIME_RAW:           mach_time = mach_absolute_time();               break;
    case CLOCK_UPTIME_RAW_APPROX:    mach_time = mach_approximate_time();            break;
    case CLOCK_THREAD_CPUTIME_ID:    mach_time = __thread_selfusage();               break;
    default: errno = EINVAL; return 0;
    }

    return (mach_time * tb_info.numer) / tb_info.denom;
}
```

`CLOCK_UPTIME_RAW` → `mach_absolute_time()`, **converted to nanoseconds by the mach timebase inside
Libc**. This closes `08-avalonia.md`'s "UNVERIFIED at code level" at the code level. Note also, for
free, that Darwin's `CLOCK_MONOTONIC` is a *different* clock (wall time minus boot time — it tracks
`gettimeofday`, so it is NTP-affected) and `CLOCK_MONOTONIC_RAW` is `mach_continuous_time()` (advances
through sleep). Only `CLOCK_UPTIME_RAW` is the CoreAnimation base. .NET picks the right one.

**(3) CONFIRMED by production consumers — Source, Chromium `main`:**

This is the hop `03-apple.md` could not close because CoreAnimation is closed source. It can be
closed by evidence rather than by documentation, and the evidence is decisive:

`base/time/time_apple.mm` — Chromium's `TimeTicks` on Apple **is** `mach_absolute_time`:
```cpp
:40   int64_t MachTimeToMicroseconds(uint64_t mach_time) { … timebase_info … }
:83   int64_t ComputeCurrentTicks() {
:84     // mach_absolute_time is it when it comes to ticks on the Mac. …
:87     return MachTimeToMicroseconds(mach_absolute_time());
      }
:167  TimeTicks TimeTicksNowIgnoringOverride() { return TimeTicks() + Microseconds(ComputeCurrentTicks()); }
:187  TimeTicks TimeTicks::FromMachAbsoluteTime(uint64_t mach_absolute_time) {
:188    return TimeTicks(MachTimeToMicroseconds(mach_absolute_time)); }
```

`ui/display/mac/ca_display_link_mac.mm:59-80, 205-206` — Chromium converts `CADisplayLink`
timestamps into that same `TimeTicks` with **`TimeTicks() + Seconds(x)`, i.e. an identity, no offset
term at all**:
```cpp
ui::VSyncParamsMac ComputeVSyncParametersMac(CADisplayLink* display_link, CGDirectDisplayID display_id) {
  base::TimeTicks callback_time = base::TimeTicks() + base::Seconds(display_link.timestamp);
  base::TimeTicks target_time   = base::TimeTicks() + base::Seconds(display_link.targetTimestamp);
  …
  if (callback_time.is_null() || target_time.is_null()) {
    callback_time = base::TimeTicks() + base::Seconds(CACurrentMediaTime());   // interchangeable fallback
    target_time = callback_time + interval;
  }
…
base::TimeTicks CADisplayLinkMac::GetCurrentTime() const {
  return base::TimeTicks() + base::Seconds(CACurrentMediaTime());
}
```

Chromium treats `CADisplayLink.timestamp`, `CADisplayLink.targetTimestamp`, `CACurrentMediaTime()`
and `mach_absolute_time()` as **one timeline with a zero offset**, in shipping code, on the exact API
Uno would use. Combined with Flutter's `CACurrentMediaTime() - link.timestamp` arithmetic
(`vsync_waiter_ios.mm:117`, cited in `03-apple.md`), that is two independent production consumers.
I am upgrading this from **UNVERIFIED** to **verified by two independent production consumers**;
it remains not-provable from Apple source because CoreAnimation is closed.

**Hazard neither note raised: raw mach ticks are NOT nanoseconds on Apple Silicon.**
`clock_gettime_nsec_np` applies `numer/denom` internally, and `CADisplayLink.timestamp` is already
seconds — both are safe. But `CVTimeStamp.hostTime` (the macOS `CVDisplayLink` route
`03-apple.md` §2.4 describes) is **raw mach units**, and Chromium's `MachTimeToMicroseconds` has an
explicit `if (timebase_info->numer == timebase_info->denom)` fast path *and* a general path — proving
that the 1:1 case is not universal on Apple hardware. If the CVDisplayLink route is ever taken,
`hostTime` must be divided by `CVGetHostClockFrequency()` (or scaled by `mach_timebase_info`) before
it is comparable to `Stopwatch.GetTimestamp()`. `03-apple.md:178` says this correctly; flagging it
here because it is the one Apple path where "same epoch" does **not** mean "just multiply by 1e9".
(The specific `numer/denom = 125/3` figure often quoted for Apple Silicon is **UNVERIFIED** — I did
not find it in source. The point stands without it: assume non-1:1.)

**Sleep behaviour.** `mach_absolute_time` / `CLOCK_UPTIME_RAW` do not advance during system sleep.
Both sides stop together, so they stay consistent — this is a *shared* property, not a divergence.

### 3.4 Browser WASM — claim **CONFIRMED**, but the note's chain is **wrong**, and the offset is **0**, not "unknown"

Claims under audit (`04-wasm.md` §3, `08-avalonia.md` row 4):
- `04-wasm.md`: chain is `Stopwatch.Unix.cs` → `pal_time.c` → `minipal/time.c` → *musl
  `clock_gettime.c` `__EMSCRIPTEN__` branch → `__wasi_clock_time_get` → emscripten `libwasi.js`
  `clock_time_get`* → `emscripten_get_now` → `performance.now()`. Marked
  **UNVERIFIED**: *"I did not read the emscripten link flags of the shipped .NET browser-wasm
  runtime packs."*
- `08-avalonia.md`: *"**Needs a constant offset.** … **UNVERIFIED** what emscripten's
  `CLOCK_MONOTONIC` is anchored to in the .NET WASM runtime."*

**The conclusion is right. The chain is wrong. And the offset is exactly zero for the pack Uno
actually ships.**

**Binary — the shipped .NET 10.0.8 browser-wasm runtime pack**
(`C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.browser-wasm\10.0.8\runtimes\browser-wasm\native\`):

`dotnet.native.js` contains, verbatim:

```js
_emscripten_get_now=()=>performance.now()
```

No `performance.timeOrigin` term. `dotnet.native.js.symbols` contains:

```
43:_emscripten_get_now_is_monotonic
44:emscripten_get_now
45:emscripten_get_now_res
4008:minipal_hires_ticks
4011:SystemNative_GetTimestamp
8461:__clock_gettime
```

and contains **no** `clock_gettime_nsec_np` (→ `CLOCK_MONOTONIC` branch, as expected) and **no**
`clock_time_get` / `__wasi_clock_time_get` — the WASI shim `04-wasm.md` describes **is not in the
shipped build**. `_clock_time_get` does not appear in `dotnet.native.js` either.

**Source — the exact emscripten toolchain that built that pack**, shipped alongside it at
`C:\Program Files\dotnet\packs\Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64\10.0.8\tools\emscripten\`:

`system/lib/libc/emscripten_time.c:43-63`:
```c
weak int __clock_gettime(clockid_t clk, struct timespec *ts) {
  if (!checked_monotonic) { is_monotonic = _emscripten_get_now_is_monotonic(); checked_monotonic = true; }

  double now_ms;
  if (clk == CLOCK_REALTIME) {
    now_ms = emscripten_date_now();
  } else if ((clk == CLOCK_MONOTONIC || clk == CLOCK_MONOTONIC_RAW) && is_monotonic) {
    now_ms = emscripten_get_now();
  } else { errno = EINVAL; return -1; }

  long long now_s = now_ms / 1000;
  ts->tv_sec = now_s;
  ts->tv_nsec = (now_ms - (now_s * 1000)) * 1000 * 1000;
  return 0;
}
```
(`:100` — `weak_alias(__clock_gettime, clock_gettime);`)

`src/library.js:2289-2323` — the conditional that decides whether `timeOrigin` is added:
```js
  emscripten_get_now: `;
#if PTHREADS && !AUDIO_WORKLET
    // Pthreads need their clocks synchronized to the execution of the main
    // thread, so, when using them, make sure to adjust all timings to the
    // respective time origins.
    _emscripten_get_now = () => performance.timeOrigin + {{{ getPerformanceNow() }}}();
#else
…
    _emscripten_get_now = () => {{{ getPerformanceNow() }}}();
#endif
`,
```

**Corrected chain (Source + Binary):**

```
Stopwatch.GetTimestamp()                       Stopwatch.Unix.cs:14
  → Interop.Sys.GetTimestamp()                 Interop.GetTimestamp.cs  (LibraryImport SystemNative_GetTimestamp)
  → SystemNative_GetTimestamp                  pal_time.c:82-85          [symbol 4011 in the shipped wasm]
  → minipal_hires_ticks()                      minipal/time.c:76-92      [symbol 4008]  → CLOCK_MONOTONIC branch
  → __clock_gettime(CLOCK_MONOTONIC, …)        emscripten_time.c:43-63   [symbol 8461]
  → emscripten_get_now()                       library.js:2289           [symbol 44]
  → performance.now()                          dotnet.native.js          [verbatim: _emscripten_get_now=()=>performance.now()]
```

**Verdict for the pack Uno ships (single-threaded):** `Compositor.TimestampInTicks` *is*
`performance.now()` in 100 ns ticks, produced by the same JS call, in the same `Window`, on the same
`performance.timeOrigin` that the `requestAnimationFrame` argument is relative to.
**Offset is exactly 0.** `08-avalonia.md`'s "needs a constant offset" is **over-cautious for this
configuration** and `04-wasm.md`'s UNVERIFIED flag is **now closed** — from the shipped artifact,
not from the emscripten conditional.

**Verdict for `WasmEnableThreads`:** `performance.timeOrigin + performance.now()`. In a Web Worker,
`performance.now()` is relative to the *worker's* origin and `performance.timeOrigin` is the
*worker's* creation time, so the **sum is the same absolute value in every thread** — which is
exactly why emscripten does it. The rAF argument stays relative to the **document's** origin.
Offset = the document's `performance.timeOrigin`, a per-document constant. **Fixed, not drifting.**
Still **UNVERIFIED at the binary level** — the multithread pack
(`Microsoft.NETCore.App.Runtime.Mono.multithread.browser-wasm`) is not installed on this machine.

**Keep `04-wasm.md`'s startup offset sample anyway.** It costs one JS call, it is 0 in the shipping
configuration, and it is the assertion that catches an upstream change to any of the six hops above.

**Precision, quantified (this is the one place the epoch is *coarse* rather than *wrong*):**

- `performance.now()` is privacy-clamped. Chromium `third_party/blink/renderer/core/timing/time_clamper.h:23-24`
  (**Source**):
  ```cpp
  static constexpr int kCoarseResolutionMicroseconds = 100;
  static constexpr int kFineResolutionMicroseconds = 5;
  ```
  So `Compositor.TimestampInTicks` on WASM is quantized to **1000 ticks (100 µs)** on a normal page
  and 50 ticks (5 µs) when cross-origin isolated. Which constant applies to which context is
  **UNVERIFIED** (I read the constants, not the call sites).
- Both sides get the same clamp, so the *comparison* is unaffected. But it means the existing
  median-of-32 estimator on WASM is reconstructing a period from samples quantized at 100 µs — an
  extra ±100 µs of noise on top of the ms-scale task jitter. Another argument for taking the rAF
  value directly.
- In the threaded configuration the `timeOrigin + now()` sum is ~1.8e12 ms in a `double`, giving
  ~0.4 µs of representation granularity. Below the clamp. Irrelevant.

**Unrelated bug `04-wasm.md` §7 already flagged, restated because it is an epoch bug in the same
subsystem:** `BrowserPointerInputSource` builds `PointerPoint.Timestamp` as
`_bootTime + (timestamp * 1000)` where `_bootTime` is `Date.now() - performance.now()` in
**milliseconds** and the other term is **microseconds** — a 1000× scale mismatch. Constant, so
deltas and drag velocity are unaffected, but any future code that compares a pointer timestamp to a
frame timestamp will be off by ~1.8e12 µs. Flagging, not fixing (out of scope).

### 3.5 Linux — claim **CONFIRMED for the common case**, but this is the only target that can drift

Claim under audit (`05-x11.md` §3): *"The epoch works out perfectly (UST and .NET `Stopwatch` are the
*same clock* on Linux, off only by a factor of 1000)."*

**Confirmed for the configuration the note describes — and the note under-states the failure modes.**

**.NET side — Source.** `HAVE_CLOCK_GETTIME_NSEC_NP` is a `check_symbol_exists(clock_gettime_nsec_np
time.h …)` probe; glibc and musl do not export it, so Linux RIDs compile the `CLOCK_MONOTONIC` branch
of `minipal/time.c:76-92` → ns. **UNVERIFIED at the binary level** — no `linux-*` runtime pack is
installed on this Windows machine. The identical probe demonstrably resolved that way for two other
non-Darwin RIDs whose binaries *are* here (android-arm64, browser-wasm; §3.2, §3.4), which is strong
circumstantial support.

**DRM/KMS side — Source, `torvalds/linux` `master`, `include/uapi/drm/drm.h:715-725`:**
```c
/**
 * DRM_CAP_TIMESTAMP_MONOTONIC
 *
 * If set to 0, the kernel will report timestamps with ``CLOCK_REALTIME`` in
 * struct drm_event_vblank. If set to 1, the kernel will report timestamps with
 * ``CLOCK_MONOTONIC``. …
 * Starting from kernel version 2.6.39, the default value for this capability
 * is 1. Starting kernel version 4.15, this capability is always set to 1.
 */
#define DRM_CAP_TIMESTAMP_MONOTONIC	0x6
```
Uno's DRM host already receives these values and discards them —
`src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Rendering/DRMRenderer.cs:378`:
```csharp
private static unsafe void OnPageFlip(int fd, uint sequence, uint tv_sec, uint tv_usec, uint crtd_id, void* user_data)
```
Same kernel, same process, same `CLOCK_MONOTONIC`. **Identity after `µs → ns`. Cannot drift.**
This is the *strongest* Linux path and it needs no discriminator.

**X server side — Source, `os/utils.c:496-515`** (read from the `mirror/xserver` GitHub mirror;
`gitlab.freedesktop.org` raw fetches are behind an anti-bot challenge and
`raw.githubusercontent.com/freedesktop/xorg-xserver` 404s — so **the exact upstream revision is
UNVERIFIED**, the mirror content is what I read):
```c
GetTimeInMicros(void)
{
    struct timeval tv;
#ifdef MONOTONIC_CLOCK
    struct timespec tp;
    static clockid_t uclockid;

    if (!uclockid) {
        if (clock_gettime(CLOCK_MONOTONIC, &tp) == 0)
            uclockid = CLOCK_MONOTONIC;
        else
            uclockid = ~0L;
    }
    if (uclockid != ~0L && clock_gettime(uclockid, &tp) == 0)
        return (CARD64) tp.tv_sec * (CARD64)1000000 + tp.tv_nsec / 1000;
#endif

    X_GETTIMEOFDAY(&tv);
    return (CARD64) tv.tv_sec * (CARD64)1000000 + (CARD64) tv.tv_usec;
}
```

**Three ways the X11 `ust` stops being on our epoch — and `05-x11.md` names none of them in epoch
terms:**

1. **The `X_GETTIMEOFDAY` fallback (drifting).** If the server was built without `MONOTONIC_CLOCK`,
   or the first `clock_gettime(CLOCK_MONOTONIC)` fails, every `ust` is `gettimeofday` — i.e.
   **`CLOCK_REALTIME`**. That is a different epoch (Unix epoch, ~1.78e15 µs) *and* a drifting one:
   NTP slews it continuously and can step it. Feeding a fling curve from a clock that gets stepped
   backwards by `ntpd` is a spectacular failure mode.
   **Detection is trivial and must be implemented:** `CLOCK_REALTIME` µs is ~1.78e15; `CLOCK_MONOTONIC`
   µs is uptime, typically < 1e10. A magnitude check discriminates them unambiguously and forever.
2. **Remote / networked X (foreign machine, drifting).** Over a TCP `DISPLAY`, the X server runs on a
   *different computer*. Its `CLOCK_MONOTONIC` is a different machine's uptime, on a different
   crystal — a foreign epoch **and** a drifting one (independent oscillators, tens of ppm). This is
   not an exotic case: `ssh -X` is routine. `05-x11.md` notes GLX_OML is absent over indirect GLX but
   does not draw the epoch conclusion. Present/XCB *would* still deliver a `ust` over a remote
   connection, and it would be garbage for our purposes.
3. **Linux time namespaces (`CLONE_NEWTIME`, kernel ≥ 5.6) — fixed offset, not drifting.**
   `CLOCK_MONOTONIC` is per-time-namespace-offsettable. An app in a namespace with a monotonic offset
   and an X server outside it disagree by a constant. Docker/Podman/Flatpak/Snap do not enable time
   namespaces by default; CRIU checkpoint/restore does. **UNVERIFIED** that any shipping Uno
   deployment does — flagged for completeness because it converts "identity" into "measured offset".
4. **XWayland — right cadence, still our epoch.** `05-x11.md` §2.5 establishes that XWayland's `ust`
   is `GetTimeInMicros()` at `wl_surface.frame` processing time rather than a scanout timestamp. That
   is an *accuracy* problem, not an *epoch* problem: it is still the X server's `CLOCK_MONOTONIC`,
   and under XWayland the X server is a local process. Same epoch, worse signal.

**Verdict for Linux/X11: identity after `µs → ns`, GATED on a runtime discriminator.** Concretely:
compare the first `ust` against `Stopwatch.GetTimestamp() / 1000`; accept only if
`|ust − sw_us| < 1 second`; otherwise fall back to the estimator and log once. That single check
kills failure modes 1, 2 and 3 at once. It is not optional.

---

## 4. Which notes asserted "same epoch" without reading source

| Note | Claim | Was it source-read? | Now |
|---|---|---|---|
| `01-win32.md` §2 | `Stopwatch == QPC` | **No — and it says so** (`:110-111`, *"Not read from `dotnet/runtime` source — no local clone"*). Verified empirically instead. | **Source-verified** (§2.2) + independently re-measured (§3.1). Claim stands. |
| `06-winui.md` §6 | `Stopwatch == QPC` | No — measurement only (`winuiclock.cs`). | **Source-verified.** Claim stands. |
| `08-avalonia.md` §7 | *"`Stopwatch.GetTimestamp()` bottoms out in `minipal_hires_ticks()`"* on **Windows** | It read `minipal/time.c` but not `Stopwatch.Windows.cs`. | **REFUTED as a mechanism.** Right value, wrong chain (§3.1). Windows P/Invokes QPC directly. |
| `08-avalonia.md` §7 table | Apple: *"Man-page-level, **UNVERIFIED at code level**"* | Correctly self-flagged. | **Now source-verified** from Apple Libc (§3.3). |
| `08-avalonia.md` §7 table | Browser: *"**Needs a constant offset** … **UNVERIFIED**"* | Correctly self-flagged. | **Offset is exactly 0** for the shipping single-threaded pack, binary-verified (§3.4). Correct only for the threads pack. |
| `02-android.md` §3 | `frameTimeNanos` and `Stopwatch` are both `CLOCK_MONOTONIC` | **Yes** — source + disassembly of the shipped `.so`. The most rigorous of the eight. | **Independently confirmed** by a different method (§3.2). Its `main`-branch citations are re-pinned to `v10.0.8`. |
| `03-apple.md` §3 | `CLOCK_UPTIME_RAW == mach_absolute_time` | **Yes** — Apple Libc. | Confirmed (§3.3). |
| `03-apple.md` §3.3 | `CACurrentMediaTime` / `CADisplayLink` in that base — self-marked **UNVERIFIED** | Docs + Flutter's arithmetic. | **Upgraded**: Chromium `ca_display_link_mac.mm` treats them as identity with mach-absolute `TimeTicks`, zero offset (§3.3). Two independent production consumers. |
| `04-wasm.md` §3 | chain via musl `clock_gettime.c` → `__wasi_clock_time_get` → `libwasi.js` | Emscripten source, but **not** the shipped .NET build. | **Chain REFUTED, conclusion CONFIRMED.** The shipped build resolves `clock_gettime` to emscripten's own `emscripten_time.c`; no WASI shim is linked (§3.4). |
| `05-x11.md` §3 | UST and `Stopwatch` are *"the same clock … off only by a factor of 1000"* | Yes for the modesetting/DRM path. | **Confirmed for that path**, but the note omits three foreign-epoch modes, two of them **drifting** (§3.5). Needs a discriminator. |
| `07-flutter.md` | Makes no independent `Stopwatch` epoch claim — it documents Flutter's own clocks. | n/a | Its §7 observation is the right defensive posture and is echoed in §6 below. |

---

## 5. Notes' shared citation hygiene problem

Every note cites `dotnet/runtime` **`main`** (`03-apple.md:256`, `04-wasm.md:353`, `05-x11.md`,
`07-flutter.md:13-16`, `08-avalonia.md:286`) — except `02-android.md`, which pins **`v10.0.0`**.
`main` is not a reproducible ref; a reader six months from now cannot check the quote. All quotes in
this document are pinned to **`v10.0.8`**, which is the runtime that is actually installed here.

Two quotes in the notes are paraphrases rather than verbatim, in ways that matter slightly:
- The `#elif HAVE_CLOCK_MONOTONIC` / `#error` structure is rendered as a bare `#else` in
  `03-apple.md`, `05-x11.md` and `08-avalonia.md`. The `#error` is the guarantee that no third clock
  exists.
- `08-avalonia.md` labels the code block *"`src/native/minipal/time.c` — Windows branch"* and then
  attributes `Stopwatch` to it. The block itself is accurate; the attribution is not (§3.1).

---

## 6. What this means for the implementation

### 6.1 Conversions, per platform, final

```csharp
// Windows — identity. Keep the multiply; do not hard-code 1.0.
long ticks = unchecked((long)(qpcVBlank * s_tickFrequency));

// Android — integer, exact.
long ticks = frameTimeNanos / 100;

// Apple — CADisplayLink gives CFTimeInterval seconds.
long ticks = (long)(link.TargetTimestamp * TimeSpan.TicksPerSecond);   // ×1e7
// (CVTimeStamp.hostTime is RAW MACH UNITS — scale by mach_timebase_info / CVGetHostClockFrequency first.)

// Browser WASM — rAF gives DOMHighResTimeStamp milliseconds.
long ticks = (long)(rafMs * TimeSpan.TicksPerMillisecond) + _epochOffsetTicks;  // offset == 0 single-threaded

// Linux DRM — page-flip event.
long ticks = ((long)tv_sec * 1_000_000L + tv_usec) * 10L;              // µs → 100 ns ticks

// Linux X11 Present/GLX — same arithmetic, GATED on the §3.5 discriminator.
long ticks = ust * 10L;
```

### 6.2 The one-time assertion every platform should carry

Not because any of these are expected to fail — because the branch selection in §2.3 happens at
*runtime-pack build time*, per-RID, outside our control, and because §2.4 shows `Stopwatch.Frequency`
on non-Windows is a hard-coded constant rather than a measured one. One log line at startup:

```
|platformFrameTicks − Compositor.TimestampInTicks|  sampled once, adjacently
  expect: < 1 frame period.   Windows/Android/Apple/WASM-1T → ~0.
                              WASM-threads → performance.timeOrigin (record it, then subtract it).
                              X11 → if > 1 second, REJECT the source and fall back to the estimator.
```

Use a **tolerance**, never equality — §2.5 shows the double conversion can differ from integer
`/ 100` by 1 tick at high uptime.

### 6.3 Adopt Flutter's posture where it is cheap

`07-flutter.md` §7 is right that Flutter never assumes epochs match: it does a paired read of both
clocks at the callback instant and applies the delta (`vsync_waiter_ios.mm:117-118`,
`vsync_waiter_android.cc:89-90`, `FlutterTimeConverter.mm:27-33`). This document proves Uno does not
*need* that on Windows, Android, Apple or WASM — but the paired read **is** the §6.2 assertion, and
on X11 it is the §3.5 discriminator. Same code, three purposes. Write it once.

### 6.4 Where the offset must be sampled, not assumed

| Target | Offset | Sample it? |
|---|---|---|
| Win32 / WinUI | 0 | assert only |
| Android | 0 | assert only |
| Apple | 0 | assert only |
| WASM single-threaded | 0 | assert only |
| WASM `WasmEnableThreads` | `performance.timeOrigin` | **yes — measure once** |
| Linux DRM/KMS | 0 | assert only |
| Linux X11 Present/GLX | 0 *if local + monotonic*, otherwise **reject** | **yes — measure once, and gate on it** |

---

## 7. Sources

**Uno** (worktree `D:/Work/uno-worktrees/scrollsmooth`, branch `dev/mazi/smooth-scroll`)
`src/Uno.UI.Composition/Composition/Compositor.cs:33,38` ·
`src/Uno.UI.Composition/Composition/Compositor.skia.cs:209,220-222,244-290,312-313` ·
`src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.InertiaProcessor.cs:355-357` ·
`src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs:621-628,669-677` ·
`src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32RenderPacer.cs:61` ·
`src/Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:99` (`DoFrame(long frameTimeNanos) => onFrame();` — the value is discarded) ·
`src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Rendering/DRMRenderer.cs:378` ·
`src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/BrowserRenderer.ts:48`

**dotnet/runtime, tag `v10.0.8`** (fetched 2026-07-31 via `raw.githubusercontent.com`)
`src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.cs:13,18` ·
`…/Stopwatch.Windows.cs:8-28` · `…/Stopwatch.Unix.cs:8-14` ·
`…/System.Private.CoreLib.Shared.projitems:1630,2240,2340,2567` ·
`src/coreclr/nativeaot/System.Private.CoreLib/src/System.Private.CoreLib.csproj:554` ·
`src/mono/System.Private.CoreLib/System.Private.CoreLib.csproj:283` ·
`src/libraries/Common/src/Interop/Unix/System.Native/Interop.GetTimestamp.cs` ·
`src/native/libs/System.Native/pal_time.c:82-85` ·
`src/native/minipal/time.c:12-28,71-92` · `src/native/minipal/configure.cmake:14-16` ·
`src/native/minipal/minipalconfig.h.in:9-11`

**Shipped .NET 10.0.8 artifacts on this machine** (`C:\Program Files\dotnet\packs\…`)
`Microsoft.NETCore.App.Runtime.Mono.android-arm64/10.0.8/…/libSystem.Native.so` ·
`Microsoft.NETCore.App.Runtime.Mono.ios-arm64` / `.tvos-arm64` / `.maccatalyst-arm64` `/…/libSystem.Native.dylib` ·
`Microsoft.NETCore.App.Runtime.Mono.browser-wasm/10.0.8/…/dotnet.native.js` + `dotnet.native.js.symbols` ·
`Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64/10.0.8/tools/emscripten/system/lib/libc/emscripten_time.c:43-63,100` ·
`…/tools/emscripten/src/library.js:2289-2323`

**AOSP `main`** (`android.googlesource.com`, base64 `?format=TEXT`)
`platform/libcore` `ojluni/src/main/native/System.c:254-258` ·
`platform/frameworks/base` `core/java/android/view/Choreographer.java:751,782-783,1269-1271,1313-1314,1322-1323` ·
`platform/system/core` `libutils/Timers.cpp:33-41`, `libutils/SystemClock.cpp:48-50,64-75`,
`libutils/include/utils/Timers.h:75-81`

**apple-oss-distributions/Libc `main`** `gen/clock_gettime.c:77-135`

**chromium/chromium `main`** `base/time/time_apple.mm:40-51,83-87,160,167,187-188` ·
`ui/display/mac/ca_display_link_mac.mm:59-80,205-206` ·
`third_party/blink/renderer/core/timing/time_clamper.h:23-24`

**torvalds/linux `master`** `include/uapi/drm/drm.h:715-725`

**xorg-xserver** `os/utils.c:496-515` — read from the `mirror/xserver` GitHub mirror;
upstream `gitlab.freedesktop.org` and `freedesktop/xorg-xserver` were not reachable. **Exact upstream
revision UNVERIFIED.**

**Runtime evidence produced for this document** (scratchpad, not committed)
`epochcheck/qpccheck.cs` — QPF/`Stopwatch.Frequency` equality + 20 000 bracketed interleave samples ·
`epochcheck/dwmepoch.cs` — `DwmFlush` → `Stopwatch` → `DwmGetCompositionTimingInfo` epoch cross-check ·
`epochcheck/convcheck.cs` — double-vs-integer tick conversion divergence sweep

---

## 8. Aside, out of scope

`Compositor.skia.cs:209` declares `internal event Action<long>? FrameStarting;`. `AGENTS.md`
prohibits `event Action`/`event Action<T>` outright in favour of `EventHandler<TEventArgs>`. Noted
because this audit read that line; not changed — this document touches nothing outside
`specs/scroll-smoothness/clock/`.
