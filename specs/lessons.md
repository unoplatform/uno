# Review-panel lessons

Corrections the `/review-panel` reviewers should apply on future passes. Every reviewer
reads this file before returning findings (the "lessons loop"); the orchestrator appends
to it — only on the user's go-ahead — when the panel produces a false positive, misses
something, mis-rates a severity, or didn't know a repo convention.

Keep each lesson **generalizable**: a rule that applies to future changes, not a note about
one diff. No specific file paths from the change that triggered it.

## Entry format

```markdown
## YYYY-MM-DD — <short title>
- **Lens:** <architect | security | skeptic | quality | operability | contract | performance | jerome | all>
- **Lesson:** <what the panel got wrong, or the repo nuance it didn't know>
- **Apply:** <one-line rule for future reviews>
```

---

## 2026-06-24 — Generated DependencyProperty fields are expected public surface
- **Lens:** contract
- **Lesson:** `[GeneratedDependencyProperty]` emits `public static DependencyProperty …Property` fields and public CLR accessors by design; they mirror WinUI and are required by the property system.
- **Apply:** Don't flag generated `…Property` fields or their accessors as gratuitous public surface or a contract-minimality issue. See `.claude/rules/dependency-properties.md`.

## 2026-06-24 — A guard-only change is not a root-cause fix
- **Lens:** skeptic
- **Lesson:** Adding null/bounds guards while the broken lifecycle/ownership invariant still produces stale state is symptom-patching, not a fix.
- **Apply:** Per `.claude/rules/debugging-discipline.md`, require the mutation point to be corrected and labelled `root-cause fix`; treat guard-only changes as incomplete.

## 2026-06-24 — Don't strip Uno divergence markers
- **Lens:** quality
- **Lesson:** `// TODO Uno:` comments and `#if HAS_UNO` / `#if !__SKIA__` blocks deliberately mark intentional divergences from WinUI; they are not dead code or stale TODOs.
- **Apply:** Don't flag these markers as comment noise or unreachable code; flag only genuine drift.
