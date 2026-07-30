---
description: MANDATORY methodology for fixing crashes/rendering/selection/indexing bugs and reporting validation evidence in Uno source. Auto-loaded when editing src C# files.
paths:
  - "src/**/*.cs"
---

# Debugging & validation discipline (MANDATORY)

AGENTS.md carries the short version. This is the full protocol for bug fixes and PR reviews.

## Root-cause-first debugging
When fixing crashes, rendering issues, or selection/indexing bugs, follow this order:

1. **Reproduce first** — capture exact repro steps and expected vs actual. Keep one known-good repro path and rerun it after each meaningful change.
2. **Identify the broken invariant** — prefer state/lifecycle invariants over symptom-level checks. For pipelines that derive secondary state (maps, indices, caches, metadata), verify those derived structures are rebuilt from final post-mutation state.
3. **Fix root cause before adding guards** — do not lead with null/index guards if ownership/lifecycle is wrong. Defensive guards are allowed only *after* root-cause correction, as secondary hardening.
4. **Prove correctness with targeted tests** — add/extend tests that fail before and pass after the root fix; cover the triggering scenario and one adjacent regression scenario.
5. **Validate with runtime behavior, not compile-only** — run the closest runtime/integration path (the `/runtime-tests` skill for UI). If full runtime execution isn't possible here, say so explicitly and give the exact command(s) for maintainers.
6. **Communicate confidence accurately** — separate (a) code-review assessment, (b) compile validation, (c) runtime validation. Never present guard-only mitigation as a complete root-cause fix.

**Anti-pattern:** symptom-driven patching that accumulates bounds checks while stale/invalid intermediate state remains possible.

## Diagnosis bias checks (before proposing a crash fix)
1. **Invariant checkpoint** — name the invariant likely broken (ownership, lifecycle, index/map coherence, post-mutation consistency). If none identified, don't claim a root-cause fix.
2. **Mutation-point review** — inspect where state is created/trimmed/reordered and confirm all dependent structures refresh *there*. Prefer fixing the mutation point over guarding downstream consumers.
3. **Guard classification** — label each change `root-cause fix` or `defensive hardening`. Guard-only changes aren't a complete resolution.

## Validation evidence protocol
For bug fixes and PR reviews, report evidence with explicit labels:
- **Code review assessment** — what logic appears correct by inspection.
- **Compile validation** — which project/solution was built, and result.
- **Runtime validation** — which app/test path was executed, and result.

Rules: don't present compile-only checks as runtime validation; if runtime execution is skipped/blocked, state it and provide exact commands; when reviewing another PR, distinguish confidence from diff inspection vs. local execution.
