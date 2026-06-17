# Spec 044 — Appendix A: Publisher Guide

> **Companion to** `spec.md`. **Audience**: any component that wants to **publish** tools or
> resources into the Tool & Resource Registry — a *publisher*. This includes a registry
> consumer (an MCP server codebase), should it choose to publish its own built-in tools
> through the registry. **Whether to do so is the publisher's own decision** (see §7).
> No specific internal project is named here. For the consumer (MCP) side, see **Appendix B**.

This guide is written from the **publisher's point of view**: how to declare capabilities,
what the handler contract is, and how lifetime works. For the registry's overall design, see
`spec.md`.

---

## 1. What a publisher does

A publisher declares **tools** (callable operations) and/or **resources** (readable content)
into the process-wide `ToolRegistry`. The registry is an **in-memory store with no
transport**: declaring a tool does not by itself talk to a devserver or an agent. One or more
consumers running in the same process read the registry and are responsible for exposing it
(over MCP, in practice). The publisher never sees MCP and never sends messages.

A publisher only needs the **publication face** of the registry, `IToolPublisher`, reached
through the static accessor `ToolRegistry.Publisher`:

```csharp
using Uno.UI.RemoteControl.Tools;

IToolPublisher publisher = ToolRegistry.Publisher; // the publication face
```

You never touch the consumption face (`IToolCatalog`) — that belongs to consumers. All
examples below call `ToolRegistry.Publisher.…`.

> **Access**: the API is `internal`. A publisher assembly is granted access via
> `[InternalsVisibleTo]` (added in `Uno.UI.RemoteControl/AssemblyInfo.cs`). Coordinate
> adding your assembly name there.

> **Debug-only**: the registry ships in the debug-only `Uno.WinUI.DevServer` package
> (`Uno.UI.RemoteControl`), absent in Release. Call it from a path gated the same way you
> already consume the Remote Control client, or behind a thin optional shim.

---

## 2. Declaring a tool

```csharp
IDisposable registration = ToolRegistry.Publisher.RegisterTool(
    new ToolDescriptor(
        Name: "mymodule_set_property",
        Description: "Sets a property on the element identified by the given handle.",
        Parameters:
        [
            new ToolParameter("handle",   "Opaque element handle.", ToolParameterKind.String, IsRequired: true),
            new ToolParameter("property", "Property name.",         ToolParameterKind.String, IsRequired: true),
            new ToolParameter("value",    "New value (string form).", ToolParameterKind.String, IsRequired: true),
        ],
        IsReadOnly: false),
    handler: static async (invocation, ct) =>
    {
        var handle   = invocation.GetString("handle");
        var property = invocation.GetString("property");
        var value    = invocation.GetString("value");

        // … perform the operation on the visual tree …

        return ToolResult.Text($"Set {property} = {value} on {handle}.");
    });
```

- `RegisterTool` returns an **`IDisposable`**. Keep it; dispose it to remove the tool.
- The handler runs **on the UI thread by default** (`runOnUIThread: true`). Pass
  `runOnUIThread: false` only for handlers that must not touch the UI and do their own
  synchronization.

### 2.1 Naming

`Name` is the registry key and **must be unique across all publishers**. **Namespace by
module** with a `<module>_` prefix (e.g. `mymodule_select_element`). A duplicate name is
rejected and logged — it does not throw, but your tool will not be registered.

Use lower_snake_case, descriptive verbs (`mymodule_select_element`, `mymodule_get_state`).
Names and descriptions are surfaced to an agent — write them for that reader.

### 2.2 `IsReadOnly`

Set `IsReadOnly: true` for tools that do not mutate application state (queries, reads,
screenshots). A consumer maps it to the MCP read-only hint. Default to `false` when in doubt.

---

## 3. Parameters (typed model)

Parameters are described declaratively with `ToolParameter`; the consumer turns them into a
JSON Schema. You do **not** write JSON Schema yourself.

| Field | Meaning |
|---|---|
| `Name` | parameter key (read back via `invocation.Get*`) |
| `Description` | shown to the agent |
| `Kind` | `String` / `Integer` / `Number` / `Boolean` / `Array` / `Object` |
| `IsRequired` | feeds the schema `required` list |
| `DefaultValue` | optional default (string form) |
| `AllowedValues` | optional enum constraint |

Inside the handler, read arguments through the typed accessors on `ToolInvocation`
(`GetString`, `GetInt32`, `GetBoolean`, …), or the raw `invocation.Arguments` (`JsonObject`)
for `Array`/`Object` shapes.

---

## 4. The handler contract

```csharp
internal delegate ValueTask<ToolResult> ToolHandler(ToolInvocation invocation, CancellationToken ct);
```

- **Return a `ToolResult`** describing the outcome. Use `ToolResult.Text(...)` for the
  common case, or build `ToolContent` items for JSON/image/blob output:

  | Output | `ToolContentKind` |
  |---|---|
  | plain text | `Text` |
  | a JSON document (as string) | `Json` |
  | an image (`Base64Data` + `MimeType`) | `Image` |
  | binary blob (`Base64Data` + `MimeType`) | `Blob` |

- **Do not throw for expected failures.** Return `ToolResult` with `IsError: true` and a
  textual explanation. The registry also catches unhandled exceptions and converts them to
  an error result, but returning one explicitly gives a better message to the agent.
- **Honor the `CancellationToken`**. Long operations should observe `ct`.
- **Threading**: with the default `runOnUIThread: true`, you are on the UI thread — touch
  the visual tree freely. Do not block it on long synchronous work; `await` instead.
- **Keep handlers stateless across calls.** A tool may be invoked **concurrently from several
  consumers**; do not assume a single caller or stash per-call state in shared fields.

---

## 5. Declaring a resource

Resources are readable, addressable content (by URI).

```csharp
IResourceRegistration resource = ToolRegistry.Publisher.RegisterResource(
    new ResourceDescriptor(
        Uri: "mymodule://state/current",
        Name: "Current module state",
        Description: "A JSON snapshot of the module's current state.",
        MimeType: "application/json"),
    reader: static async ct =>
    {
        var json = /* build current state */;
        return new ToolResult([new ToolContent(ToolContentKind.Json, Text: json, MimeType: "application/json")]);
    });
```

### 5.1 Signaling updates

When the content behind a resource changes, call `NotifyUpdated()` on its registration:

```csharp
resource.NotifyUpdated();
```

This raises the registry's `ResourceUpdated` event (observed by consumers via
`ToolRegistry.Catalog`). Each consumer forwards it to whichever agents subscribed to that URI
(consumers track subscriptions; you do not). Call it whenever the underlying state changes —
it is cheap and idempotent from your side.

---

## 6. Lifetime

- A registration lives until you **dispose** it. Dispose when your feature deactivates so the
  catalogue reflects reality.
- Typical pattern for a module that turns on/off:

```csharp
// On activate:
_toolRegistrations =
[
    ToolRegistry.Publisher.RegisterTool(/* … */),
    ToolRegistry.Publisher.RegisterTool(/* … */),
];

// On deactivate:
foreach (var r in _toolRegistrations) r.Dispose();
_toolRegistrations = [];
```

- Add/remove each raise `Changed`; consumers re-publish. You do not debounce — register in
  bursts freely.
- Registrations are **session-scoped**: they never survive a process restart or devserver
  reset. Re-register on each activation.

---

## 7. Should a consumer publish its own tools?

A registry consumer (an MCP server codebase) **may** publish its existing, currently
hard-coded tools through the registry as a publisher — unifying its own tools and external
modules' tools behind a single mechanism. It may equally keep its built-in tools as they are
and use the registry **only** for additional tools from other modules.

**This is entirely the consumer's decision.** The registry imposes nothing: dynamic and
built-in tools coexist indefinitely. The natural dividing line is *where the handler runs* —
tools whose logic runs **in the app process** fit the registry's publisher model; purely
devserver-side tools are unaffected by this registry.

---

## 8. Do / Don't

**Do**
- Namespace tool names by module.
- Write agent-facing `Description`s.
- Return `IsError: true` for failures instead of throwing.
- Dispose registrations on deactivation.
- Keep handlers stateless across calls.

**Don't**
- Reference MCP, messaging, `Frame`, or `RemoteControlClient` from a publisher — you need
  none of it.
- Assume a single consumer or devserver.
- Block the UI thread in a handler.
- Persist or cache registrations across sessions.
