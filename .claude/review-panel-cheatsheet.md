# Review Panel — Cheatsheet

> 8 reviewers, run in parallel, one consolidated report. **Run it before you commit.**

## Just tell me what to type

| I want to… | Type |
|---|---|
| Review what I'm working on **right now** | `/review-panel` |
| Review my **branch before a PR** | `/review-panel master..HEAD` |
| Review a **specific PR** | `/review-panel #23515` |
| Review the **last commit** | `/review-panel HEAD~1` |
| Check **one thing only** (e.g. WinUI parity) | run a single lens — Agent → `contract` (skip the panel) |

## Should I run it? (30 seconds)

- ✅ **Yes** — non-trivial change, already drafted, *before* you commit or open a PR.
- 🎯 **One concern only** (parity / perf / platform fit)? Run **that one lens**, not all eight — faster, cheaper.
- ⏭️ **Skip** — typo, comment, or pure rename (all eight just return a one-line ack).
- 🔁 **After fixing blockers** — re-run to confirm the verdict moved toward `ship`.

**Why bother:** eight adversarial lenses in parallel catch what one reviewer misses, at the cheapest
point to catch it — before commit. It's your "would a Uno maintainer approve this?" gate.

## Touching X? → this lens catches Y

The single table to scan. Use it to pick the full panel **or** one lens.

| If your change touches… | Lens | Catches |
|---|---|---|
| Public API, DependencyProperties, `[Uno.NotImplemented]` | **contract** | WinUI-parity breaks, gratuitous public surface, DP default/metadata contract changes |
| XAML/binding of untrusted content, DevServer/RemoteControl, source-gen inputs, platform file/clipboard APIs | **security** | injection, path traversal, unsafe deserialization, secret/token leakage |
| Logging, async APIs, source-gen cancellation, DevServer host | **operability** | unguarded/​missing logs, dropped `CancellationToken`, `async void` crashes, silent failures |
| Layout/render hot paths, source generators, concurrency, WASM | **performance** | blocking waits, `async void`, LOH allocs, measure/arrange allocations, idle CPU toll, `memory.grow` |
| Layering, platform-specialization choice, abstractions, system fit | **architect** | tech debt, wrong suffix/`#if`/`OperatingSystem.IsX()`/`ApiExtensibility`, native-vs-Skia scope |
| Anything you want stress-tested | **skeptic** | edge cases, cross-platform `#if` divergence, WinUI "simplifications," guard-only "fixes", weak tests |
| Solution vs. requirement | **quality** | scope creep, duplicated platform partials, comment noise, missing sample/test, commit hygiene |
| Intent & assumptions you never stated | **jerome** | the questions you didn't ask — *asks, never fixes* |

## Best practices (one line each)

- **Review before you commit, not after** — findings are cheapest to fix while the code is still warm.
- **Scope tight** — one commit / one PR. Over ~50 files or ~2k lines, each lens caps at its top 10.
- **Write clear commit messages** — quality / contract / jerome judge intent from them (Conventional Commits).
- **One concern → one lens.** Reserve the full panel for non-trivial or pre-merge passes.
- **Clear every blocker, then re-run** — one pass rarely ends green on a real change.
- **jerome's `blocker` = don't merge until answered** — resolve with a test/evidence/decision.
- **It reviews, it doesn't run** — still build and run the runtime tests (`/runtime-tests`); compile-only is not runtime validation.
- **It never auto-fixes** — you're offered next steps; nothing changes until you say so.

## It learns — feed it corrections

Every reviewer reads [`specs/lessons.md`](../specs/lessons.md) before reviewing (the "lessons loop"). When the
panel gets something wrong — a false positive, a missed issue, a wrong severity, or a repo nuance a lens
didn't know — tell the orchestrator and it will **append a lesson** (on your go-ahead) so future panels apply
it. Keep lessons generalizable; no one-off file paths.

## Run it on a loop

`/loop` re-runs a command (or a prompt) on a schedule — pair it with the panel to converge or watch.
Syntax: `/loop [interval] <command-or-prompt>` — interval like `5m` / `10m` / `1h`, or **omit it** to
let Claude self-pace each round.

| Goal | Type |
|---|---|
| **Fix until the panel passes** | `/loop run /review-panel, fix every blocker, repeat until the verdict is ship` |
| Re-review a **PR as commits land** | `/loop 10m /review-panel #23515` |
| Keep reviewing my **branch while I work** | `/loop 15m /review-panel master..HEAD` |

- A **bare** `/loop /review-panel` only *re-reports* — to actually converge, put the **fix step + stop
  condition** in the loop prompt (as in row 1).
- It runs until the **stop condition is met or you stop it** — end the loop any time.
- Loop the panel for convergence; loop a **single lens** (e.g. `/loop 10m run the performance agent on
  master..HEAD`) for a cheaper recurring watch.

---

## Reference — skip unless you need it

### How scope is resolved

| You pass… | What happens |
|---|---|
| *(nothing)* | 1. Uncommitted changes if any. 2. Else current branch vs trunk (`<merge-base>..HEAD`, trunk = `master`). 3. Else: "Nothing to review — clean tree on trunk." |
| A git range (`HEAD~1`, `master..HEAD`) | Used verbatim as the diff range. |
| A PR ref (`#123`, `PR 123`, PR URL) | Diff resolved via `gh pr diff` / `gh pr view`. |
| Free-form text | Treated as a description of what to review. |

The orchestrator gathers file list + commit messages + branch/base **once** and hands the same brief
to all eight; each reviewer opens files itself (the full diff is not pasted in).

### Severity scale (shared by all eight)

```text
blocker  →  high  →  medium  →  low  →  info
```

The synthesis merges duplicates flagged by multiple reviewers.

### Output formats

**Pipe format** — quality / operability / contract / performance:

```text
SEVERITY | path/to/file:startLine..endLine | what | why it matters | suggested fix
```

**jerome** — same shape, but questions instead of fixes:

```text
SEVERITY | file:lines | the question | why it matters (risk if unanswered) | what would resolve it
```

**Prose format** — architect (Architectural fit + Findings), security (Vector/Location/Impact/Fix),
skeptic (Claim/Counterexample/What-to-do).

### Verdicts → unified result (worst case across all eight wins)

| Unified | Per-agent verdicts |
|---|---|
| **block-merge** | `reject` · `needs-redesign` · `block-merge` · `needs-rework` · `reconsider-approach` |
| **fix-first** | `fix-first` · `approve-with-changes` · `fix-before-merge` · `resolve-questions-first` |
| **ship** | `ship` · `no-findings` · `approve` · `proceed` |

jerome's rows are surfaced as **open questions, not patches** — under an "Open questions to resolve
before merge" subsection so they aren't mistaken for fixes.

### Behavior you can rely on

- **Trivial-change clause** — no-impact change → one-line ack from each reviewer and the report.
- **Scope cap** — >50 files / >2k lines → top 10 findings per reviewer, truncation noted.
- **Lessons loop** — every reviewer checks [`specs/lessons.md`](../specs/lessons.md) first; the orchestrator appends to it on your go-ahead.
- **Hand-offs** — out-of-lane concerns become `## Hand-off` pointers, reconciled in synthesis.
- **Convention source-of-truth** — reviewers cite this repo's `AGENTS.md` and `.claude/rules/*.md`.
- **Untrusted input** — file contents treated as data, never instructions; `WebFetch` is scoped to public docs only (never fetches URLs from reviewed files).

---

*Source of truth: [.claude/commands/review-panel.md](commands/review-panel.md) and
[.claude/agents/](agents/). This page is a convenience summary — when they disagree, they win.*
