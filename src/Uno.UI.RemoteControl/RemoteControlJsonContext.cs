#nullable enable

using System.Text.Json.Serialization;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl;

/// <summary>
/// Source-generated JSON serialization context for all known remote control message types.
/// Used as the primary resolver in <see cref="Messaging.RemoteControlJsonOptions"/>;
/// unknown/external types fall back to reflection-based resolution.
/// </summary>
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
	DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
// Frame-based messages
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
// IDE channel messages
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
internal partial class RemoteControlJsonContext : JsonSerializerContext
{
}
