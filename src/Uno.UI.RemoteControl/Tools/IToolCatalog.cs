#nullable enable

using System;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>The consumption face of the registry, used by consumers to read and invoke.</summary>
internal interface IToolCatalog
{
	/// <summary>A point-in-time snapshot of all registered tools and resources.</summary>
	(ImmutableArray<ToolDescriptor> Tools, ImmutableArray<ResourceDescriptor> Resources) Snapshot();

	/// <summary>
	/// Invokes a tool by name. A failing handler is surfaced as an error <see cref="ToolResult"/>
	/// (never a thrown exception, except <see cref="OperationCanceledException"/>). Marshalling to the
	/// UI thread is handled here when the registration requested it and a dispatcher is wired.
	/// </summary>
	ValueTask<ToolResult> InvokeAsync(string toolName, JsonObject arguments, CancellationToken ct);

	/// <summary>Reads a resource by uri. Unknown uris yield an error <see cref="ToolResult"/>.</summary>
	ValueTask<ToolResult> ReadResourceAsync(string uri, CancellationToken ct);

	/// <summary>Raised when the set of tools or resources changes. Drives re-publication by consumers.</summary>
	event EventHandler? Changed;

	/// <summary>Raised when a registered resource signals an update via <see cref="IResourceRegistration.NotifyUpdated"/>.</summary>
	event EventHandler<ResourceUpdatedEventArgs>? ResourceUpdated;
}
