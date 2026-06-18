# Spec 044 — Progress

> **Status**: Registry core implemented & green — 2026-06-17
> Scope of the current PR: the **Uno-side registry only**. Publishers (design-time modules)
> and consumers (MCP server) live in separate repos and are **not** part of this PR.

## Done (committed)

- [x] Spec + Publisher guide (Appendix A) + Consumer guide (Appendix B) — two review rounds folded in.
- [x] `src/Uno.UI.RemoteControl/Tools/` — registry implementation:
  - [x] In-memory types: `ToolDescriptor`, `ToolParameter` (+ `JsonSchema` escape hatch), `ToolParameterKind`, `ResourceDescriptor`, `ToolContent`, `ToolContentKind`, `ToolResult` (`Text`/`Error` factories), `ToolInvocation` (typed accessors → `IsError` on bad arg).
  - [x] Segregated interfaces: `IToolPublisher` / `IToolCatalog` / combined `IToolRegistry`; `IResourceRegistration`; delegates; `ResourceUpdatedEventArgs`; `IToolDispatcher` seam.
  - [x] `ToolRegistry` facade (`Publisher` / `Catalog` / `SetDispatcher` / `SetForTesting`).
  - [x] `ToolRegistryImpl`: lock-free `ImmutableInterlocked` store; coalescing + reentrancy-safe multicast `Changed` / `ResourceUpdated` with per-subscriber exception isolation + logging; duplicate-name no-op disposable (cannot evict the winner); in-flight-invoke completes on dispose; `NotifyUpdated()` post-dispose no-op; `InvokeAsync` deadlock guard (inline when `HasThreadAccess`); cancellation propagates (not `IsError`).
  - [x] `ToolArgumentValidator` — pure validation of `arguments` against `ToolParameter[]` (required/kind/`AllowedValues`; `JsonSchema` delegated to the consumer), invoked at the `InvokeAsync` boundary.
- [x] Review hardening (panel + bot reviews):
  - [x] `RaiseChanged` lost-update window closed (re-check pending after releasing the guard).
  - [x] `RaiseResourceUpdated` made reentrancy-safe via a per-uri queue drain (was synchronous, could `StackOverflow`).
  - [x] Handler/reader failures logged at `Warning`; generic error result to the consumer (no `ex.Message` leak).
  - [x] Invocation timeout (`InvocationTimeout`, default 30 s) via linked `CancelAfter`; **cooperative** (cancels the handler token), logged at `Warning` on fire, discriminated from caller cancellation via the linked token; timeout → error result, caller cancel (and a handler's own OCE) → throws.
  - [x] `AllowedValues` enforced for `String` kind only; `Integer` validation requires an integral Number (agrees with `GetInt32`).
  - [x] `ToolParameter.AllowedValues` normalized so a `default` value reads as empty (no NRE).
  - [x] `TryGetInt32`/`TryGetBoolean` added; `TryGetString` null-guarded; `value!` removed.
  - [x] `RestoreToken.Dispose` uses `Volatile.Write`; non-nullable `arguments` contract honored; `NoOpDisposable.Instance` unified.
- [x] `src/Uno.UI.RemoteControl.DevServer.Tests/Tools/ToolRegistryTests.cs` — 43 tests (added concurrency/reentrancy stress, validation, timeout, and param-model coverage; file `#nullable enable`).

## Validation

- **Compile**: `dotnet build Uno.UI.RemoteControl.Skia.csproj -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true` → **0 errors**. Test project builds → **0 errors**.
- **Runtime**: ran the MTP test assembly directly → **43 passed, 0 failed**.
  ```
  dotnet "src/Uno.UI.RemoteControl.DevServer.Tests/bin/Debug/net10.0/Uno.UI.RemoteControl.DevServer.Tests.dll" --filter "FullyQualifiedName~ToolRegistry"
  ```
  > Note: `dotnet test --project … -- --filter …` reports zero discovered tests on the .NET 10
  > SDK CLI here; running the built MTP assembly directly works. Unrelated to the change.
- **Pre-existing env issue**: restore fails with `NU1903` (MessagePack 3.1.4 audit promoted to
  error) across unrelated projects (Host, VS); worked around locally with `-p:NuGetAudit=false`.
  Not caused by this change.

## Deferred (not in this PR — see spec §7 Open Items)

- [ ] **Live dispatcher wiring** (Open Item 2, P1): wire a real `IToolDispatcher` over the app's
  window dispatcher via `ToolRegistry.SetDispatcher` in the host integration that owns the window
  (and can exercise it end-to-end). The registry runs **inline** until then — correct, no dead code.
- [ ] **`[InternalsVisibleTo]`** for the publisher / consumer assemblies — added when those
  integrations land.
- [ ] **Consumer** (MCP server) and **publishers** (design-time module) — separate repos.
