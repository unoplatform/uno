# Preprocessor constant cleanup for Uno Platform 7.0

**Status**: Proposal
**Audience**: Internal engineering (Uno Platform maintainers)

> Companion to `specs/056-platform-targeting-vocabulary/spec.md`, which covers the platform/host vocabulary
> (`__DESKTOP__`, `HAS_UNO_SKIA_<host>`, `UNO_REFERENCE_API`, the XAML prefixes and the file suffixes). This one
> covers everything else: the rest of the constants defined or branched on across the repository.

## 1. Method

Two sets were enumerated and cross-referenced:

- **Defined** — every symbol inside a `<DefineConstants>` element in any `.props`, `.targets`, `.csproj` or
  `.projitems`: **105 symbols**.
- **Used** — every symbol appearing in a `#if` / `#elif` across `src/**/*.cs`, excluding `Generated/` folders
  (which are regenerated and must not be hand-edited) and excluding trailing comments on the directive line.

The cross-reference produced **47 defined-but-never-used** and **77 used-but-never-defined** symbols, after
subtracting those provided by the .NET SDK and the platform SDKs (`DEBUG`, `NET*_OR_GREATER`, `__ANDROID__`,
`__IOS__`, …) and those introduced by a file-local `#define`.

**"Unused" does not mean "dead".** Sorting the two lists by *why* they are asymmetric is most of the work, and is
what section 2 does. Deleting on the raw lists would break working code.

## 2. Taxonomy

| Category | Meaning | Disposition |
|---|---|---|
| **Consumer-facing** | Defined for user code to branch on. Unused in-repo is correct | Keep — covered by spec 056 |
| **File-local `#define`** | Declared at the top of the file that uses it (14 symbols) | Keep |
| **Developer diagnostics** | Opt-in tracing/profiling switches, enabled by hand while debugging | Keep, document as a category |
| **Ported-code parking** | Ported WinUI code kept in the tree but not compiled, behind a deliberately never-defined symbol | Keep, but see §3.3 |
| **Vendored third-party** | Belongs to imported sources, not ours | Do not touch |
| **MUX port conventions** | Carried verbatim from upstream WinUI sources per the porting rules | Keep |
| **Dead** | Names an era, platform or product that no longer exists | Remove |

## 3. Findings

### 3.1 Typos that silently disable code

These are the actionable bugs. Each reads as a live condition and can never be true.

| Location | Written | Should be |
|---|---|---|
| `src/SamplesApp/SamplesApp.Samples/Windows/UI/Xaml/Controls/ToggleSwitchControl/ToggleSwitch_TemplateReuse.xaml.cs:44` | `#if __ANDROID \|\| __APPLE_UIKIT__` | `__ANDROID__` — the condition currently reduces to `__APPLE_UIKIT__` |
| `src/SamplesApp/SamplesApp.Samples/Windows/UI/Xaml/Controls/TextBox/FromEmptyStringToValueConverter.cs:5` | `#if WinUI` | `WINUI` — preprocessor symbols are case-sensitive |
| `src/SolutionTemplate/5.3/uno53net9blank/uno53net9blank/ControlWithXamlEverywhereExceptDesktop.xaml.cs:6` | `#if !DESKTOP` | `__DESKTOP__` — `!DESKTOP` is always true |

### 3.2 Dead definitions to remove

Defined, never branched on, and naming something that no longer exists.

**Umbrella / Xamarin era** — `src/Uno.UI.Tests.ViewLibrary` and `src/Uno.UI.Tests.ViewLibraryProps` each carry a
block of eight: `HAS_UMBRELLA_BINDING`, `HAS_CRIPPLEDREFLECTION`, `HAS_GEOCOORDINATE`,
`HAS_GEOCOORDINATE_WATCHER`, `HAS_ISTORAGEFILE`, `HAS_SEMAPHORE`, `HAS_FILE_IO`, `HAS_COMPILED_REGEX`. Plus
`HAS_UMBRELLA_UI`, defined in all four `Uno.UWP` project heads, and `HAS_TESTCLOUD_AGENT` in the mobile
SamplesApp head (Xamarin Test Cloud is retired).

**Vendored mono/xaml test projects** — `MONO`, `MULTIPLEX_OS`, `NET_4_0`, `NET_4_5`, `NET_4_6`, `WIN_PLATFORM` in
`System.Xaml.Tests` / `System.Xaml.Tests.MS`. These came with the imported sources; removing them is safe only if
the vendored tests are not resynced from upstream.

**Uno-authored, never consumed** — `IS_CI_OR_DEBUG` (`src/Uno.CrossTargetting.targets`),
`IS_UNO_UI_XamlHost_PROJECT`, `UNO_MIXIN_GENERATION`, `WINAPPSDK_PACKAGED`.

**Harmless redundancy, listed for completeness** — `IS_MPE_X11` is defined but never branched on, because the
file shared between the two media-player projects selects on `IS_MPE_WIN32` and lets `#else` cover X11. Correct
as written; the symbol is simply unnecessary. `RUNTIME_CORECLR` is the same shape next to `RUNTIME_NATIVE_AOT`.

### 3.3 Ported-code parking needs one reserved spelling

A real and useful pattern: ported WinUI code that is not yet implemented is kept visible but uncompiled behind a
symbol that is never defined, with an explanatory comment.

```csharp
#if ApplicableRangeType // UNO TODO
```

Occurrences include `ApplicableRangeType` (27), `ScrollPresenterViewKind_RelativeToEndOfInertiaView` (7),
`IsMouseWheelZoomDisabled` (5), `IsMouseWheelScrollDisabled` (4), `LOOPING_SELECTOR_AVAILABLE`,
`CALENDARVIEW_DENSITYCOLORS_SUPPORTED`, `SCROLLVIEWER_SUPPORTS_ANCHORING`, `FOCUS_IMPLEMENTED`,
`VALIDATE_UITREE_IMPLEMENTED`, `USE_NEW_TP_CODEGEN`, `IS_DESIRED_SMALLER_THAN_CONSTRAINTS_ALLOWED`,
`TAIL_SHADOW`, `WIP`.

The problem is not the pattern, it is that **each one invents its own name**, which makes the category
indistinguishable from a typo — exactly the failure in §3.1, where `#if __ANDROID` looks identical to a parking
symbol. Proposal: adopt a single reserved prefix, e.g. `UNO_TODO_<topic>`, so a symbol that is never defined and
does not carry that prefix is a defect by definition and can be caught automatically.

### 3.4 Dead legacy platform branches

Used in `#if`, never defined, naming a platform Uno no longer targets. Every one of these branches is unreachable:

`SILVERLIGHT` (8), `DOTNET` (6), `METRO` (3), `WINPRT` (2), `__XAMARIN__` (2), `XAMARIN_ANDROID` (2),
`WINDOWS_PHONE` (1), `WPF_APP` (1), `__UWP__` (1), `UAP10_0_19041` (1).

Native-renderer leftovers in the same shape: `HAS_NATIVE_COMMANDBAR` (3), `IS_NATIVE_ELEMENT` (3).

These overlap the dead-`#if` sweep already tracked separately; they are listed here so the two efforts do not
each assume the other covered them.

> **Correction.** `SUPPORTS_NATIVE_DATEPICKER` was listed here and does not belong: it is not a leftover.
> `NativeDatePickerFlyout` is live on Skia-Android through `ISkiaNativeDatePickerProviderExtension`, so the
> branch it guards is a *reachable* path that an undefined symbol had switched off — `DatePicker` never
> forwarded `UseNativeMinMaxDates` to the flyout, silently ignoring the property on the one target where it
> means anything. Deleting the branch would have cemented the defect. This is the §3.1 category (a condition
> that reads live and can never be true), not the §3.4 one, and it is the reason §3.4 must be read as a list of
> candidates rather than a list of deletions: a symbol being undefined proves the branch is dead, not that the
> code behind it was meant to be.

### 3.5 Keep — intentional, but currently undocumented as categories

- **Developer diagnostics** (~17): `TRACE_HIT_TESTING`, `TRACE_LEAKS`, `TRACE_REUSE`, `TRACE_MEMORY_LAYOUT`,
  `TRACE_EFFECTIVE_VIEWPORT`, `TRACE_ROUTED_EVENT_BUBBLING`, `TRACE_COMPOSITION`, `MFSI_DEBUG`, `TICKBAR_DBG`,
  `CMH_DEBUG`, `DBG`, `DEBUG_VERBOSE`, `DEBUG_SET_RESOURCE_SOURCE`, `PROFILE`, `REPORT_FPS`,
  `PRINT_FRAME_TIMES`, `TRACK_REFS`, `DETECT_LEAKS`, `CHECK_LAYOUTED`, `VISUALTREEWALK_DEBUG`,
  `ENABLE_CONTAINER_VISUAL_TRACKING`, `WRITE_EXPECTED`.
- **MUX port conventions**: `MUX_PRERELEASE` (25), `USING_TAEF` (38), `MUX_DEBUG` (11), `MUX` (4). Carried
  verbatim from upstream; the porting rules require fidelity to the original.
- **Vendored third-party**: the `NO_*` BCL-polyfill family (17 symbols) and `NO_EXPOSED_NULLANNOTATIONS` (24)
  belong to `WeCantSpell.Hunspell` under `Uno.WinUI.SpellChecking`; `SYSTEM_PRIVATE_CORELIB` (14) belongs to
  vendored BCL sources.

## 4. Suggested sequencing

1. Fix the three typos in §3.1 — independent, small, and each one is a latent behaviour bug.
2. Remove the dead definitions in §3.2 — no `#if` reads them, so this cannot change behaviour.
3. Agree the reserved parking prefix in §3.3 and rename to it; only then can an automated check exist.
4. Fold §3.4 into the existing dead-`#if` sweep rather than doing it twice.

## 5. Open decisions

1. **Is a build-time guard wanted?** Once §3.3 has a reserved prefix, a check could fail the build on any `#if`
   symbol that is neither defined, file-local, SDK-provided, nor reserved. That is what would have caught
   `#if __ANDROID`. It needs an allow-list for vendored sources.
2. ~~**Do the vendored `System.Xaml.Tests` constants get touched?**~~ **Resolved — no.** Those sources are still
   resynced from upstream, so `MONO`, `MULTIPLEX_OS`, `NET_4_0`, `NET_4_5`, `NET_4_6`, `WIN_PLATFORM`, the six
   `DOTNET` branches and `WPF_APP` stay as they are. This is intentional, not an oversight: the cost of the six
   dead symbols is lower than the cost of a noisier upstream merge. Any future automated check (decision 1) needs
   `src/SourceGenerators/System.Xaml*` and `src/SourceGenerators/XamlGenerationTests` on its allow-list.
3. **Are the developer diagnostic switches worth keeping in-tree at all**, or should they move behind a single
   documented `UNO_DIAGNOSTICS` switch? They are individually harmless but collectively a large share of the
   symbol surface.
4. **`HAS_UMBRELLA_UI` is defined in all four `Uno.UWP` heads.** Confirm no downstream consumer branches on it
   before removal — it is defined in shipped projects, unlike the test-library block.
