#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>Handles a tool invocation. Implementations should be tolerant of (rare) concurrent calls.</summary>
internal delegate ValueTask<ToolResult> ToolHandler(ToolInvocation invocation, CancellationToken ct);
