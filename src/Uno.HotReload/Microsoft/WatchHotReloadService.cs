#nullable enable

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;

namespace Uno.HotReload.Microsoft;

/// <summary>
/// Reflection shim over Roslyn's <c>ExternalAccess.UnitTesting.Api.UnitTestingHotReloadService</c>,
/// the stable EnC session surface across the Roslyn lines we embed (identical shape from 4.14 to
/// 5.6, verified). The historical target, <c>ExternalAccess.Watch.Api.WatchHotReloadService</c>,
/// was removed from Microsoft.CodeAnalysis.Features between Roslyn 5.0 and 5.3; the UnitTesting
/// twin differs by taking the capabilities at <c>StartSessionAsync</c> (instead of the
/// constructor), by an explicit <c>commitUpdates</c> flag on emit (Watch always committed ready
/// updates — passing <c>true</c> preserves that behavior), and by forwarding the capabilities
/// verbatim where Watch implicitly granted runtime-supported-but-undeclared ones (restored by
/// <see cref="AddImplicitCapabilities"/>).
/// </summary>
internal partial class WatchHotReloadService
{
	private readonly Func<Solution, CancellationToken, Task>? _startSessionAsync;
	private readonly EnCEngine? _engine;
	private readonly Action? _endSession;
	private readonly object? _targetInstance;

	public WatchHotReloadService(HostWorkspaceServices services, string[] metadataUpdateCapabilities)
	{
		if (Assembly.Load("Microsoft.CodeAnalysis.Features") is { } featuresAssembly)
		{
			if (featuresAssembly.GetType("Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api.UnitTestingHotReloadService", false) is { } hotReloadServiceType)
			{
				_targetInstance = Activator.CreateInstance(hotReloadServiceType, services);

				// Typed lookups: an overload added by a future Roslyn would make the name-only
				// GetMethod(name) throw AmbiguousMatchException; these keep resolving (or fail
				// with the explicit messages below).
				if (hotReloadServiceType.GetMethod(nameof(StartSessionAsync), [typeof(Solution), typeof(ImmutableArray<string>), typeof(CancellationToken)]) is { } startSessionAsyncMethod)
				{
					// Bind strongly so a signature drift on a future Roslyn bump fails here, at
					// session creation, instead of surfacing as a mid-session invocation error.
					var startSessionAsync = (Func<Solution, ImmutableArray<string>, CancellationToken, Task>)startSessionAsyncMethod
						.CreateDelegate(typeof(Func<Solution, ImmutableArray<string>, CancellationToken, Task>), _targetInstance);
					var capabilities = AddImplicitCapabilities(metadataUpdateCapabilities);

					_startSessionAsync = (s, ct) => startSessionAsync(s, capabilities, ct);
				}
				else
				{
					throw new InvalidOperationException($"Cannot find {nameof(StartSessionAsync)}");
				}

				// NOT hotReloadServiceType.EmitSolutionUpdateAsync: that wrapper decides on its own
				// whether the emit becomes the next baseline, and on the 5.x line it decides wrong
				// for a rude edit. The engine is driven directly instead — see EnCEngine.
				_engine = EnCEngine.Create(_targetInstance
					?? throw new InvalidOperationException($"Failed to create {hotReloadServiceType.Name}."));

				if (hotReloadServiceType.GetMethod(nameof(EndSession), Type.EmptyTypes) is { } endSessionMethod)
				{
#pragma warning disable CA2263
					_endSession = (Action)endSessionMethod.CreateDelegate(typeof(Action), _targetInstance);
#pragma warning restore CA2263
				}
				else
				{
					throw new InvalidOperationException($"Cannot find {nameof(EndSession)}");
				}
			}
			else
			{
				// Historically silent (null service, first use threw a bare "cannot be null"):
				// name the missing type so a future Roslyn bump that moves it again is diagnosable
				// from the session log.
				throw new InvalidOperationException("Cannot find Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api.UnitTestingHotReloadService in Microsoft.CodeAnalysis.Features.");
			}
		}
	}

	/// <summary>
	/// Adjusts the capabilities reported by the app's runtime for the EnC session: grants the
	/// one dotnet-watch treats as implicitly available, and removes one whose runtime-side
	/// implementation is defective (see the remarks and the inline note below). The grant:
	/// <c>AddExplicitInterfaceImplementation</c> is supported by .NET (CoreCLR) and Mono but
	/// DECLARED by neither — only .NET Framework lacks it (adding an InterfaceImpl row there can
	/// crash with an access violation), so Roslyn keeps it out of the runtimes' baseline set and
	/// expects hosts that never target .NET Framework to grant it themselves, "rather than
	/// servicing all of them" (dotnet-watch's own wording).
	/// </summary>
	/// <remarks>
	/// Without this grant, Roslyn (4.10+) refuses ANY update of a reloadable
	/// (<c>[CreateNewOnMetadataUpdate]</c>) type having an explicitly-implemented member — rude
	/// edit ENC0106, plus CS9346 at emit on 5.x — which makes every generated XAML
	/// ResourceDictionary singleton (explicit <c>IXamlResourceDictionaryProvider.GetResourceDictionary()</c>)
	/// non-hot-reloadable. The pre-5.3 shim target, <c>ExternalAccess.Watch.Api.WatchHotReloadService</c>,
	/// made this grant internally, so it never appeared here; <c>UnitTestingHotReloadService</c>
	/// forwards the capabilities verbatim, so the shim now makes it — exactly like dotnet-watch
	/// itself does since Watch's removal:
	/// <list type="bullet">
	/// <item><description>dotnet-watch (current):
	/// https://github.com/dotnet/sdk/blob/2e5fa46b2d150671d77cf313930d4de322907118/src/Dotnet.Watch/HotReloadClient/HotReloadClient.cs#L44-L49</description></item>
	/// <item><description>Roslyn 4.x <c>WatchHotReloadService.AddImplicitDotNetCapabilities</c> (the behavior restored here):
	/// https://github.com/dotnet/roslyn/blob/f7706e3f398fcc7671351dd4d5deb5757e02a0f2/src/Features/Core/Portable/ExternalAccess/Watch/Api/WatchHotReloadService.cs#L131-L137</description></item>
	/// </list>
	/// A runtime that would someday report the capability itself stays harmless: the list is
	/// parsed into flags, duplicates collapse.
	/// </remarks>
	internal static ImmutableArray<string> AddImplicitCapabilities(string[] metadataUpdateCapabilities)
		=> ImmutableArray<string>.Empty.AddRange(metadataUpdateCapabilities)
			.Add("AddExplicitInterfaceImplementation")
			.Remove("AddFieldRva");

	// Why AddFieldRva is REMOVED even though the runtime reports it — CoreCLR (verified on
	// .NET 10.0.10, src/coreclr/md/enc/metamodelrw.cpp) builds a lookup hash for a metadata
	// table once it exceeds INDEX_ROW_COUNT_THRESHOLD (25) rows, `GenericFindWithHash` has no
	// linear fallback, and the EnC delta-apply path never maintains/invalidates that hash — so
	// any row an EnC delta ADDS after the hash was built is invisible to lookups. With the
	// capability granted, Roslyn 5.x emits one FieldRVA row per generation for method bodies
	// containing array initializers / u8 literals (a fresh <PrivateImplementationDetails> per
	// generation): on a baseline with ~22+ FieldRVA rows the table crosses the threshold after
	// a few reloads and `EditAndContinueModule::ApplyEditAndContinue` fails its GetFieldRVA
	// lookup with CLDB_E_RECORD_NOTFOUND — surfaced as "System.InvalidOperationException: The
	// assembly update failed", killing every subsequent update of the session. Roslyn 4.x
	// never emitted FieldRVA deltas (the capability did not exist), which is why this never
	// fired before the 5.x embed. Without the capability, Roslyn falls back to the historical
	// element-wise array-initializer EnC codegen: no FieldRVA rows, correct semantics.
	// Diagnosed with a standalone MetadataUpdater.ApplyUpdate replay of captured deltas and
	// proven both ways on a Checked CoreCLR (failing lookup traced; hash invalidation in the
	// apply path makes the same deltas apply). Remove this once the runtime fix ships.

	internal Task StartSessionAsync(Solution currentSolution, CancellationToken cancellationToken)
	{
		if (_startSessionAsync is null)
		{
			throw new InvalidOperationException($"_startSessionAsync cannot be null");
		}

		return _startSessionAsync(currentSolution, cancellationToken);
	}

	/// <summary>
	/// Emits the deltas between <paramref name="solution"/> and the session baseline, and advances
	/// that baseline only when deltas are actually produced.
	/// </summary>
	/// <returns>
	/// The deltas to apply, the Edit-and-Continue diagnostics, and the names of the projects the
	/// engine reports as un-updatable — a non-empty set means the application has to be rebuilt or
	/// restarted before it matches its sources again.
	/// </returns>
	public Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<string> projectsRequiringRebuild)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
	{
		if (_engine is null)
		{
			throw new InvalidOperationException($"{nameof(_engine)} cannot be null");
		}

		return _engine.EmitAsync(solution, cancellationToken);
	}

	public void EndSession()
	{
		if (_endSession is null)
		{
			throw new InvalidOperationException($"_endSession cannot be null");
		}

		_endSession();
	}
}
