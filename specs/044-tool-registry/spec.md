# Spec 044: Tool & Resource Registry

> **Status**: Draft - 2026-06-17
> **Author**: Carl de Billy
> 2026-06-17 - Draft

> **Audience & boundary**: this spec covers **only the registry** added to this repository
> — an in-process store where modules declare tools and resources. It carries **no
> messaging and no MCP knowledge**. Everything about transporting the catalogue to a
> devserver and exposing it as MCP lives in separate codebases.
> Two companion guides, one per role:
> **Appendix A — Publisher Guide** (`spec-appendix-a-publisher-guide.md`) and
> **Appendix B — Consumer Guide** (`spec-appendix-b-consumer-guide.md`).
> Third-party technologies are named generically; no specific internal project is named.

## Terminology (defined relative to the registry)

| Term | Definition | Interface | Example |
|---|---|---|---|
| **Publisher** | Registers (publishes) tools/resources **into** the registry. | `IToolPublisher` (`ToolRegistry.Publisher`) | a design-time tooling module |
| **Consumer** | **Reads** the registry and exposes it to agents (over MCP), owning all messaging. | `IToolCatalog` (`ToolRegistry.Catalog`) | an MCP server |

> ⚠️ **Disambiguation**: a Consumer does "provide" tools to an agent — but **relative to the
> registry it is a reader**, hence *consumer*. Both terms are always defined relative to the
> registry: a Publisher writes to it, a Consumer reads from it.
>
> The registry is a **many-to-many hub**: there can be **several Publishers** (each
> contributing its own tools/resources) and **several Consumers** (different ways of calling
> those tools — an MCP server, another bridge, a test harness…). A Consumer **may also** be a
> Publisher if it chooses to publish its own tools (its decision — §5).

---

## Executive Summary

### The Problem

Uno tooling exposes capabilities to AI agents through an MCP server. Today that surface is
**closed**: the set of tools is discovered by reflecting over a **fixed, compile-time
list**, and app-side operations are carried by **per-operation message types** correlated
by a sequence id. No component above the Remote Control client can contribute a tool that
reaches an agent. A design-time tooling module (and, later, other modules — and possibly
applications themselves, as a bonus) needs to publish its own tools dynamically.

### What We're Shipping

A single, in-process **Tool & Resource Registry** in this repository. It is intentionally
**just a registry**:

- **Publishers** declare *tools* and *resources* into it. The vocabulary mirrors
  tools/resources but has **no reference to MCP** — no notion of an agent or of the
  protocol.
- A **Consumer** (an MCP server, in a separate codebase) reads the registry **in-process**
  and is responsible for **all** the rest: the messaging to the devserver and the mapping
  onto MCP. The consumer already runs inside the app process and already owns that
  pipeline; the registry simply gives it an in-memory source of truth to read. Several
  consumers can read the same registry concurrently.

```csharp
// A publisher declares a tool — internal API. Tool names are namespaced by the owning module.
var registration = ToolRegistry.Publisher.RegisterTool(
    new ToolDescriptor(
        Name: "mymodule_select_element",
        Description: "Selects the element identified by the given handle.",
        Parameters:
        [
            new ToolParameter("handle", "Opaque element handle.", ToolParameterKind.String, IsRequired: true),
        ],
        IsReadOnly: false),
    handler: static async (invocation, ct) =>
    {
        var handle = invocation.GetString("handle");
        // … act on the visual tree (runs on the UI thread by default) …
        return ToolResult.Text($"Selected {handle}.");
    });

// Later, when the feature deactivates:
registration.Dispose(); // removes the tool and raises Changed
```

### Why This Approach

- **The registry has no transport.** It depends on no messaging, no `Frame`, no
  `RemoteControlClient`. This deliberately keeps the entire MCP pipeline — including the
  devserver messaging — in the consumer's codebase, exactly as it is today.
- **In-process, single source of truth.** Both publishers and consumers run in the app
  process; a consumer reads the registry directly via the same shared-ALC singleton
  mechanism `RemoteControlClient.Instance` already relies on. No new wire protocol.
- **Debug-only by packaging.** The registry lives in `Uno.UI.RemoteControl` (the
  `Uno.WinUI.DevServer` package), which is a debug-only reference — so it is absent in
  Release, matching "same logic as the Remote Control client".
- **MCP-free vocabulary.** Publishers depend only on `ToolDescriptor`, `ToolParameter`,
  `ResourceDescriptor`, `ToolResult`. The fact that a consumer is an MCP server is
  invisible to them.
- **Dynamic.** Registrations come and go; each returns an `IDisposable`, and add/remove
  raises `Changed` so consumers can re-publish (list-changed semantics on their side).

### Scope

- **In Uno (this spec)**: `ToolRegistry` and its in-memory types in `Uno.UI.RemoteControl`,
  `internal` + `[InternalsVisibleTo]` to publisher assemblies and consumer in-app
  components. Tools and resources both, including a resource-update signal.
- **Publisher guide**: Appendix A (`spec-appendix-a-publisher-guide.md`).
- **Consumer guide**: Appendix B (`spec-appendix-b-consumer-guide.md`) — the normative MCP
  obligations for any consumer; consumer *code* is out of scope (separate codebase), as is
  **all** messaging and MCP mapping.
- **Not in scope**: any wire protocol / processor / message types in Uno; a public publisher
  API (the "applications publish tools" bonus); persistence across sessions; agent
  authentication/authorization.

### Relationship with Spec 042 (Element Ref Handle Registry)

Complementary: a tool operating on a visual-tree element takes an opaque element handle
(`ElementRefHandle`, spec 042) as a `ToolParameter` and resolves it inside its handler. 042
addresses *elements*; this spec addresses *capabilities*.

---

## 1. Architecture

```
 Publishers (modules)                          [Uno.UI.RemoteControl, internal + IVT]
        │  ToolRegistry.Publisher.RegisterTool(descriptor, handler) -> IDisposable
        ▼
 ToolRegistry  (singleton, in-process, lock-free — NO transport dependency)
        │  Publisher (IToolPublisher)  |  Catalog (IToolCatalog: Snapshot/Invoke/Read + events)
        ▼
 Consumer(s) — in-app component(s)             [separate codebase — see Appendix B]
   - read the registry in-process (shared-ALC singleton)
   - OWN all messaging to the devserver + the MCP mapping
        │  (each consumer's existing pipeline)
        ▼
 devserver add-in -> HTTP MCP server -> agent
```

The registry sits in `Uno.UI.RemoteControl` purely for the debug-only packaging and
proximity ("available wherever the Remote Control client is"); it does **not** use the
client's transport.

---

## 2. Registry Types

`src/Uno.UI.RemoteControl/Tools/`, namespace `Uno.UI.RemoteControl.Tools`. In-memory
contracts (not wire DTOs). Immutable records; `ImmutableArray<T>` for collections.

```csharp
public sealed record ToolDescriptor(
    string Name,
    string Description,
    ImmutableArray<ToolParameter> Parameters,
    bool IsReadOnly);

public sealed record ToolParameter(
    string Name,
    string Description,
    ToolParameterKind Kind,
    bool IsRequired = false,
    string? DefaultValue = null,
    ImmutableArray<string> AllowedValues = default); // enum constraint, optional

public enum ToolParameterKind { String, Integer, Number, Boolean, Array, Object }

public sealed record ResourceDescriptor(
    string Uri,
    string Name,
    string Description,
    string? MimeType);

public sealed record ToolContent(
    ToolContentKind Kind,
    string? Text = null,
    string? MimeType = null,
    string? Base64Data = null);

public enum ToolContentKind { Text, Json, Image, Blob }

public sealed record ToolResult(
    ImmutableArray<ToolContent> Content,
    bool IsError = false)
{
    public static ToolResult Text(string text) => /* single Text content */;
}
```

> **Visibility**: these types are `internal` (per the v1 decision) and reachable by
> publishers and consumers via `[InternalsVisibleTo]`. They are **not** serialized by Uno —
> each consumer maps them onto its own message payloads. The **typed parameter model** lets a
> consumer generate a JSON Schema (Appendix B) without Uno referencing any JSON-Schema or MCP
> type.

---

## 3. `ToolRegistry`

`src/Uno.UI.RemoteControl/Tools/`. A **process-wide singleton** exposing **two segregated
interfaces** and **no transport dependency**:

- **`IToolPublisher`** — the *publication* face, used by **publishers**.
- **`IToolCatalog`** — the *consumption* face, used by **consumers**.

A static facade `ToolRegistry` exposes each face; the singleton implements both. Splitting
the faces means each side depends only on what it needs, both are mockable in tests, and the
publication face can later be promoted to public (the "apps publish tools" bonus) without
exposing the consumption face.

```csharp
namespace Uno.UI.RemoteControl.Tools;

/// <summary>Static accessor to the process-wide registry singleton.</summary>
internal static class ToolRegistry
{
    /// <summary>Publication face — used by publishers.</summary>
    public static IToolPublisher Publisher => _instance;

    /// <summary>Consumption face — used by consumers (read via IVT).</summary>
    public static IToolCatalog Catalog => _instance;

    private static IToolRegistry _instance = new ToolRegistryImpl();

    /// <summary>Swaps the backing registry for tests; dispose the result to restore.</summary>
    internal static IDisposable SetForTesting(IToolRegistry instance);
}

/// <summary>Combined contract; the singleton implements both faces.</summary>
internal interface IToolRegistry : IToolPublisher, IToolCatalog;

internal interface IToolPublisher
{
    /// <summary>Declares a tool. Dispose the result to remove it.</summary>
    IDisposable RegisterTool(ToolDescriptor descriptor, ToolHandler handler, bool runOnUIThread = true);

    /// <summary>Declares a resource. Use the result to read or signal updates.</summary>
    IResourceRegistration RegisterResource(ResourceDescriptor descriptor, ResourceReader reader);
}

internal interface IToolCatalog
{
    (ImmutableArray<ToolDescriptor> Tools, ImmutableArray<ResourceDescriptor> Resources) Snapshot();

    /// <summary>Invokes a tool by name; marshals to the UI thread when the registration requested it.</summary>
    ValueTask<ToolResult> InvokeAsync(string toolName, JsonObject arguments, CancellationToken ct);

    ValueTask<ToolResult> ReadResourceAsync(string uri, CancellationToken ct);

    /// <summary>Raised when the set of tools or resources changes (drives re-publication by consumers).</summary>
    event EventHandler? Changed;

    /// <summary>Raised when a registered resource signals an update (each consumer maps it to its subscribers).</summary>
    event EventHandler<ResourceUpdatedEventArgs>? ResourceUpdated;
}

internal delegate ValueTask<ToolResult> ToolHandler(ToolInvocation invocation, CancellationToken ct);
internal delegate ValueTask<ToolResult> ResourceReader(CancellationToken ct);

internal interface IResourceRegistration : IDisposable
{
    /// <summary>Signals that the resource content changed (raises <see cref="IToolCatalog.ResourceUpdated"/>).</summary>
    void NotifyUpdated();
}

internal sealed class ResourceUpdatedEventArgs(string uri) : EventArgs
{
    public string Uri { get; } = uri;
}

/// <summary>Parsed arguments passed to a tool handler.</summary>
internal sealed class ToolInvocation
{
    public JsonObject Arguments { get; }
    public string GetString(string name);
    public int GetInt32(string name);
    public bool GetBoolean(string name);
    // … typed accessors mirroring ToolParameterKind …
}
```

### 3.1 UI-thread marshalling

`InvokeAsync` / `ReadResourceAsync` carry the marshalling: the registry knows each
registration's `runOnUIThread` and dispatches the handler onto the UI thread (the same
dispatcher the app uses) by default. A handler that declares `runOnUIThread: false` runs on
the caller's thread. Consumers therefore never deal with threading — they just await.

### 3.2 Concurrency

Lock-free, per repository preference: registrations are held in an
`ImmutableDictionary<string, …>` mutated via `ImmutableInterlocked`. No `lock`. Events use
`EventHandler`/`EventHandler<T>` (never `event Action`, per repo convention).

### 3.3 Name uniqueness

`Name` is the key and must be unique across all publishers. A duplicate registration is
rejected and logged. Publishers namespace by module (a `<module>_` prefix) — documented in
Appendix A.

### 3.4 Inert without a consumer

`Publisher.RegisterTool`/`RegisterResource` only populate the store. If no consumer reads it
(Release, no devserver, or no consumer present), nothing is published and nothing throws. The
registry is safe to call unconditionally from a module.

### 3.5 Many publishers, many consumers (single instance)

The registry is a **many-to-many hub** backed by **one** singleton:

- The singleton lives in `Uno.UI.RemoteControl`, whose statics load once in the shared ALC,
  so `ToolRegistry.Publisher` and `ToolRegistry.Catalog` resolve to the **same** instance for
  every publisher and every consumer.
- **Several publishers** contribute concurrently — names are unique, and each owns and
  disposes its own registrations. `Snapshot()` aggregates them all.
- **Several consumers** read concurrently. The `IToolCatalog` face is **purely
  observational**: `Snapshot`/`InvokeAsync`/`ReadResourceAsync` are reentrant, and `Changed`
  / `ResourceUpdated` are **multicast** events — every consumer is notified and filters
  against **its own** subscriptions. The registry does not know its consumers (anonymous
  observers) and holds **no per-connection state**.
- This also covers multiple `RemoteControlClient` instances (`Instance` + each
  `CreateAdditional(...)`, each on a **distinct devserver**): the per-devserver fan-out and
  the tracking of which agent subscribed to which URI live in the consumer(s), not the
  registry.
- Because tools may be invoked concurrently from several consumers, **handlers must be
  stateless across calls** (re-entrant).

---

## 4. Debug-only & Lifetime

- The registry ships in `Uno.UI.RemoteControl` (the `Uno.WinUI.DevServer` package), a
  debug-only reference — so it is absent in Release without any `#if DEBUG`.
- Registrations are **session-scoped** and ephemeral: alive while the publisher holds the
  `IDisposable`; never persisted across a process restart or devserver reset.
- A publisher that activates/deactivates registers on activate and disposes on deactivate;
  each transition raises `Changed`.

> **Publisher integration note**: `Uno.UI.RemoteControl` is a debug-only package reference. A
> debug-only publisher must call `ToolRegistry` from a path gated the same way it consumes
> Remote Control today; otherwise a thin optional shim is required. Finalized per module.

---

## 5. Roles & Guides

The registry itself imposes no policy beyond §2–§4. Each role has a dedicated guide:

- **Publishers** — see **Appendix A** (`spec-appendix-a-publisher-guide.md`): how to declare
  tools/resources, the handler contract, parameters, naming, lifetime.
- **Consumers** — see **Appendix B** (`spec-appendix-b-consumer-guide.md`): the **normative**
  obligations to read the registry and expose it over MCP — capability declaration,
  list-changed, `ToolParameter[]` → JSON Schema mapping, invocation, resource
  read/subscribe/update, content mapping — and the fact that the consumer owns **all**
  messaging. Written generically for **a** consumer, since several may coexist.

**A consumer may also be a publisher.** Whether a consumer **also** publishes its own
(currently hard-coded) tools into the registry — unifying everything behind one mechanism —
is **entirely its decision**. The registry imposes nothing on a consumer's existing tools;
dynamic and built-in tools can coexist indefinitely.

---

## 6. Tests

TDD (red → green). Location: `src/Uno.UI.RemoteControl.DevServer.Tests` (already in
`[InternalsVisibleTo]`). Purely registry-level — there is no transport in Uno to round-trip.

| Test | Description |
|------|-------------|
| `RegisterTool_ThenSnapshot_ContainsDescriptor` | A registered tool appears in `Snapshot()`. |
| `RegisterTool_Dispose_RemovesDescriptor` | Disposing removes the tool and raises `Changed`. |
| `RegisterTool_DuplicateName_IsRejected` | A second registration with the same `Name` is rejected and logged. |
| `Registry_Concurrent_RegisterUnregister_IsConsistent` | Parallel register/dispose leaves a coherent snapshot (lock-free). |
| `Changed_RaisedOnAddAndRemove` | `Changed` fires on both add and remove. |
| `InvokeAsync_Success_ReturnsResult` | `InvokeAsync` routes to the registered handler and returns its `ToolResult`. |
| `InvokeAsync_HandlerThrows_ReturnsIsError` | A throwing handler yields `IsError = true`, no exception escapes. |
| `InvokeAsync_UnknownTool_ReturnsIsError` | An unknown/removed tool returns an error result. |
| `InvokeAsync_RunsOnUIThread_WhenRequested` | With `runOnUIThread: true`, the handler executes on the dispatcher. |
| `ReadResourceAsync_ReturnsContent` | `ReadResourceAsync` returns the reader's `ToolResult`. |
| `NotifyUpdated_RaisesResourceUpdated_WithUri` | `NotifyUpdated()` raises `ResourceUpdated` with the correct URI; not after the registration is disposed. |
| `Publisher_And_Catalog_ResolveToSameInstance` | `ToolRegistry.Publisher` and `ToolRegistry.Catalog` are the same singleton; a tool registered via the publisher is visible through the catalog. |
| `SetForTesting_SwapsAndRestores` | `SetForTesting(...)` swaps the backing instance and restores it on dispose. |
| `MultiplePublishers_Snapshot_AggregatesAll` | Tools from several publishers all appear in one `Snapshot()`. |
| `MultipleConsumers_Changed_AllNotified` | Several subscribers to `Changed` are all notified on a registration change. |
| `MultipleConsumers_ResourceUpdated_Multicast` | `NotifyUpdated()` reaches every subscriber of `ResourceUpdated`. |

---

## 7. Open Items

| # | Item | Priority |
|---|------|----------|
| 1 | **Registry location**: `Uno.UI.RemoteControl` (recommended — debug-only + proximity) vs a more neutral assembly decoupled from the DevServer package. | P2 |
| 2 | **Publisher debug-only wiring**: confirm the call path / optional shim so a debug-only publisher reaches `ToolRegistry` without a Release dependency. | P1 |
| 3 | **Public publisher API** (bonus: applications publish their own tools): stabilize under SemVer; decide where public types live. | P2 |
| 4 | **Argument validation**: optionally validate `arguments` against `ToolParameter[]` inside `InvokeAsync`, vs. delegating to the consumer/handler. | P3 |
| 5 | **Resource templates** (parameterized URIs, MCP `resourceTemplates`): evaluate whether `ResourceDescriptor` needs a template variant. | P3 |
