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
	private readonly Func<Solution, CancellationToken, Task<ITuple>>? _emitSolutionUpdateAsync;
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

				if (hotReloadServiceType.GetMethod(nameof(EmitSolutionUpdateAsync), [typeof(Solution), typeof(bool), typeof(CancellationToken)]) is { } emitSolutionUpdateAsyncMethod)
				{
					// Same fail-fast binding as StartSessionAsync (the method's Task<T> relaxes to
					// Task under delegate variance); only the Result/ITuple decomposition below
					// stays reflective — the tuple's type arguments are internal to Roslyn.
					var emitSolutionUpdateAsync = emitSolutionUpdateAsyncMethod
						.CreateDelegate<Func<Solution, bool, CancellationToken, Task>>(_targetInstance);

					_emitSolutionUpdateAsync = async (s, ct) =>
					{
						// commitUpdates: true == the historical Watch behavior (the EnC service
						// commits the emitted solution update when its status is Ready, making it
						// the baseline of the next emit).
						var task = emitSolutionUpdateAsync(s, true, ct);

						await task.ConfigureAwait(false);

						var resultPropertyInfo = task.GetType().GetProperty("Result")
							?? throw new InvalidOperationException($"Unable to find Result property on [{task}]");

						var value = resultPropertyInfo.GetValue(task, null);

						if (value is ITuple tuple)
						{
							return tuple;
						}

						throw new InvalidOperationException(
							$"Expected {nameof(EmitSolutionUpdateAsync)} result to be ITuple but got [{value?.GetType().FullName ?? "null"}].");
					};
				}
				else
				{
					throw new InvalidOperationException($"Cannot find {nameof(EmitSolutionUpdateAsync)}");
				}

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
	/// Grants, on top of the capabilities reported by the app's runtime, the one dotnet-watch
	/// treats as implicitly available on every runtime hot reload targets:
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
		=> ImmutableArray<string>.Empty.AddRange(metadataUpdateCapabilities).Add("AddExplicitInterfaceImplementation");

	internal Task StartSessionAsync(Solution currentSolution, CancellationToken cancellationToken)
	{
		if (_startSessionAsync is null)
		{
			throw new InvalidOperationException($"_startSessionAsync cannot be null");
		}

		return _startSessionAsync(currentSolution, cancellationToken);
	}

	public async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
	{
		if (_emitSolutionUpdateAsync is null)
		{
			throw new InvalidOperationException($"_emitSolutionUpdateAsync cannot be null");
		}

		var ret = await _emitSolutionUpdateAsync(solution, cancellationToken).ConfigureAwait(false);

		var updatesSource = (IEnumerable)ret[0]!;
		var diagnostics = (ImmutableArray<Diagnostic>)ret[1]!;

		var builder = ImmutableArray<Update>.Empty.ToBuilder();
		foreach (var updateSource in updatesSource)
		{
			var updateType = updateSource.GetType();

			var update = new Update(
				(Guid)GetField(updateType, nameof(Update.ModuleId)).GetValue(updateSource)!
				, (ImmutableArray<byte>)GetField(updateType, nameof(Update.ILDelta)).GetValue(updateSource)!
				, (ImmutableArray<byte>)GetField(updateType, nameof(Update.MetadataDelta)).GetValue(updateSource)!
				, (ImmutableArray<byte>)GetField(updateType, nameof(Update.PdbDelta)).GetValue(updateSource)!
				, (ImmutableArray<int>)GetField(updateType, nameof(Update.UpdatedTypes)).GetValue(updateSource)!
			);

			builder.Add(update);
		}

		return (builder.ToImmutable(), diagnostics);

		FieldInfo GetField(Type type, string name)
		{
			if (type.GetField(name) is { } moduleIdField)
			{
				return moduleIdField;
			}
			else
			{
				throw new InvalidOperationException($"Failed to find {name}");
			}
		}
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
