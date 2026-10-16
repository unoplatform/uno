# Appendix J: Workspace Transition Matrix

> **Parent**: [Main Spec](spec.md) — Section 3a
> **Related**: [Workspace Resolution Matrix](spec-appendix-i-workspace-resolution-matrix.md) | [Testing](spec-appendix-c-testing.md)

This appendix defines the **normative transition behavior** when the repo or workspace structure changes after the MCP bridge has already started.

It covers scenarios such as:
- creating a new Uno solution in an initially empty directory
- `git checkout`, `git switch`, `git pull`, or `git clean` changing the solution tree
- adding, removing, or replacing `global.json`
- moving from one valid Uno workspace to another

---

## 1. Normative Rules

The following rules define the required behavior for dynamic workspace changes:

1. The bridge must re-evaluate workspace resolution when the effective candidate set changes because of file-system or repo-structure mutations affecting:
   - `global.json`
   - `*.sln`
   - `*.slnx`
   - MCP roots
2. If the recomputed effective workspace is the **same** as the current one, the bridge must refresh diagnostics in place and **must not restart** the DevServer.
3. If the bridge moves from `NoCandidates` or `NoValidWorkspace` to a single valid Uno workspace, it must **start** a DevServer for that workspace automatically.
4. If the bridge moves from a valid workspace to `NoCandidates` or `NoValidWorkspace`, it must **stop** the current DevServer and surface immediate diagnostic health.
5. If the bridge moves from workspace **A** to a different valid workspace **B**, it must **stop A then start B**. It must not keep serving A silently.
6. If the recomputed result is `Ambiguous`, the bridge must **not** arbitrarily choose a workspace. It must surface diagnostics and must not switch silently.
7. If a repo mutation does not change the effective workspace identity, the session must remain stable even if many unrelated files changed.
8. Health must update immediately after each recomputation; no transition may wait for an upstream timeout before surfacing the new state.
9. An explicit `uno_app_select_solution` request (or `--solution` at startup) overrides auto-discovery for the current session and is allowed to trigger `Start` or `Restart` when the selected solution resolves to a valid Uno workspace. Later file changes re-resolve that solution rather than re-running discovery: an SDK change restarts it, and a solution that stops resolving degrades the session while staying selected until it resolves again.

---

## 2. Transition Matrix

| Before | Trigger | After | Expected behavior | Test expected | Current status |
|---|---|---|---|---|---|
| `NoCandidates` | Create Uno solution in scope | Single valid Uno workspace | Auto-resolve workspace and start DevServer | Lifecycle/proxy unit + integration | Covered |
| `NoValidWorkspace` | Add or fix Uno `global.json` | Single valid Uno workspace | Start DevServer automatically | Lifecycle/proxy unit + integration | Covered |
| `NoCandidates` | Git operation adds non-Uno solution only | `NoValidWorkspace` | Stay without DevServer; health updates immediately | Lifecycle/proxy unit + health/report unit | Covered |
| Single valid Uno workspace | Delete or rename `global.json` | `NoValidWorkspace` | Stop current DevServer; health becomes immediately unhealthy | Lifecycle/proxy unit + health/report unit | Covered |
| Single valid Uno workspace | Delete or rename solution file | `NoCandidates` | Stop current DevServer; health becomes immediately unhealthy | Lifecycle/proxy unit + health/report unit | Covered |
| Single valid Uno workspace A | Git operation changes repo so workspace B becomes the only valid Uno workspace | Single valid Uno workspace B | Stop A, start B, refresh health and cache identity | Lifecycle/proxy unit + integration | Covered |
| Single valid Uno workspace | Repo mutation changes files but effective workspace stays the same | Same workspace | No restart; refresh diagnostics only if needed | Lifecycle/proxy unit | Covered |
| Single valid Uno workspace | Repo mutation introduces a second equally valid Uno workspace | `Ambiguous` | Do not switch silently; move to diagnostic state | Lifecycle/proxy unit + health/report unit | Covered |
| `Ambiguous` | Repo mutation leaves one clear valid Uno workspace | Single valid Uno workspace | Start or resume on the single clear workspace | Lifecycle/proxy unit + integration | Covered |
| Single valid Uno workspace | MCP roots confirm the same workspace | Same workspace | No restart | Lifecycle/proxy unit | Covered |
| Single valid Uno workspace A, DevServer running | MCP roots point to a different workspace B (valid or not) | Workspace A | Ignore the roots: keep A running, not degraded; log it | Decisions unit + lifecycle/proxy unit | Covered |
| Single valid Uno workspace A, no DevServer yet (deferred roots startup) | MCP roots point to a different valid workspace B | Workspace B | Start DevServer on B | Decisions unit + lifecycle/proxy unit | Covered |
| Deferred or ambiguous session | Explicit `uno_app_select_solution` for current valid Uno workspace | Single valid Uno workspace | Start DevServer on selected workspace without restarting the MCP session | Lifecycle/proxy unit + MCP integration | Covered |
| Running workspace A | Explicit `uno_app_select_solution` for valid Uno workspace B | Single valid Uno workspace B | Stop A then start B; refresh health and cache identity | Lifecycle/proxy unit + MCP integration | Covered |
| Explicitly selected solution S | File change (`dotnet restore`, another solution added) | Same S | Re-resolve S, no rediscovery: refresh, or restart S when its SDK changed | Lifecycle/proxy unit | Covered |
| Explicitly selected solution S | S deleted, or its global.json loses the Uno SDK | S unresolved, still selected | Stop and degrade; watch its global.json folder; start S again once it resolves | Lifecycle/proxy unit | Covered |
| `NoCandidates` (force-roots-fallback) | `uno_app_initialize` called with empty or non-Uno directory | Workspace accepted, monitoring started | Accept directory as workspace root; start file watcher; health shows `effectiveWorkspaceDirectory` but no solution; watcher will auto-start DevServer when Uno solution appears | Lifecycle/proxy unit | Covered |

---

## 3. Session Policy

To avoid silent misrouting, the bridge must distinguish these actions after recomputation:

- **Refresh**: same effective workspace, keep current DevServer
- **Start**: no DevServer runs yet, and a valid Uno workspace is known (newly found, selected, or named by MCP roots before startup)
- **Stop**: current workspace lost, no valid replacement
- **Restart**: valid workspace changed from A to B, by an explicit selection while a DevServer runs or by a file-system change (a workspace identity covers its directory, solution, global.json and Uno SDK package and version, so an SDK change restarts the same solution)
- **Ignore**: MCP roots that would override an explicit selection, or replace or degrade a running workspace; the current workspace is kept and the event is logged
- **Diagnose**: the new state is ambiguous or does not resolve to an Uno workspace (for instance MCP roots before startup that resolve to nothing); no DevServer is started and health reports why

If the bridge cannot safely decide between these actions because the new state is ambiguous, it must choose the diagnostic path rather than an arbitrary switch.

---

## 4. Coverage Notes

This appendix defines the transition cases that must be added to the automated test audit after the initial-state matrix in [Appendix I](spec-appendix-i-workspace-resolution-matrix.md).

Status labels in this appendix mean:

- **Covered**: a focused automated test exercises the real lifecycle or proxy behavior.
- **Covered (policy)**: the transition is locked by a focused decision/policy test, but the file-system/repo mutation trigger is still simulated rather than observed end-to-end.
- **Gap**: the behavior is not yet locked by a focused automated test and may also require implementation work.
