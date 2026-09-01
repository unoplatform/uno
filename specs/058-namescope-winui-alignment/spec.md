# Spec 058: Name Resolution & NameScope — WinUI Alignment

> **Status**: Draft — 2026-09-01
> **Author**: Martin Zikmund
> **Umbrella issues**: #21129 (`Align FindName with WinUI`), #23391
> **Scope**: `x:Name` / `Name` registration, `NameScope`, `FrameworkElement.FindName`,
> `{Binding ElementName}`, deferred (`x:Load`) name entries, runtime XAML reader namescope.

---

## Executive Summary

Uno resolves elements by name with a **push** model invented for Uno; WinUI uses a **pull**
model built on a namescope owned by a `DependencyObject`. The divergence is not cosmetic: it
produces a recurring class of bug that has been point-fixed at least nine times since 2019 and
keeps returning, and it blocks two draft PRs that have each been open for over a year.

The enabling precondition — `DependencyObject` being a class rather than an interface — landed
only recently (BC26). **This is the first time the aligned design is expressible.** Both prior
attempts predate it by two years and failed for exactly that reason.

### Honest scoping note

This epic does **not** clear a large backlog. It closes roughly **7 distinct open user-filed
bugs** plus two umbrella issues. Most of the historical evidence (#16250, #7161, #7189, #5248,
#7244, #23088, #6124, #4505, #598) is already closed as *completed* — those were the piecemeal
point-fixes. The value is structural: stopping the recurrence, and unblocking work that is
currently stalled.

---

## 1. What WinUI Actually Does (verified against C++ sources)

Reference: the `microsoft-ui-xaml` tree (paths below have no `src/` prefix).

### 1.1 Storage

`String ElementName;` on `runtimeclass Binding`
(`dxaml/xcp/dxaml/idl/winrt/core/microsoft.ui.xaml.coretypes.idl:5243`). The XamlOM model
declares it with **no** `[OffsetFieldName]`
(`dxaml/xcp/tools/XCPTypesAutoGen/XamlOM/Model/Microsoft.UI.Xaml.Data.cs:141-145`), so the
generated type table marks it `IsSparse | IsPublic` — literally *no native backing field*.
The core object `CBinding` (`core/inc/Binding.h:40-67`) has exactly one member: `m_strPath`.

The parser has **zero** ElementName-specific code: `{Binding ElementName=Foo}` becomes
`StartMember` plus a plain `TextValue` node (`core/Parser/MePullParser.cpp:365-380`).

`Binding` also **freezes** on first use (`BindingExpression_Partial.cpp:93`), so `ElementName`
is legally set-once-before-attach. That is why no mutable routing object is needed.

### 1.2 Resolution

All of it lives in `BindingExpression`:

```
OnAttach -> CalculateEffectiveSource          (BindingExpression_Partial.cpp:144, 1199)
         -> AttachToEffectiveSource           (:460)   first check is get_ElementName() != NULL
         -> AttachToElementName               (:831)   anchors on target FE, or a MENTOR FE
         -> GetSourceElement
         -> FrameworkElement::FindNameInPage  (FrameworkElement_partial.cpp:1150-1250)
```

`FindNameInPage` probes `FindName`, then climbs `TemplatedParent`, then hops through ItemsHost
panels and `ItemsRepeater`. Precedence: **explicit `Source` beats `ElementName` beats
`RelativeSource` beats `DataContext`**.

**Deferral is one-shot, two channels.** (1) On miss *and* `!IsInLiveTree()`, a handler is added
to the anchor's `Loaded` (`:869-881`); `TargetLoaded` (`:1749`) removes it, retries **once**,
and stops — the in-source comment says *"the name lookup happens exactly once"* (`:1792-1793`).
On miss while already live: no retry, no error, no trace. Permanently dead and silent.
(2) Non-FrameworkElement targets (Setter values, brushes, transforms) get an
`InheritanceContextChangedHandler` (`:384-397`, `:1701-1735`) to acquire a mentor first.

**Lifetime.** ElementName is the **only** effective-source kind held weakly
(`ValueWeakReference`, `:864`); `Source` and `DataContext` are strong (`:1206`, `:597`).

### 1.3 Where the indirection actually lives

The "name to maybe-not-yet-there element" indirection WinUI *does* have is in the **namescope**,
not on `Binding`: `NameScopeTableEntry` is a tagged union
`{Empty, StrongRef, WeakRef, DeferredElementCreator}`
(`components/namescope/inc/NameScopeTableEntry.h:46-52`) with a retry loop in
`NameScopeTable::TryGetElement` (`components/namescope/lib/NameScopeTable.cpp:28-49`).

Registration is a **tree-lifecycle** property: `CDependencyObject::Enter` calls `RegisterName` +
`RegisterDeferredStandardNameScopeEntries` (`depends.cpp:986-1001`); `Leave` calls the matching
`UnregisterName` + `UnregisterDeferredStandardNameScopeEntries` (`depends.cpp:1275-1280`).

`x:Load` elements register a **proxy** under the real name
(`CDeferredElement::RegisterProxyName`, `core/core/elements/DeferredElement.cpp:443`), and
`CCoreServices::GetNamedObject` **realizes the proxy inline on lookup**
(`core/dll/xcpcore_namescope.cpp:94-102`). A name lookup force-materialises an `x:Load` element
rather than waiting for it.

Standard-namescope entries are stored **strongly**; template entries and self-registrations are
weak. Uno's `NameScope` is unconditionally weak (`NameScope.cs:50`).

### 1.4 The other by-name features use four *different* mechanisms

WinUI does **not** unify these. Any single shared abstraction is an Uno choice, not a WinUI one.

| Feature | Mechanism | Timing | Ref | Failure |
|---|---|---|---|---|
| `{Binding ElementName}` | `FindNameInPage` | attach + 1 `Loaded` retry | weak | silent |
| `Timeline.TargetName` | `GetNamedObject` | at `Begin` | weak | `AG_E_RUNTIME_SB_BEGIN_INVALID_TARGET` |
| `Setter.Target` | `TryGetElementByName` | on `GetProperty` | **strong** | silent |
| `RelativePanel` | child-`Name` scan **fast path**, namescope **fallback** for deferred elements | every `MeasureOverride` | raw ptr | `AG_E_RELATIVEPANEL_NAME_NOT_FOUND` |

> **Correction (design round).** An earlier revision of this table claimed `RelativePanel` uses no
> namescope at all. That is wrong: `CRelativePanel::ResolveConstraints` calls
> `GetAdjustedReferenceObjectAndNamescopeType(this)` (`RelativePanel.cpp:100`) and
> `RPGraph::GetNodeByValue` uses that owner/type for `GetDeferredElementIfExists` on a miss
> (`RPGraph.cpp:279-293`) before raising the error. The child scan is only the fast path.
> **Consequence: workstream 0 is NOT namescope-independent** and cannot be used as a warm-up
> that ships ahead of the namescope model.

`x:Bind` is a fifth, unrelated path: the compiler emits direct field access, and `x:Bind` has no
`ElementName` property at all.

---

## 2. Empirically Measured WinUI Behaviour

These were measured by **running tests against WinAppSDK** (prior-art branch `findname`,
commit `c4e52a9b0d2 "test: Adjust FindName tests so that they pass on WinUI"`). They outrank
anything derived by reading C++, and several are counter-intuitive.

1. **`FindName` is a pure namescope probe — no tree search whatsoever.** Code-created elements
   with `Name` set but never entered into a namescope are **not** findable. Confirmed across
   three scenarios. Every one of Uno's walker special cases (`ContentControl.Content`,
   `UserControl.Content`, `Popup.Child`, `Button.Flyout`, `UIElement.ContextFlyout`) is
   therefore **wrong behaviour**, not a helpful extension.
2. **Flyout content declared in XAML with `x:Name` IS findable from the page root**, despite
   never entering the visual tree. So the walker's flyout special cases produced right-ish
   answers for entirely the wrong reason. This is also the strongest argument that registration
   must happen on **Enter** (which reaches non-visual and collapsed content), never on `Loaded`.
3. **Renaming an element does NOT unregister its old name.** After `btn.Name = "MyButton2"`,
   *both* old and new names resolve to `btn`. Root cause in WinUI is arguably a bug:
   `CDependencyObject::SetName` does `std::swap(strOldName, m_strName)` **before** calling
   `UnregisterName`, which is guarded by `if (!m_strName.IsNull() && ...)` — so it no-ops.
   **Decision required: replicate the quirk, or diverge and document.**
4. **`Leave` unregisters only the element's *current* name.** Stale pre-rename entries leak
   permanently, and no subsequent rename ever cleans them up.
5. **Duplicate names in one scope: last writer wins, silently.** No exception, no warning.
   Uno's `NameScope.RegisterName` logs *"The name [X] already exists in the current XAML scope"*
   (`NameScope.cs:45-48`) — **that warning has no WinUI counterpart.**
6. **Name assignment alone registers nothing; tree entry does.**
7. **Elements inside a `ResourceDictionary` are findable**; a child can find its ancestor by
   name (resolution goes upward and sideways, not only into descendants).
   *(Added after the WinUI-adjust commit — treat as expected-not-yet-proven.)*
8. **A resolved ElementName binding survives its source leaving the tree** and keeps tracking
   changes. *Circumstantial — this is the load-bearing claim of the whole pull design and
   carries no CI evidence. Re-measure directly before relying on it.*

---

## 3. Root Causes

| # | id | Defect | Effort |
|---|---|---|---|
| 1 | `plain-name-registration` | Plain `Name=` (and a code-set `Name`) is never registered in any namescope | M |
| 2 | `resourcedictionary-name-exclusion` | `x:Name` in a `ResourceDictionary` gets neither a backing field nor a namescope entry — **build break** | S |
| 3 | `enter-leave-registration` | No Enter/Leave registration; `UnregisterName` has zero live call sites | L |
| 4 | `push-not-pull` | ElementName resolution is a one-shot push at `Loading`, reachable only via the measure path | L |
| 5 | `findname-is-a-tree-walk` | `FindName` is an ad-hoc visual-tree walk, not a namescope read | L |
| 6 | `runtime-reader-no-namescope` | `XamlObjectBuilder` has no namescope at all | M |
| 7 | `elementname-not-a-string` | `Binding.ElementName` routes through `ElementNameSubject`, not a string | M |
| 8 | `deferred-force-realize` | Name lookup does not force-realize an `x:Load` element | M |
| 9 | `relativepanel-by-name` | `RelativePanel` resolves siblings via `ElementNameSubject`, fails opaquely at compile time | S |

### Evidence highlights

- **(1)** The generator emits `__nameScope.RegisterName` at exactly one place —
  `XamlFileGenerator.cs:3466` — gated on
  `member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace` (i.e. `x:Name` only).
  At runtime `FrameworkElement.Name`'s changed-callback only sets `AutomationProperties.AutomationId`.
  **This gate is load-bearing**: it is PR #14942, written specifically to stop the
  *"name already exists in the current XAML scope"* warning storm. Do not simply revert it —
  see 2.5, the warning itself has no WinUI counterpart.
- **(2)** The same generator block guards on `isMemberInsideResourceDictionary.isInside`
  **twice** — `XamlFileGenerator.cs:3461` (RegisterName) and `:3473` (backing field). The
  fallthrough at `:3487` emits only a `// x:Name <value>` comment, while other parts of the
  generator still emit member-style access to that identifier, producing `CS0103`.
- **(3)** `UnregisterName` appears in four places: the `INameScope.cs:37` declaration, the
  `NameScope.cs:54` implementation, and **two commented-out** references at
  `DependencyObject.mux.cs:369` and `UIElement.mux.cs:1491`, both under `TODO Uno: NOT PORTED`
  citing `depends.cpp:986-1001` / `:1275-1280`.
- **(4)** The only retry hook's sole caller is `InnerMeasureCore`
  (`FrameworkElement.Layout.cs:286`), and `UIElement.DoMeasure` returns early when
  `Visibility == Collapsed` (`UIElement.Layout.crossruntime.cs:262-266`). A collapsed or
  never-measured subtree — hidden panel, unopened Flyout, inactive tab — never resolves.
- **(7)** `_isElementNameSource` is set only from `ParentBinding.ElementNameSubject != null`
  (`BindingExpression.cs:140-143`), so `new Binding { ElementName = "x" }` from code-behind is a
  **silent no-op** that falls through to `DataContext`.

---

## 4. The Real Blocker

**Uno has never had a namescope *owner* to register a name into, and the one walk that visits
every named element still cannot carry one.**

> **Corrections (design round).** This section originally said "two parallel Enter/Leave
> implementations" and named two ownerless call sites. Both undercount:
>
> - **There are SIX Enter/Leave families**, not two. Beyond `DependencyObject`'s and `UIElement`'s:
>   (3) `UIElement.EnterImpl(bool live)` / `LeaveImpl(bool live)` (`UIElement.mux.cs:1091`, `:1596`) —
>   a same-named *third arity* and an overload-resolution trap; (4) `FlyoutBase.Enter/Leave`
>   (`FlyoutBase.cs:83`, `:91`, `internal new virtual`); (5) `KeyboardAcceleratorCollection.Enter/Leave`;
>   (6) `KeyboardAccelerator.EnterImpl`. Plus a seventh namescope mechanism entirely,
>   `Flyout.SynchronizeNamescope` (`Flyout.cs:86-92`), which pushes a scope onto `Content` via
>   `SetNameScope`.
> - **`FlyoutBase` HIDES `DependencyObject`'s non-virtual `Enter`/`Leave`**, so `base.Enter(...)` in
>   `Flyout.mux.cs:10` and `MenuFlyout.mux.cs:69` reaches an **empty stub**.
>   **CORRECTION (refactor-mapping round): this is a naming defect, not a missing walk.** An earlier
>   revision claimed "the DependencyObject walk has never run for any flyout". That is **false**.
>   `DependencyObject.PropertySystem.mux.cs:292` calls `provider.Enter(pAdjustedNamescopeOwner, @params)`
>   where `provider` is *statically typed* `DependencyObject`, so it binds to the real
>   `DependencyObject.Enter` — not to FlyoutBase's hiding member. `ContextFlyoutProperty` and
>   `Button.FlyoutProperty` are reference-typed DPs that pass `ShouldEnterLeaveProperty`, so **every
>   entering `UIElement` with a flyout set already runs the full DO walk on it**, from
>   `UIElement.mux.cs:1242`, *before* the explicit `pFlyoutBase.Enter(...)` at `:1265`.
>   Consequence: simply dropping `new` would add a **second** DO walk in the same `EnterImpl`
>   (the `_isProcessingEnterLeave` guard has already cleared by `:1265`). The correct fix is a pure
>   **rename** that removes the hiding without changing a single dispatch.
> - **There are ELEVEN ownerless seed sites**, not nine and not two: `UIElement.mux.cs:1242/1265/1276/1833/1846/1861`,
>   `Button.mux.cs:21/33`, `UIElement.cs:803/806`, and `ResourceDictionary.cs:256`
>   (`fe.LeaveImpl(new LeaveParams())` — a different case: that overload has **no owner parameter at
>   all**, so there is nothing to pass). A further ~11 sites are ownerless only because their overload
>   never had an owner slot: `VisualTree.cs:445`, `UIElement.crossruntime.cs:126/146/151`,
>   `DependencyObject.PropertySystem.mux.cs:249`, `Flyout.mux.cs:28/44`, `MenuFlyout.mux.cs:35/62`,
>   `MenuBarItem.cs:72/88`. Most importantly the original text omitted
>   **`UIElement.cs:1674-1675`**, the **incremental attach** path behind every runtime `Children.Add`,
>   ListView container realisation and Frame graft — and therefore the actual mechanism of
>   #19420 / #22987.
> - **The owner is `null` on every runtime path today**, verified by exhaustive call-graph trace. That
>   is what makes the signature-convergence refactor provably inert. The only sites passing a non-null
>   owner (`UIElement.Properties.cs:134/138`, passing `this`) target `KeyboardAcceleratorCollection`'s
>   *hiding* members, which consume and discard it.
> - **The two walks recurse over different edge sets** and are already composed, not parallel:
>   `EnterProperties` recurses over DP values, `UIElement.EnterImpl` over `_children`. They must stay
>   distinct — merging the bodies would double-visit every element that is both a DP value and a visual
>   child (`ContentControl.Content`, `Border.Child`: the common case).
> - "Always null" is not literally true: `UIElement.Properties.cs:134/138` already passes `this`.
> - **C++ line citations in this document have drifted ~4 lines** against the current checkout.
>   Enter-side registration is `depends.cpp:978-993` (not `:986-1001`); Leave-side is `:1267-1272`
>   (not `:1275-1280`); the Popup dual-namescope block is `:902-920` (not `:910-928`). The
>   `TODO Uno: NOT PORTED` comments in `DependencyObject.mux.cs` quote the stale numbers and should
>   be corrected as the holes are filled. Also `NameScopeTableEntry.h` is under
>   `components/namescope/lib/`, not `inc/`.

There are **two parallel, unmerged Enter/Leave implementations** at the core of it, and only the
wrong one has the namescope parameter:

- **WinUI-faithful, on `DependencyObject`** —
  `internal void Enter(DependencyObject? namescopeOwner, EnterParams @params)`
  (`DependencyObject.mux.cs:74`), with `EnterImpl` (`:148`), `Leave`/`LeaveImpl` (`:288`, `:347`).
  The registration holes live here (`:163`, `:366`). `EnterParams.SkipNameRegistration` and
  `LeaveParams.SkipNameRegistration` already exist (`EnterParams.cs:20`, `LeaveParams.cs:20`).
- **A duplicate on `UIElement` with no namescope parameter** —
  `internal void Enter(EnterParams @params, int depth)` (`UIElement.mux.cs:928`). Its own header
  comment states the problem: *"NOTE: This should actually be on DependencyObject, not
  UIElement. We'll be able to do it once DependencyObject is a class instead of an interface."*

The `UIElement` walk is the one that actually recurses into visual children (`ChildEnter`,
`UIElement.crossruntime.cs:132-152`), and where it reaches the DO layer it **hardcodes the owner
away**:

```
UIElement.mux.cs:1242   ((DependencyObject)this).EnterImpl(null, @params);
UIElement.mux.cs:1833   ((DependencyObject)this).LeaveImpl(null, @params);
```

So even uncommenting `depends.cpp:986-1001` verbatim today would call `RegisterName(null)` for
every element. The entry point never establishes an owner either — `VisualTree.cs:423-424`:
*"TODO Uno: The logic here is more complex in WinUI, setting the namescope owner. Not needed
currently."*

**Why this killed both prior attempts.** Lacking an owner, both faked one by climbing ancestors
for an *inherited* attached DP, and both then wrote unregistration that is not owner-aware.
Registration is the easy half; **unregistration is where an ownerless model fails**, and that is
exactly where each branch stopped.

`DependencyObject`-as-class (BC26) is the precondition, and it has only just landed.

---

## 5. Backlog Map

### Closed by this epic

| Issue | Root cause |
|---|---|
| #24293 — `x:Name` in Resources: no backing field, generated code references it (**build break**) | 2 |
| #4032 — Named resource entries of top-level XAML element aren't generated | 2 |
| #19420 — ElementName binding not evaluated for programmatically added elements | 1, 3 |
| #22987 — ElementName bindings don't resolve for programmatically-added named elements | 1, 3 |
| #21129 — Align `FindName` with WinUI *(umbrella; absorbs #14663, #7043, #17020)* | 5, 6 |
| #16743 — Can't use element name for Binding inside a Flyout | 3, 4 |
| #8532 — `Binding.ElementName.Name` returns null instead of name | 7 |
| #19558 — `GetTemplateChild` can't find element with `x:Load="true"` — **needs a repro** (see below) | 5, 8 |
| #11750 — `RelativePanel` attached properties should fail gracefully | 9 |
| #6602 — ElementName not working in `CommandBar` inside `NavigationView` | 1 |
| #3362 — Referencing parent by name does not work | 2 |
| #5489 — `x:Load` resolving DPs on non-UWP platforms | 4 |
| #18509 — Two-way `x:Bind` on `x:Load` doesn't always unload the bound control | 3 |

> **#19558 downgraded (design round).** Literal `x:Load="true"` produces **no `ElementStub` in Uno
> at all** — `XamlFileGenerator.cs:6750-6752` defers only on `x:DeferLoadStrategy="Lazy"`,
> `x:Load="false"`, or a markup-extension `x:Load`. The `Control.cs:502-506` ElementStub rejection is
> a verified gap of the same *shape*, but is not provably the reported symptom, and the issue carries
> no repro. Do not claim it closed without one.

> **Root cause 1 needs NO generator change (design round).** Verified in the golden output
> (`Out/Given_LazyLoading/WhLoChHaBi/…cs:58,64`) that **both** `Name=` and `x:Name=` set the `Name`
> DP in the object initializer. So Enter-time registration keyed on `FrameworkElement.Name` closes
> root cause 1 by itself, and PR #14942's `x:Name`-only gate never has to be touched — it simply
> becomes behaviourally inert. This removes the risk the evidence note above warns about.

### Action required outside the epic

- **#22987 should be reopened.** It is closed `NOT_PLANNED` **purely by the stale bot** (carries
  the `stale` label). Its body is the best root-cause write-up in the entire backlog and
  includes a working manual workaround. It is a live defect invisible to any open-issue query.
- **#18589** (MarkupExtension + `x:Load` + `x:Name`, generated reference to an undeclared
  field) is shaped identically to root cause 2 but the triggering generator branch was not
  confirmed. Check during the epic; do not claim it as fixed.

### Explicitly rejected (not name-resolution defects)

#21178, #6041, #20723, #23328, #19173, #14913, #8239, #23002, #1206, #17399 and others.
Common false positives: `TemplatedParent` (a parent-pointer mechanism, no namescope
involvement), `x:Load` *deferral* bugs (as opposed to *lookup*), and issues where ElementName
appears only in a working workaround.

---

## 6. Prior Art

Three attempts exist. All are useful as reference; **none should be cherry-picked**, because
`master` has since overtaken their plumbing.

### PR #16976 — `RegisterName in NameScope when entering the visual tree` (draft, conflicting)

Branch `Youssef1313:csm733-2`. Commits authored **2024-06-03/04**, merge-base `81e55a410ee`
(the "last updated 2026" metadata is activity, not code).

Threads an `INameScope?` through the enhanced-lifecycle Enter/Leave virtuals and registers
`FrameworkElement.Name` on Enter. Adds a *parallel* pull path for a plain-string
`Binding.ElementName` (resolve via `NameScope.FindInNamescopes` at attach, then exactly one
`Loaded` retry) while **keeping** `ElementNameSubject` as the primary path. Makes
`VisualTree.AddRoot` mint a root `NameScope` for pure-C# trees.

**Where it stopped:** an attempted native Android/iOS retrofit (`02eada538fc`), kept
deliberately "just for future reference" and reverted the same day. Likely moot now that the
native UI heads are maintenance-only — but note that commit still contains a live debugger stub
(`if (name == "textBox") { }`). **Never cherry-pick it.**

### PR #16177 — `Align FindName behavior with WinUI` (draft)

Branch `Youssef1313:findname`. Commits authored **2024-04-08**, merge-base `c6235f1aa24`.
Closes #7043, #14663.

Rewrites `FindName` as a namescope probe plus `ElementStub.Materialize()`, deleting the 88-line
visual-tree walker (`IFrameworkElement.cs:136-219`). Gives `XamlObjectBuilder` a real
`Stack<NameScope>` — its first namescope ever. Contains the **WinUI-validated test suite**
(section 2).

**Where it stopped:** the terminal commit is literally `chore: Blocked on lifecycle...`, with
in-code comments naming the blocker: *"This should be in Enter, not Loaded. Needs changes from
lifecycle PR :/"* and *"This doesn't work. GetNameScope will be null because the element is
already unloaded. This needs to be in Leave. So, has to wait for lifecycle."*

### PR #7108 — `Generate properties and register names inside resource dictionaries` (closed)

Branch `Youssef1313:issues/4032`, closes #4032. **21 lines** in `XamlFileGenerator.cs` plus 5
test files including `Given_NameScope_When_Inside_ResourceDictionary.xaml` and
`..._ResourceDictionary_Inside_Top_Level_Element.xaml`. Reviewed positively and then **closed by
the stale bot on 2025-02-09** — abandoned, not rejected. Known unhandled sub-case: `x:Name` in a
*top-level* `ResourceDictionary` (a standalone dictionary file). The review consensus was that
it need only not *regress*.

### Relationship

**Sequential, not rival.** #16976 is the producer (populate the namescope), #16177 the consumer
(read from it). Near-disjoint file sets; #16177's terminal commit names #16976's subject as its
blocker. Neither is sufficient alone.

**But merging them is not the plan.** `master` now has a *more* WinUI-faithful lifecycle than
#16976 proposed — a namescope-**owner `DependencyObject`** rather than an `INameScope` reference,
plus the `SkipNameRegistration` flags #16976's own comments wished for. Take #16976's
**ordering**, neither branch's **mechanism**.

---

## 7. Workstream Ordering

Ordering is load-bearing: **step 3 is unsafe before step 2**, because the three existing
`Given_FindName` tests pass today *only* because `FindName` is a tree walk matching on `.Name`.

| # | Workstream | Root causes | Notes |
|---|---|---|---|
| 0 | **RelativePanel diagnostics** | 9 | Independent of everything. Good warm-up, early merged PR. Closes #11750 (`low-hanging-cherry`). |
| 0b | **ResourceDictionary backing fields** | 2 | Small, self-contained, the only *build-breaking* item. Closes #24293 + #4032. Reference PR #7108. |
| 1 | **Namescope owner data model** | — | Unify the two Enter/Leave walks; owner-keyed tables; Standard vs Template split; `NameScopeTableEntry {Empty, StrongRef, WeakRef, DeferredElementCreator}`; strong standard entries. **The part neither prior attempt could attempt.** |
| 2 | **Complete + symmetric registration** | 1, 3 | Fill the `DependencyObject.mux.cs:163/:366` holes. Register plain `Name=`. **Register and unregister must ship in the same PR** — unregistration alone against today's construction-time registration permanently deletes names. |
| 3 | **Lookup: tree walk to namescope pull** | 5, 6 | Rewrite `FindName`; then `XamlObjectBuilder`'s namescope. Ship as two PRs; the reader can follow a release behind. |
| 4 | **ElementName push to pull at attach** | 4 | Retire the measure-gated `ApplyElementNameBindings`. Requires the Collapsed-subtree tests to exist and fail first. |
| 5 | **Deferred entries, realize-on-lookup** | 8 | `RegisterDeferredStandardNameScopeEntries` + realize in `GetNamedObject`. |
| 6 | **Retype `Binding.ElementName` to string** | 7 | Public-API break; belongs on the breaking-changes train. Closes #8532. Move `ElementNameSubject` to `Uno.UI.DataBinding` and delete the pre-existing public `Storyboard.SetTarget(Timeline, ElementNameSubject)` overload. |

---

## 8. Test Matrix

### Existing coverage (baseline — do not duplicate)

Compiled-XAML ElementName **inside templates** is well covered: eight XAML controls under
`Uno.UI.UnitTests/Windows_UI_Xaml_Data/BindingTests/Controls/` driven from `Given_Binding.cs`.
`FindName` has **three** tests (`Given_FindName.cs`) — all set plain `Name` imperatively and
pass *only because FindName is a tree walk*. `NameScope` has two (`Given_Namescope.cs`, one
`[Ignore]`d on #17399). `x:Load` has unit and runtime coverage. One runtime test covers
Setter+ElementName (`When_Refresh_Setter_BindingOnInvocation_ElementName`).
`Given_Binding.cs:158-159` has a commented-out ElementName assertion citing #8532.

Prior art adds ~12 more, several WinUI-measured — see section 2.

### Gaps (must be authored, ideally runnable under `/winui-runtime-tests`)

1. Plain `Name="X"` (no `x:` prefix) as the **target** of `{Binding ElementName=X}` — zero tests exist.
2. `el.Name = "X"` set imperatively **before and after** tree insertion (#19420 / #22987 repros).
3. Element created in `OnApplyTemplate`, bound by ElementName from within that same template
   (the `DatePickerFlyoutPresenter` / `MonochromaticOverlayPresenter` scenario).
4. **Resolution inside a `Collapsed` or never-measured subtree** — the entire measure-gating class.
5. **Leave-side teardown**: named element removed, then a *different* element registered under the
   same name. Must land in the same PR as unregistration.
6. Re-entry: same element removed and re-added; a distinct instance materialised twice via `x:Load`.
7. `FindName` called from a **non-ancestor** (sibling, child, own descendant) — the #14663 matrix.
8. `FindName` for an `x:Name`d object inside a `ResourceDictionary`, and for a
   **non-`FrameworkElement` `DependencyObject`** (`KeyboardAccelerator`, `ThemeShadow`, `DataTemplate`).
9. **A SourceGenerators.Tests compile case** asserting the backing field is generated for an
   `x:Name`d ResourceDictionary entry — #24293 is a *build* break, so no runtime test can catch it.
10. `XamlReader.Load` + `root.FindName` for the full #17020 fragment.
11. `GetTemplateChild` against a template part carrying `x:Load="true"` (#19558); `FindName`
    force-realizing a deferred element rather than returning null.
12. `new Binding { ElementName = "X" }` from code + `SetBinding`, with a negative control proving
    it currently does *not* resolve.
13. `RelativePanel` referencing a non-existent name (diagnostic, not a `CS`-level build failure) and
    a deferred `x:Load` sibling.
14. **`Storyboard`/`Timeline.TargetName` — SETTLED, in the uncomfortable direction.**
    `Timeline.TargetName` *is* pull-based (re-evaluated on every `PropertyInfo` access,
    `Timeline.cs:183` — not a one-shot push), but it is **not** insulated from a `FindName` rewrite:
    `Timeline.GetTargetFromName` (`Timeline.cs:236-250`) calls `fe.FindName(...)` **directly** — the
    very walker workstream 3 replaces — reaching its anchor by climbing `GetParent()`. Only a
    Debug-level log on miss, and **no test guards it**. So it is safe *through* this epic and stops
    being safe the moment workstream 3 lands. A guarding test is mandatory before workstream 3.
15. Popup/Flyout **dual-namescope** entry (a Popup child entered from both logical and visual
    parent namescopes — `depends.cpp:902-920`, unported). Directly under #16743.
16. A WinUI parity pass for the whole matrix.
17. **D1 — the rename divergence.** Rename a live element and assert the **old name no longer
    resolves** while the new one does; then remove it and assert nothing stale is left behind.
    Name the tests for the divergence and comment them with the `depends.cpp:625` reference, so it
    reads as deliberate rather than as a parity miss. Prior art's WinUI-measured rename assertions
    (spec 2.3 / 2.4) must be re-homed with inverted expectations plus that comment — never silently
    deleted. `When_Child_With_Same_Name_As_Modified_Is_Added` is already correct and becomes a gate.
18. **D2 — the duplicate-name flag.** With `FeatureConfiguration.NameScope.WarnOnDuplicateName`
    true (default) a duplicate registration warns; with it false it is silent. In **both** modes
    resolution must be last-writer-wins. Plus: plain `Name=` registration must not reintroduce a
    warning storm — assert on a page mixing `x:Name` and `Name=` with no collisions.
19. **D3 — force-realize on lookup.** `FindName` / `GetTemplateChild` / ElementName resolution
    each materialise an `x:Load="False"` element on hit (#19558), and the un-realize path restores
    the deferred entry rather than leaving a dead strong reference. Also assert `Setter`'s former
    special-case `stub.Materialize()` is now redundant, not merely unused.

---

## 9. Traps (discovered the hard way by prior attempts)

1. **Registration without owner-aware unregistration is what killed both branches.** Uno's
   `NameScope` is a flat `Dictionary<string, ManagedWeakReference>` and `RegisterName` overwrites
   on collision. #16976's `Leave` calls `UnregisterName(name)` with no check that `this` is the
   element currently registered — with a duplicate name, the loser's `Leave` deletes the winner's
   entry. **Fix the data model before writing any registration code.**
2. **Do not build on the inherited attached DP.** `NameScopeProperty` is registered with
   `FrameworkPropertyMetadataOptions.Inherits` (`NameScope.cs:98`), so on a settled tree *every*
   element reports a namescope. Both branches' `AdjustNameScope`/`FindNameScope` helpers work only
   by DP-timing accident.
3. **`Loaded`/`Unloaded` can never be the registration hooks.** By `Unloaded` the element is
   detached and DP inheritance is severed. `Loaded` is also too late (async) and too narrow (never
   fires for an unopened flyout's items, an unrealised `DataTemplate` root, or a `Collapsed` subtree).
4. **Do not copy #16976's `nameScope.FindName(name) is null` dedup guard** — its own comment admits
   it is a hack and it makes registration order-dependent. `EnterParams.SkipNameRegistration` is
   the WinUI answer; decide the parse-time-vs-Enter-time split up front.
4b. **The rename divergence is deliberate (D1).** Uno unregisters the old name on rename; WinUI
   leaks it forever due to the `depends.cpp:625` swap-before-unregister bug. Anyone "restoring
   parity" here is reintroducing a bug — the comment at each site says so.
5. **Deleting the `FindName` walker is bigger than the 2024 diff implies — it has grown.**
   `IFrameworkElement.cs:136-219` now carries a `hasAnyChildren` gate, a `UserControl.Content` case,
   and a `TextCommandBarFlyout` infinite-recursion guard added after a real bug. Verify the
   namescope model covers all three, especially the recursion case.
6. **#16177 silently *deleted* coverage rather than moving it** — ~10 assertions in
   `Given_Binding.cs` and `Given_XamlReader.cs` were replaced with `Assert.IsNull(...)` on the
   `FindName` call that fetched the element. Those tests still pass and no longer test anything.
   Re-home those scenarios.
7. **No inventory exists of product code relying on today's over-finding `FindName`.** A
   namescope-only `FindName` will start returning null where things currently work by accident.
   Budget time for fallout.
8. **`_isElementNameSource` is never set on a string ElementName path**, so while unresolved the
   binding transiently evaluates against the ambient `DataContext`.
9. **`IsInLiveTree` is not universally safe** — `UIElement.MuxInternal.cs:9-15` throws
   `NotSupportedException` outside `__CROSSRUNTIME__`/`IS_UNIT_TESTS`.

---

## 10. Decisions

### Settled (2026-09-01)

**D1 — The rename quirk (2.3): DIVERGE, and report it upstream.**

*(Revised after the design round produced harder evidence; this section previously said replicate.)*

The stale-entry leak is a **verified WinUI bug**, not a design choice:
`CDependencyObject::SetName` does `std::swap(strOldName, m_strName)` at `depends.cpp:625-627`,
*then* calls `UnregisterName` at `:631`, whose guard `if (!m_strName.IsNull() && …)` (`:718`) is
by then false — a guaranteed no-op. The comment immediately above the swap says *"it is necessary
to unregister the old name prior to setting the new value"*, directly contradicting the code, and
the error-recovery path at `:638` swaps back **and re-registers**, proving the author expected
`strOldName` still populated there. The swap was hoisted one statement too early.

Decisive practical point: the registration receipt (see the design's `unregistrationSafety`)
makes the **correct** behaviour free and the buggy behaviour *actively harder* — replicating it
would mean deliberately not unregistering what we recorded. And no application can depend on the
quirk without a test asserting that a stale name still resolves.

Consequences the implementation must honour:
- `FrameworkElement.OnNameChanged` identity-clears the receipt's old name, then registers the new
  one. Prior-art PR #16177 already does this — it is **correct**, and its currently-red
  `When_Child_With_Same_Name_As_Modified_Is_Added` should be re-homed rather than inherited.
- Every divergent site carries
  `// Uno diverges here (WinUI depends.cpp:625 SetName bug) — spec 058 D1`.
- The tests are named for the divergence, so a future reader sees it is deliberate.
- The WinUI-measured assertions from prior art that encode the quirk (spec 2.3 / 2.4) must be
  **re-homed with their expectations inverted and a comment explaining why**, never silently
  deleted (see Trap 6).

**Follow-up:** file an issue upstream on `microsoft/microsoft-ui-xaml` describing the `SetName`
swap-before-unregister ordering. If it is ever fixed, this divergence becomes parity for free.

**D2 — The duplicate-name warning: KEEP, but make it switchable.**
Default stays Uno's current behaviour (log *"The name [X] already exists in the current XAML
scope"*), because it catches real authoring mistakes. But WinUI silently lets the last writer win,
so both behaviours must be reachable via a feature flag:

```csharp
// src/Uno.UI/FeatureConfiguration.cs
public static class NameScope
{
    /// <summary>
    /// When true (default), registering a name that already exists in the same scope logs a
    /// warning. WinUI has no such diagnostic and silently lets the last registration win;
    /// set to false to match that behaviour exactly.
    /// </summary>
    public static bool WarnOnDuplicateName { get; set; } = true;
}
```

The flag governs the **diagnostic only** — the resolution semantics are last-writer-wins in both
modes, matching WinUI. This also settles the generator gate: the `x:Name`-only restriction from
PR #14942 exists purely to suppress this warning, so with the warning made switchable the gate
can be widened to plain `Name=` (root cause 1) without reintroducing the warning storm.

**D3 — `x:Load` force-realize: MATCH WINUI.**
A name lookup force-materialises a deferred element, per `CCoreServices::GetNamedObject`
realizing the proxy inline. This inverts today's Uno semantics (which wait for materialisation),
so:
- `ElementStub` content must be registered under its real name as a **deferred** entry
  (`RegisterDeferredStandardNameScopeEntries` / `NameScopeTableEntry.DeferredElementCreator`),
  not merely findable after the fact.
- `FindName`, `GetTemplateChild` and ElementName resolution all realize on hit. Closes #19558.
- `Setter.cs:242` currently forces `stub.Materialize()` as a special case — that becomes the
  general rule and the special case should be removed.
- Explicitly test the un-realize path: `x:Load` flipping back to `False` must restore the
  deferred entry, not leave a dead strong reference.

### Still open

**D4 — Strong vs weak entries.** WinUI stores standard-namescope entries strongly and template
entries weakly; Uno is unconditionally weak (`NameScope.cs:50`). Going owner-keyed with strong
standard entries has real lifetime implications in Uno, whose object graph and collection
semantics differ — and it interacts with collectible ALCs used by Hot Reload. Decide with the
design round's lifetime analysis in hand.

**D5 — Ownership of #16177 / #16976** — close as prior art, or hand back? See section 11.

---

## 11. Open Questions for the Prior-Art Author

Both PRs are open and were touched recently; these are worth asking rather than re-deriving.

1. Which `Given_FindName` tests were **literally executed** on WinAppSDK vs reasoned about?
   `c4e52a9b0d2` covers six; `When_FromResources` and `When_ChildCanFindParent` came later.
   We need to know exactly where the authoritative line falls before treating any of it as spec.
2. Was the WinUI rename-leak (2.3) traced to the `SetName` `std::swap` no-op, or is there a
   deliberate mechanism we're missing? Replicate or diverge?
3. `When_Child_With_Same_Name_As_Modified_Is_Added` carries unconditional WinUI assertions that
   `OnNameChanged` cannot satisfy — deliberate spec-first authoring, or unfinished?
4. Did `When_Binding_By_Programmatically_Setting_Name` ever run on the WinAppSDK head? That single
   claim — a resolved ElementName binding survives its source leaving the tree — is load-bearing
   for the whole pull design and has no CI evidence.
5. Does `When_FromResources` actually pass with #16177 applied, given the generator still gates
   `RegisterName` on `!isMemberInsideResourceDictionary.isInside`?
6. PR #14942 was deliberate — how should Enter-time registration reconcile with it? Is
   `EnterParams.SkipNameRegistration` the plan behind the `PostParseRegisterNames` comment?
7. On the native retrofit (`02eada538fc`): which failure was hit first? (Likely moot post
   native-drop, but it may say something about the design.)
8. Was the all-weak `NameScope` a deliberate Uno choice?
9. Both branches reinvented a `FindNameScope` ancestor climb while `NameScope.FindInNamescopes`
   (`NameScope.cs:107`) already does that walk **plus** a `scope.Owner` hop for non-`UIElement`
   DOs (XAML Behaviors triggers). Was dropping the `Owner` hop intentional?
10. In #16976, `bindingExpression.OnAttach()` is called immediately **before**
    `details.SetBinding(bindingExpression)` — deliberate ordering, or incidental?
11. Any inventory of product code relying on today's over-finding `FindName`?
12. Are both PRs still live for you, or shall we close them and cite them as prior art?

---

## 12. Provenance

Everything above was derived by reading real sources — the WinUI C++ tree, this repo, the public
issue tracker, and the three prior-art branches. Behavioural claims about WinUI are either
(a) read from C++, or (b) **measured** on WinAppSDK by the prior-art tests, and are labelled as
such in section 1 vs section 2. Nothing here was validated by running Uno at runtime; that is the
next phase.
