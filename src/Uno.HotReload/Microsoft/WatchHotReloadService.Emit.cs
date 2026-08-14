#nullable enable

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Microsoft;

internal partial class WatchHotReloadService
{
	/// <summary>
	/// Drives Roslyn's Edit-and-Continue engine directly instead of going through
	/// <c>UnitTestingHotReloadService.EmitSolutionUpdateAsync</c>, so that THIS host decides whether
	/// an emit becomes the baseline of the next one — and gets to see the projects the engine says
	/// cannot be hot-reloaded.
	/// </summary>
	/// <remarks>
	/// <para>Why the wrapper cannot be used for this: on the Roslyn 5.x line a rude edit no longer
	/// makes the emit non-Ready. 4.x answered <c>ModuleUpdateStatus.RestartRequired</c>, so the
	/// wrapper's <c>if (Status == Ready)</c> was false and nothing was committed; 5.x answers
	/// <c>Ready</c> with ZERO updates and moves the project into <c>ProjectsToRebuild</c>, so that
	/// same condition is now TRUE and the wrapper commits a baseline for an update the application
	/// never received. The session then believes the rejected source is the running code: submitting
	/// the same rude edit again reports nothing at all, and reverting it looks like a fresh rude
	/// edit. Measured on 4.14 vs 5.6 from identical sources — see
	/// <c>Given_WatchHotReloadService_RudeEditRecovery</c>.</para>
	/// <para>The wrapper also returns only <c>(updates, diagnostics)</c>, dropping
	/// <c>ProjectsToRebuild</c>/<c>ProjectsToRestart</c> — which is how 5.x says "this project cannot
	/// be updated". A host reading just those two cannot tell a clean cycle from a rejected one once
	/// the diagnostic has been reported the first time.</para>
	/// <para>Roslyn's own replacement, <c>ExternalAccess.HotReload.HotReloadService</c>, returns those
	/// sets and leaves <c>CommitUpdate</c>/<c>DiscardUpdate</c> to the caller — exactly this contract
	/// — but it ships in no released package yet (5.6.0, the latest, carries only the UnitTesting
	/// twin), hence the reflection. Move to it when it lands.</para>
	/// </remarks>
	private sealed class EnCEngine
	{
		private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private readonly object _service;
		private readonly object _encService;
		private readonly FieldInfo _sessionIdField;
		private readonly MethodInfo _emit;
		private readonly MethodInfo _commit;
		private readonly MethodInfo _discard;
		private readonly object _spanProvider;
		private readonly object _noRunningProjects;

		private EnCEngine(
			object service,
			object encService,
			FieldInfo sessionIdField,
			MethodInfo emit,
			MethodInfo commit,
			MethodInfo discard,
			object spanProvider,
			object noRunningProjects)
		{
			_service = service;
			_encService = encService;
			_sessionIdField = sessionIdField;
			_emit = emit;
			_commit = commit;
			_discard = discard;
			_spanProvider = spanProvider;
			_noRunningProjects = noRunningProjects;
		}

		/// <summary>
		/// Resolves everything up-front so a Roslyn bump that moves a member fails here — at session
		/// creation, with the member named — instead of mid-session.
		/// </summary>
		internal static EnCEngine Create(object service)
		{
			var serviceType = service.GetType();

			var encService = Field(serviceType, "_encService").GetValue(service)
				?? throw new InvalidOperationException($"{serviceType.Name}._encService is null.");
			var spanProvider = Field(serviceType, "s_solutionActiveStatementSpanProvider").GetValue(null)
				?? throw new InvalidOperationException($"{serviceType.Name}.s_solutionActiveStatementSpanProvider is null.");

			var encType = encService.GetType();
			var emit = encType
				.GetMethods(AnyInstance)
				.SingleOrDefault(m => m.Name == "EmitSolutionUpdateAsync")
				?? throw new InvalidOperationException($"Cannot find a single EmitSolutionUpdateAsync on {encType}.");

			// The running-projects parameter changed shape between the lines: 4.x takes
			// IImmutableSet<ProjectId>, 5.x an ImmutableDictionary<ProjectId, RunningProjectOptions>.
			// Neither matters here — this host never asks for restart-on-no-effect — so an empty
			// instance of whichever the loaded Roslyn wants is enough.
			var runningProjectsType = emit.GetParameters()[2].ParameterType;
			var concreteRunningProjects = runningProjectsType.IsInterface
				? typeof(ImmutableHashSet<>).MakeGenericType(runningProjectsType.GetGenericArguments())
				: runningProjectsType;
			var noRunningProjects =
				concreteRunningProjects.GetField("Empty", AnyStatic)?.GetValue(null)
				?? concreteRunningProjects.GetProperty("Empty", AnyStatic)?.GetValue(null)
				?? throw new InvalidOperationException($"Cannot find an empty {concreteRunningProjects}.");

			return new EnCEngine(
				service,
				encService,
				Field(serviceType, "_sessionId"),
				emit,
				Method(encType, "CommitSolutionUpdate"),
				Method(encType, "DiscardSolutionUpdate"),
				spanProvider,
				noRunningProjects);
		}

		internal async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<string> projectsRequiringRebuild)> EmitAsync(
			Solution solution,
			CancellationToken ct)
		{
			var sessionId = _sessionIdField.GetValue(_service)
				?? throw new InvalidOperationException("The Edit-and-Continue session has not been started.");

			// ValueTask<EmitSolutionUpdateResults>, not Task: unwrap before awaiting.
			var valueTask = _emit.Invoke(_encService, [sessionId, solution, _noRunningProjects, _spanProvider, ct])
				?? throw new InvalidOperationException("EmitSolutionUpdateAsync returned null.");
			var task = (Task)Method(valueTask.GetType(), "AsTask").Invoke(valueTask, null)!;
			await task.ConfigureAwait(false);
			var results = Property(task.GetType(), "Result").GetValue(task)
				?? throw new InvalidOperationException("EmitSolutionUpdateAsync produced no result.");

			var resultsType = results.GetType();
			var moduleUpdates = Property(resultsType, "ModuleUpdates").GetValue(results)!;
			var status = Property(moduleUpdates.GetType(), "Status").GetValue(moduleUpdates)?.ToString();
			var diagnostics = (ImmutableArray<Diagnostic>)Method(resultsType, "GetAllDiagnostics").Invoke(results, null)!;

			var emitted = ReadUpdates((IEnumerable)Property(moduleUpdates.GetType(), "Updates").GetValue(moduleUpdates)!);

			// The wrapper's contract, preserved: an emit carrying errors hands out no delta.
			var updates = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
				? ImmutableArray<Update>.Empty
				: emitted;

			// THE POLICY. Commit exactly when deltas are handed to the application, so the baseline
			// only ever advances to code that is actually running. Roslyn stores a pending update
			// only when the status is Ready (that is the condition the wrapper itself branches on),
			// and leaving one pending makes EndSession throw "Pending update has not been committed
			// or discarded" — so the Ready case must always resolve to one or the other.
			if (string.Equals(status, "Ready", StringComparison.Ordinal))
			{
				(updates.IsEmpty ? _discard : _commit).Invoke(_encService, [sessionId]);
			}

			return (updates, diagnostics, ReadBlockedProjects(solution, results, resultsType));
		}

		/// <summary>
		/// Projects the engine says cannot be updated in place. <c>ProjectsToRebuild</c> is an
		/// <c>ImmutableArray&lt;ProjectId&gt;</c> and <c>ProjectsToRestart</c> an array on 4.x but a
		/// dictionary keyed by project on 5.x — enumerating either yields the projects (the entries of
		/// a dictionary being keyed pairs), which is all this needs.
		/// </summary>
		private static ImmutableArray<string> ReadBlockedProjects(Solution solution, object results, Type resultsType)
		{
			var names = ImmutableArray.CreateBuilder<string>();

			foreach (var name in new[] { "ProjectsToRebuild", "ProjectsToRestart" })
			{
				if (resultsType.GetProperty(name, AnyInstance)?.GetValue(results) is not IEnumerable projects)
				{
					continue;
				}

				foreach (var entry in projects)
				{
					var id = entry as ProjectId
						?? entry?.GetType().GetProperty("Key", AnyInstance)?.GetValue(entry) as ProjectId;

					if (id is not null && solution.GetProject(id) is { Name: { } projectName } && !names.Contains(projectName))
					{
						names.Add(projectName);
					}
				}
			}

			return names.ToImmutable();
		}

		private static ImmutableArray<Update> ReadUpdates(IEnumerable rawUpdates)
		{
			var updates = ImmutableArray.CreateBuilder<Update>();

			foreach (var rawUpdate in rawUpdates)
			{
				var type = rawUpdate.GetType();

				updates.Add(new Update(
					(Guid)Property(type, "Module").GetValue(rawUpdate)!,
					(ImmutableArray<byte>)Property(type, nameof(Update.ILDelta)).GetValue(rawUpdate)!,
					(ImmutableArray<byte>)Property(type, nameof(Update.MetadataDelta)).GetValue(rawUpdate)!,
					(ImmutableArray<byte>)Property(type, nameof(Update.PdbDelta)).GetValue(rawUpdate)!,
					(ImmutableArray<int>)Property(type, nameof(Update.UpdatedTypes)).GetValue(rawUpdate)!));
			}

			return updates.ToImmutable();
		}

		private static FieldInfo Field(Type type, string name)
			=> type.GetField(name, AnyInstance | AnyStatic)
				?? throw new InvalidOperationException($"Cannot find field {type.Name}.{name}.");

		private static PropertyInfo Property(Type type, string name)
			=> type.GetProperty(name, AnyInstance)
				?? throw new InvalidOperationException($"Cannot find property {type.Name}.{name}.");

		private static MethodInfo Method(Type type, string name)
			=> type.GetMethod(name, AnyInstance)
				?? throw new InvalidOperationException($"Cannot find method {type.Name}.{name}.");
	}
}
