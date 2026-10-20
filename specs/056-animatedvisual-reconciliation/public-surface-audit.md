# AnimatedVisualPlayer port — public-surface remediation spec

Scope: the public/protected API surface introduced or changed by **PR #23889**
(`origin/agents/animated-visual-player-port`, tip `8f26be34a3d`) relative to its base
`96019901b8764794f186e03636db687e9ee28325`.

**Line anchors in this document are against the live worktree at HEAD `36260236c3d`
(branch `dev/mazi/avp-composition`), not against the PR branch.** The worktree is
downstream of the PR branch and has already moved; every anchor below was re-read from
the working tree while writing this spec. Each remediation block quotes the exact
declaration line it attaches to, so an exact-string editor can apply them in any order.

Ground truth used:
- `D:/Work/microsoft-ui-xaml2/controls/dev/AnimatedVisualPlayer/AnimatedVisualPlayer.idl` (133 lines, read in full)
- `D:/Work/microsoft-ui-xaml2/controls/dev/AnimatedVisualPlayer/AnimatedVisualPlayerAutomationPeer.idl` (11 lines, read in full)
- `D:/Work/microsoft-ui-xaml2/controls/dev/AnimatedIcon/AnimatedIcon.idl`
- `D:/Work/microsoft-ui-xaml2/dxaml/xcp/dxaml/idl/winrt/controls/microsoft.ui.xaml.controls.controls2.idl`
- Microsoft Learn (`?view=windows-app-sdk-2.0`), fetched live; every URL cited below was actually retrieved.

---

## 1. Summary

| Category | Count | Status |
|---|---|---|
| Members enumerated / reviewed | 83 (+9 adjacent, untouched by the PR) | — |
| **Over-exposed members** (public/protected but must not be) | **1** | §2 |
| **Signature divergences vs `.idl`** | **1** (accepted; doc-only remediation) | §3 |
| **Missing XML docs — PR-scoped, required** | **37** across 8 files | §4A |
| Missing XML docs — adjacent, optional | 9 across 4 files | §4B |
| XML doc amendments (existing doc now wrong) | 1 | §4C |
| **Missing `PackageDiffIgnore` entries** | **1** | §5 |
| Regressions already remediated in-tree | 1 | §6 |

Categories that are **empty**, stated plainly:

- **No member is wrongly `internal`/`private` that WinUI exposes.** All 5 WinUI interfaces
  and every WinUI-declared member of `AnimatedVisualPlayer` are present and public.
- **No IL-breaking signature change** was introduced. The one divergence (§3) is a
  nullable annotation only; `Uno.PackageDiff` does not track nullability.
- **No new Uno-only public member** was added that lacks a `PackageDiffIgnore` entry.
  The single missing entry (§5) is for a *removed* member.
- **No wrongly-public Uno-only type.** `IPanel` is `internal` and explicitly implemented;
  `ILottieVisualSourceProvider` is correctly public (see §6).

Priority order for the follow-up PR: **§2 → §5 → §4A → §3 → §4C → §4B.**
§2 and §5 are the only items that are harder to walk back after a release.

---

## 2. Over-exposed members

One finding. `protected` on a `public abstract` class in a shipped add-in assembly is
externally bindable by any derived type, so this is release-hard-to-reverse.

| file:line | member | current | should be | WinUI `.idl` citation |
|---|---|---|---|---|
| `src/AddIns/Uno.UI.Lottie/LottieVisualSourceBase.cs:481` | `ReadAnimationJsonAsync(Stream sourceJson, long? knownLength, CancellationToken cancellationToken)` | `protected static` | `private static` | **No counterpart.** `AnimatedVisualPlayer.idl` (read end-to-end) declares only `IAnimatedVisual`, `IAnimatedVisual2`, `IAnimatedVisualSource`, `IDynamicAnimatedVisualSource`, `IAnimatedVisualSource3`, `ISelfPlayingAnimatedVisual`, `PlayerAnimationOptimization`, `AnimatedVisualPlayer`. Nothing Lottie-related. Learn's toolkit `LottieVisualSource` page lists no such member either. |

**Why it is a widening, not pre-existing:** the base commit's `LottieVisualSourceBase`
had exactly two non-public-but-derivable members — `IsPayloadNeedsToBeUpdated` (`96019901b8:…:163`)
and `LoadAndObserveAnimationData` (`96019901b8:…:168`). `ReadAnimationJsonAsync` did not
exist at all. The PR added both overloads as `protected`.

**Why narrowing compiles:** the `(Stream, long?, CancellationToken)` overload has exactly one
call site in the repo — `LottieVisualSourceBase.cs:478`, inside the sibling
`ReadAnimationJsonAsync(IInputStream, CancellationToken)` overload, i.e. the same class. The
other two call sites bind the `IInputStream` overload (`LottieVisualSourceBase.cs:536` and
`ThemableLottieVisualSource.cs:79`, both passing an `IInputStream`).

**Exact edit:**

```diff
-		protected static async Task<string> ReadAnimationJsonAsync(Stream sourceJson, long? knownLength, CancellationToken cancellationToken)
+		private static async Task<string> ReadAnimationJsonAsync(Stream sourceJson, long? knownLength, CancellationToken cancellationToken)
```

**Keep `protected`** on the sibling `ReadAnimationJsonAsync(IInputStream, CancellationToken)`
at `:473` — an external overrider of `LoadAndObserveAnimationData` only ever receives an
`IInputStream`, never a `(Stream, long?)` pair, so that overload is the real extensibility
seam. It still needs a doc block (§4A.5).

**Also correctly `protected` — do not narrow these:**
- `LottieVisualSourceBase.cs:455` `protected sealed class AnimationDataLoadSubscription` —
  `LoadAndObserveAnimationData` (`protected virtual`, pre-existing since `96019901b8:…:168`)
  is documented to return this exact type. `LottieVisualSourceBase.cs:272-274` does
  `if (loadSubscription is AnimationDataLoadSubscription asyncLoad) { await asyncLoad.InitialLoad… }`
  and `:283-288` fails the load otherwise. An out-of-assembly override returning a plain
  `IDisposable` would break. Narrowing this would be a behavioural regression.

---

## 3. Signature divergences vs the `.idl`

One divergence. It is **deliberate and should be kept**; the remediation is documentation
only (§4C), not a code change.

| file:line | member | current | should be | WinUI `.idl` citation |
|---|---|---|---|---|
| `src/Uno.UI/UI/Xaml/Hosting/ElementCompositionPreview.cs:50` | `SetElementChildVisual(UIElement element, Visual? visual)` | `Visual?` (nullable) | **keep `Visual?`** — document the null contract instead | `dxaml/xcp/dxaml/idl/winrt/controls/microsoft.ui.xaml.controls.controls2.idl:2906` — `static void SetElementChildVisual(Microsoft.UI.Xaml.UIElement element, Microsoft.UI.Composition.Visual visual);` |

**Why keep it.** The IDL parameter is non-nullable, but the real WinUI implementation
accepts null and treats it as "clear the child visual":
`dxaml/xcp/core/core/elements/uielement.cpp:7380` declares
`CUIElement::SetHandInVisual(_In_opt_ WUComp::IVisual* pChildVisual)` — note `_In_opt_` —
and `:7402` calls `DiscardHandInVisual(pChildVisual != nullptr /*isHandInVisualReplaced*/);`.
So `Visual?` is a **parity fix**, not a divergence from behaviour. Uno's implementation at
`ElementCompositionPreview.cs:53-63` matches: remove the existing child container, set
`element.HasCompositionChildVisual = false`, return.

**Compatibility:** source-compatible and binary-compatible. Nullable annotations do emit a
`[Nullable]` attribute into metadata, but the method signature/token is unchanged, which is
what the Cecil-based `Uno.PackageDiff` compares — **no `PackageDiffIgnore` entry required.**
(Do not restate this as "no IL change"; that phrasing is loose.)

**The only defect is the doc**: `ElementCompositionPreview.cs:49` still says
`<param name="visual">The Visual to add to the element's visual tree.</param>`, which does
not describe the new null behaviour. Fix in §4C.

Everything else checked and found signature-clean:
- `AnimatedVisualPlayer` — all 5 methods, 11 properties and 10 DPs match `AnimatedVisualPlayer.idl:78-130` exactly.
- `AnimatedVisualPlayerAutomationPeer(AnimatedVisualPlayer owner)` matches `AnimatedVisualPlayerAutomationPeer.idl:8`.
- `IAnimatedVisual2 : IAnimatedVisual` (idl:16), `CreateAnimations()` (idl:18), `DestroyAnimations()` (idl:19).
- `IAnimatedVisualSource3` with **no** base interface (idl:41), `TryCreateAnimatedVisual(Compositor, out object, bool)` (idl:43-46).
- `IDynamicAnimatedVisualSource : IAnimatedVisualSource` (idl:33-34), `AnimatedVisualInvalidated` (idl:36).
- `IAnimatedVisualSource2.Markers` as `IReadOnlyDictionary<string, double>` — correct .NET projection of `AnimatedIcon.idl:6-10`'s `IMapView<String, Double>`.

---

## 4. Missing XML docs

House style: `/// <summary>` blocks (see `AnimatedVisualPlayer.cs:10-12`). Tabs, not spaces.

**Indentation per file** (derived from whether the file uses a file-scoped or block namespace):

| file | namespace style | indent for type members |
|---|---|---|
| `AnimatedVisualPlayerAutomationPeer.cs` | file-scoped | 1 tab |
| `AnimatedVisualPlayer.mux.cs` | file-scoped | 1 tab |
| `AnimationController.cs` | file-scoped | 1 tab |
| `ElementCompositionPreview.cs` | file-scoped | 1 tab |
| `IAnimatedVisualSource.cs`, `IAnimatedVisualSource2.cs`, `IThemableAnimatedVisualSource.cs`, `ILottieVisualSourceProvider.cs` | block | 2 tabs |
| `Compositor.cs`, `CompositionPropertySet.cs` | block | 2 tabs |
| `LottieVisualSourceBase.cs` | block | 2 tabs (3 tabs inside `AnimationDataLoadSubscription`) |

**Application note.** Blocks are listed in ascending line order per file, as requested. Each is
anchored by the exact declaration line quoted above it, so if you apply with an exact-string
editor the order is irrelevant and there is no drift. If you insert by line number instead,
apply **bottom-up within each file**.

**`// Public API.` comments in `AnimatedVisualPlayer.mux.cs` are carried verbatim from the
WinUI C++ source — do not delete them.** Insert the `///` block *between* those comments and
the declaration line, i.e. immediately above `public …`.

Summaries marked **[Uno-authored]** have no Microsoft Learn page — the type or member does
not exist in WinUI. They were written from the implementation and are not sourced from Learn.

### 4A. PR-scoped — required

#### 4A.1 `src/Uno.UI/UI/Xaml/Automation/Peers/AnimatedVisualPlayerAutomationPeer.cs`

**:17** — anchor `	public AnimatedVisualPlayerAutomationPeer(AnimatedVisualPlayer owner) : base(owner)`
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.animatedvisualplayerautomationpeer.-ctor>

```csharp
	/// <summary>
	/// Initializes a new instance of the AnimatedVisualPlayerAutomationPeer class.
	/// </summary>
	/// <param name="owner">The AnimatedVisualPlayer control instance to create the peer for.</param>
```

#### 4A.2 `src/Uno.UI/UI/Xaml/Controls/AnimatedVisualPlayer/IAnimatedVisualSource.cs`

The type ships with **zero** XML documentation: the hand-written partial carries only the
mechanical `//` comment on `:5-7`, and the generated partial
(`src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/IAnimatedVisualSource.cs:9`) has
none. Put the block on the hand-written partial — the generated file is regenerated.

**:8** — anchor `	public partial interface IAnimatedVisualSource`
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.ianimatedvisualsource>

```csharp
		/// <summary>
		/// An animated Visual that can be used by other objects, such as an AnimatedVisualPlayer.
		/// </summary>
```

Insert **below** the existing `//` comment on lines 5-7 and directly above the declaration.

#### 4A.3 `src/Uno.UI/UI/Xaml/Controls/AnimatedVisualPlayer/ILottieVisualSourceProvider.cs`

Uno-only interface with **no** WinUI counterpart — confirmed against the full
`AnimatedVisualPlayer.idl`. It is **correctly public**: it is the `ApiExtensibility`
contract between `Uno.UI` and the `Uno.UI.Lottie` add-in
(`[assembly: ApiExtension(typeof(ILottieVisualSourceProvider), typeof(Uno.UI.Lottie.LottieVisualSourceProvider))]`
at `src/AddIns/Uno.UI.Lottie/LottieVisualSourceProvider.cs:16`, consumed at
`src/Uno.UI/UI/Xaml/Controls/ProgressRing/ProgressRing.cs:24`). Do **not** narrow it.
All four members are undocumented; do the whole file in one pass.

**:10** — anchor `	public interface ILottieVisualSourceProvider` — **[Uno-authored]**

```csharp
		/// <summary>
		/// Provides Lottie animated visual sources to Uno.UI, supplied by the Uno.UI.Lottie
		/// add-in through ApiExtensibility.
		/// </summary>
```

**:12** — anchor `		IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile);` — **[Uno-authored]**

```csharp
		/// <summary>
		/// Creates an animated visual source from a Lottie JSON asset.
		/// </summary>
		/// <param name="sourceFile">The URI of the Lottie JSON asset.</param>
		/// <returns>An animated visual source backed by the specified asset.</returns>
```

**:13** — anchor `		IThemableAnimatedVisualSource CreateThemableFromLottieAsset(Uri sourceFile);` — **[Uno-authored]**

```csharp
		/// <summary>
		/// Creates a themable animated visual source from a Lottie JSON asset.
		/// </summary>
		/// <param name="sourceFile">The URI of the Lottie JSON asset.</param>
		/// <returns>An animated visual source whose colors can be overridden per theme.</returns>
```

**:14** — anchor `		public bool TryCreateThemableFromAnimatedVisualSource(IAnimatedVisualSource animatedVisualSource, out IThemableAnimatedVisualSource? themableAnimatedVisualSource);` — **[Uno-authored]**

```csharp
		/// <summary>
		/// Attempts to obtain a themable animated visual source for an existing animated visual source.
		/// </summary>
		/// <param name="animatedVisualSource">The animated visual source to convert.</param>
		/// <param name="themableAnimatedVisualSource">When this method returns, the themable source, or <c>null</c> if none could be produced.</param>
		/// <returns><c>true</c> if a themable source was produced; otherwise, <c>false</c>.</returns>
```

Behaviour confirmed against the sole implementation,
`src/AddIns/Uno.UI.Lottie/LottieVisualSourceProvider.cs:30-46`: returns the source when it is
already a `ThemableLottieVisualSource`, else rebuilds from `LottieVisualSource.UriSource`,
else `false`.

While in this file, drop the redundant `public` modifier on `:14` — lines 12 and 13 omit it.

#### 4A.4 `src/Uno.UI/UI/Xaml/Controls/AnimatedVisualPlayer/AnimatedVisualPlayer.mux.cs`

All six carry `// Public API.` line comments but no XML doc.

**:347** — anchor `	public AnimatedVisualPlayer()`
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.animatedvisualplayer> (Constructors table) · idl:78

```csharp
	/// <summary>
	/// Initializes a new instance of the AnimatedVisualPlayer class.
	/// </summary>
```

**:607** — anchor `	public void Pause()` · idl:110
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.animatedvisualplayer>

```csharp
	/// <summary>
	/// Pauses the currently playing animated visual, or does nothing if no play is underway.
	/// </summary>
```

**:616** — anchor `	public IAsyncAction PlayAsync(double fromProgress, double toProgress, bool looped)` · idl:111
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.animatedvisualplayer.playasync>

```csharp
	/// <summary>
	/// Starts playing the loaded animated visual, or does nothing if no animated visual is loaded.
	/// </summary>
	/// <param name="fromProgress">The point from which to start the animation, as a value from 0 to 1.</param>
	/// <param name="toProgress">The point at which to finish the animation, as a value from 0 to 1.</param>
	/// <param name="looped">If <c>true</c>, the animation loops continuously between <paramref name="fromProgress"/> and <paramref name="toProgress"/>. If <c>false</c>, the animation plays once then stops.</param>
	/// <returns>An async action that is completed when the play is stopped or, if <paramref name="looped"/> is not set, when the play reaches <paramref name="toProgress"/>.</returns>
	/// <remarks>
	/// If <paramref name="toProgress"/> is less than <paramref name="fromProgress"/>, the animated visual will
	/// play from <paramref name="fromProgress"/> to the end, then play from the beginning until it reaches
	/// <paramref name="toProgress"/>. To play an animated visual in reverse, set the playback rate to a negative value.
	/// </remarks>
```

**:695** — anchor `	public void Resume()` · idl:112
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.animatedvisualplayer>

```csharp
	/// <summary>
	/// Resumes the currently paused animated visual, or does nothing if there is no animated visual
	/// loaded or the animated visual is not paused.
	/// </summary>
```

**:705** — anchor `	public void SetProgress(double progress)` · idl:113
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.animatedvisualplayer.setprogress>

```csharp
	/// <summary>
	/// Moves the progress of the animated visual to the given value, or does nothing if no animated
	/// visual is loaded.
	/// </summary>
	/// <param name="progress">A value from 0 to 1 that represents the progress of the animated visual.</param>
	/// <remarks>If the animated visual was playing it will behave as if Stop was called first.</remarks>
```

**:738** — anchor `	public void Stop()` · idl:114
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.animatedvisualplayer.stop>

```csharp
	/// <summary>
	/// Stops the current play, or does nothing if no play is underway.
	/// </summary>
```

#### 4A.5 `src/AddIns/Uno.UI.Lottie/LottieVisualSourceBase.cs`

**:31** — anchor `	public abstract partial class LottieVisualSourceBase : DependencyObject, IAnimatedVisualSource, IAnimatedVisualSource3, IDynamicAnimatedVisualSource, IAnimatedVisualSourceWithUri` — **[Uno-authored]**

Learn's toolkit page for `LottieVisualSource` shows a definition block but no prose summary,
so this is not Learn-sourced. Accessibility is unchanged from the base commit
(`96019901b8:…:31` was already `public abstract partial class`); only the implemented-interface
list widened. `IAnimatedVisualSourceWithUri` is `internal`
(`src/Uno.UI/UI/Xaml/Controls/AnimatedVisualPlayer/IAnimatedVisualSource.cs:12`) and is
implemented explicitly at `:64`, so it does not leak.

```csharp
		/// <summary>
		/// Base class for animated visual sources that render Lottie JSON animations.
		/// </summary>
```

**:97** — anchor `		public event TypedEventHandler<IDynamicAnimatedVisualSource, object>? AnimatedVisualInvalidated;` · idl:36
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.idynamicanimatedvisualsource.animatedvisualinvalidated>

```csharp
		/// <summary>
		/// Occurs when the animated visual previously provided by this source should be discarded.
		/// </summary>
```

**:99** — anchor `		public IAnimatedVisual? TryCreateAnimatedVisual(Compositor compositor, out object diagnostics)` · idl:26-28
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.ianimatedvisualsource.trycreateanimatedvisual>

```csharp
		/// <summary>
		/// Attempts to create an animated visual.
		/// </summary>
		/// <param name="compositor">The compositor for the animated visual.</param>
		/// <param name="diagnostics">The diagnostics information about the attempt to create an animated visual.</param>
		/// <returns>An animated visual that can be used by other objects.</returns>
```

> The `<returns>` text above is **Learn-verbatim**. Do not append ", or `null` if it could not
> be created" — that clause appears nowhere on the Learn page. If you want the nullability
> documented, add it as a separate sentence and mark it Uno-authored.

**:102** — anchor `		public IAnimatedVisual2? TryCreateAnimatedVisual(Compositor compositor, out object diagnostics, bool createAnimations)` · idl:43-46
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.ianimatedvisualsource3.trycreateanimatedvisual>

```csharp
		/// <summary>
		/// Attempts to create an animated visual.
		/// </summary>
		/// <param name="compositor">The compositor for the animated visual.</param>
		/// <param name="diagnostics">The diagnostics information about the attempt to create an animated visual.</param>
		/// <param name="createAnimations"><c>true</c> to create the animations; otherwise, <c>false</c>.</param>
		/// <returns>An animated visual that can be used by other objects.</returns>
```

> Same caveat on `<returns>` as `:99`.

**:455** — anchor `		protected sealed class AnimationDataLoadSubscription : IDisposable` — **[Uno-authored]**

```csharp
		/// <summary>
		/// The subscription returned by <see cref="LoadAndObserveAnimationData"/> when the load is
		/// asynchronous, exposing the initial load task so callers can await first paint.
		/// </summary>
```

**:459** (3 tabs) — anchor `			public AnimationDataLoadSubscription(Task initialLoad, Action dispose)` — **[Uno-authored]**

```csharp
			/// <summary>
			/// Initializes a new instance of the AnimationDataLoadSubscription class.
			/// </summary>
			/// <param name="initialLoad">A task that completes when the first animation payload has been loaded.</param>
			/// <param name="dispose">The action invoked once when the subscription is disposed.</param>
```

**:465** (3 tabs) — anchor `			public Task InitialLoad { get; }` — **[Uno-authored]**

```csharp
			/// <summary>
			/// Gets a task that completes when the first animation payload has been loaded.
			/// </summary>
```

**:467** (3 tabs) — anchor `			public void Dispose()` — **[Uno-authored]**

```csharp
			/// <summary>
			/// Cancels the load and releases the subscription. Subsequent calls do nothing.
			/// </summary>
```

Idempotence verified at `:469`: `Interlocked.Exchange(ref _dispose, null)?.Invoke();`.
Must stay `public` — it is the implicit `IDisposable` implementation.

**:473** — anchor `		protected static async Task<string> ReadAnimationJsonAsync(IInputStream sourceJson, CancellationToken cancellationToken)` — **[Uno-authored]**

```csharp
		/// <summary>
		/// Reads a Lottie JSON payload from a stream, disposing the stream when done.
		/// </summary>
		/// <param name="sourceJson">The stream containing the Lottie JSON payload. It is disposed by this method.</param>
		/// <param name="cancellationToken">A token that cancels the read.</param>
		/// <returns>The Lottie JSON payload, with any byte-order mark removed.</returns>
```

Behaviour verified: `using var _ = sourceJson;` at `:475`, BOM stripping at `:525-527`.

**:481** needs no doc — it becomes `private` (§2).

#### 4A.6 `src/Uno.UI.Composition/Composition/AnimationController.cs`

`MinPlaybackRate` (`:79`) and `MaxPlaybackRate` (`:84`) are **already documented** — skip them.
Note they now return `-16f`/`16f`, matching Learn's documented range.

**:8** — anchor `public partial class AnimationController : CompositionObject`
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.animationcontroller>

```csharp
/// <summary>
/// Provides playback controls for a KeyFrameAnimation.
/// </summary>
```

(Type-level: zero indent — the file uses a file-scoped namespace.)

**:54** — anchor `	public void Resume()`

```csharp
	/// <summary>
	/// Starts playback of an animation that was previously paused.
	/// </summary>
```

**:65** — anchor `	public void Pause()`

```csharp
	/// <summary>
	/// Pauses playback of the animation.
	/// </summary>
```

**:86** — anchor `	public float PlaybackRate`
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.animationcontroller.playbackrate>

```csharp
	/// <summary>
	/// Gets or sets the rate at which the animation plays.
	/// </summary>
	/// <value>The rate at which the animation plays. The default is 1.0.</value>
	/// <remarks>
	/// You can modify the playback rate to speed up or reverse the animation. Playback rate can range
	/// from -16 to 16. A positive value greater than 1 speeds up the animation. A negative value
	/// reverses the animation.
	/// </remarks>
```

**:105** — anchor `	public float Progress`

```csharp
	/// <summary>
	/// Gets or sets a value that indicates the current playback position of the animation.
	/// </summary>
```

#### 4A.7 `src/Uno.UI.Composition/Composition/Compositor.cs`

⚠️ This file is under concurrent edit. Anchors were re-read at HEAD `36260236c3d`;
**locate by member name, not line number.**

**:221** — anchor `		public AnimationController CreateAnimationController()`
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.compositor.createanimationcontroller>

```csharp
		/// <summary>
		/// Creates an instance of AnimationController.
		/// </summary>
		/// <returns>The created AnimationController object.</returns>
```

**:224** — anchor `		public IAsyncAction RequestCommitAsync()`
Learn: <https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.compositor.requestcommitasync>

```csharp
		/// <summary>
		/// Attempts to initiate a commit cycle asynchronously.
		/// </summary>
		/// <returns>An asynchronous action.</returns>
```

> Learn's own text misspells this as "ansynchronously" / "An ansynchronous action." The
> spelling is corrected above; the wording is otherwise verbatim. Do **not** extend
> `<returns>` to "…that completes once the commit has been processed" — that is true of
> Uno's implementation but is not on the Learn page.

#### 4A.8 `src/Uno.UI.Composition/Composition/CompositionPropertySet.cs`

Nine members, `:25`-`:41`. The PR changed bodies only (`stopAnimation: true`); the docs gap
is pre-existing. Lowest priority in §4A.

**Learn does not use one shared wording.** Eight of nine use the "key associated with the
value" param text; **`InsertScalar` is the sole outlier**. Summary casing also varies
(`boolean` and `quaternion` are lowercase on Learn; the rest are type-cased). All nine pages
were fetched individually. Base URL:
`https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.compositionpropertyset.insert<type>`

Shared param block for **all except `InsertScalar`**:

```csharp
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

**:25** — anchor `		public void InsertColor(string propertyName, Color value)`

```csharp
		/// <summary>
		/// Inserts a Color key-value pair.
		/// </summary>
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

**:27** — anchor `		public void InsertMatrix3x2(string propertyName, Matrix3x2 value)`

```csharp
		/// <summary>
		/// Inserts a Matrix3x2 key-value pair.
		/// </summary>
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

**:29** — anchor `		public void InsertMatrix4x4(string propertyName, Matrix4x4 value)`

```csharp
		/// <summary>
		/// Inserts a Matrix4x4 key-value pair.
		/// </summary>
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

**:31** — anchor `		public void InsertQuaternion(string propertyName, Quaternion value)`

```csharp
		/// <summary>
		/// Inserts a quaternion key-value pair.
		/// </summary>
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

**:33** — anchor `		public void InsertScalar(string propertyName, float value)` — **note the different param text**

```csharp
		/// <summary>
		/// Inserts a Single key-value pair.
		/// </summary>
		/// <param name="propertyName">The name of the property to insert.</param>
		/// <param name="value">The value of the property to insert.</param>
```

**:35** — anchor `		public void InsertVector2(string propertyName, Vector2 value)`

```csharp
		/// <summary>
		/// Inserts a Vector2 key-value pair.
		/// </summary>
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

**:37** — anchor `		public void InsertVector3(string propertyName, Vector3 value)`

```csharp
		/// <summary>
		/// Inserts a Vector3 key-value pair.
		/// </summary>
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

**:39** — anchor `		public void InsertVector4(string propertyName, Vector4 value)`

```csharp
		/// <summary>
		/// Inserts a Vector4 key-value pair.
		/// </summary>
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

**:41** — anchor `		public void InsertBoolean(string propertyName, bool value)`

```csharp
		/// <summary>
		/// Inserts a boolean key-value pair.
		/// </summary>
		/// <param name="propertyName">The key associated with the value. This key can be used to retrieve the value.</param>
		/// <param name="value">The value to insert.</param>
```

### 4B. Adjacent — optional

These are **not** in the PR delta. They are public, undocumented, and live in the folders the
PR reorganised. Include only if the follow-up PR intends to close the folder out. Skipping
them is a defensible scope decision — but then do not claim the AVP folder is documented.

| file:line | member | Learn | note |
|---|---|---|---|
| `IAnimatedVisualSource2.cs:7` | `public partial interface IAnimatedVisualSource2 : IAnimatedVisualSource` | yes | WinUI parity — `AnimatedIcon.idl:6-10` |
| `IAnimatedVisualSource2.cs:9` | `Markers` | yes | |
| `IAnimatedVisualSource2.cs:11` | `SetColorProperty(string, Color)` | yes | |
| `IThemableAnimatedVisualSource.cs:7` | `public partial interface IThemableAnimatedVisualSource : IAnimatedVisualSource` | **none** | Uno-only; no counterpart in either IDL |
| `IThemableAnimatedVisualSource.cs:9` | `SetColorThemeProperty(string, Color?)` | **none** | Uno-only |
| `IThemableAnimatedVisualSource.cs:10` | `GetColorThemeProperty(string)` | **none** | Uno-only |
| `ElementCompositionPreview.cs:72` | `SetIsTranslationEnabled(UIElement, bool)` | yes | WinUI parity — `controls2.idl:2910` |
| `LottieVisualSourceBase.cs:33` | `public delegate void UpdatedAnimation(string, string)` | **none** | Uno-only; unchanged from `96019901b8:…:33` |
| `LottieVisualSourceBase.cs:139` | `public Task SetSourceAsync(Uri sourceUri)` | **none** | unchanged from `96019901b8:…:90` |

### 4C. Doc amendment — existing doc is now wrong

**`src/Uno.UI/UI/Xaml/Hosting/ElementCompositionPreview.cs:49`**

The signature became `Visual?` (§3) and null now has real behaviour, but the param doc still
describes only the add case. Replace line 49:

```diff
-	/// <param name="visual">The Visual to add to the element's visual tree.</param>
+	/// <param name="visual">The Visual to add to the element's visual tree, or <c>null</c> to remove the previously set child visual.</param>
```

The added clause is **[Uno-authored]**. Learn's page for this method documents only z-order
("added as the last child, therefore on top of the rest of the element in z-order") and says
nothing about null:
<https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.hosting.elementcompositionpreview.setelementchildvisual>

---

## 5. Missing `PackageDiffIgnore` entries

One entry. The PR removes `public Size Measure(Size availableSize)` from
`LottieVisualSourceBase` (base commit `96019901b8:src/AddIns/Uno.UI.Lottie/LottieVisualSourceBase.cs:129`)
without an ignore entry.

Evidence it ships in the baseline: `Measure` sat inside the **same**
`#if !(__WASM__ || HAS_SKOTTIE)` block (opened at base `:100`) as `Play`, `Stop`, `Pause`,
`Resume`, `SetProgress`, `Load`, `Unload` — and all seven of those *do* have entries at
`build/PackageDiffIgnore.xml:2710-2716`. Verified absent:
`grep -c "LottieVisualSourceBase.Measure" build/PackageDiffIgnore.xml` → `0`.

The existing entry at `:2707` is for `Microsoft.UI.Xaml.Controls.IAnimatedVisualSource.Measure`,
a **different type** — it does not cover this.

**Add immediately after line 2716** (`…LottieVisualSourceBase.Unload()`), keeping the
surrounding indentation (2 tabs + 2 spaces, matching its neighbours):

```xml
		  <Member fullName="Windows.Foundation.Size CommunityToolkit.WinUI.Lottie.LottieVisualSourceBase.Measure(Windows.Foundation.Size availableSize)" reason="Removed legacy source-driven Lottie measure API (breaking changes for 7.0)" />
```

The `CommunityToolkit.WinUI.Lottie` namespace is correct — base `:25-30` selects it under
`#if HAS_UNO_WINUI`, and the seven sibling entries use the same namespace.

**Not required, but noted (pre-existing, inert):** `build/PackageDiffIgnore.xml:1207` and
`:1222` both spell the member `CreateTheamableFromLottieAsset` — a transposed typo. The real
members are `ILottieVisualSourceProvider.CreateThemableFromLottieAsset`
(`ILottieVisualSourceProvider.cs:13`) and `LottieVisualSourceProvider.CreateThemableFromLottieAsset`.
Those two entries therefore match nothing. This is harmless today because the members still
exist, but it is a latent trap if either is ever removed. Fixing the spelling is a one-line
hygiene change unrelated to this PR.

---

## 6. Not issues — do not re-raise

1. **Guard inversion on the generated `IAnimatedVisualSource` — REAL, but ALREADY FIXED in
   this worktree.** PR #23889 flipped
   `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/IAnimatedVisualSource.cs:6` from
   `#if false` to `#if __SKIA__`, which activates `[global::Uno.NotImplemented]` on an
   interface the PR *does* implement on Skia. That would fire `Uno0001` on every call site
   (`UnoNotImplementedAnalyzer.cs:74-75` propagates from the containing symbol; `:144-147`
   returns `true` for the parameterless attribute), including
   `AnimatedVisualPlayer.mux.cs:976` — `animatedVisual = source.TryCreateAnimatedVisual(m_rootVisual.Compositor, out diagnostics);`.
   (Adjudication notes circulated a line `967` for this call. That was correct on the PR
   branch; at HEAD the call is at `976` and `967` is the `if (source is IAnimatedVisualSource3 source3)`
   test. Anchor by text, not number.)
   **Commit `f127dfc8fbd` already reverted it to `#if false` and added the member-less
   hand-written partial that makes the sync generator emit the correct polarity.** Verified:
   `git diff origin/agents/animated-visual-player-port HEAD -- <that file>` shows exactly
   `-#if __SKIA__` / `+#if false`. **No action** — but if you rebase the follow-up work
   directly onto the PR branch instead of this worktree, re-apply it.

2. **`AnimatedVisualPlayer : FrameworkElement, IPanel`** — the IDL (`:75-76`) lists only
   `FrameworkElement`, but `IPanel` is `internal partial interface`
   (`src/Uno.UI/UI/Xaml/Controls/Repeater/IPanel.cs:15`) and is implemented explicitly
   (`AnimatedVisualPlayer.h.mux.cs:41`, `UIElementCollection IPanel.Children => …`).
   Zero externally visible surface.

3. **`AnimationController` is not `sealed`** — Learn shows `public sealed class`. Uno declares
   `public partial class`. This is a **repo-wide convention**, not a PR defect: the sync
   generator emits non-sealed partials for every Composition type
   (`CompositionPropertySet`, `CompositionScopedBatch`, `ScalarKeyFrameAnimation`, …). Zero
   `sealed` classes exist under `src/Uno.UI.Composition/Composition/`.

4. **`ILottieVisualSourceProvider` stays public** — it is the cross-assembly
   `ApiExtensibility` contract (see §4A.3). Narrowing it would break the `Uno.UI.Lottie`
   add-in. It also has valid `PackageDiffIgnore` coverage for two of three members
   (`:1206`, `:1208`; the third is the typo in §5).

5. **Nine public members removed from `IAnimatedVisualSource`** (`Update`, `Load`, `Unload`,
   `Play`, `Stop`, `Pause`, `Resume`, `SetProgress`, `Measure`) — correct per the WinUI-parity
   rule; `AnimatedVisualPlayer.idl:22-29` declares exactly one member. All nine already have
   `PackageDiffIgnore` entries added by this PR at `build/PackageDiffIgnore.xml:2699-2707`.

6. **`AnimatedVisualPlayer.legacy.cs` deletion** — the file contained only private members
   (`OnSourceChangedLegacy`, `OnLoadedLegacy`, `OnUnloadedLegacy`, `MeasureOverrideLegacy`,
   `ArrangeOverrideLegacy`). No public-surface impact.

7. **`Duration`, `IsAnimatedVisualLoaded`, `IsPlaying` setters narrowed `internal` → `private`** —
   `internal` was never externally visible, so this is not a public-surface change. It matches
   the IDL, which declares all three as `{ get; }` only (`:81`, `:91`, `:92`).

8. **`Source` / `FallbackContent` nullable annotations** — annotation only; the IDL types are
   reference types and WinUI permits null. No `PackageDiffIgnore` entry needed.

9. **`UIElement.ElementVisualCompositor` (`private protected`), `UIElement.HasCompositionChildVisual`
   (`internal`), `UIElement.SetElementVisualCompositor` (`internal`),
   `ElementCompositionPreview.SetElementVisualCompositor` (`internal`),
   `FrameworkElement.IsViewHit` (`internal override`), `AnimatedVisualPlayer.IsViewHit`
   (`internal override`)** — all correctly non-public. Grep of the whole WinUI IDL tree for
   `ElementVisualCompositor` / `SetElementVisualCompositor` / `HasCompositionChildVisual`
   returns zero hits; these are genuinely Uno-only. Do **not** widen
   `ElementVisualCompositor` to `protected` to satisfy `SKCanvasElement.cs:27` — cross-assembly
   access to a `private protected` `Uno.UI` member is an established shipped pattern
   (`GLCanvasElement.cs:263`, `ElevatedView.cs:138`), enabled by
   `[assembly: InternalsVisibleTo("Uno.WinUI.Graphics2DSK")]` at `src/Uno.UI/AssemblyInfo.cs:33`.

10. **`AsyncAction.GetResults()` is `public`** — but on `internal class AsyncAction`
    (`src/Uno.Foundation/Internal/AsyncAction.cs:8`), so it is unreachable externally, and it
    is the `IAsyncAction` interface implementation. Correct as-is.

11. **`Generator.IsCompositionType`** (`src/Uno.WinAppSDKSyncGenerator/Generator.cs:693`) is
    `private static` on an internal type in an `<OutputType>Exe</OutputType>` build tool that
    is never packed. Not shipped surface.

12. **Learn-wording divergences in already-written summaries** — several existing docs
    paraphrase rather than quote Learn. All are acceptable; listed so nobody "fixes" them
    twice: `AnimatedVisualPlayer.cs:11` "Represents a player for animated visual content."
    (Learn: "An element that displays and controls an IAnimatedVisual."); `IAnimatedVisual2.cs:8`;
    `IAnimatedVisualSource3.cs:10` (Learn's own text here says "Extends IAnimatedVisualSource2",
    which is a Learn copy-paste error — Uno's wording is better); `IDynamicAnimatedVisualSource.cs:10`.
    Note also that Learn's `IAnimatedVisual2` page lists `CreateAnimations`/`DestroyAnimations`
    with **empty** description cells, so `IAnimatedVisual2.cs:13` and `:18` are effectively
    Uno-authored already.

13. **`Generator.cs:201-202` aliases `_netstdReferenceCompositionCompilation = _skiaCompositionCompilation`** —
    not a shortcut. There is only one `Uno.UI.Composition` project
    (`src/Uno.UI.Composition/Uno.UI.Composition.csproj`); no `.Reference` variant exists to load.

---

## 7. Unresolved

1. **`private protected` treatment by `Uno.PackageDiff`.** `build/PackageDiffIgnore.xml`
   contains no entry for any `private protected` member touched here, and treating
   `famandassem` as non-public is the standard call (external assemblies cannot bind it).
   But the differ's source is not in this worktree, so this was **not positively proven**.
   *Settles it:* read `Uno.PackageDiff`'s member-filter, or run the package-diff CI stage
   against a build of this branch and confirm no diagnostic for
   `UIElement.ElementVisualCompositor` or `SKCanvasElement.CreateElementVisual`.

2. **Runtime validation of the `ReadAnimationJsonAsync` narrowing (§2).** The
   single-call-site analysis is a `grep` over `src/**/*.cs` plus overload-resolution
   reasoning; it was **not compiled**. *Settles it:* build
   `src/AddIns/Uno.UI.Lottie/Uno.UI.Lottie.csproj` after the change.

3. **`src/AddIns/Uno.UI.Lottie/LottieVisualSource.reference.cs` exists on the PR branch but
   not at HEAD** (`git diff --stat origin/agents/animated-visual-player-port HEAD` shows it as
   46 deletions). Its `private sealed class ReferenceAnimatedVisual(Compositor) : IAnimatedVisual2`
   is private-nested, so it carried no public surface either way — but if the follow-up PR
   restores that file, re-check it. *Settles it:* decide whether the reference head still needs
   a Lottie implementation, then re-run this audit over that one file.

4. **Doc-coverage baseline for `src/Uno.UI.Composition`.** A figure of "31 of 112 files" was
   circulated during adjudication; measured directly it is **19 of 112** non-recursive, or
   **31 of 224** recursive. Neither matches. Nothing in this spec depends on it, but do not
   quote the original number. *Settles it:* re-measure with a stated denominator if the
   follow-up PR wants a coverage target.

5. **`#if false` vs `#if __SKIA__` corpus counts** in
   `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/` ("295 vs 52") could not be
   confirmed — enumerating that directory times out. The guard *polarity* is independently
   proven by two exemplars read directly (`AnnotatedScrollBarLabel.cs:6-8` = not-implemented
   form; `ContentPresenter.cs:6-8` = implemented form) plus the same-diff A/B control in §6.1,
   so nothing here rests on the counts.
