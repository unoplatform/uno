using System;
using System.Linq;
using Uno.UI.RemoteControl.Messaging.HotReload;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Request to the IDE to add use the debugger to hot reload.
/// </summary>
public record HotReloadThruDebuggerIdeMessage(string ModuleId, string MetadataDelta, string IlDelta, string PdbBytes) : IdeMessage(WellKnownScopes.HotReload);
