# Appendix I: Workspace Resolution Matrix

> **Parent**: [Main Spec](spec.md) — Section 3a
> **Related**: [Testing](spec-appendix-c-testing.md) | [Startup Workflow](spec-appendix-a-startup-workflow.md) | [Workspace Transition Matrix](spec-appendix-j-workspace-transition-matrix.md)

This appendix is the **normative support matrix** for initial workspace discovery in MCP mode and for CLI health diagnostics.

It answers three questions for each initial layout:
- is the layout supported
- what exact outcome is expected
- what automated test coverage is required

---

## 1. Normative Rules

The following rules are part of the supported behavior:

1. Auto-discovery scans for `.sln` / `.slnx` files recursively up to **3 directory levels** below the requested working directory.
2. DevServer auto-start is supported only for **Uno workspaces**. A solution is considered Uno only when a `global.json` on the solution directory path or one of its parents declares `Uno.Sdk` or `Uno.Sdk.Private` in `msbuild-sdks`.
3. When multiple solutions are discovered, **Uno solutions take priority** over non-Uno solutions.
4. For a given solution, the **closest `global.json`** on that solution's parent chain is authoritative. Discovery does not skip a nearer non-Uno `global.json` to use a farther Uno `global.json`.
5. If exactly one best Uno workspace is found, the bridge resolves it and starts DevServer for that workspace.
6. If multiple Uno workspaces are equally good candidates, the result is `Ambiguous`; DevServer must **not** start automatically.
7. If no solution is found, the result is `NoCandidates`; health is available immediately and reports the missing workspace without waiting for upstream timeouts.
8. If solutions are found but none resolve to a valid Uno workspace, the result is `NoValidWorkspace`; health is available immediately and DevServer does not start.
9. Late MCP roots that confirm the already selected workspace must **not** restart the session.
10. Late MCP roots that resolve to a different workspace must **not** switch the running session to a new workspace. The session remains diagnostic/degraded for that run.
11. `uno.devserver health --json` must return the same logical `HealthReport` shape as `uno_health`.

---

## 2. Support Matrix

| Case | Expected result | Test expected | Current code | Current dedicated coverage |
|---|---|---|---|---|
| Empty directory, no `.git`, no solution | `ResolutionKind=NoCandidates`; no host start; health immediately available with `HostNotStarted` + `NoSolutionFound` | Resolver unit + health/report unit | Yes | Gap |
| Git repo with no solution | Same as empty directory | Resolver unit + health/report unit | Yes | Gap |
| One non-Uno solution | `ResolutionKind=NoValidWorkspace`; no host start; immediate unhealthy health | Resolver unit + health/report unit | Yes | Covered |
| One Uno solution in requested directory | `ResolutionKind=CurrentDirectory`; DevServer starts from requested directory | Resolver unit | Yes | Gap |
| One nested Uno solution (`.slnx`) | `ResolutionKind=AutoDiscovered`; auto-descend to nested workspace | Resolver unit | Yes | Covered |
| One nested Uno solution (`.sln`) | Same as above | Resolver unit | Yes | Covered |
| Root solution non-Uno, deeper solution Uno | Uno solution wins; only Uno workspace starts | Resolver unit + health/report unit | Yes | Partial |
| Multiple solutions, only one Uno | Uno solution selected automatically | Resolver unit | Yes | Covered |
| Multiple Uno solutions, one clear best candidate | Best candidate selected automatically | Resolver unit | Yes | Gap |
| Multiple Uno solutions, equal priority | `ResolutionKind=Ambiguous`; no host start; diagnostic health | Resolver unit + health/report unit + lifecycle unit | Yes | Partial |
| Solution exists, but no `global.json` on its parent chain | `ResolutionKind=NoValidWorkspace`; fail fast; no upstream timeout | Resolver unit + health/report unit | Yes | Gap |
| `global.json` exists but does not declare Uno | Same as non-Uno solution | Resolver unit | Yes | Covered |
| `global.json` is malformed / invalid JSON | Treated as non-valid workspace; no host start | Resolver unit + health/report unit | Yes | Gap |
| Multiple `global.json` on one path, nearest is non-Uno, parent is Uno | Nearest `global.json` wins; solution is not treated as Uno | Resolver unit | Yes | Gap |
| Multiple `global.json` on one path, nearest is Uno | Nearest `global.json` wins; workspace resolves normally | Resolver unit | Yes | Gap |
| Solutions under `node_modules`, `bin`, `obj`, `.vs`, `.idea`, `packages` | Ignored during scan | Finder unit | Yes | Covered |
| Git repo with `.gitignore` excluding solution folders | Ignored solutions are not candidates | Finder unit | Yes | Covered |
| Fake/corrupt `.git` repo or `git` unavailable | Hardcoded fallback; no crash; scan still works | Finder unit | Yes | Covered |
| Solution deeper than 3 levels | Not supported for auto-discovery; result is `NoCandidates` | Resolver unit | Yes | Gap |
| MCP roots arrive later and confirm same workspace | No restart; session continues unchanged | Lifecycle/proxy unit | Yes | Gap |
| MCP roots arrive later and point to different workspace | No hot switch; session stays diagnostic/degraded | Lifecycle/proxy unit + health/report unit | Yes | Gap |
| CLI `health --json` | Same logical payload shape as `uno_health` | CLI integration + formatter/model unit | Yes | Partial |

---

## 3. Coverage Notes

Coverage labels in this appendix mean:

- **Covered**: there is already a focused automated test for the exact scenario.
- **Partial**: the behavior is exercised indirectly or only at one layer.
- **Gap**: the code appears to support the scenario, but there is no focused automated test yet.

This appendix is the source of truth for the follow-up audit and for new tests added under `Uno.UI.RemoteControl.DevServer.Tests`.

Changes that happen **after startup** are specified separately in [Appendix J](spec-appendix-j-workspace-transition-matrix.md).

