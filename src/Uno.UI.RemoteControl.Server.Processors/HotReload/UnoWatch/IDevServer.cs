using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.HotReload;

namespace Uno.DotNet.Watch;

/// <summary>
/// Contract shared between the Dev Server and Uno.Watch.
/// This represents the API surface exposed by the Dev Server that Uno.Watch can call into.
/// </summary>
internal interface IDevServer
{
	Task<ApplyStatus> ApplyManagedCodeUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken);

	Task<ApplyStatus> ApplyStaticAssetUpdatesAsync(ImmutableArray<HotReloadStaticAssetUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken);
}

/// <summary>
/// Contract shared between the Dev Server and Uno.Watch.
/// This represents the API surface exposed by Uno.Watch that the Dev Server can call into.
/// </summary>
internal interface IUnoWatch
{
	
}
