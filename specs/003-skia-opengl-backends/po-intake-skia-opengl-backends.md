## Status: Ready for spec

> Profile: Full product feature

## Requirement Brief

- Problem: The team needs a validated graphics-backend strategy for Uno Skia workloads as platform capabilities evolve beyond OpenGL (Vulkan, Metal, WebGPU), but current information is fragmented across meeting notes, external examples, and internal code knowledge.
- Business outcome: Reduce migration risk and avoid premature or misaligned backend investments by producing a clear, evidence-based decision package for backend direction.
- Primary user: Uno Platform graphics/runtime maintainers and contributors planning backend evolution.
- Priority: P1
- Target release window: Not required for this intake

---

# Spec Input Pack

## Executive Summary

Uno Platform currently relies on Skia-based rendering paths that are primarily OpenGL- or Metal-driven depending on platform, with software fallback paths in multiple hosts. The team wants an intake that formalizes discovery and planning work for future backend decisions, especially Vulkan exploration for Android/Desktop and Graphite readiness tracking, while also clarifying where WebGPU fits.

This intake packages meeting outcomes, external ecosystem signals (Avalonia Vulkan work), and verified Uno implementation baseline so that follow-up engineering specs can be written from a single source of truth. The objective is to produce a decision-ready brief with explicit acceptance criteria, risks, and unresolved questions.

## Feature Summary

- Name: Skia OpenGL Backends Intake and Migration Discovery
- Priority: P1
- Requested by: Graphics/backend team discussion (Ramez, Martin, Francois, Jenny)
- Approved by: TBD
- Issue: https://github.com/unoplatform/uno-private/issues/1822
- Problem: Backend direction decisions are currently spread across meeting notes and ad-hoc research, with no consolidated intake that defines scope, success conditions, and ownership.
- Goal: Create a formal handoff input for subsequent implementation specs covering backend research, platform feasibility, and migration decision points.
- Non-goals (v1 intake):
  - Shipping a Vulkan renderer in Uno.
  - Shipping Graphite in production.
  - Designing a full cross-backend 3D abstraction API.
- Future considerations:
  - Backend-specific 3D controls beyond OpenGL.
  - Graphite production rollout criteria.
  - Potential refactors to centralize backend-specific integrations when justified.

## Users and Scenarios

- Primary persona: Runtime/graphics maintainers responsible for cross-platform rendering decisions.
- Secondary personas: Product leadership planning roadmap sequencing; contributors implementing backend experiments.
- Key scenarios:
  - Maintainer needs a single document describing current backend support and known platform constraints.
  - Team needs to decide whether Vulkan exploration should proceed to prototype phase.
  - Team needs to evaluate whether Graphite work should be blocked, parallelized, or deferred.

## Requirements

- REQ-001: The intake must document current backend usage expectations by platform (WASM, Linux, Windows, macOS, iOS, Android) in plain language.
- REQ-002: The intake must define discovery scope for Vulkan feasibility and performance validation, with explicit focus on Android and desktop.
- REQ-003: The intake must capture Graphite-related unknowns and readiness criteria for Uno adoption planning.
- REQ-004: The intake must clarify whether backend APIs (OpenGL, Vulkan, Metal, WebGPU) can share one 3D control abstraction or require separate backend-specific surfaces.
- REQ-005: The intake must list concrete pre-spec action items with owners and expected output artifacts.

## Acceptance Criteria

- AC-001 (REQ-001): Given the intake document, when a maintainer reviews platform sections, then they can identify current backend behavior and fallback expectations for each target without consulting source code.
- AC-002 (REQ-002): Given the discovery scope, when planning starts, then owners can execute a defined Vulkan feasibility track (market scan + local benchmark plan + reporting template).
- AC-003 (REQ-003): Given the Graphite section, when roadmap planning occurs, then decision-makers can see explicit stability gaps, integration blockers, and defer/advance criteria.
- AC-004 (REQ-004): Given 3D-control requirements, when design discussion occurs, then it is clear whether a unified abstraction is required now or backend-specific controls are acceptable for v1.
- AC-005 (REQ-005): Given the pre-spec action list, when tasks are assigned, then each task has an owner, a deliverable, and a review checkpoint.

## Constraints

- Technical constraints:
  - Discovery must account for differing platform graphics APIs and runtime models.
  - Any proposed migration must preserve cross-platform behavior expectations for existing Skia targets.
  - The intake must separate research findings from implementation commitments.
- Security/compliance constraints:
  - No additional end-user data exposure is introduced by this intake.
- Privacy/data:
  - Discovery may require telemetry/benchmark traces; handling must follow existing project privacy standards.
- Performance expectations:
  - Discovery outputs must include explicit measurement criteria (frame time, startup/render latency, stability) rather than qualitative claims.
- UX/design constraints:
  - Not applicable for intake; applies in follow-up specs.

## Current Baseline (Verified in Uno Repository)

- Win32 Skia host attempts OpenGL (WGL) first and falls back to software when unavailable.
- X11 Skia host supports OpenGL via GLX and OpenGL ES via EGL with configurable preference and software fallback.
- Android Skia host uses GLSurfaceView/OpenGL path, with a configuration switch to avoid direct OpenGL-backed drawing usage for rendering.
- WebAssembly Skia browser host attempts WebGL first (WebGL2 with fallback to WebGL1) and supports forced software rendering.
- macOS Skia host defaults to Metal (with software fallback behavior when Metal is not available or explicitly disabled).
- No explicit Vulkan/WebGPU/Graphite implementation was found in the current Uno `src` code baseline during this intake pass.

## Android Performance Note: Vulkan vs OpenGL ES

- Short answer: Vulkan is generally more performant than OpenGL ES on Android when implemented well.
- Key reason: Vulkan typically reduces CPU overhead by removing hidden driver work and enabling explicit GPU submission control.
- Key reason: Vulkan supports stronger multi-threaded command recording/submission models, while OpenGL ES usage is often effectively single-threaded.
- Key reason: Vulkan can produce more stable frame times due to explicit memory, synchronization, and pipeline control.
- Important caveat: Vulkan complexity is significantly higher; poor Vulkan implementations can underperform well-tuned OpenGL ES.
- Important caveat: Driver maturity still varies by device class; some older or low-end Android devices may have stronger OpenGL ES driver behavior.
- Important caveat: Simple, non-CPU-bound apps (UI/basic 2D/light 3D) may see little to no practical gain from Vulkan.

| Scenario | Practical winner |
| --- | --- |
| Simple UI / 2D apps | OpenGL ES (simpler, often similar performance) |
| Mid-level 3D apps | Device and implementation dependent |
| High-end games / engines | Vulkan |
| CPU-bound rendering | Vulkan |
| Fast iteration / low implementation complexity | OpenGL ES |

- Android context: Vulkan support is strong on modern Android (API 24+), and major engines commonly default to Vulkan on supported devices.
- Intake implication: Vulkan exploration should prioritize CPU-bound and complex-scene workloads on Android, with device-tier benchmarking to validate net gains versus OpenGL ES.

## Required Execution Order

Work must be executed in this sequence, with each phase producing a written summary before proceeding:

1. Vulkan support research and feasibility for Android first, using Avalonia source signals and AI-assisted research.
2. Vulkan support research and feasibility for Desktop second, using the same evidence model.
3. Performance assessment of Vulkan versus OpenGL/GLES on Android and Desktop using agreed benchmark scenarios.
4. Skia and SkiaSharp update planning to latest compatible versions, then Graphite enablement investigation.
5. Performance assessment of Graphite versus Ganesh using matched rendering scenarios.
6. WebGPU support evaluation as the final exploration phase.

Phase-gate rule: no phase advances to the next step without a short decision note (go/hold/defer) and benchmark evidence where applicable.

## Risks and Unknowns

- Risk: Vulkan benefits may be marginal if UI rendering is not the primary bottleneck.
- Risk: Graphite maturity may be insufficient for production timelines.
- Risk: Backend-specific API differences may prevent a practical unified 3D-control abstraction.
- Risk: WebGPU support maturity and default browser enablement may delay reliable usage.

## Pre-Spec Action Items

- [ ] Phase 1 (Android Vulkan): Investigate Vulkan support and integration feasibility on Android, grounded by Avalonia source and AI-assisted research. Owner: Ramez.
- [ ] Phase 2 (Desktop Vulkan): Investigate Vulkan support and integration feasibility on Desktop after Android findings are documented. Owner: Ramez.
- [ ] Phase 3 (Vulkan vs OGL/GLES performance): Run comparative benchmarks for Vulkan versus OpenGL/GLES on prioritized Android/Desktop scenarios and summarize wins/tradeoffs. Owner: Ramez.
- [ ] Phase 4 (Skia/SkiaSharp + Graphite path): Assess upgrade path to latest compatible Skia/SkiaSharp and define Graphite enablement tasks/blockers. Owner: Jenny.
- [ ] Phase 5 (Graphite vs Ganesh performance): Benchmark Graphite versus Ganesh on matched workloads with frame-time, CPU, and stability metrics. Owner: Jenny.
- [ ] Phase 6 (WebGPU evaluation): Document WebGPU support status, feasibility, and strategic fit after Vulkan and Graphite phases complete. Owner: Ramez.
- [ ] Cross-phase API surface study: Analyze Silk.NET API differences/similarities across OpenGL, Vulkan, Metal, and WebGPU to determine unified versus separate canvas strategy. Owner: Ramez.
- [ ] Upstream contribution track: Prepare SkiaSharp Graphite API contribution plan and coordination points. Owner: Ramez.
- [ ] Research accelerator usage: Use Copilot research assistant for initial evidence gathering and maintain a phase-by-phase findings log. Owner: Ramez.

## References

- Research baseline (initial Copilot-assisted survey of Skia backends, Vulkan/Metal/WebGPU ecosystem signals, and relevant docs used to seed this intake): https://mattt98.sg-host.com/
- Avalonia Vulkan directory (reference implementation signal): https://github.com/AvaloniaUI/Avalonia/tree/658afb87173081b9444d30dd70ae47a04e519e4e/src/Avalonia.Vulkan
- Avalonia discussion (custom Vulkan integration considerations): https://github.com/AvaloniaUI/Avalonia/discussions/7137
- Avalonia Vulkan PR (merged backend work): https://github.com/AvaloniaUI/Avalonia/pull/12737
- Uno docs search anchor (Skia rendering): https://platform.uno/docs/articles/features/using-skia-rendering.html#the-skia-renderer
- Uno docs search anchor (GLCanvasElement): https://platform.uno/docs/articles/controls/GLCanvasElement.html#glcanvaselement
- Uno docs search anchor (macOS Metal rendering): https://platform.uno/docs/articles/features/using-skia-macos.html#metal-hardware-accelerated-rendering
- Meeting notes: Internal backend strategy discussion (Ramez, Martin, Francois, Jenny).

## Open Questions

- None currently.
