#if NET6_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates
{
	public class WatchHotReloadService
	{
		private Func<Solution, CancellationToken, Task> _startSessionAsync;
		private Func<Solution, CancellationToken, Task<ITuple>> _emitSolutionUpdateAsync;
		private Action _endSession;
		private object _targetInstance;

		public readonly record struct Update
		{
			public readonly Guid ModuleId;
			public readonly ImmutableArray<byte> ILDelta;
			public readonly ImmutableArray<byte> MetadataDelta;
			public readonly ImmutableArray<byte> PdbDelta;

			public Update(Guid moduleId, ImmutableArray<byte> ilDelta, ImmutableArray<byte> metadataDelta, ImmutableArray<byte> pdbDelta)
			{
				ModuleId = moduleId;
				ILDelta = ilDelta;
				MetadataDelta = metadataDelta;
				PdbDelta = pdbDelta;
			}
		}

		public WatchHotReloadService(HostWorkspaceServices services, string[] metadataUpdateCapabilities)
		{
			if (Assembly.Load("Microsoft.CodeAnalysis.Features") is { } featuresAssembly)
			{
				if (featuresAssembly.GetType("Microsoft.CodeAnalysis.ExternalAccess.Watch.Api.WatchHotReloadService", false) is { } watchHotReloadServiceType)
				{
					_targetInstance = Activator.CreateInstance(
						watchHotReloadServiceType,
						services,
						ImmutableArray<string>.Empty.AddRange(metadataUpdateCapabilities));

					if (watchHotReloadServiceType.GetMethod(nameof(StartSessionAsync)) is { } startSessionAsyncMethod)
					{
						_startSessionAsync = (Func<Solution, CancellationToken, Task>)startSessionAsyncMethod
							.CreateDelegate(typeof(Func<Solution, CancellationToken, Task>), _targetInstance);
					}
					else
					{
						throw new InvalidOperationException($"Cannot find {nameof(StartSessionAsync)}");
					}

					if (watchHotReloadServiceType.GetMethod(nameof(EmitSolutionUpdateAsync)) is { } emitSolutionUpdateAsyncMethod)
					{
						_emitSolutionUpdateAsync = async (s, ct) =>
						{
							var r = emitSolutionUpdateAsyncMethod.Invoke(_targetInstance, new object[] { s, ct });

							if (r is Task t)
							{
								await t;

								var value = r.GetType().GetProperty("Result").GetValue(r, null);

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

					if (watchHotReloadServiceType.GetMethod(nameof(EndSession)) is { } endSessionMethod)
					{
						_endSession = (Action)endSessionMethod.CreateDelegate(typeof(Action), _targetInstance);
					}
					else
					{
						throw new InvalidOperationException($"Cannot find {nameof(EmitSolutionUpdateAsync)}");
					}
				}
			}
		}

		internal Task StartSessionAsync(Solution currentSolution, CancellationToken cancellationToken)
			=> _startSessionAsync(currentSolution, cancellationToken);

		public async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
		{
			var ret = await _emitSolutionUpdateAsync(solution, cancellationToken);

			var updatesSource = (IEnumerable)ret[0];
			var diagnostics = (ImmutableArray<Diagnostic>)ret[1];

			var builder = ImmutableArray<Update>.Empty.ToBuilder();
			foreach (var updateSource in updatesSource)
			{
				var updateType = updateSource.GetType();

				var update = new Update(
					(Guid)GetField(updateType, nameof(Update.ModuleId)).GetValue(updateSource)
					, (ImmutableArray<byte>)GetField(updateType, nameof(Update.ILDelta)).GetValue(updateSource)
					, (ImmutableArray<byte>)GetField(updateType, nameof(Update.MetadataDelta)).GetValue(updateSource)
					, (ImmutableArray<byte>)GetField(updateType, nameof(Update.PdbDelta)).GetValue(updateSource)
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

		public void EndSession() => _endSession();
	}
}
#endif
