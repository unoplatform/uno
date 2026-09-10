# Generates spec.md (master checklist) from assessments-raw.json + curated metadata.
import json, os, datetime
HERE = os.path.dirname(os.path.abspath(__file__))
A = {x['id']: x for x in json.load(open(os.path.join(HERE, 'assessments-raw.json'), encoding='utf-8'))['assessments']}

TITLE = {
 'BC01':'Remove legacy templated-parent mechanism','BC02':'Remove legacy WASM `IJSObject` DOM-interop API','BC03':'`BaseActivity.Current` annotated nullable',
 'BC04':'Remove `EventsBubblingInManagedCode` DP','BC05':'Remove no-op `RethrowNativeExceptions` config','BC06':'Remove `DefaultsStartingValueFromAnimatedValue` flag',
 'BC07':'Remove redundant bootstrapper meta-packages','BC08':'`NavigatingCancelEventArgs` ctor -> non-public','BC09':'`ComboBox.OnIsDropDownOpenChanged` -> private protected',
 'BC10':'`ItemsControl.OnItemsChanged` parameter type','BC11':'`PageStackEntry.SourcePageType` setter -> non-public','BC12':'`Frame.BackStack` setter -> non-public',
 'BC13':'Fix `WindowActivatedEventArgs.WindowActivationState` type','BC14':'`UserControl` inherits `Control`','BC15':'Seal `PropertyPath`',
 'BC16':'Re-apply `PropertyChangedParams` change','BC17':'`XamlCompositionBrushBase.CompositionBrush` -> protected','BC18':'Remove `UseLegacyHitTest` flag',
 'BC19':'Remove `FlyoutBase.Close()` (use `Hide()`)','BC20':'`FrameworkPropertyMetadata.DefaultUpdateSourceTrigger` -> internal','BC21':'Delete legacy `AutomationProperties` member',
 'BC22':'`IMenu`/`IMenuPresenter`/`ISubMenuOwner` -> non-public','BC23':'Remove `AdaptNative` native-hosting path','BC24':'Hide `BrushConverter` from IntelliSense',
 'BC25':'`Border.ChildProperty` -> internal','BC26':'`DependencyObject` becomes a class','BC27':'`DoubleCollection`: composition not `List<T>`',
 'BC28':'`CompositionSpriteShape.StrokeDashArray` get-only','BC29':'Remove iOS `BypassCheckToCloseKeyboard`','BC30':'Delete `WindowView.ios.cs`',
 'BC31':'Delete `BinderDetails` stub','BC32':'Delete `DelegatedInkTrailVisual` stub','BC33':'Remove `UseInvalidate(Measure/Arrange)Path` flags',
 'BC34':'Remove `TextBox.OnVerticalContentAlignmentChanged` override','BC35':'Remove `IgnoreINPCSameReferences` flag','BC36':'`ContentPresenter.ContentTemplateRoot` -> internal',
 'BC37':'Remove non-DP (CLR) `Setter` codegen fallback','BC38':'Move `Background` `FrameworkElement` -> `Control`','BC39':'Clean up `DependencyPropertyValuePrecedences` enum',
 'BC40':'Delete legacy `ListViewBase` folder','BC41':'`VisualInteractionSource` param -> `Microsoft.UI.Input`','BC42':'Remove `clr-namespace:` XAML leniency',
 'BC43':'Remove `PointerPoint.op_Explicit` shim','BC44':'Remove `Setter<T>`','BC45':'Remove `UseLegacyContentAlignment` flag',
 'BC46':'Remove `ShowClippingBounds` flag','BC47':'Remove `NSObjectExtensions.ValidateDispose`','BC48':'`MaterializableList<T>` -> internal',
 'BC49':'Remove empty `RestoreBindings`/`ClearBindings`','BC50':'Default `UseLegacyPrimaryLanguageOverride` = false','BC51':'Implement `ms-resource:///` URI rewrite',
 'BC52':'Reparent `RadioMenuFlyoutItem` -> `MenuFlyoutItem`','BC53':'Rename `Uno.UI.Toolkit` assembly/namespace','BC54':'`FlyoutBase.DataContext` -> non-public',
 'BC55':'Replace deprecated Android `PreferenceManager`','BC56':'Remove Android `EnableExperimentalKeyboardFocus`','BC57':'`OnTemplateRecycled` -> explicit interface impl',
 'BC58':'`DataContext` on `FrameworkElement` only','BC59':'Enable nullable reference types by default','BC60':'Remove Android native popups',
 'BC61':'Delete iOS `StyleSelector2`/`StyleSelectorCollection`','BC62':'Fix `Lauched`->`Launched` trace constants','BC63':'Remove `SetterBase.set_Property` shim',
 'BC64':'`Duration.TimeSpan` -> read-only property','BC65':'`FrameworkElement`/`ContentControl`: drop `IEnumerable`','BC66':'Remove Android `Window.IsStatusBarTranslucent()`',
 'BC67':'Remove `ColorKeyFrameCollection` throwing shadow setters','BC68':'WinUI sync-generator fix','BC69':'Remove `ApplySettersBeforeTransition` flag',
 'BC70':'Remove no-op `AdjustArrange`','BC71':'Remove Fluent V1 public surface','BC72':'Delete `Uno.UWP` `Vector2Extensions`',
 'BC73':'`TimePickerFlyoutPresenter` -> `Control` base','BC74':'Android drawable extension in retarget keys','BC75':'Move MRT Core (`ApplicationModel.Resources`) -> `Uno.UWP`',
 'BC76':'Remove/internalize `Windows.UI.Input.*` (~173 types)','BC77':'Adjust Android conditionals','BC78':'`BorderLayerRenderer` refactor','BC79':'`Brush` change-subscription perf',
}
REF = {
 'BC01':'#18672','BC02':'PR #19230','BC03':'PR #18582','BC07':'PR #17788','BC10':'(WinUI already matches)','BC16':'PR #17414','BC26':'#17099','BC31':'PR #16757',
 'BC36':'#16148','BC37':'PR #16134','BC39':'PR #15684','BC49':'#13046','BC50':'#13704','BC53':'#12322','BC54':'#12491','BC55':'#1833','BC57':'#13083','BC58':'#13201',
 'BC59':'#9964','BC62':'#13709','BC63':'#13050','BC64':'#13096','BC68':'PR #13867','BC69':'#13326','BC70':'#14478','BC71':'#14765','BC74':'PR #15891','BC76':'#18875',
 'BC77':'PR #12563','BC78':'PR #12593','BC79':'PR #12595',
}
# action / decision-resolved note per item; '' -> use category default
ACT = {
 'BC01':'Hard-remove legacy path incl. public `FrameworkTemplateBuilder` delegate + legacy ctors. **First** fix the tests that currently force `_isLegacyTemplate=true`.',
 'BC04':'Hard-remove the public DP — native bubbling is gone.',
 'BC07':'**Verify** not already removed, then hard-delete the meta-packages.',
 'BC09':'Make `private protected`.',
 'BC16':'**VERIFY design first.** The post-revert pooled `DependencyPropertyChangedEventArgs` may already capture the allocation win, and the struct drops precedence/bypass fields. Decide whether re-applying is still desired before doing it.',
 'BC20':'Make `DefaultUpdateSourceTrigger` internal/remove. Optional stretch: make the whole `FrameworkPropertyMetadata` internal (verify no custom-control author relies on it).',
 'BC21':'**INVESTIGATE / likely drop.** The member at ~L44 (`AnnotationsProperty`) is valid WinUI and must NOT be deleted — the line ref drifted. Real target is probably the native `OnAutomationIdChanged` branches in `AutomationProperties.uno.cs`.',
 'BC23':'Hard-remove the public `AdaptNative`/`TryAdaptNative` surface (native hosting gone).',
 'BC25':'Make the DP `internal` (keep the DP, hide the field) to match WinUI.',
 'BC33':'Hard-delete both flags; inline the always-true dirty-path branch.',
 'BC34':'Delete the `TextBox` override; make base `OnVerticalContentAlignmentChanged` `private protected`.',
 'BC36':'Make `internal` (not `private` — in-assembly callers read it cross-type).',
 'BC39':'Hard-remove obsolete enum members (no `[EditorBrowsable]` aliases).',
 'BC42':'Remove the leniency; migrate repo XAML/tests/docs to `using:`. Hard-error, no deprecation cycle.',
 'BC50':'Flip default to `false` (WinUI restart-to-apply). **Behavior change** — runtime language-switch apps must opt back in; needs a migration note.',
 'BC51':'Implement the rewrite to `ms-resource:///Files/`. **Changes compiled URI value** — behavior-changing, validate at runtime.',
 'BC52':'Reparent to `MenuFlyoutItem`; re-implement the toggle behavior currently inherited from `ToggleMenuFlyoutItem`.',
 'BC53':'**DECIDE the new assembly/namespace name.** Hard rename, no type-forwarders / xmlns alias (per hard-remove policy).',
 'BC54':'Keep `DataContext` internal so `FlyoutBase`->`Popup` forwarding still works; hide only the public surface. **Folded into the BC58 spec.**',
 'BC55':'Use `Context.GetSharedPreferences` with the legacy default name (or `AndroidX.Preference`) — MUST read the **same backing file** so existing user settings survive upgrade.',
 'BC59':'Standalone. Flip `<Nullable>enable</Nullable>` globally + annotate ~4300 files. Low runtime risk, large churn; can run anytime independent of the phases.',
 'BC64':'Make `TimeSpan` a read-only property (WinUI). Confirm treatment of the WinUI-divergent public `Type` field (issue notes UWP may differ).',
 'BC66':'Hard-remove the public Android `Window.IsStatusBarTranslucent()`; repoint the internal consumer to `NativeWindowWrapper`.',
 'BC67':'Delete the entire `new`-shadow back-compat block to match `DoubleKeyFrameCollection`/`ObjectKeyFrameCollection`.',
 'BC71':'Hard-remove `XamlControlsResourcesV1` + `ControlsResourcesVersion.Version1` + build/package scaffolding (V1 content is already gone).',
 'BC75':'Move the MRT-Core types to `Uno.UWP` (lower risk than a new assembly).',
 'BC76':'Hard-remove the ~173 stub types. The **real** `InputInjector` family (test infra) -> move to `Microsoft.UI.Input.Preview.Injection` and update call sites.',
}
SPEC = {'BC14':'bc14-usercontrol-to-control.md','BC26':'bc26-dependencyobject-as-class.md','BC38':'bc38-background-to-control.md','BC58':'bc58-datacontext-frameworkelement-only.md'}
DEFAULT = {
 'native-removal':'Hard-delete.','legacy-dead-code':'Hard-delete.','feature-flag-removal':'Hard-delete the flag; inline the always-taken branch.',
 'visibility-reduction':'Reduce visibility to match WinUI.','signature-change':'Adjust signature to match WinUI.','base-class-change':'Reparent to match WinUI.',
 'namespace-or-assembly-move':'Move to match WinUI.','new-feature':'Implement.','type-system-change':'See notes.','investigation':'Investigate, then act.','decision-needed':'See notes.','other':'See notes.',
}

PHASES = [
 ('Phase 1 — Native-only removals & inert dead-code','Danger 1. The safe vanguard: deletions scoped to native targets being dropped, plus orphaned public stubs with zero consumers. Cannot affect Skia runtime. Ship as one batch.',
  ['BC05','BC29','BC30','BC31','BC40','BC47','BC61','BC60','BC35','BC67']),
 ('Phase 2 — Native-only public-API removals','Danger 2. Native-scoped, but delete public members a Skia user still sees in IntelliSense / compiles against. Native rendering is gone so the symbols are meaningless; hard-remove.',
  ['BC23','BC66']),
 ('Phase 3 — Internal feature-flag, dead-code & shim removals','Danger 2. Cross-target but low blast radius: delete always-on/off flags (inline the live branch), dead helpers, binary-compat shims, no-op members. Behaviour unchanged on defaults. The Setter pipeline trio BC37 -> BC44 -> BC63 must land adjacently in that order.',
  ['BC04','BC02','BC32','BC46','BC06','BC69','BC33','BC49','BC70','BC72','BC07','BC43','BC18','BC45','BC55','BC37','BC44','BC63']),
 ('Phase 4 — Visibility reductions & signature/rename tweaks','Danger 2. Tighten Uno-only public surface to match WinUI (internal/protected/sealed/explicit-interface/hide) plus tiny signature & typo fixes. Each is narrow and independent; effort S. Ideal single sweep.',
  ['BC24','BC15','BC22','BC57','BC48','BC08','BC11','BC12','BC09','BC20','BC25','BC16','BC62','BC41','BC28','BC64']),
 ('Phase 5 — Behavioural defaults, base-class & type tweaks','Danger 3. Wider but localized: visibility on more-derivable hooks, per-type base-class realignments, enum/type-shape changes, and behaviour-changing defaults. Several silently change runtime behaviour even on defaults — gate each on a migration note + runtime/visual validation, not compile-only.',
  ['BC17','BC19','BC27','BC73','BC13','BC65','BC34','BC36','BC52','BC74','BC50','BC42','BC51','BC21','BC75']),
 ('Phase 6 — Larger structural moves & legacy-mechanism removal','Danger 3-4. Heavier multi-file changes: remove the legacy templated-parent mechanism, repurpose the precedence enum, tear down Fluent V1 scaffolding, rename the Toolkit assembly, and remove the legacy `Windows.UI.Input.*` surface. Each is its own PR.',
  ['BC01','BC39','BC71','BC53','BC76']),
 ('Phase 7 — Pervasive type-system changes (each has its own impact spec)','Danger 4-5. Ship last, never batched — each lands as its own separately-stabilized PR with full runtime-test passes. See the dedicated spec per item for Pros/Cons and impact.',
  ['BC58','BC54','BC26','BC14','BC38']),
]

def files(idv, n=3):
    p = A.get(idv, {}).get('relevantPaths', []) or []
    p = [x for x in p if x][:n]
    return ', '.join('`%s`' % x for x in p) if p else '_(see issue)_'

def line(idv):
    a = A.get(idv, {})
    d = a.get('danger','?'); e = a.get('effort','?')
    ref = REF.get(idv,''); reftxt = (' · '+ref) if ref else ''
    act = ACT.get(idv) or DEFAULT.get(a.get('category',''), 'See notes.')
    spec = ''
    if idv in SPEC:
        spec = ' · **[impact spec](%s)**' % SPEC[idv]
    flag = ' ⚠️' if (idv in ('BC07','BC16','BC21','BC53','BC55') or 'VERIFY' in act.upper() or 'DECIDE' in act.upper() or 'INVESTIGATE' in act.upper()) else ''
    out = []
    out.append('- [ ] **%s** — %s%s  `d%s·%s`%s%s' % (idv, TITLE.get(idv, idv), flag, d, e, reftxt, spec))
    out.append('  - %s' % act)
    out.append('  - Files: %s' % files(idv))
    return '\n'.join(out)

today = '2026-06-15'
md = []
md.append('# Uno Platform — Breaking-Changes Rollout (Epic [#8339](https://github.com/unoplatform/uno/issues/8339))')
md.append('')
md.append('> Ordered, danger-ranked checklist for the next major. Items are sequenced **least dangerous first** (native-only & dead code) to **most dangerous last** (pervasive type-system changes). Each unchecked item is meant to be picked up by a single subagent in its own worktree.')
md.append('')
md.append('_Generated %s from a per-item investigation of the current `dev/mazi/breaking` worktree (see `assessments-raw.json` for the raw danger/effort/impact data behind every line)._' % today)
md.append('')
md.append('## Ground rules (decided)')
md.append('')
md.append('1. **Hard-remove everything now** — no `[Obsolete]`/`[EditorBrowsable]` deprecation shims. This is THE breaking major; many items have been pending since 2022.')
md.append('2. **Match WinUI exactly** — when reducing visibility / changing signatures, the target shape is the WinUI one. Do not invent intermediate Uno-only surfaces. Native divergences are removed, not preserved.')
md.append('3. **Native targets are being dropped** (native Android Views, iOS/UIKit, WASM DOM). Native-only changes are therefore very safe and ship first. _Non-UI WinRT APIs in `Uno.UWP`/`Uno.Foundation` are NOT being dropped_ (Skia-on-Android still consumes them) — those are judged on normal merit.')
md.append('4. **The 5 pervasive type-system items get their own impact specs** (BC14, BC26, BC38, BC58 + BC54 folded into BC58) with Pros/Cons. They ship last, one stabilized PR each.')
md.append('5. **Validate at runtime, not compile-only.** For anything that changes behaviour (Phase 5 especially) prove it with a runtime test that fails-before/passes-after.')
md.append('')
md.append('## How a subagent picks up an item')
md.append('')
md.append('1. Take the **topmost unchecked** item (respect the cross-item ordering in the watchlist below).')
md.append('2. Spin up a worktree; make the change per the item\'s action note + its WinUI reference.')
md.append('3. Build the relevant Skia slice; run/extend runtime tests; for behavioural items add a fails-before/passes-after test.')
md.append('4. One focused Conventional-Commit PR per item, referencing #8339. Check the box here.')
md.append('')
md.append('**Legend:** `dN` = danger 1 (safest) → 5 (most dangerous) · `S/M/L` = effort · ⚠️ = has an open decision/verify-first flag (see the item note).')
md.append('')

total = 0
for name, desc, ids in PHASES:
    md.append('---')
    md.append('')
    md.append('## %s' % name)
    md.append('')
    md.append('_%s_' % desc)
    md.append('')
    for idv in ids:
        md.append(line(idv)); total += 1
    md.append('')

# Standalone
md.append('---')
md.append('')
md.append('## Standalone — large but low runtime-risk (run anytime)')
md.append('')
md.append(line('BC59'))
md.append('')

# Watchlist
md.append('---')
md.append('')
md.append('## Sequencing watchlist')
md.append('')
md.append('- **Setter pipeline (Phase 3):** `BC37` (remove CLR-property Setter codegen) → `BC44` (remove `Setter<T>`) → `BC63` (remove `SetterBase.set_Property`). Land adjacently, in order.')
md.append('- **DataContext / DependencyObject (Phase 7):** do `BC58` (generator-wide DataContext->FE-only) **first**; it gates `BC54` (FlyoutBase symptom) and de-risks `BC26` (DependencyObject->class). All touch the same generated DO mixin.')
md.append('- **Background / UserControl (Phase 7):** `BC38` (Background -> Control) precedes `BC14` (UserControl -> Control) so the property relocation is not redone.')
md.append('- **Behaviour-drift items (validate at runtime):** `BC50` (culture no longer changes on setter), `BC45` (Center/Center default), `BC74` (drawable key collision), `BC42` (drop `clr-namespace:`), `BC51` (`ms-resource:///` rewrite) — each changes runtime behaviour even for default users.')
md.append('- **Verify-first / open-decision items:** `BC07` (maybe already removed), `BC16` (struct design maybe obsolete), `BC21` (line ref drifted; may drop), `BC53` (needs a new name), `BC55` (must preserve the prefs backing file).')
md.append('')

# Done/obsolete appendix
done = ['BC03','BC56','BC77','BC78','BC10','BC68','BC79']
md.append('---')
md.append('')
md.append('## Appendix A — Already done / obsolete (verify & check off, no work expected)')
md.append('')
for idv in done:
    a = A.get(idv,{})
    md.append('- [ ] **%s** — %s — _%s_%s' % (idv, TITLE.get(idv,idv), a.get('currentState','?'), (' · '+REF[idv]) if idv in REF else ''))
    md.append('  - %s' % (a.get('notes','')[:240]))
md.append('')
md.append('_BC54 is listed in Phase 7 but has no standalone task — it is implemented as part of BC58._')
md.append('')

open(os.path.join(HERE,'spec.md'),'w',encoding='utf-8').write('\n'.join(md))
print('wrote spec.md with', total, 'phased items (+ BC59 standalone, +', len(done),'appendix)')
