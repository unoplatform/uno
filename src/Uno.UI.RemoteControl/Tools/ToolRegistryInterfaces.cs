#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>Handles a tool invocation. Implementations should be tolerant of (rare) concurrent calls.</summary>
internal delegate ValueTask<ToolResult> ToolHandler(ToolInvocation invocation, CancellationToken ct);

/// <summary>Reads the current content of a resource.</summary>
internal delegate ValueTask<ToolResult> ResourceReader(CancellationToken ct);

/// <summary>The registration face of the registry, used by publishers to declare capabilities.</summary>
internal interface IToolPublisher
{
	/// <summary>
	/// Declares a tool. Dispose the result to remove it. On a duplicate <see cref="ToolDescriptor.Name"/>
	/// the registration is rejected (a warning is logged), the existing tool is left untouched, and a
	/// no-op <see cref="IDisposable"/> is returned whose <see cref="IDisposable.Dispose"/> affects nothing.
	/// </summary>
	IDisposable RegisterTool(ToolDescriptor descriptor, ToolHandler handler, bool runOnUIThread = true);

	/// <summary>
	/// Declares a resource. Use the result to read it and to signal updates. Duplicate
	/// <see cref="ResourceDescriptor.Uri"/> values follow the same no-op rule as <see cref="RegisterTool"/>.
	/// </summary>
	IResourceRegistration RegisterResource(ResourceDescriptor descriptor, ResourceReader reader);
}

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
	ValueTask<ToolResult> InvokeAsync(string toolName, System.Text.Json.Nodes.JsonObject arguments, CancellationToken ct);

	/// <summary>Reads a resource by uri. Unknown uris yield an error <see cref="ToolResult"/>.</summary>
	ValueTask<ToolResult> ReadResourceAsync(string uri, CancellationToken ct);

	/// <summary>Raised when the set of tools or resources changes. Drives re-publication by consumers.</summary>
	event EventHandler? Changed;

	/// <summary>Raised when a registered resource signals an update via <see cref="IResourceRegistration.NotifyUpdated"/>.</summary>
	event EventHandler<ResourceUpdatedEventArgs>? ResourceUpdated;
}

/// <summary>The combined contract implemented by the registry singleton; used by <see cref="ToolRegistry.SetForTesting"/>.</summary>
internal interface IToolRegistry : IToolPublisher, IToolCatalog
{
}

/// <summary>Handle for a registered resource. Signal content changes via <see cref="NotifyUpdated"/>; dispose to remove.</summary>
internal interface IResourceRegistration : IDisposable
{
	/// <summary>Raises <see cref="IToolCatalog.ResourceUpdated"/> for this resource. A no-op once disposed.</summary>
	void NotifyUpdated();
}

/// <summary>
/// Optional seam letting the registry marshal tool invocations onto the UI thread. Supplied by the
/// host (the Remote Control client) which knows the dispatcher; when absent, handlers run inline.
/// </summary>
internal interface IToolDispatcher
{
	/// <summary>Whether the current thread is the UI thread (run inline when true to avoid a re-dispatch/deadlock).</summary>
	bool HasThreadAccess { get; }

	/// <summary>Runs <paramref name="action"/> on the UI thread and returns its result.</summary>
	Task<T> RunAsync<T>(Func<Task<T>> action);
}

internal sealed class ResourceUpdatedEventArgs(string uri) : EventArgs
{
	public string Uri { get; } = uri;
}
