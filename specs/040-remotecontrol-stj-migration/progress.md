# Progress: Remote Control STJ Migration

## Phase 1: Foundation
- [x] Add `System.Text.Json` NuGet to `Uno.UI.RemoteControl.Messaging.csproj`
- [x] Create `Uno.UI.RemoteControl.Messaging/RemoteControlJsonOptions.cs`
- [x] Create `Uno.UI.RemoteControl/RemoteControlJsonContext.cs` (source-gen context)
- [x] Register context at startup (client via `CreateClient`)

## Phase 2: Message Type Attributes
- [x] `AssemblyDeltaReload.cs` — remove `[JsonProperty]`, swap `[JsonIgnore]`
- [x] `HotReloadWorkspaceLoadResult.cs` — remove `[JsonProperty]`, swap `[JsonIgnore]`
- [x] `HotReloadStatusMessage.cs` — remove `[property: JsonProperty]`, delete `#if !HAS_UNO` shim
- [x] `HotReloadClientOperationEvent.cs` — remove `[JsonProperty]`, swap `[JsonIgnore]`
- [x] `UpdateFileRequest.cs` — remove `[JsonProperty]`, swap `[JsonIgnore]`
- [x] `UpdateFileResponse.cs` — remove `[property: JsonProperty]`, remove `[JsonIgnore]` (explicit interface only)
- [x] `UpdateSingleFileRequest.cs` — remove `[JsonProperty]`, swap `[JsonIgnore]`
- [x] `UpdateSingleFileResponse.cs` — remove `[property: JsonProperty]`, remove `[JsonIgnore]` (explicit interface only)
- [x] `ConfigureServer.cs` — add `[JsonIgnore]` on `MSBuildProperties`

## Phase 3: Serialization Call Sites
- [x] `Frame.cs` — `JsonConvert` to `JsonSerializer`
- [x] `IdeMessageSerializer.cs` — `JsonConvert` to `JsonSerializer` with `message.GetType()`
- [x] `RemoteControlServer.cs` — direct `JsonConvert` to `frame.GetContent<T>()`
- [x] `FileUpdateProcessor.cs` — direct `JsonConvert` to `frame.GetContent<T>()`

## Phase 4: Project Cleanup (remove Newtonsoft.Json)
- [x] `Uno.UI.RemoteControl.Messaging.csproj`
- [x] `Uno.UI.RemoteControl.Host.csproj`
- [x] `Uno.UI.RemoteControl.Server.csproj`
- [x] `Uno.UI.RemoteControl.Server.Processors.csproj`
- [x] `Uno.UI.RemoteControl.ServerCore.csproj`
- [x] `Uno.UI.RemoteControl.VS.csproj`
- [x] `Uno.UI.RemoteControl.Skia.csproj`
- [x] `Uno.UI.RemoteControl.Wasm.csproj`
- [x] `Uno.UI.RemoteControl.Reference.csproj`
- [x] `Uno.UI.RemoteControl.netcoremobile.csproj`

## Phase 5: Tests
- [x] `RemoteControlServerBehaviorTests.cs` — migrate to `frame.GetContent<T>()`
- [x] `InProcessDevServerTests.cs` — migrate to `frame.TryGetContent<T>()`
- [ ] Remove `Newtonsoft.Json` from test project (N/A -- was transitive)

## Build Verification
- [x] `dotnet build Uno.UI.RemoteControl.Messaging` -- succeeded
- [x] `dotnet build Uno.UI.RemoteControl.Skia.csproj` -- succeeded (0 errors)
- [x] `dotnet build Uno.UI.RemoteControl.DevServer.Tests.csproj` -- succeeded (0 errors)
- [x] `dotnet test Uno.UI.RemoteControl.DevServer.Tests` -- 325 passed, 1 failed (pre-existing, unrelated), 1 skipped

## Notes
- `RemoteControlJsonOptions` changed from `internal` to `public` (needed for cross-assembly `SetSourceGeneratedContext` registration)
- Skia full solution build: 91 pre-existing errors in Uno.UWP (net10.0 enum codegen issue), 0 errors from our changes
- Server-side does not register source-gen context (graceful fallback to reflection -- acceptable since server is not ALC-constrained)
