# WinUI port sync

Keeps code ported from WinUI C++ in step with the latest **stable** WinUI release.

Every Monday the `WinUI Port Sync` workflow (`.github/workflows/winui-port-sync.yml`):

1. finds the newest `winui3/release/X.Y.Z` tag (suffixed tags such as `-experimental` are ignored);
2. for each area in `areas.json`, compares the area's upstream folders at its recorded `baseline`
   commit with the same folders at that tag — plain git, no agent, no cost when nothing changed;
3. for each changed area, has an agent port the changes and build `Uno.UI`;
4. replaces the area's `Strings` folder content with upstream's (no agent involved);
5. opens or updates **one issue and one draft PR per area** (`sync/winui/<id>` branch).

Nothing merges on its own.

## Reading a sync issue and PR

- The issue lists every upstream change as **Ported**, **Not applicable** or **Skipped**.
- **Skipped** means a change was deliberately left out, usually a dependency too large to port
  automatically or code that belongs to another area. The PR moves the baseline forward, so the
  issue is the only remaining record of a skipped change: it stays open until someone ports it.
- The PR says `closes #issue` only when nothing was skipped **and** the port build passed.
  Otherwise it says `related to #issue`, and a failing build is flagged in the PR title.
- The report lives on the sync branch in `build/winui-sync/reports/<id>.md`, one section per run.
  When a newer WinUI release arrives while the PR is still open, the new run adds a section and the
  skipped counts add up, so an earlier run's skipped items keep the issue open. After porting a
  skipped item by hand on the branch, set `skipped=` in that section's marker line to the number
  still outstanding.
- Nothing is published for an area when the agent fails, times out or writes no well-formed
  report, or when its patch touches anything other than code under `src/`. The run summary says
  why, and the next run redoes the port.
- Tests are not run by the workflow. PR CI does that.
- Review fixes can be pushed straight to the sync branch. Later runs merge the default branch
  into it and never force-push while the PR is open.
- Closing a PR without merging rejects that attempt; the next run starts a fresh branch and PR.

## Onboarding an area

An area is a control or a feature area: one or more upstream folders and the Uno folders they were
ported to. Paths are written without a leading `src/`; the tooling finds them at each commit.

```pwsh
./build/winui-sync/seed-area.ps1 -Id teachingtip -Title TeachingTip `
    -UpstreamRepo <path to a full microsoft-ui-xaml clone> `
    -UpstreamSources controls/dev/TeachingTip `
    -UpstreamTests controls/dev/TeachingTip/APITests, controls/dev/TeachingTip/InteractionTests `
    -UpstreamStrings controls/dev/TeachingTip/Strings `
    -UnoSources src/Uno.UI/UI/Xaml/Controls/TeachingTip `
    -UnoTests src/Uno.UI.RuntimeTests/Microsoft/UI/Xaml/Controls/TeachingTip `
    -UnoStrings src/Uno.UI/UI/Xaml/Controls/TeachingTip/Strings
```

It reads the `MUX Reference` headers under the Uno paths, resolves the commits and tags they name,
and proposes the oldest one as the baseline. **Check the proposal before adding `-Write`:**

- a baseline that is too new silently drops upstream changes;
- a baseline that is much older than most files (one stale header) makes the first run re-check
  years of changes — pick the commit most files agree on and set it by hand in `areas.json`;
- files listed as having no recorded commit are yours to judge.

## Running it by hand

*Actions → WinUI Port Sync → Run workflow*:

- `dry_run`: only detect; the run summary lists each area, its status and the upstream diff size.
- `area`: limit the run to one area id.
- `target_tag`: sync to a specific tag instead of the latest stable one.

Locally, with a WinUI clone:

```pwsh
./build/winui-sync/detect.ps1 -UpstreamRepo <clone>      # what would run
./build/winui-sync/tests/run.ps1                          # tooling tests (Pester 5.5+)
```

## Setup and limits

- Secret `ANTHROPIC_CODEREVIEW_KEY`: the API key the port agent uses. It is the same key as the Claude review workflow, so sync and review spend share one budget.
- The port agent runs on Sonnet (`claude-sonnet-5-5`). An area can name another model with a
  `model` field in its `areas.json` entry.
- Label `winui-sync`: created by the workflow if missing; issues and PRs are found by it plus a
  hidden `<!-- winui-sync:<id> -->` marker, so do not remove either.
- The repository must allow GitHub Actions to create pull requests.
- Pull requests opened with the workflow's own token do not trigger other GitHub Actions
  `pull_request` workflows (for example the XAML style check). Push a commit to the branch, or
  close and reopen the PR, to run them. Switching the publish job to a GitHub App token removes
  this limitation.
- At most 10 areas are processed per run, oldest baseline first, three at a time. The rest are
  listed as deferred and picked up the following week.
- The agent job is read-only. It produces a patch; a separate job without an agent pushes it.
  Changes to CI files, tooling, agent rules or `.resw` files are never taken from the agent.
