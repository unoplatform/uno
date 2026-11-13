---
description: Onboard a ported control or feature area to the weekly WinUI port sync (build/winui-sync/areas.json). Use when asked to add, track, or onboard a control/area for WinUI sync, or to fix an area's baseline.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **WinUI Sync Onboarding Skill**. It adds one entry to `build/winui-sync/areas.json` so the `WinUI Port Sync` workflow starts tracking that area. Background and the meaning of each field: `build/winui-sync/README.md`.

The whole job is getting **two things right**: the path mapping, and the baseline. A baseline that is too new makes the workflow skip upstream changes forever, without any error. When in doubt, pick the older commit.

The user input names the control or area (for example `TeachingTip`, `ItemsRepeater`, `focus management`). Onboard one area per run; for several, repeat the workflow for each.

---

## Execution Workflow

### Phase 0: Preconditions

1. Check the area is not already tracked: look for its `id` and its Uno paths in `build/winui-sync/areas.json`.
2. Find a **full** clone of `microsoft/microsoft-ui-xaml` (all tags and branches, not shallow). Look in the usual sibling work folders first; if none exists, ask the user for the path rather than cloning without asking. Run `git -C <clone> fetch --tags origin` so the newest release tags are present.
3. Refer to the clone as `<clone>` below.

### Phase 1: Map the paths

Manifest paths are written **without** a leading `src/`. Upstream keeps the same folders under `src/` on newer release tags and at the repository root on older commits; the tooling resolves that per commit.

1. **Upstream sources.** For MUX controls this is one folder, `controls/dev/<Name>`. The folder name is not always the control name (`BreadcrumbBar` lives in `controls/dev/Breadcrumb`, `ItemsRepeater` in `controls/dev/Repeater`). List the real names:
   `git -C <clone> ls-tree -d --name-only <latest stable tag> src/controls/dev/`
   For a feature area (focus, text, layout), list every upstream folder that makes it up, usually under `dxaml/xcp/...`. Check where the existing Uno files say they came from: their `MUX Reference` headers name the upstream files.
2. **Upstream tests.** Subfolders such as `APITests`, `InteractionTests`, or `<Name>_ApiTests`. Skip `TestUI` unless the Uno port has a matching sample page that was ported from it.
3. **Upstream strings.** `controls/dev/<Name>/Strings`, if the folder exists.
4. **Uno sources.** Usually `src/Uno.UI/UI/Xaml/Controls/<Name>`. Search for the control's files rather than assuming; some ports are split over several folders, and all of them go in the list.
5. **Uno tests.** Ported MUX tests live under `src/Uno.UI.RuntimeTests/Microsoft/UI/Xaml/Controls/<Name>` (files named `*.mux.cs`). Uno-written tests such as `Given_<Name>.cs` are not part of the mapping.
6. **Uno strings.** The `Strings` folder inside the Uno sources, if there is one. Map strings only when both sides have the folder.

Choose the `id`: lower case, no separators, matching the Uno folder name (`teachingtip`). It becomes part of the branch name `sync/winui/<id>` and must never change afterwards.

### Phase 2: Propose the baseline

Run the seed script **without** `-Write`:

```pwsh
./build/winui-sync/seed-area.ps1 -Id <id> -Title <Name> -UpstreamRepo <clone> `
    -UpstreamSources <...> -UpstreamTests <...> -UpstreamStrings <...> `
    -UnoSources <...> -UnoTests <...> -UnoStrings <...>
```

It prints every commit or tag named by the `MUX Reference` headers, how many files name each, the files that name none, and the oldest one as the proposed baseline. It fails if an upstream path does not exist; fix the mapping and rerun.

Then judge the proposal:

- **All files agree** on one commit: accept it.
- **Mixed, close together:** accept the oldest. The first sync re-checks a little already-ported code, which is harmless.
- **One or two outliers far older than the rest** (for example a single test file naming a commit years older): the oldest commit would make the first sync replay years of changes. Open those files and compare them with upstream at the commit most files agree on. If they match upstream there, use the majority commit. If they really are stale, keep the majority commit and tell the user that these files are behind and need a manual catch-up first.
- **Files with no recorded commit:** files that are Uno-only (`*.Uno.cs`, helpers with no upstream counterpart) need nothing. For a ported file, compare it with upstream at the proposed baseline; if it clearly matches a different version, treat it like an outlier.
- **A commit from before the `controls/dev` layout** (WinUI 2 era, where controls lived in `dev/<Name>`): never use it as the baseline. The area's folder does not exist at that commit, so the whole control would look newly added. Treat files naming such a commit as outliers.
- **Nothing resolves:** there is no baseline to derive. Compare a few central files with upstream at recent stable tags to find the newest tag the port fully matches, and use that.

Never choose a baseline newer than what the headers support just because the code "looks current".

### Phase 3: Check strings

If strings are mapped, the workflow will make the Uno folder an exact mirror of upstream's: files Uno has and upstream does not are **deleted**. Before onboarding, list anything Uno-only in that folder (extra `.resw` files or locales). If there is any, stop and ask the user whether it should move out of the `Strings` folder or the area should be onboarded without a strings mapping.

### Phase 4: Write and verify

1. Rerun the seed command with `-Write`, adding `-Baseline <commit>` if you chose something other than the proposal.
2. Run detection for the area and read the result:
   `./build/winui-sync/detect.ps1 -UpstreamRepo <clone> -AreaFilter <id>`
   It needs the `gh` CLI signed in. The summary row shows the status and the size of the upstream diff the first sync will have to port. A diff of thousands of lines for a control that was ported recently means the baseline is too old or a path is wrong; go back to Phase 2.
3. Run the tooling tests: `./build/winui-sync/tests/run.ps1` (Pester 5.5+).
4. Do not edit anything else. The first sync PR for the area is opened by the workflow, not by this skill.

### Phase 5: Report

Tell the user:

- the entry that was added (id, paths, baseline with its date and tag if it has one);
- which header commits were found and how the baseline was chosen, including any outlier files and what was decided about them;
- the size of the upstream diff waiting for the first sync;
- anything that needs a human: stale files, Uno-only strings, files with no recorded commit that could not be matched.

Commit only when asked: `chore(winui-sync): Track <Name>`.

---

## Rules

- One entry per area; never widen an existing area's paths to cover a second control.
- Never change the `id` or lower the care on the baseline of an area that already has an open sync PR; its branch carries its own baseline.
- To move an existing area's baseline by hand, edit `baseline` in `areas.json` to a full 40-character SHA and say in the commit message what was verified.
- Do not put local paths, private links or usernames in `areas.json` or in commit messages.
