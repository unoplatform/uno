---
description: Run the eight reviewer subagents — architect, security, skeptic, quality, operability, contract, performance, jerome — in parallel against a scope (defaults to uncommitted changes; falls back to current branch vs master).
argument-hint: [scope — e.g. HEAD~1, master..HEAD, a PR number, or free-form; omit for auto]
allowed-tools: Agent, Read, Edit, Write, Bash(git status:*), Bash(git diff:*), Bash(git log:*), Bash(git rev-parse:*), Bash(git merge-base:*), Bash(git branch:*), Bash(git ls-files:*), Bash(gh pr view:*), Bash(gh pr diff:*)
---

You are orchestrating an eight-agent review panel on this repo (the Uno Platform framework). Do not do the review yourself — your job is to pin down the scope, dispatch all eight reviewer subagents in parallel, synthesize their findings into one consolidated report, then capture any lesson the panel learns.

## 1. Resolve scope

Argument from the user: `$ARGUMENTS`

- If the argument is non-empty, use it as the scope verbatim. If it looks like a PR reference (`#123`, `PR 123`, a github.com PR URL), resolve the diff via `gh pr diff` / `gh pr view`. Otherwise treat it as a git revision range (e.g. `HEAD~1`, `master..HEAD`) or a free-form description.
- If the argument is empty, auto-resolve in this order:
  1. Run `git status --porcelain`. If there are any staged, unstaged, or untracked changes, the scope is **"uncommitted changes (working tree + index + untracked)"**.
  2. Otherwise run `git rev-parse --abbrev-ref HEAD`. If the current branch is not the trunk (`master` or `main`), the scope is **"current branch vs trunk"**. This repo's trunk is **`master`**; resolve the base with `git merge-base --fork-point master HEAD` and capture the output as `<base>`; the scope range is then the explicit `<base>..HEAD`. If `--fork-point` exits with empty stdout (typical after a rebase or across divergent histories), fall back to the literal range `master..HEAD`. `git merge-base` returns a single commit SHA, not a range — never pass it alone to `git diff` / `git log` (that would include unrelated ancestor history); always compose the two-dot range form before diffing.
  3. Otherwise stop with a one-line message: "Nothing to review — clean working tree on trunk. Pass a scope argument."

## 2. Gather scope detail

Collect — but do not analyze — the concrete change surface so each reviewer sees the same brief:

- File list with stat summary (`git diff --stat <range>` or `git status --porcelain` + untracked)
- Commit list if a range is in play (`git log --oneline <range>`)
- Full commit messages if a range is in play (`git log --format="--- commit %H%n%s%n%n%b" <range>`) — pass these verbatim to `quality`, `contract`, and `jerome` so they can evaluate requirement alignment, intentional vs accidental contract changes, and whether the change actually matches its stated intent
- Branch name and base

Keep this under ~30 lines for the file list; truncate with a note if the diff is huge. Do not paste the full diff into the panel prompt — each reviewer has `Read` / `Grep` / `Glob` and will open files itself.

## 3. Dispatch reviewers in parallel

Send **a single message with eight `Agent` tool calls** — one each to `architect`, `security`, `skeptic`, `quality`, `operability`, `contract`, `performance`, `jerome` — running concurrently. Each prompt must be self-contained (the subagents see none of this conversation) and must include:

- A one-paragraph scope statement (what's being reviewed, what commits/files, what branch).
- The file list from step 2, verbatim.
- The commit messages and problem statement verbatim — all eight agents benefit from understanding intent: `quality` for requirement alignment, `contract` to distinguish intentional vs accidental API changes, `operability` and `performance` to know which targets are in scope, `skeptic` to verify the implementation matches the stated goal, and `jerome` to interrogate the intent itself and surface the assumptions left unstated.
- What has already been verified (usually nothing — say so). If the diff touches `src/**/*.cs`, remind the panel that validation should be runtime, not compile-only (`.claude/rules/debugging-discipline.md`).
- The role-appropriate questions to focus on. Do not paraphrase the agent's own instructions back at it; the subagent's system prompt already defines its lens.
- An explicit word budget (suggest 400 words each) so the synthesis stays manageable.

Do not omit any of the eight reviewers. The panel is only meaningful when all eight lenses are applied.

## 4. Synthesize

Once all eight return, produce one consolidated report:

- Group by severity: **blocker → high → medium → low → info** (the shared scale across agents). Where a finding appears in multiple reviews, merge it and annotate which reviewers flagged it.
- Resolve `## Hand-off` pointers: if one agent hands off to another and that other agent independently raised the same concern, merge; if it didn't, surface the hand-off as its own finding attributed to the originating agent.
- For each finding keep: severity, category, `file:line`, one-line "what", one-line "why it matters", suggested direction (if any).
- **`jerome` contributes open questions, not fixes.** Its rows are `severity | file:line | the question | why it matters | what would resolve it`. Keep the question and its "what would resolve it" in place of a suggested fix, and surface jerome's items under an **"Open questions to resolve before merge"** subsection within each severity band (or inline, marked `[question]`) so they aren't mistaken for actionable patches.
- End with a unified verdict — take the worst-case across the eight agent verdicts and map:
  - → **block-merge**: `reject` (skeptic), `needs-redesign` (architect), `block-merge` (security), `needs-rework` (quality / operability / contract / performance), `reconsider-approach` (jerome)
  - → **fix-first**: `fix-first` (skeptic), `approve-with-changes` (architect / quality / operability / contract / performance), `fix-before-merge` (security), `resolve-questions-first` (jerome)
  - → **ship**: `ship` (skeptic), `no-findings` (security), `approve` (architect / quality / operability / contract / performance), `proceed` (jerome)
  - Worst-case wins across all eight.
- If the diff is trivial and all eight reviewers returned a one-line ack, the whole report is a one-line ack — do not pad.

## 5. Offer next steps

After the report, in one line, offer to apply the fixes or stage/commit when the user is ready. Do not apply fixes automatically.

## 6. Capture lessons (only on the user's go-ahead)

The panel improves by remembering its own misses. Each reviewer already reads `specs/lessons.md` before reviewing; this step is how entries get *written*. After you present the report and the user responds, watch for a signal that the panel got something wrong or didn't know a repo nuance:

- a finding the user rejects as a false positive,
- a real issue the user points out that no lens caught,
- a severity the user corrects,
- a repo-specific convention a lens didn't know (e.g. "generated `…Property` fields are expected public surface, not gratuitous API").

When that happens, ask once: *"Want me to record this as a lesson so future panels apply it?"* On a yes, append a dated entry to `specs/lessons.md` (create it from the template header if it doesn't exist) using the entry format already in that file: a `## YYYY-MM-DD — <title>` heading with `**Lens:**`, `**Lesson:**`, and `**Apply:**` lines. Record the *generalizable* rule, not transient details from this one diff (no specific file paths). Never append without the user's explicit go-ahead, and never record more than the correction warrants.
