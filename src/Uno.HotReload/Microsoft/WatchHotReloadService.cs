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
/// twin only differs by taking the capabilities at <c>StartSessionAsync</c> (instead of the
/// constructor) and an explicit <c>commitUpdates</c> flag on emit (Watch always committed ready
/// updates — passing <c>true</c> preserves that behavior).
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

				if (hotReloadServiceType.GetMethod(nameof(StartSessionAsync)) is { } startSessionAsyncMethod)
				{
					// Bind strongly so a signature drift on a future Roslyn bump fails here, at
					// session creation, instead of surfacing as a mid-session invocation error.
					var startSessionAsync = (Func<Solution, ImmutableArray<string>, CancellationToken, Task>)startSessionAsyncMethod
						.CreateDelegate(typeof(Func<Solution, ImmutableArray<string>, CancellationToken, Task>), _targetInstance);
					var capabilities = ImmutableArray<string>.Empty.AddRange(metadataUpdateCapabilities);

					_startSessionAsync = (s, ct) => startSessionAsync(s, capabilities, ct);
				}
				else
				{
					throw new InvalidOperationException($"Cannot find {nameof(StartSessionAsync)}");
				}

				if (hotReloadServiceType.GetMethod(nameof(EmitSolutionUpdateAsync)) is { } emitSolutionUpdateAsyncMethod)
				{
					_emitSolutionUpdateAsync = async (s, ct) =>
					{
						// commitUpdates: true == the historical Watch behavior (the EnC service
						// commits the emitted solution update when its status is Ready, making it
						// the baseline of the next emit).
						var r = emitSolutionUpdateAsyncMethod.Invoke(_targetInstance, new object[] { s, true, ct });

						if (r is Task t)
						{
							await t.ConfigureAwait(false);

							var resultPropertyInfo = r.GetType().GetProperty("Result")
								?? throw new InvalidOperationException($"Unable to find Result property on [{r}]");

							var value = resultPropertyInfo.GetValue(r, null);

							if (value is ITuple tuple)
							{
								return tuple;
							}
						}

						throw new InvalidOperationException();
					};
				}
				else
				{
					throw new InvalidOperationException($"Cannot find {nameof(EmitSolutionUpdateAsync)}");
				}

				if (hotReloadServiceType.GetMethod(nameof(EndSession)) is { } endSessionMethod)
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
