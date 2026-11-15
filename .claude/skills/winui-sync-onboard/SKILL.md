---
description: Onboard a ported control or feature area to the weekly WinUI port sync (build/winui-sync/areas.json), first catching the port up to the latest stable WinUI release. Use when asked to add, track, or onboard a control/area for WinUI sync, or to fix an area's baseline.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **WinUI Sync Onboarding Skill**. It brings one ported control or area up to the latest stable WinUI release and adds it to `build/winui-sync/areas.json` at that release, so the `WinUI Port Sync` workflow only ever has to port what later releases change. Background and the meaning of each field: `build/winui-sync/README.md`.

The catch-up happens here, locally, because this session can take its time, run the control's tests and fix what they find. The workflow's port agent has a cost cap, runs no tests, and is meant for small incremental diffs.

Four things must be right:

1. **The path mapping.** A path that is missing is never synced.
2. **Where the port stands today** (its current baseline). The catch-up ports everything from there. A starting point that is too new skips upstream changes forever, without any error. When in doubt, pick the older commit.
3. **The catch-up itself.** The area is recorded at the latest stable tag, so every upstream change in between must end up ported, not applicable, or recorded as skipped.
4. **The port as a whole.** Headers can claim a version the code never fully reached, and a diff from the baseline cannot show a member that was never ported. So after the catch-up, the whole port is reviewed line by line against the latest tag, and every Uno workaround in it is re-justified or removed. From then on, the weekly diffs start from a port that really matches its baseline.

The user input names the control or area (for example `TeachingTip`, `ItemsRepeater`, `focus management`). Onboard one area per run; for several, repeat the workflow for each.

**Track-only mode.** If the user input says `track only` (or the user picks it in Phase 4), skip the catch-up, the review and the audit: the area is recorded at the port's current baseline, and the workflow's first run ports the rest. Use it only for an area too large to catch up in one session.

---

## Execution Workflow

### Phase 0: Preconditions

1. Check the area is not already tracked: look for its `id` and its Uno paths in `build/winui-sync/areas.json`.
2. Check no open PR already ports this control (`gh pr list --search "<Name> in:title"`), and that no `sync/winui/<id>` branch exists.
3. Find a **full** clone of `microsoft/microsoft-ui-xaml` (all tags and branches, not shallow). Look in the usual sibling work folders first; if none exists, ask the user for the path rather than cloning without asking. Run `git -C <clone> fetch --tags origin` so the newest release tags are present.
4. Refer to the clone as `<clone>` below. Work on a branch created from the latest `master`.

### Phase 1: Map the paths

Manifest paths are written **without** a leading `src/`. Upstream keeps the same folders under `src/` on newer release tags and at the repository root on older commits; the tooling resolves that per commit.

1. **Upstream sources.** For MUX controls this is one folder, `controls/dev/<Name>`. The folder name is not always the control name (`BreadcrumbBar` lives in `controls/dev/Breadcrumb`, `ItemsRepeater` in `controls/dev/Repeater`). List the real names:
   `git -C <clone> ls-tree -d --name-only <latest stable tag> src/controls/dev/`
   For a feature area (focus, text, layout), list every upstream folder that makes it up, usually under `dxaml/xcp/...`. Check where the existing Uno files say they came from: their `MUX Reference` headers name the upstream files.
   When the upstream code sits in a folder shared with many other controls (`dxaml/xcp/dxaml/lib`, `dxaml/xcp/core/inc`), list the control's individual files instead of the folder, headers included. Do the same on the Uno side when the port shares its folder with other controls.
2. **Upstream tests.** Subfolders such as `APITests`, `InteractionTests`, or `<Name>_ApiTests`. Skip `TestUI` unless the Uno port has a matching sample page that was ported from it.
3. **Upstream strings.** `controls/dev/<Name>/Strings`, if the folder exists.
4. **Uno sources.** Usually `src/Uno.UI/UI/Xaml/Controls/<Name>`. Search for the control's files rather than assuming; some ports are split over several folders, and all of them go in the list. Include the control's automation peers.
5. **Uno tests.** Ported MUX tests live under `src/Uno.UI.RuntimeTests/Microsoft/UI/Xaml/Controls/<Name>` (files named `*.mux.cs`). Uno-written tests such as `Given_<Name>.cs` are not part of the mapping.
6. **Uno strings.** The `Strings` folder inside the Uno sources, if there is one. Map strings only when both sides have the folder.
   **Verbatim copies.** A Uno file that is byte-for-byte an upstream file apart from its header (Uno.UI's `Generic.xaml` is WinUI's `dxaml/xcp/dxaml/themes/generic.xaml`) is mapped as a mirror (`-Mirror 'upstream path=uno path'`), not as a source: the sync replaces it without an agent. Confirm it really is verbatim first (diff it with upstream at its header's tag, ignoring line endings). Uno's deltas to such a file belong in a separate file listed in the Uno sources, never inline.
7. **Theme XAML.** Uno keeps control styles in `src/Uno.UI.FluentTheme/Resources/PriorityDefault/<Name>.xaml` and `<Name>_themeresources.xaml` (merged at build time; nothing generated to commit). Upstream, MUX controls keep their styles inside `controls/dev/<Name>`; core controls keep them in the shared `controls/dev/CommonStyles/<Name>_themeresources.xaml`. The seed script finds these files itself (by their `MUX Reference` header, or by name) and adds them to the mapping, for folder and file-based areas alike: check the "Theme XAML added" lines it prints. Pass `-NoThemeXaml` only when the styles deliberately stay out of the area.

Choose the `id`: lower case, no separators, matching the Uno folder name (`teachingtip`). It becomes part of the branch name `sync/winui/<id>` and must never change afterwards.

### Phase 2: Find where the port stands

Run the seed script **without** `-Write`:

```pwsh
./build/winui-sync/seed-area.ps1 -Id <id> -Title <Name> -UpstreamRepo <clone> `
    -UpstreamSources <...> -UpstreamTests <...> -UpstreamStrings <...> `
    -UnoSources <...> -UnoTests <...> -UnoStrings <...>
```

It prints every commit or tag named by the `MUX Reference` headers, how many files name each, the files that name none, and the oldest one as the proposed baseline. It fails if an upstream path does not exist; fix the mapping and rerun.

Then judge the proposal. The result is the **starting point** of the catch-up:

- **All files agree** on one commit: accept it.
- **Mixed, close together:** accept the oldest. The catch-up re-checks a little already-ported code, which is harmless.
- **One or two outliers far older than the rest** (for example a single test file naming a commit years older): compare those files with upstream at the commit most files agree on. If they match upstream there, use the majority commit. If they really are stale, keep the majority commit as the starting point and bring those files up to date as part of the catch-up; say so in the report.
- **Files with no recorded commit:** files that are Uno-only (`*.Uno.cs`, helpers with no upstream counterpart) need nothing. For a ported file, compare it with upstream at the proposed starting point; if it clearly matches a different version, treat it like an outlier.
- **A commit from before the `controls/dev` layout** (WinUI 2 era, where controls lived in `dev/<Name>`): never use it. The area's folder does not exist at that commit, so the whole control would look newly added. Treat files naming such a commit as outliers.
- **Nothing resolves:** compare a few central files with upstream at recent stable tags to find the newest tag the port fully matches, and use that.

Never choose a starting point newer than what the headers support just because the code "looks current".

### Phase 3: Check strings

If strings are mapped, the catch-up and every later sync make the Uno folder an exact mirror of upstream's: files Uno has and upstream does not are **deleted**. List anything Uno-only in that folder (extra `.resw` files or locales). If there is any, stop and ask the user whether it should move out of the `Strings` folder or the area should be onboarded without a strings mapping.

### Phase 4: Catch up to the latest stable release

1. Record the area at its starting point in a **scratch copy** of the manifest, so the tooling can work from it. The repository's `areas.json` is written only once, in Phase 7, with the latest tag as baseline: a starting-point entry must never be left behind in it.

   ```pwsh
   Copy-Item ./build/winui-sync/areas.json <scratch>/areas.json
   ./build/winui-sync/seed-area.ps1 <same arguments as Phase 2> -ManifestPath <scratch>/areas.json -Write [-Baseline <commit>]
   ```

   Add `-Baseline <commit>` if you chose something other than the proposal.
2. Size the catch-up: `./build/winui-sync/detect.ps1 -UpstreamRepo <clone> -ManifestPath <scratch>/areas.json -AreaFilter <id>` (needs the `gh` CLI signed in). The summary row shows the upstream diff to port. Thousands of lines for a recently ported control mean the starting point is too old or a path is wrong; go back to Phase 2. Also count the upstream files in the area: the Phase 5 review covers all of them, not just the changed ones. If the diff is genuinely large (roughly 3,000 changed lines or more, or a rewrite of the control), or the area has dozens of file pairs to review, stop and ask the user: do the catch-up anyway, split it into its own PR first, or switch to track-only mode.
3. Render the workflow's port instructions for this area, so the catch-up follows exactly the rules and report format CI uses:

   ```pwsh
   Import-Module ./build/winui-sync/WinUISync.psm1 -Force
   $area = Get-SyncAreas -ManifestPath <scratch>/areas.json | Where-Object id -EQ <id>
   $latest = Get-LatestStableTag -UpstreamRepo <clone>
   Expand-SyncPrompt -TemplatePath ./build/winui-sync/prompt.md -Area $area -TargetTag $latest.Tag `
       -TargetSha $latest.Sha -UpstreamRepo <clone> -UpstreamDir <clone> | Set-Content <scratch>/port-<id>.md
   ```

4. Follow that file, with these local differences:
   - Use the `/winui-port` skill for anything beyond a small edit. Dependencies that the CI agent would have to skip can be ported here when they are needed and stay within reason; otherwise record them under *Skipped*.
   - Project and MSBuild files may change when the port needs it; call each change out in the PR.
   - Ignore the CI-only limits: the fixed build commands, "do not run tests", "do not run git commands" and "do not commit".
   - Do not write `sync-report.md` to the repository root. Keep the report (same sections) for the PR description.
5. Mirror the strings, if mapped and Phase 3 found nothing Uno-only:
   `Sync-AreaStrings -UpstreamRepo <clone> -Ref $latest.Sha -UpstreamStrings <upstream strings> -UnoStrings <uno strings>`

**Skipped items.** The baseline moves past them, so the PR is not their only record. When you open the PR, also file one issue for the area listing every *Skipped* item (upstream path, why it was not ported, what depends on it) and link it from the PR. If the user did not ask for a PR, list the items in the report and offer to file the issue.

In track-only mode, do steps 1 and 2 here, then write the entry to the repository's `areas.json` at the **starting point** (the seed command with `-Write [-Baseline <commit>]`, without `-ManifestPath`), and go to Phase 7, steps 4 and 5. Detection then shows what the workflow's first run will port.

### Phase 5: Review the port line by line

The catch-up only carried the diff since the starting point. This phase checks that the whole port matches upstream at the latest tag, including anything that was never ported, ported wrong, or drifted since.

1. **Pair the files.** For every upstream file in the area at the latest tag (`.h`, `.cpp`, `.idl`, ported test files), find its Uno counterpart(s). Use the `MUX Reference` headers and the port's naming conventions (`.mux.cs`, `.h.mux.cs`, `.partial.mux.cs`, `.Properties.cs`, generated DP partials). An upstream file with no counterpart is either ported now, or listed as *Not applicable* (Windows-only plumbing, telemetry) or *Skipped* with a reason.
2. **Walk each upstream file top to bottom, member by member:** fields and constants, constructors, methods, event handlers, property registrations and default values (from the `.idl`/generated properties), enum values. For each member, find the Uno counterpart and compare it statement by statement: conditions, early returns, call order, constants, default values, null handling, event-raising order, and which overloads are called.
3. **Classify every member:**
   - **Matches.**
   - **Differs, marked:** the difference is wrapped or commented the way the `/winui-port` skill prescribes (`#if HAS_UNO`, `#if !HAS_UNO` with `TODO Uno:`, `// TODO Uno specific:`). It goes to Phase 6.
   - **Differs, unmarked:** a gap. Fix it to match upstream. If it cannot match, mark it per `/winui-port` and send it to Phase 6.
   - **Missing:** port it. If it cannot be ported, keep the upstream code commented out with `TODO Uno: NOT PORTED - ...` per `/winui-port`.
   - **Uno-only member:** it must be marked or live in a `.uno.cs` file; otherwise mark it. It goes to Phase 6.
4. **Tests:** every upstream test method in the mapped test folders exists in the Uno test files, or is listed as not ported with a reason. A ported test that is disabled (`[Ignore]`, a `[PlatformCondition]` that excludes it) goes to Phase 6.
5. **Headers:** every reviewed Uno file has a `MUX Reference` header naming its upstream file(s); add one where it is missing. Leave versions in place for now: Phase 7 drops them once the area is recorded, because from then on `areas.json` holds the version.
6. **Large areas:** split the review by file pair. If you use subagents, give each one file pair, the classification above and the `/winui-port` conventions, and have it return its findings as a table without editing files. Apply the fixes yourself, so one session owns every change. Tell the user how many subagents you are about to start before starting them; in an unattended run, state it in the report instead.

Keep a review table for the PR: upstream file, Uno file(s), members compared, gaps fixed, members still differing (with the marker that justifies them).

### Phase 6: Audit the Uno workarounds

Every deviation from upstream is re-justified or removed. Skia is the only renderer since Uno 7.0, and Uno's capabilities keep growing, so many workarounds outlive their reason.

1. **Collect** every deviation in the area's Uno files:
   - markers: `TODO Uno`, `TODO: Uno`, `UNO TODO`, `Uno specific`, `Uno-specific`, `Uno workaround`, `// Uno:`;
   - conditional code: `#if HAS_UNO`, `#if !HAS_UNO`, `#if __SKIA__` / `#if !__SKIA__` and other platform symbols, `OperatingSystem.Is*()` checks;
   - the Uno-only members and marked differences found in Phase 5;
   - disabled ported tests.
2. **Decide each one**, with evidence rather than assumption:
   - **Obsolete:** the missing capability exists now (the API is implemented, no longer `[Uno.NotImplemented]`; the composition or input feature it waited for is there), or the workaround served a native renderer that no longer exists. Remove it, restore the upstream code, and show the build and the control's tests still pass.
   - **Still needed:** name the concrete reason and how you checked it: the `[Uno.NotImplemented]` member, the missing type, or the test that fails without it. When removing it is cheap to try, try it: remove it, build, and run the tests.
   - **Unclear:** keep it, and list it for the user with what you tried.
3. **Disabled tests:** re-enable each one and run it. If it passes, keep it enabled. If it still fails, restore the disabling attribute, with a reason that names the actual failure.
4. **Comments:** a workaround that stays gets a short, current reason in its marker comment. Rewrite stale explanations; do not leave history notes.

Keep a workaround table for the PR: location, what it does, verdict (removed / kept / unclear), and evidence.

### Phase 7: Verify and record

The repository's `areas.json` is written only when steps 1 and 2 pass. If they fail and the failure is not fixed, do not write the entry: stop and report what failed. The catch-up commits can stay on the branch for a later run, and the scratch manifest leaves the repository untouched. Switching to track-only mode instead is the user's call.

1. Build `src/Uno.UI/Uno.UI.csproj` (`-p:UnoTargetFrameworkOverride=net10.0`) and the runtime tests project when tests changed. Before committing, build once without `UnoFastDevBuild`, so analyzer errors that CI enforces show up.
2. Run the control's runtime tests with the `/runtime-tests` skill (Skia desktop). New failures caused by the port or by a removed workaround are fixed, not disabled. A failure that already happens on `master` is reported, not fixed here. Where a ported or restored behaviour is unclear, check it against WinUI with `/winui-runtime-tests`.
3. Write the entry to the repository's `areas.json`, at the latest stable tag: the seed command from Phase 2 with `-Write -Baseline $latest.Sha` (no `-ManifestPath`). Then drop the versions from the area's headers, since the entry now records the version: `Remove-MuxReferenceVersion -Paths <absolute Uno sources and tests>` (it prints the files it changed). Headers keep naming their upstream files.
4. Run detection against the repository's manifest (no `-ManifestPath`). The area must show no code or strings change. If it still shows a diff, a path is mapped wrongly or a change was not carried over; fix that before going on.
5. Run the tooling tests: `./build/winui-sync/tests/run.ps1` (Pester 5.5+).

### Phase 8: Commit

Commit in this order, one commit per group that has changes. The sync messages match the ones the workflow uses, so history reads the same either way (`<version>` is the tag's version, such as `2.6.5`):

1. `fix(<id>): Sync <Title> with WinUI <version>`: the catch-up and the gaps fixed by the review.
2. `refactor(<id>): Remove obsolete Uno workarounds`: Phase 6 removals, so reviewers can judge them on their own.
3. `test(<id>): Sync <Title> tests with WinUI <version>`: ported, fixed and re-enabled tests.
4. `chore(<id>): Update <Title> strings from WinUI <version>`: the mirrored strings.
5. `chore(winui-sync): Track <Title>`: the `areas.json` entry.

Push and open the PR when the user asked for it, following the repository's PR template. The PR description carries the report: the starting point and how it was chosen; every upstream change as *Ported*, *Not applicable* or *Skipped*; the review table; the workaround table; the tests that ran and their result; and the skipped-items issue if there is one.

### Phase 9: Report

Tell the user:

- the entry that was added (id, paths, baseline with its tag);
- the starting point, which header commits were found, and how it was chosen, including outlier files and what was done about them;
- the size of the catch-up, how many changes were ported, not applicable and skipped, and the skipped-items issue;
- what the line-by-line review found beyond the catch-up: gaps fixed, members still differing;
- the workarounds removed, kept and unclear, and the tests re-enabled;
- the build and test results;
- anything that needs a human: Uno-only strings, unclear workarounds, files with no recorded commit that could not be matched, project file changes, pre-existing test failures.

---

## Rules

- One entry per area; never widen an existing area's paths to cover a second control.
- An area is recorded at the latest stable tag only in the same change that ports everything up to it and reviews the port against it. A track-only entry stays at the port's real starting point.
- Remove a workaround only with evidence that it is no longer needed: a passing build and the control's tests without it. Never remove one because it "looks unnecessary".
- Every difference from upstream that stays in the port is marked the way `/winui-port` prescribes. An unmarked difference is a gap to fix, not a style choice.
- Never change the `id` or lower the care on the baseline of an area that already has an open sync PR; its branch carries its own baseline.
- To move an existing area's baseline by hand, edit `baseline` in `areas.json` to a full 40-character SHA and say in the commit message what was verified.
- Do not put local paths, private links or usernames in `areas.json`, commit messages or the PR.
