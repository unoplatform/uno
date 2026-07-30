#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>Reads the current content of a resource.</summary>
internal delegate ValueTask<ToolResult> ResourceReader(CancellationToken ct);
