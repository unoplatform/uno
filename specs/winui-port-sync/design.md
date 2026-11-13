# WinUI port sync — design

## Intent

Uno ports a large amount of WinUI C++ code to C#. After the initial port, upstream keeps
changing (fixes, new behavior, tests, translations) and the Uno copy silently drifts.
Catching up today is a manual, per-control effort that only happens when someone notices.

This adds a scheduled, semi-automated loop: when a new **stable** WinUI release changes
code we have ported, a draft PR with the ported changes and an issue describing them
show up without anyone asking. A maintainer reviews and merges; nothing merges on its own.

## Goals

- Weekly GitHub Actions run that, per tracked *area*, detects upstream changes between the
  area's recorded baseline and the newest stable WinUI tag.
- Port those changes (logic, tests, theme XAML) with an agent using the `/winui-port` rules.
- Replace translations (`.resw`) wholesale from upstream, no agent involved.
- Build the result before opening the PR.
- One issue + one draft PR per area, updated in place on later runs.
- Areas are controls first; the same mechanism must work for feature areas
  (e.g. focus management) that span several upstream folders.

## Non-goals

- Running runtime tests in the sync run. PR CI does that.
- Tracking WinUI `main` or experimental/preview tags.
- Auto-merging anything.
- Porting controls that are not ported yet.
- Onboarding every ported control on day one.

## Anti-goals

- A PR that bumps the baseline while quietly dropping an upstream change. Anything not
  ported must be listed, and the issue must stay open for it.
- Spending agent tokens when nothing changed upstream.
- A workflow whose agent holds a token that can push or write to the repo.
- Duplicate issues/PRs for the same area.

## Constraints

- Ported files carry `MUX Reference <file>, commit <sha>` / `tag <tag>` headers. About 92% of
  the ~700 headered files record a commit or tag; formats and SHA lengths vary, and files
  in one control often point at different commits.
- Headers cannot see new upstream files, tests, `.resw`, or theme XAML.
- Stable upstream tags look like `winui3/release/X.Y.Z`; anything with a suffix
  (`-experimental`, …) is not stable.
- Upstream layout: MUX controls in `controls/dev/<Control>/` (sources, `Strings/`, tests,
  theme XAML); framework code under `dxaml/`.
- Upstream layout is not stable: newer release tags keep everything under `src/`
  (`src/controls/dev/...`), older commits at the repository root, and a release tag is not a
  descendant of an older baseline. Manifest paths are therefore layout-neutral, resolved per
  commit, and changes are found by comparing folder trees (`git diff <base>:<path>
  <target>:<path>`), not by walking history.
- Uno `.cs` files need UTF-8 BOM + CRLF, tabs, MUX/MIT headers (see `.claude/rules/code-style.md`).
- Repo-tracked docs and the manifest must not reference private artifacts.
- The existing agent workflow (`claude-code-review.yml`) sets the security bar: no
  `id-token: write`, scoped token, credentials not persisted, pinned action.

## Approaches

Where the per-area baseline lives:

1. **Headers only** — diff each upstream file from the SHA in its Uno counterpart's header.
   No new state, but blind to new files, tests, translations and theme XAML, and useless
   for files with no recorded SHA.
2. **Manifest only** — checked-in file with upstream paths, Uno paths and one baseline per
   area. Complete diffs, but drifts from headers when someone ports by hand.
3. **Manifest seeded from headers** — manifest as in 2, baseline seeded from the headers
   (oldest SHA when mixed), and every sync PR bumps both manifest and headers.

**Chosen: 3.** Area-level diffs are the only way to see new files, tests and themes; the
oldest-SHA rule means the agent may re-check something already ported but never misses a
change; headers stay truthful for humans.

## Design

### Manifest

`build/winui-sync/areas.json` (builder may pick YAML if tooling is simpler). Per area:

| Field | Meaning |
|-------|---------|
| `id` | Stable slug, used in branch name and issue marker (`scrollview`) |
| `title` | Display name |
| `baseline` | Full upstream commit SHA the Uno port is known to match |
| `upstream.sources` | Upstream paths for logic, IDL, theme XAML |
| `upstream.tests` | Upstream test paths (optional) |
| `upstream.strings` | Upstream `Strings` folder (optional) |
| `uno.sources` / `uno.tests` / `uno.strings` | Matching Uno paths |
| `model` | Optional per-area model override |

A feature area is just an entry with several `upstream.sources` paths. Paths, not single
files, so new and deleted upstream files show up in the diff.

### Onboarding an area

A script (`build/winui-sync/seed`) proposes an entry: it collects every `MUX Reference`
commit/tag under the Uno paths, resolves them against a WinUI clone, and picks the oldest
ancestor as `baseline`. Files with no recorded SHA are listed for a human to settle. The
human reviews and commits the entry. The pilot is four areas, chosen by the maintainer: NavigationView, BreadcrumbBar,
SelectorBar and TabView. Each is a self-contained `controls/dev/<Control>` folder.

### Weekly workflow

`.github/workflows/winui-port-sync.yml`, `schedule` (weekly) + `workflow_dispatch`
(inputs: area filter, target tag override, dry run). Guarded by
`github.repository == 'unoplatform/uno'`. No `pull_request`/comment triggers, so no
contributor-controlled text reaches the agent.

**Job 1 — detect (plain git, no agent)**

1. Partial clone of `microsoft/microsoft-ui-xaml` with tags.
2. Target = highest `winui3/release/X.Y.Z` tag with no suffix, by version order.
3. For each area: take `baseline` from the area's open sync branch if one exists,
   otherwise from the default branch. Run
   `git diff --quiet <baseline>..<target> -- <sources> <tests>` and compare the upstream
   `Strings` folder at `<target>` with Uno's.
4. Emit a matrix of areas with `code-changed` and/or `strings-changed`. Empty matrix →
   workflow ends, zero agent cost.
5. Cap areas per run (default 10, oldest baseline first) and `max-parallel: 3`; the rest
   wait for next week. The run summary lists everything detected, capped or not.

**Job 2 — sync (matrix, one per area)**

1. Check out Uno on the area's sync branch `sync/winui/<id>` (create from the default
   branch, or merge the default branch into the existing one). `persist-credentials: false`.
2. **Strings (script):** if the area maps strings, delete the Uno `Strings` folder and copy
   upstream's at `<target>`. Always latest; upstream removals are removals here too.
3. **Port (agent, only if `code-changed`):** the agent gets the area entry, the upstream
   clone, and the diff range. It ports following `/winui-port`, updates `MUX Reference`
   headers on touched files to `<target>`, and writes `sync-report.md`:
   upstream changes grouped by commit/PR, what was ported, what was judged not applicable
   and why, and what was **skipped**.
4. **Build (agent loop):** build the Skia `Uno.UI` project (plus runtime tests project when
   tests changed) with analyzers on, single TFM. The agent fixes its own build errors, up
   to a small bounded number of attempts. Final build status goes in the report.
5. **Bump baseline (script):** set `baseline` to the target commit in the manifest.
6. **Publish (script, separate step holding the write token):** commit in logical groups
   (logic, tests, strings, manifest — Conventional Commits), push, then create or update
   the issue and the draft PR.

Dependency rule for the agent: if a ported change needs code outside the area's paths
that changed in the same range, port it too when it is small and self-contained. If it
is large, or belongs to another tracked area, leave it and list it as skipped with the
upstream reference.

### Issue and PR

- Found by hidden body marker `<!-- winui-sync:<id> -->` plus a `winui-sync` label; at most
  one open issue and one open PR per area. A later run updates both in place.
- Issue: follows the repo's enhancement form fields; body is the report — baseline →
  target, upstream change list, ported / not applicable / skipped.
- PR: draft, fills `.github/PULL_REQUEST_TEMPLATE.md`, states build result.
  `closes #issue` only when nothing was skipped and the build passed; otherwise it
  references the issue, which stays open as the tracker for the leftovers.
- Strings-only change: same PR flow without the agent; issue body is the file list.
- Build still failing after the attempts: PR is still opened as draft, flagged in title
  and body.
- The report is kept on the sync branch (`build/winui-sync/reports/<id>.md`), one section per
  run. Skipped counts add up across runs on the same PR, so a later run that skips nothing
  cannot turn the PR into one that closes the issue.
- Nothing is published, and the baseline does not move, when the agent did not finish, its
  report is missing or malformed, or its patch touches anything outside code under `src/`.
- An area that cannot be checked (bad baseline, missing upstream path) is reported and fails
  the run, without stopping the other areas.

### Sync branch rules

- Only an **open** sync PR makes its branch the working base. The run merges the default
  branch into it; it never force-pushes, so maintainer commits on the branch survive.
- Merge conflict, or a baseline on the branch the run cannot read: skip the area, say so
  in the run summary and in a PR comment. No partial push.
- PR closed without merging: the old branch is ignored; the next run starts a fresh
  branch from the default branch and opens a new PR (the issue is reused).

### Security

- The agent runs in its own job with `contents: read` only and no git credentials. It
  hands over a patch plus `sync-report.md` as an artifact. A separate publish job, with no
  agent in it, applies the patch and holds `contents: write`, `pull-requests: write`,
  `issues: write`. This works whatever token the action itself needs.
- Model key: a dedicated repository secret for this workflow (API key auth, as the review
  workflow uses), so sync spend is separable from review spend.
- No `id-token: write`; action pinned to the same patched version as the review workflow.
- Upstream diff content is treated as data. The prompt is loaded from the default branch.
- Agent tool allowlist: file read/edit, `git` read-only against the upstream clone,
  `dotnet build`. No network tools.
- Known residual: the model API key is present in the agent step and `dotnet build`
  executes repo code. Accepted because triggers are schedule/dispatch only.

### Cost control

- Agent runs only for areas whose tracked paths changed; with stable releases a few times
  a quarter, most weekly runs end in job 1.
- Default model for porting is the Opus tier (faithful C++→C# needs it), overridable per
  area; dry-run input prints the matrix and diff stats without running agents.

## Calls made by the author (not explicitly decided by the maintainer)

- Manifest location/format, branch naming, label and marker scheme.
- "Latest stable" = highest non-suffixed version across all release lines.
- Per-run cap of 10 areas, 3 in parallel.
- Analyzers on for the build step, so PRs match CI.
- `closes` only when nothing was skipped; otherwise the issue stays open.
- Splitting agent and publish into separate jobs so the agent never holds a write token.
- Sync branch rules (merge, never force-push; skip on conflict; fresh branch after a
  closed PR).
- A dedicated model-key secret rather than reusing the review one.

## Left to the builder

- Exact build command/solution filter and runner OS for the build step.
- Seed script language; resolution of abbreviated SHAs and of tags in headers.
- Checking that each Uno `Strings` folder really mirrors upstream (locale folder casing,
  Uno-only files) before enabling wholesale replace for that area.
- Prompt wording, attempt limits, timeouts.
- How the agent maps an upstream file to its Uno partial files when headers are missing.
