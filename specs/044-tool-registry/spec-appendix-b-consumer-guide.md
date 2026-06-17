# Spec 044 — Appendix B: Consumer Guide

> **Companion to** `spec.md`. **Audience**: a component that **reads** the Tool & Resource
> Registry and exposes it to agents — a *consumer* (in practice, an MCP server). This guide is
> **normative**: it defines the obligations a consumer honors. The consumer's *code* lives in
> a separate codebase and is out of scope; **all** messaging and MCP mapping are the
> consumer's responsibility — the registry provides none.
> Written generically for **a** consumer: **several consumers may read the same registry**
> (an MCP server, another bridge, a test harness…). No specific internal project is named.
> For the publishing side, see **Appendix A**.

---

## 1. What a consumer does

A consumer reads the registry's **consumption face**, `IToolCatalog`, via the static accessor
`ToolRegistry.Catalog`, and bridges it to its agents:

```csharp
using Uno.UI.RemoteControl.Tools;

IToolCatalog catalog = ToolRegistry.Catalog; // the consumption face
```

It is responsible for everything beyond the registry:

- transporting the catalogue to a devserver over **its own existing pipeline**;
- mapping tools/resources onto the MCP protocol;
- tracking which agent subscribed to what.

The registry imposes **no transport** and holds **no per-consumer state**. A consumer never
mutates the registry (unless it also acts as a publisher — §7).

> **Access**: the API is `internal`. A consumer assembly is granted access via
> `[InternalsVisibleTo]` (added in `Uno.UI.RemoteControl/AssemblyInfo.cs`). Coordinate adding
> your assembly name there.

> **Debug-only**: the registry ships in the debug-only `Uno.WinUI.DevServer` package
> (`Uno.UI.RemoteControl`), absent in Release.

---

## 2. Capabilities & list-changed

Declare the MCP capabilities needed for change and subscription support:

```json
{
  "capabilities": {
    "tools":     { "listChanged": true },
    "resources": { "subscribe": true, "listChanged": true }
  }
}
```

- Read `ToolRegistry.Catalog.Snapshot()` to obtain the current tools and resources, and
  expose them to agents over your pipeline.
- Subscribe to `ToolRegistry.Catalog.Changed`; on each change, re-read the snapshot and emit
  `notifications/tools/list_changed` and/or `notifications/resources/list_changed`.

The snapshot is the full current state; treat each read as authoritative (do not maintain a
diff against the registry).

---

## 3. Tool mapping & invocation

For each `ToolDescriptor`, build an MCP tool whose `inputSchema` is generated from
`ToolParameter[]`:

| `ToolParameterKind` | JSON Schema `type` |
|---|---|
| `String` | `"string"` |
| `Integer` | `"integer"` |
| `Number` | `"number"` |
| `Boolean` | `"boolean"` |
| `Array` | `"array"` |
| `Object` | `"object"` |

- `IsRequired` feeds the schema `required` array.
- `AllowedValues` becomes `enum`.
- `DefaultValue` becomes `default`.
- `IsReadOnly` maps to the MCP read-only tool hint.

On an MCP `tools/call`:

```csharp
ToolResult result = await ToolRegistry.Catalog.InvokeAsync(name, arguments, ct);
```

- `arguments` is the call's arguments as a `JsonObject`.
- The registry has **already handled UI-thread marshalling** (per the tool's registration);
  just `await`.
- Map the returned `ToolResult` to the MCP tool-result shape (§5).
- A consumer **may** apply its own invocation timeout.

---

## 4. Resource read, subscribe & update

- For each `ResourceDescriptor`, build an MCP resource (`uri`, `name`, `description`,
  `mimeType`).
- On MCP `resources/read`:

  ```csharp
  ToolResult result = await ToolRegistry.Catalog.ReadResourceAsync(uri, ct);
  ```

- On MCP `resources/subscribe` / `resources/unsubscribe`: track the subscription **on your
  side** — the registry holds none.
- Subscribe to `ToolRegistry.Catalog.ResourceUpdated`; when it fires for a URI an agent
  subscribed to, emit `notifications/resources/updated` (`{ "uri": … }`). This is the
  end-to-end resource-update path, fully supported by MCP.

---

## 5. Content mapping

| `ToolContentKind` | MCP content |
|---|---|
| `Text` | text content |
| `Json` | text content carrying the JSON string (or structured content where supported) |
| `Image` | image content (`data` = `Base64Data`, `mimeType`) |
| `Blob` | binary/resource content (`data` = `Base64Data`, `mimeType`) |

`ToolResult.IsError = true` maps to the MCP tool-call error result. The registry never throws
across an invocation — a failing handler is surfaced as `IsError`, not an exception.

---

## 6. Multiple consumers & concurrency

The registry is a **many-to-many hub**; design your consumer so it coexists with others:

- The `IToolCatalog` face is **observational**. `Snapshot`/`InvokeAsync`/`ReadResourceAsync`
  are reentrant and may run **concurrently with other consumers** invoking the same tools.
- `Changed` / `ResourceUpdated` are **multicast**: every consumer is notified. **Filter
  `ResourceUpdated` against your own subscriptions** — you will also receive updates for URIs
  only other consumers care about.
- Hold **no assumption of exclusivity**: another consumer may have invoked a tool, or a
  publisher may have come and gone, between your snapshot and your next call. Always treat the
  latest snapshot as truth.
- If you run several `RemoteControlClient` connections (e.g. multiple devservers), the
  per-devserver fan-out is yours to manage; the registry raises each event once.

---

## 7. Coexistence with built-in tools; also being a publisher

- Dynamic registry tools **coexist** with any fixed (compile-time) tool list a consumer
  already ships. Merge both into the surface you expose to agents.
- A consumer **may also be a publisher** — registering its own tools into the registry via
  `ToolRegistry.Publisher` (see **Appendix A**) to unify everything behind one mechanism.
  Whether to do so is the consumer's decision; the registry imposes nothing.
- The natural dividing line is *where the handler runs*: tools whose logic runs **in the app
  process** fit the registry; purely devserver-side tools are unaffected by it.

---

## 8. Do / Don't

**Do**
- Re-read `Snapshot()` on every `Changed` and treat it as authoritative.
- Filter `ResourceUpdated` against your own subscriptions.
- Map `IsError` to the MCP error result; map content kinds faithfully.
- Apply your own timeout if you need one.

**Don't**
- Assume you are the only consumer, or that handlers are single-threaded across consumers.
- Maintain a private diff of the registry instead of re-reading the snapshot.
- Expect the registry to track subscriptions, fan out to devservers, or know about MCP.
- Mutate the registry from the consumer path (use `ToolRegistry.Publisher` only if you
  deliberately act as a publisher).
