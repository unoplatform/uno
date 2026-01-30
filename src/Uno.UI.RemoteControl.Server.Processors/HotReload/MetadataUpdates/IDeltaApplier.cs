using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Uno.HotReload;
using Uno.HotReload.Microsoft;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates
{
	interface IDeltaApplier : IDisposable
	{
		ValueTask InitializeAsync(CancellationToken cancellationToken);

		ValueTask<bool> Apply(string changedFile, ImmutableArray<Update> solutionUpdate, CancellationToken cancellationToken);

		ValueTask ReportDiagnosticsAsync(IEnumerable<string> diagnostics, CancellationToken cancellationToken);
	}
}
