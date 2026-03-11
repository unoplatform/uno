# Appendix E: Reference Tables & Analysis

> **Parent**: [Main Spec](spec.md)

---

## E.1 MCP Tools (reference from uno.app-mcp)

> **Note**: The tool count visible to the AI model depends on the user's license tier. `MCPToolsObserverService` filters tools via `[LicenseFeatures]` attributes. The tool cache (`tools-cache.json`) reflects the license tier active at cache time.
>
> **Known discrepancy**: The public docs (`doc/articles/features/using-the-uno-mcps.md`) do not list all tools (e.g., `uno_app_start` is missing, Business tier not documented). The table below reflects the **actual server code** (`uno.app-mcp`), not the docs. See also `uno.app-mcp/README.md` alongside this spec for upstream action items.

| Tool Name | Description | Min License |
|-----------|-------------|:----------:|
| `uno_app_start` | Start app with Hot Reload | Community |
| `uno_app_close` | Graceful or forced termination | Community |
| `uno_app_get_runtime_info` | PID, window title, uptime | Community |
| `uno_app_get_screenshot` | Take PNG/JPEG screenshot | Community |
| `uno_app_pointer_click` | Pointer interaction | Community |
| `uno_app_key_press` | Keyboard input | Community |
| `uno_app_type_text` | Text input | Community |
| `uno_app_visualtree_snapshot` | XML visual tree dump | Community |
| `uno_app_element_peer_default_action` | Default automation action | Community |
| `uno_app_element_peer_action` | Advanced automation action | Pro |
| `uno_app_get_element_datacontext` | Element data context | Pro |

**Visible tools by tier**: Community 9, Pro 11 (verified against `MCPToolsObserverService`). Counts may change as tools are added.

---

## E.2 Related Specifications

- [`discovery-roadmap.md`](spec-appendix-f-discovery-roadmap.md) — Broader discovery and startup redesign roadmap (host manifest, add-in manifest, CLI bootstrap, port ownership).
- This spec covers both the general add-in discovery optimization (all modes) and MCP-specific fast-startup improvements. It implements Phases 3-4 of the discovery roadmap using `.targets` parsing as the first step, with `devserver-addin.json` manifest as the target.

---

## E.3 Architectural Convergence with Discovery Roadmap

[`discovery-roadmap.md`](spec-appendix-f-discovery-roadmap.md) defines a broader roadmap for manifest-based discovery. This spec is an intermediate step on that trajectory:

```
Current state          This spec (Phase 0-1)           Discovery Roadmap target
────────────────       ─────────────────────           ──────────────────────────────
MSBuild targets        .targets XML parsing            devserver-addin.json manifest
dotnet build (10-30s)  Direct XML read (<200ms)        Direct JSON read (<10ms)
UnoRemoteControlAddIns UnoRemoteControlAddIns          Manifest-declared entry points
Per-IDE discovery      CLI-centralized                 CLI-centralized
```

**The `.targets` parsing approach is explicitly transitional:**
1. It works today with zero changes to existing add-in packages
2. It proves the fast-path concept and enables immediate startup gains
3. It will be superseded by `devserver-addin.json` manifests when packages are updated
4. The MSBuild fallback will be deprecated when all maintained SDKs support either `.targets` parsing or manifests

**Not a competing direction** — this spec implements Phase 3 ("Host manifest") and Phase 4 ("Add-in manifest") of the discovery roadmap using `.targets` parsing as the first implementation, with `devserver-addin.json` manifest as the target.

---

## E.4 IDE Extension Behavior

Moved to [`.github/agents/devserver-agent.md` § 10](../../.github/agents/devserver-agent.md) (IDE Compatibility Constraints).
