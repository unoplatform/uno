# Spec 040: Remote Control Serialization Migration (Json.NET to System.Text.Json)

> **Status**: Draft
> **Date**  : 2026-04-10

---

## Executive Summary

### The Problem

The remote control feature (`Uno.UI.RemoteControl*`) uses Newtonsoft.Json (Json.NET) for all message serialization over its binary frame protocol. Json.NET maintains **global static** type-metadata caches that root `Type` objects from any assembly that participates in serialization. When the DevServer loads processors into **collectible** `AssemblyLoadContext` instances (one per connection in `DefaultRemoteControlProcessorFactory`), these global caches prevent the ALCs from being garbage-collected after connection release, causing a memory leak proportional to the number of connections served over the process lifetime.

Additionally, reflection-based serialization cannot be fully disabled because message types may be provided by external processor assemblies that do not (and cannot) register a `JsonSerializerContext` for source-generated serialization.

### What We're Changing

Replace all Newtonsoft.Json usage in remote control with System.Text.Json (STJ). STJ caches type metadata **per-`JsonSerializerOptions` instance** rather than globally, which means:

- A shared `JsonSerializerOptions` instance in the default ALC can serve known message types without rooting types from collectible ALCs.
- If needed in the future, per-ALC options instances can be scoped to connection lifetimes and disposed with the ALC.

### Why This Approach

- **Solves the ALC leak**: Source-generated types have no runtime metadata cache at all. The reflection fallback caches per-`JsonSerializerOptions` instance, not globally -- so even unknown external types don't block ALC collection.
- **Zero reflection for known types**: Source generators produce compile-time metadata for all ~15 known message types and ~12 IDE message types, eliminating reflection overhead on the hot path.
- **External types still work**: `DefaultJsonTypeInfoResolver` provides a reflection fallback for message types from external assemblies that can't register a `JsonSerializerContext`.
- **Low migration risk**: The current serialization surface uses no custom converters, no `TypeNameHandling`, no custom contract resolvers, and no property renaming.
- **Already partially adopted**: Several areas of the RemoteControl codebase (`AmbientRegistry`, `RemoteControlExtensions`, MCP tooling, telemetry) already use `System.Text.Json`.

### Expected Gains

| Aspect | Before | After |
|--------|--------|-------|
| ALC unload after connection close | Blocked by Json.NET global caches | Clean unload (source-gen types have no runtime cache; reflection fallback caches per-options-instance) |
| Reflection cost for known types | Full reflection on every serialize/deserialize | Zero reflection -- source-generated metadata for all known message types |
| Newtonsoft.Json dependency in RemoteControl | 10 projects | 0 projects |
| Serialization library alignment | Split (Json.NET for protocol, STJ for peripheral code) | Unified on STJ |

### Scope

- All Newtonsoft.Json usage across 10 `Uno.UI.RemoteControl*` projects
- 4 direct `JsonConvert` call sites
- ~15 message types with `[JsonProperty]` / `[JsonIgnore]` attributes
- Associated test code in `Uno.UI.RemoteControl.DevServer.Tests`

### Key Constraints

| Constraint | Impact |
|------------|--------|
| `Uno.UI.RemoteControl.Messaging` targets **netstandard2.0** | STJ must be added as a NuGet package reference (not inbox). Source generators run at compile time so they work on netstandard2.0. |
| External assemblies may provide message types | Source generators cover known types; `DefaultJsonTypeInfoResolver` provides reflection fallback for external types. |
| `#if !HAS_UNO` shim in `HotReloadStatusMessage.cs` | Uno.Toolkit builds compile this file without a Json dependency. The shim currently fakes `Newtonsoft.Json.JsonPropertyAttribute`. |
| Server and client ship together | Both ends always use the same Uno version, so format parity between Json.NET and STJ is not a concern. |

---

## 1. Current Architecture

### 1.1 Serialization Call Sites

There are four direct serialization entry points plus test usage:

| Location | Call | Project TFM |
|----------|------|-------------|
| `Uno.UI.RemoteControl.Messaging/Messages/Frame.cs:37` | `JsonConvert.SerializeObject(content)` | netstandard2.0 |
| `Uno.UI.RemoteControl.Messaging/Messages/Frame.cs:47` | `JsonConvert.DeserializeObject<T>(Content)` | netstandard2.0 |
| `Uno.UI.RemoteControl.Host/IDEChannel/IdeMessageSerializer.cs:13` | `JsonConvert.DeserializeObject(body, type)` | net9.0/net10.0 |
| `Uno.UI.RemoteControl.Host/IDEChannel/IdeMessageSerializer.cs:29` | `JsonConvert.SerializeObject(message)` | net9.0/net10.0 |
| `Uno.UI.RemoteControl.ServerCore/RemoteControlServer.cs:313` | `JsonConvert.DeserializeObject<ProcessorsDiscovery>(frame.Content)` | net9.0/net10.0 |
| `Uno.UI.RemoteControl.Server.Processors/HotReload/FileUpdateProcessor.cs:45` | `JsonConvert.DeserializeObject<UpdateSingleFileRequest>(frame.Content)` | net9.0/net10.0 |

`Frame.cs` is the central serialization hub. All message types flow through `Frame.Create<T>()` (serialize) and `Frame.TryGetContent<T>()` (deserialize). The two calls in `RemoteControlServer.cs` and `FileUpdateProcessor.cs` duplicate what `Frame.GetContent<T>()` already provides and should be refactored to use the `Frame` API.

### 1.2 Protocol Flow

```
Client                                           Server
  │                                                │
  │  Frame (binary wire format)                    │
  │  ┌─────────────────────────────────────────┐   │
  │  │ version: int16                          │   │
  │  │ scope:   string  (message routing)      │   │
  │  │ name:    string  (message type name)    │   │
  │  │ content: string  (JSON payload) ◄───────┤───┤── This is what we're migrating
  │  └─────────────────────────────────────────┘   │
  │──────────────── WebSocket ────────────────────►│
  │                                                │
  │  IdeMessageEnvelope (IDE channel)              │
  │  ┌─────────────────────────────────────────┐   │
  │  │ MessageType: AssemblyQualifiedName      │   │
  │  │ MessageBody: JSON string ◄──────────────┤───┤── And this
  │  └─────────────────────────────────────────┘   │
```

### 1.3 Message Type Inventory

**Frame-based messages** (routed by Scope + Name, serialized as JSON `Content`):

| Type | Style | Json.NET Attributes | Special Types |
|------|-------|---------------------|---------------|
| `KeepAliveMessage` | record | None | — |
| `AppLaunchMessage` | record | None | `Guid`, enum |
| `ProcessorsDiscovery` | class, parameterized ctor | None | — |
| `ProcessorsDiscoveryResponse` | record | None | `IImmutableList<T>` |
| `ConfigureServer` | record | None | Computed `Dictionary<string,string>` property |
| `AssemblyDeltaReload` | class | `[JsonProperty]` x6, `[JsonIgnore]` x2 | `ImmutableHashSet<string>` |
| `HotReloadWorkspaceLoadResult` | class | `[JsonProperty]` x1, `[JsonIgnore]` x2 | — |
| `HotReloadStatusMessage` | record | `[property: JsonProperty]` x3, `[JsonProperty]` x2 | `IImmutableList<T>`, `ImmutableHashSet<T>` |
| `HotReloadClientOperationEvent` | class | `[JsonProperty]` x7, `[JsonIgnore]` x3 | `DateTimeOffset`, enum (computed, ignored) |
| `UpdateFileRequest` | class | `[JsonProperty]` x6, `[JsonIgnore]` x2 | `ImmutableArray<FileEdit>`, `TimeSpan?` |
| `UpdateFileResponse` | record | `[property: JsonProperty]` x4, `[JsonIgnore]` x2 | `ImmutableArray<FileEditResult>` |
| `UpdateSingleFileRequest` | class | `[JsonProperty]` x7, `[JsonIgnore]` x3 | `TimeSpan?` |
| `UpdateSingleFileResponse` | record | `[property: JsonProperty]` x5, `[JsonIgnore]` x2 | enum |
| `XamlLoadError` | class, parameterized ctor | None | — |

**IDE channel messages** (routed by `AssemblyQualifiedName`, serialized through `IdeMessageSerializer`):

All are simple records inheriting from `IdeMessage(string Scope)`. None use Json.NET attributes. ~12 types including `HotReloadEventIdeMessage`, `UpdateFileIdeMessage`, `ForceHotReloadIdeMessage`, `AppLaunchRegisterIdeMessage`, etc.

**Supporting types** (nested in message payloads):

`FileEdit` (record), `FileEditResult` (record), `DiscoveredProcessor` (record), `HotReloadServerOperationData` (record with `ImmutableHashSet<string>`, `DateTimeOffset`, `IImmutableList<string>`).

### 1.4 Newtonsoft.Json Package References

10 projects reference `Newtonsoft.Json`:

- `Uno.UI.RemoteControl.Messaging.csproj` (netstandard2.0)
- `Uno.UI.RemoteControl.Host.csproj` (net9.0/net10.0)
- `Uno.UI.RemoteControl.Server.csproj`
- `Uno.UI.RemoteControl.Server.Processors.csproj`
- `Uno.UI.RemoteControl.ServerCore.csproj`
- `Uno.UI.RemoteControl.VS.csproj`
- `Uno.UI.RemoteControl.Skia.csproj`
- `Uno.UI.RemoteControl.Wasm.csproj`
- `Uno.UI.RemoteControl.Reference.csproj`
- `Uno.UI.RemoteControl.netcoremobile.csproj`

### 1.5 The `#if !HAS_UNO` Shim

`HotReloadStatusMessage.cs` contains a conditional shim to avoid a hard Newtonsoft.Json dependency when compiled into Uno.Toolkit:

```csharp
#if !HAS_UNO // We don't want to add a dependency on Newtonsoft.Json in Uno.Toolkit
namespace Newtonsoft.Json
{
    internal class JsonPropertyAttribute : Attribute { }
}
#endif
```

### 1.6 ALC Lifecycle Context

`DefaultRemoteControlProcessorFactory.cs` creates collectible ALCs (`isCollectible: true`) for each app instance and manages them via reference-counted leases. When a connection releases its lease, the ALC should be collected. However, any type from the ALC that passed through `JsonConvert` remains cached in Json.NET's global metadata tables, preventing collection.

---

## 2. Design

### 2.1 Strategy: Source-Generated STJ with Reflection Fallback

Use STJ source generators for all known message types (zero reflection cost, no runtime metadata caching), combined with `DefaultJsonTypeInfoResolver` as a fallback for external/unknown types. The two resolvers are chained via `JsonTypeInfoResolver.Combine()`.

This approach:

- Eliminates reflection overhead and metadata caching for all known types
- Still supports external message types that can't register a `JsonSerializerContext`
- Works on netstandard2.0 via the `System.Text.Json` NuGet package (source generators run at compile time)
- Caches per-options-instance, not globally, solving the ALC leak

#### Project layout

The known message types live in `Uno.UI.RemoteControl`, not in `Uno.UI.RemoteControl.Messaging` (which contains `Frame.cs`). So the source-generated context is defined in `Uno.UI.RemoteControl` and registered into the shared options at startup:

```csharp
// Uno.UI.RemoteControl/RemoteControlJsonContext.cs
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(KeepAliveMessage))]
[JsonSerializable(typeof(AppLaunchMessage))]
[JsonSerializable(typeof(ProcessorsDiscovery))]
[JsonSerializable(typeof(ProcessorsDiscoveryResponse))]
[JsonSerializable(typeof(ConfigureServer))]
[JsonSerializable(typeof(AssemblyDeltaReload))]
[JsonSerializable(typeof(HotReloadWorkspaceLoadResult))]
[JsonSerializable(typeof(HotReloadStatusMessage))]
[JsonSerializable(typeof(HotReloadClientOperationEvent))]
[JsonSerializable(typeof(UpdateFileRequest))]
[JsonSerializable(typeof(UpdateFileResponse))]
[JsonSerializable(typeof(UpdateSingleFileRequest))]
[JsonSerializable(typeof(UpdateSingleFileResponse))]
[JsonSerializable(typeof(XamlLoadError))]
// IDE messages
[JsonSerializable(typeof(HotReloadEventIdeMessage))]
[JsonSerializable(typeof(UpdateFileIdeMessage))]
[JsonSerializable(typeof(ForceHotReloadIdeMessage))]
[JsonSerializable(typeof(HotReloadThruDebuggerIdeMessage))]
[JsonSerializable(typeof(HotReloadRequestedIdeMessage))]
[JsonSerializable(typeof(CommandRequestIdeMessage))]
[JsonSerializable(typeof(AppLaunchRegisterIdeMessage))]
[JsonSerializable(typeof(KeepAliveIdeMessage))]
[JsonSerializable(typeof(DevelopmentEnvironmentStatusIdeMessage))]
[JsonSerializable(typeof(NotificationRequestIdeMessage))]
[JsonSerializable(typeof(AddMenuItemRequestIdeMessage))]
[JsonSerializable(typeof(IdeResultMessage))]
internal partial class RemoteControlJsonContext : JsonSerializerContext { }
```

```csharp
// Uno.UI.RemoteControl.Messaging/RemoteControlJsonOptions.cs
internal static class RemoteControlJsonOptions
{
    private static JsonSerializerOptions? _options;

    public static JsonSerializerOptions Default => _options ??= CreateDefault();

    /// <summary>
    /// Registers a source-generated context to be used for known types.
    /// Must be called before the first serialization operation.
    /// Unknown types fall back to reflection-based resolution.
    /// </summary>
    public static void SetSourceGeneratedContext(JsonSerializerContext context)
    {
        _options = CreateOptions(context);
    }

    private static JsonSerializerOptions CreateDefault()
        => CreateOptions(sourceGenerated: null);

    private static JsonSerializerOptions CreateOptions(JsonSerializerContext? sourceGenerated)
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = sourceGenerated is not null
                ? JsonTypeInfoResolver.Combine(sourceGenerated, new DefaultJsonTypeInfoResolver())
                : new DefaultJsonTypeInfoResolver()
        };
    }
}
```

At startup (in `RemoteControlClient` initialization and server host startup):

```csharp
RemoteControlJsonOptions.SetSourceGeneratedContext(RemoteControlJsonContext.Default);
```

If `SetSourceGeneratedContext` is never called (e.g., a minimal external host), the options degrade gracefully to reflection-only on first use.

#### How the resolver chain works

When `JsonSerializer.Serialize<T>()` or `Deserialize<T>()` is called:

1. STJ asks the first resolver (`RemoteControlJsonContext.Default`) for type info for `T`
2. If `T` is a known type (listed in `[JsonSerializable]`), the source-generated metadata is returned -- no reflection, no caching
3. If `T` is unknown, the first resolver returns `null` and STJ falls through to `DefaultJsonTypeInfoResolver`, which uses reflection
4. The reflection resolver caches per-`JsonSerializerOptions` instance, not globally -- so the ALC leak is still avoided even for the fallback path

### 2.2 Attribute Migration

| Current (Newtonsoft.Json) | Target (System.Text.Json) | Rationale |
|---------------------------|---------------------------|-----------|
| `[JsonProperty]` (no rename) | **Remove entirely** | STJ serializes all public properties by default. `[JsonProperty]` was only used to include properties, not to rename them. |
| `[JsonIgnore]` | `[System.Text.Json.Serialization.JsonIgnore]` | Same name, different namespace. Applied to computed properties (`Scope`, `Kind`) and explicit interface implementations. |
| `[property: JsonProperty]` on records | **Remove entirely** | STJ handles record constructor parameters natively. |
| `using Newtonsoft.Json;` | `using System.Text.Json.Serialization;` | Only where `[JsonIgnore]` is needed. |

**`#if !HAS_UNO` shim resolution**: Since all `[JsonProperty]` attributes are removed from `HotReloadStatusMessage`, and no `[JsonIgnore]` is needed (the `Scope` property is public and can safely be serialized for backward compatibility; the `IMessage.Name` is an explicit interface implementation invisible to STJ), the shim is deleted entirely. No STJ-equivalent shim is needed.

### 2.3 Core Serialization Migration

**Frame.cs** (the central hub):

```csharp
// Before:
JsonConvert.SerializeObject(content)
JsonConvert.DeserializeObject<T>(Content)

// After:
JsonSerializer.Serialize(content, content.GetType(), RemoteControlJsonOptions.Default)
JsonSerializer.Deserialize<T>(Content, RemoteControlJsonOptions.Default)
```

Note: `Serialize(content, content.GetType(), options)` is used instead of `Serialize<T>(content, options)` to ensure the runtime type's properties are serialized when `T` is an interface or base type.

**IdeMessageSerializer.cs** (polymorphic dispatch):

```csharp
// Before:
JsonConvert.DeserializeObject(envelope.MessageBody, messageType)
JsonConvert.SerializeObject(message)

// After:
JsonSerializer.Deserialize(envelope.MessageBody, messageType, RemoteControlJsonOptions.Default)
JsonSerializer.Serialize(message, message.GetType(), RemoteControlJsonOptions.Default)
```

The `message.GetType()` overload is critical: `message` is declared as `IdeMessage` but we must serialize the runtime type's properties (e.g., `HotReloadEventIdeMessage`).

**RemoteControlServer.cs and FileUpdateProcessor.cs**: Refactor from direct `JsonConvert.DeserializeObject<T>(frame.Content)` to `frame.GetContent<T>()`, which delegates to `Frame`'s serialization. This eliminates direct JSON library references from these projects.

### 2.4 Constructor Deserialization

Two types have parameterized constructors without `[JsonConstructor]`:

- **`ProcessorsDiscovery(string basePath, string appInstanceId = "")`**: STJ matches constructor parameter names case-insensitively (`basePath` matches `BasePath`). The default parameter `appInstanceId = ""` is honored. Should work without changes.
- **`XamlLoadError(string filePath, string message, string? stackTrace, string exceptionType)`**: STJ matches all four parameters case-insensitively. Should work without changes.

If any fail, add `[JsonConstructor]` to the constructor.

### 2.5 `ConfigureServer` Computed Property

`ConfigureServer` has a lazy-computed `MSBuildProperties` dictionary:

```csharp
private Dictionary<string, string>? _msbuildProperties;
public Dictionary<string, string> MSBuildProperties => _msbuildProperties ??= ParseMSBuildProperties(MSBuildPropertiesRaw);
```

This is a public read-only property. STJ will serialize it (producing the dictionary JSON) but cannot deserialize into it (no setter). On deserialization, the property will be skipped and `_msbuildProperties` remains null, recomputed lazily from `MSBuildPropertiesRaw`. This is correct behavior but produces unnecessary JSON on serialization. Add `[JsonIgnore]` to `MSBuildProperties` for cleanliness.

---

## 3. Implementation

### Phase 1: Foundation

1. Add `System.Text.Json` NuGet package to `Uno.UI.RemoteControl.Messaging.csproj` (netstandard2.0)
2. Create `Uno.UI.RemoteControl.Messaging/RemoteControlJsonOptions.cs` with the configurable options instance and `SetSourceGeneratedContext` API
3. Create `Uno.UI.RemoteControl/RemoteControlJsonContext.cs` -- source-generated `JsonSerializerContext` with `[JsonSerializable]` for all known message types
4. Register the context at startup in `RemoteControlClient` initialization and server host startup via `RemoteControlJsonOptions.SetSourceGeneratedContext(RemoteControlJsonContext.Default)`

### Phase 2: Message Type Attributes

For each message type with Json.NET attributes:

- Remove `using Newtonsoft.Json`
- Remove all `[JsonProperty]` and `[property: JsonProperty]` attributes
- Replace `[JsonIgnore]` from `Newtonsoft.Json` with `[JsonIgnore]` from `System.Text.Json.Serialization`
- Delete the `#if !HAS_UNO` shim in `HotReloadStatusMessage.cs`
- Add `[JsonIgnore]` to `ConfigureServer.MSBuildProperties`
- Add `[JsonConstructor]` to `ProcessorsDiscovery` and `XamlLoadError` constructors if needed (test first)

### Phase 3: Serialization Call Sites

1. **Frame.cs**: Replace `JsonConvert` calls with `JsonSerializer` using `RemoteControlJsonOptions.Default`
2. **IdeMessageSerializer.cs**: Replace `JsonConvert` calls with `JsonSerializer`, using `message.GetType()` for serialization
3. **RemoteControlServer.cs**: Replace `JsonConvert.DeserializeObject<ProcessorsDiscovery>(frame.Content)` with `frame.GetContent<ProcessorsDiscovery>()`
4. **FileUpdateProcessor.cs**: Replace `JsonConvert.DeserializeObject<UpdateSingleFileRequest>(frame.Content)` with `frame.GetContent<UpdateSingleFileRequest>()`

### Phase 4: Project Cleanup

Remove `Newtonsoft.Json` package reference from all 10 project files. For net9.0/net10.0 projects, STJ is inbox. For netstandard2.0, the NuGet package added in Phase 1 provides it transitively.

### Phase 5: Tests

1. Replace `JsonConvert` calls in `RemoteControlServerBehaviorTests.cs` and `InProcessDevServerTests.cs` with `frame.GetContent<T>()` or `JsonSerializer.Deserialize`
2. Remove `Newtonsoft.Json` from test project

---

## 4. Affected Files

### New files (2)

| File | Purpose |
|------|---------|
| `Uno.UI.RemoteControl.Messaging/RemoteControlJsonOptions.cs` | Shared `JsonSerializerOptions` with `SetSourceGeneratedContext` API |
| `Uno.UI.RemoteControl/RemoteControlJsonContext.cs` | Source-generated `JsonSerializerContext` for all known message types |

### Core serialization (2)

| File | Change |
|------|--------|
| `Uno.UI.RemoteControl.Messaging/Messages/Frame.cs` | `JsonConvert` to `JsonSerializer` |
| `Uno.UI.RemoteControl.Host/IDEChannel/IdeMessageSerializer.cs` | `JsonConvert` to `JsonSerializer` with `message.GetType()` |

### Server-side refactoring (2)

| File | Change |
|------|--------|
| `Uno.UI.RemoteControl.ServerCore/RemoteControlServer.cs` | Direct `JsonConvert` call to `frame.GetContent<T>()` |
| `Uno.UI.RemoteControl.Server.Processors/HotReload/FileUpdateProcessor.cs` | Direct `JsonConvert` call to `frame.GetContent<T>()` |

### Message type attribute migration (9)

| File | Change |
|------|--------|
| `Uno.UI.RemoteControl/HotReload/Messages/AssemblyDeltaReload.cs` | Remove `[JsonProperty]`, swap `[JsonIgnore]` namespace |
| `Uno.UI.RemoteControl/HotReload/Messages/HotReloadWorkspaceLoadResult.cs` | Remove `[JsonProperty]`, swap `[JsonIgnore]` namespace |
| `Uno.UI.RemoteControl/HotReload/Messages/HotReloadStatusMessage.cs` | Remove `[property: JsonProperty]`, delete `#if !HAS_UNO` shim |
| `Uno.UI.RemoteControl/HotReload/Messages/HotReloadClientOperationEvent.cs` | Remove `[JsonProperty]`, swap `[JsonIgnore]` namespace |
| `Uno.UI.RemoteControl/HotReload/Messages/UpdateFileRequest.cs` | Remove `[JsonProperty]`, swap `[JsonIgnore]` namespace |
| `Uno.UI.RemoteControl/HotReload/Messages/UpdateFileResponse.cs` | Remove `[property: JsonProperty]`, swap `[JsonIgnore]` namespace |
| `Uno.UI.RemoteControl/HotReload/Messages/UpdateSingleFileRequest.cs` | Remove `[JsonProperty]`, swap `[JsonIgnore]` namespace |
| `Uno.UI.RemoteControl/HotReload/Messages/UpdateSingleFileResponse.cs` | Remove `[property: JsonProperty]`, swap `[JsonIgnore]` namespace |
| `Uno.UI.RemoteControl/HotReload/Messages/ConfigureServer.cs` | Add `[JsonIgnore]` on `MSBuildProperties` |

### Project files (11)

| File | Change |
|------|--------|
| `Uno.UI.RemoteControl.Messaging.csproj` | Add `System.Text.Json`, remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.Host.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.Server.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.Server.Processors.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.ServerCore.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.VS.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.Skia.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.Wasm.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.Reference.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.netcoremobile.csproj` | Remove `Newtonsoft.Json` |
| `Uno.UI.RemoteControl.DevServer.Tests.csproj` | Remove `Newtonsoft.Json` |

### Test files (2)

| File | Change |
|------|--------|
| `Uno.UI.RemoteControl.DevServer.Tests/RemoteControlServerBehaviorTests.cs` | `JsonConvert` to STJ / `frame.GetContent<T>()` |
| `Uno.UI.RemoteControl.DevServer.Tests/InProcessDevServerTests.cs` | `JsonConvert` to STJ / `frame.GetContent<T>()` |

---

## 5. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Constructor deserialization fails for `ProcessorsDiscovery` or `XamlLoadError` | Runtime crash on first message of that type | Add `[JsonConstructor]` attribute. Caught by existing test suite. |
| `ConfigureServer.MSBuildProperties` produces unnecessary JSON without `[JsonIgnore]` | Slightly larger wire payloads (non-breaking) | Add `[JsonIgnore]` as described. |
| External processor assemblies that define custom message types and use `[JsonProperty]` for rename | Deserialization failure for renamed properties | No known external types use renaming. Document in migration guide that `[JsonPropertyName("x")]` replaces `[JsonProperty("x")]`. |
| `System.Text.Json` NuGet on netstandard2.0 increases package size | Marginal size increase for Messaging package | Acceptable. STJ is already transitively pulled by other dependencies in most configurations. |
| `IdeMessageSerializer` serializes only base type properties without `GetType()` | Missing fields in IDE messages | Use `JsonSerializer.Serialize(message, message.GetType(), options)` explicitly. |

---

## 6. Design Decisions

| Decision | Rationale |
|----------|-----------|
| Source-generated STJ with reflection fallback | Source generators eliminate reflection overhead and runtime metadata caching for all known types. `DefaultJsonTypeInfoResolver` fallback handles external assemblies that can't register a `JsonSerializerContext`. |
| `JsonSerializerContext` in `Uno.UI.RemoteControl`, not `Messaging` | The known message types live in `Uno.UI.RemoteControl`. The `Messaging` project (netstandard2.0) contains `Frame.cs` but can't see message types. The context is registered at startup via `SetSourceGeneratedContext`. |
| Graceful degradation to reflection-only | If `SetSourceGeneratedContext` is never called, `RemoteControlJsonOptions.Default` initializes with `DefaultJsonTypeInfoResolver` only. External hosts that don't register the context still work. |
| Single shared `JsonSerializerOptions` instance | Known message types all live in default-ALC assemblies. A shared instance avoids per-request allocations and warmup cost. |
| Remove `[JsonProperty]` rather than replace with `[JsonPropertyName]` | Current usage only marks properties for inclusion (no renaming). STJ includes all public properties by default, so the attribute is unnecessary. |
| Refactor `RemoteControlServer.cs` and `FileUpdateProcessor.cs` to use `frame.GetContent<T>()` | Eliminates direct JSON library coupling. These projects no longer need any JSON package reference. |
| Delete `#if !HAS_UNO` shim rather than replace | With all `[JsonProperty]` removed, `HotReloadStatusMessage` needs no serialization attributes at all, so no shim is needed for the Uno.Toolkit build. |
| Atomic migration (no dual-library transition period) | Both ends always use the same Uno version. No need for a transition period or dual-format support. |

---

## 7. Validation

### Build

```bash
cd src
dotnet build Uno.UI-Skia-only.slnf --no-restore
```

### Unit Tests

```bash
dotnet test Uno.UI.RemoteControl.DevServer.Tests/Uno.UI.RemoteControl.DevServer.Tests.csproj
```

### Integration

- Start DevServer with Skia desktop SamplesApp
- Verify hot reload round-trip (edit XAML, observe update)
- Verify processor discovery handshake completes
- Verify keep-alive pings succeed

---

## 8. References

- Spec `000-alc-secondary-app-support.md` -- ALC architecture context
- Spec `039-alc-aware-hotreload-handlers/spec.md` -- Related ALC-scoping work
- `src/Uno.UI.RemoteControl.Server/Processors/DefaultRemoteControlProcessorFactory.cs` -- ALC lifecycle management
- [System.Text.Json migration guide (Microsoft)](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft)
- [System.Text.Json NuGet package (netstandard2.0 support)](https://www.nuget.org/packages/System.Text.Json)
