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
		private readonly object _service;
		private readonly object _encService;
		private readonly EngineShape _shape;

		private EnCEngine(object service, object encService, EngineShape shape)
		{
			_service = service;
			_encService = encService;
			_shape = shape;
		}

		/// <summary>
		/// Members of the engine that are present but whose shape this shim cannot read, so the
		/// corresponding <see cref="HotReloadEmitResult"/> values stay empty. Absences that are
		/// simply the older Roslyn line not reporting something are NOT listed — those are expected
		/// and documented on the result itself; these are the ones worth looking at.
		/// </summary>
		internal ImmutableArray<string> ShapeWarnings => _shape.Warnings;

		internal static EnCEngine Create(object service)
		{
			var serviceType = service.GetType();
			var encService = EngineShape.RequireField(serviceType, "_encService").GetValue(service)
				?? throw new InvalidOperationException($"{serviceType.Name}._encService is null.");

			return new EnCEngine(service, encService, EngineShape.For(serviceType, encService.GetType()));
		}

		internal async Task<HotReloadEmitResult> EmitAsync(Solution solution, CancellationToken ct)
		{
			var sessionId = _shape.SessionId.GetValue(_service)
				?? throw new InvalidOperationException("The Edit-and-Continue session has not been started.");

			// ValueTask<EmitSolutionUpdateResults>, not Task: unwrap before awaiting.
			var valueTask = _shape.Emit.Invoke(_encService, [sessionId, solution, _shape.NoRunningProjects, _shape.SpanProvider, ct])
				?? throw new InvalidOperationException("EmitSolutionUpdateAsync returned null.");
			var task = (Task)_shape.AsTask.Invoke(valueTask, null)!;
			await task.ConfigureAwait(false);
			var results = _shape.TaskResult.GetValue(task)
				?? throw new InvalidOperationException("EmitSolutionUpdateAsync produced no result.");

			var moduleUpdates = _shape.ModuleUpdates.GetValue(results)!;
			var rawStatus = _shape.Status.GetValue(moduleUpdates)?.ToString();
			var diagnostics = (ImmutableArray<Diagnostic>)_shape.GetAllDiagnostics.Invoke(results, null)!;

			var emitted = ReadUpdates((IEnumerable)_shape.Updates.GetValue(moduleUpdates)!);

			// An emit carrying errors hands out no delta (the wrapper's contract, preserved).
			var deltas = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
				? ImmutableArray<Update>.Empty
				: emitted;

			// THE POLICY. Commit exactly when deltas are handed to the application, so the baseline
			// only ever advances to code that is actually running. Roslyn stores a pending update
			// only when the status is Ready (that is the condition the wrapper itself branches on),
			// and leaving one pending makes EndSession throw "Pending update has not been committed
			// or discarded" — so the Ready case must always resolve to one or the other.
			if (string.Equals(rawStatus, "Ready", StringComparison.Ordinal))
			{
				(deltas.IsEmpty ? _shape.Discard : _shape.Commit).Invoke(_encService, [sessionId]);
			}

			return new HotReloadEmitResult
			{
				Status = rawStatus switch
				{
					"None" => HotReloadEmitStatus.NoChanges,
					"Ready" => HotReloadEmitStatus.Ready,
					"Blocked" => HotReloadEmitStatus.Blocked,
					"RestartRequired" => HotReloadEmitStatus.RestartRequired,
					_ => HotReloadEmitStatus.Unknown,
				},
				Deltas = deltas,
				Diagnostics = diagnostics,
				PersistentDiagnostics = ReadFlatDiagnostics(_shape.GetPersistentDiagnostics, results),
				TransientDiagnostics = ReadPairedDiagnostics(results),
				SyntaxError = _shape.SyntaxError?.GetValue(results) as Diagnostic,
				ProjectsToRebuild = ReadProjectNames(solution, _shape.ProjectsToRebuild, results),
				ProjectsToRestart = ReadProjectNames(solution, _shape.ProjectsToRestart, results),
				ProjectsToRedeploy = ReadProjectNames(solution, _shape.ProjectsToRedeploy, results),
			};
		}

		private static ImmutableArray<Diagnostic> ReadFlatDiagnostics(MethodInfo? accessor, object results)
			=> accessor?.Invoke(results, null) is ImmutableArray<Diagnostic> diagnostics
				? diagnostics
				: ImmutableArray<Diagnostic>.Empty;

		/// <summary>
		/// <c>GetTransientDiagnostics</c> answers per-project pairs rather than a flat array; the
		/// project is already carried by each diagnostic's location, so the pairs are flattened.
		/// </summary>
		private ImmutableArray<Diagnostic> ReadPairedDiagnostics(object results)
		{
			if (_shape.GetTransientDiagnostics is not { } accessor
				|| _shape.TransientPairDiagnostics is not { } pairDiagnostics
				|| accessor.Invoke(results, null) is not IEnumerable pairs)
			{
				return ImmutableArray<Diagnostic>.Empty;
			}

			return pairs
				.Cast<object>()
				.SelectMany(pair => (ImmutableArray<Diagnostic>)pairDiagnostics.GetValue(pair)!)
				.ToImmutableArray();
		}

		private static ImmutableArray<string> ReadProjectNames(Solution solution, EngineShape.ProjectSet? set, object results)
		{
			if (set is null || set.Value.Property.GetValue(results) is not IEnumerable projects)
			{
				return ImmutableArray<string>.Empty;
			}

			var names = ImmutableArray.CreateBuilder<string>();

			foreach (var entry in projects)
			{
				if (set.Value.ReadId(entry) is { } id
					&& solution.GetProject(id) is { Name: { } projectName }
					&& !names.Contains(projectName))
				{
					names.Add(projectName);
				}
			}

			return names.ToImmutable();
		}

		private ImmutableArray<Update> ReadUpdates(IEnumerable rawUpdates)
		{
			var updates = ImmutableArray.CreateBuilder<Update>();

			foreach (var rawUpdate in rawUpdates)
			{
				updates.Add(new Update(
					(Guid)_shape.UpdateModule.GetValue(rawUpdate)!,
					(ImmutableArray<byte>)_shape.UpdateILDelta.GetValue(rawUpdate)!,
					(ImmutableArray<byte>)_shape.UpdateMetadataDelta.GetValue(rawUpdate)!,
					(ImmutableArray<byte>)_shape.UpdatePdbDelta.GetValue(rawUpdate)!,
					(ImmutableArray<int>)_shape.UpdateUpdatedTypes.GetValue(rawUpdate)!));
			}

			return updates.ToImmutable();
		}
	}

	/// <summary>
	/// Every reflective lookup this shim needs, resolved once and validated against the type it is
	/// expected to have.
	/// </summary>
	/// <remarks>
	/// <para>Resolution is per-process, not per-session: each member below is type-level, and the
	/// loaded Roslyn cannot change once the assembly is loaded. Doing it per emit — as the first
	/// version of this did — pays a member lookup on every keystroke-driven hot reload for a result
	/// that is constant for the lifetime of the host.</para>
	/// <para>Nothing waits for a first emit: the result type is read off the emit method's own return
	/// type (<c>ValueTask&lt;EmitSolutionUpdateResults&gt;</c>), and every other type follows from
	/// it, so a Roslyn bump that changes any shape fails at session creation with the member named
	/// rather than mid-session.</para>
	/// </remarks>
	private sealed class EngineShape
	{
		private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly object _gate = new();
		private static EngineShape? _cached;

		private readonly Type _serviceType;
		private readonly Type _encServiceType;

		/// <summary>A project set of the engine, plus how to read a project out of one of its entries.</summary>
		internal readonly struct ProjectSet(PropertyInfo property, Func<object?, ProjectId?> readId)
		{
			internal PropertyInfo Property { get; } = property;

			/// <summary>
			/// The entries are <see cref="ProjectId"/> directly on some sets and keyed pairs on others
			/// (<c>ProjectsToRestart</c> is an array on Roslyn 4.x and a dictionary on 5.x), so which
			/// one it is gets decided once, here, rather than sniffed per entry.
			/// </summary>
			internal Func<object?, ProjectId?> ReadId { get; } = readId;
		}

		internal static EngineShape For(Type serviceType, Type encServiceType)
		{
			lock (_gate)
			{
				// The type check is not paranoia about Roslyn changing mid-process — it cannot — but
				// about WHICH Roslyn: the dev-server hosts a collectible AssemblyLoadContext per
				// connected application (spec 053 R4). This static lives in whichever context loaded
				// this assembly, so it is naturally scoped to that context's Roslyn and unloads with
				// it; should a host ever place the two on different sides of that boundary, rebuilding
				// beats handing back members belonging to another version.
				if (_cached is not { } cached || cached._serviceType != serviceType || cached._encServiceType != encServiceType)
				{
					_cached = cached = new EngineShape(serviceType, encServiceType);
				}

				return cached;
			}
		}

		private EngineShape(Type serviceType, Type encServiceType)
		{
			_serviceType = serviceType;
			_encServiceType = encServiceType;

			var warnings = ImmutableArray.CreateBuilder<string>();

			SessionId = RequireField(serviceType, "_sessionId");
			SpanProvider = RequireField(serviceType, "s_solutionActiveStatementSpanProvider").GetValue(null)
				?? throw Missing(serviceType, "s_solutionActiveStatementSpanProvider", "is null");

			Emit = encServiceType
				.GetMethods(AnyInstance)
				.SingleOrDefault(m => m.Name == "EmitSolutionUpdateAsync")
				?? throw Missing(encServiceType, "EmitSolutionUpdateAsync", "was not found, or is overloaded");
			Commit = RequireMethod(encServiceType, "CommitSolutionUpdate", parameterCount: 1);
			Discard = RequireMethod(encServiceType, "DiscardSolutionUpdate", parameterCount: 1);

			// The running-projects parameter changed shape between the lines: 4.x takes
			// IImmutableSet<ProjectId>, 5.x an ImmutableDictionary<ProjectId, RunningProjectOptions>.
			// Neither matters here — this host never asks for restart-on-no-effect — so an empty
			// instance of whichever the loaded Roslyn wants is enough.
			var runningProjectsType = Emit.GetParameters() is { Length: 5 } parameters
				? parameters[2].ParameterType
				: throw Missing(encServiceType, "EmitSolutionUpdateAsync", $"takes {Emit.GetParameters().Length} parameters where 5 were expected");
			var concreteRunningProjects = runningProjectsType.IsInterface
				? typeof(ImmutableHashSet<>).MakeGenericType(runningProjectsType.GetGenericArguments())
				: runningProjectsType;
			NoRunningProjects =
				concreteRunningProjects.GetField("Empty", AnyStatic)?.GetValue(null)
				?? concreteRunningProjects.GetProperty("Empty", AnyStatic)?.GetValue(null)
				?? throw Missing(concreteRunningProjects, "Empty", "was not found");

			// ValueTask<EmitSolutionUpdateResults> -> the result type, and everything under it.
			var valueTaskType = Emit.ReturnType;
			AsTask = RequireMethod(valueTaskType, "AsTask", parameterCount: 0);
			var resultsType = valueTaskType.IsGenericType
				? valueTaskType.GetGenericArguments()[0]
				: throw Missing(valueTaskType, "ValueTask<T>", "is not generic, so the result type cannot be read from it");
			TaskResult = RequireProperty(typeof(Task<>).MakeGenericType(resultsType), "Result", resultsType);

			ModuleUpdates = RequireProperty(resultsType, "ModuleUpdates");
			Status = RequireProperty(ModuleUpdates.PropertyType, "Status");
			Updates = RequireProperty(ModuleUpdates.PropertyType, "Updates");
			GetAllDiagnostics = RequireMethod(resultsType, "GetAllDiagnostics", parameterCount: 0, typeof(ImmutableArray<Diagnostic>));

			var updateType = Updates.PropertyType.IsGenericType
				? Updates.PropertyType.GetGenericArguments()[0]
				: throw Missing(Updates.PropertyType, "ImmutableArray<T>", "is not generic, so the update type cannot be read from it");
			UpdateModule = RequireProperty(updateType, "Module", typeof(Guid));
			UpdateILDelta = RequireProperty(updateType, nameof(Update.ILDelta), typeof(ImmutableArray<byte>));
			UpdateMetadataDelta = RequireProperty(updateType, nameof(Update.MetadataDelta), typeof(ImmutableArray<byte>));
			UpdatePdbDelta = RequireProperty(updateType, nameof(Update.PdbDelta), typeof(ImmutableArray<byte>));
			UpdateUpdatedTypes = RequireProperty(updateType, nameof(Update.UpdatedTypes), typeof(ImmutableArray<int>));

			// Optional from here on: a member the running line simply does not report leaves the
			// corresponding result empty and is not worth a word — that is expected, and documented
			// on HotReloadEmitResult. A member that IS there but cannot be read is worth a word.
			SyntaxError = OptionalProperty(resultsType, "SyntaxError", typeof(Diagnostic), warnings);
			GetPersistentDiagnostics = OptionalMethod(resultsType, "GetPersistentDiagnostics", typeof(ImmutableArray<Diagnostic>), warnings);

			if (OptionalMethod(resultsType, "GetTransientDiagnostics", expectedReturnType: null, warnings) is { } transient)
			{
				var pairType = transient.ReturnType.IsGenericType
					? transient.ReturnType.GetGenericArguments()[0]
					: null;

				// ValueTuple's Item2 is a FIELD, not a property.
				TransientPairDiagnostics = pairType?.GetField("Item2", AnyInstance) is { } item2
					&& item2.FieldType == typeof(ImmutableArray<Diagnostic>)
						? item2
						: null;

				if (TransientPairDiagnostics is null)
				{
					warnings.Add(Unreadable(resultsType, "GetTransientDiagnostics", $"answers {transient.ReturnType} whose entries carry no 'Item2' of type ImmutableArray<Diagnostic>"));
				}
				else
				{
					GetTransientDiagnostics = transient;
				}
			}

			ProjectsToRebuild = OptionalProjectSet(resultsType, "ProjectsToRebuild", warnings);
			ProjectsToRestart = OptionalProjectSet(resultsType, "ProjectsToRestart", warnings);
			ProjectsToRedeploy = OptionalProjectSet(resultsType, "ProjectsToRedeploy", warnings);

			Warnings = warnings.ToImmutable();
		}

		internal ImmutableArray<string> Warnings { get; }

		internal FieldInfo SessionId { get; }
		internal object SpanProvider { get; }
		internal object NoRunningProjects { get; }
		internal MethodInfo Emit { get; }
		internal MethodInfo AsTask { get; }
		internal PropertyInfo TaskResult { get; }
		internal MethodInfo Commit { get; }
		internal MethodInfo Discard { get; }
		internal PropertyInfo ModuleUpdates { get; }
		internal PropertyInfo Status { get; }
		internal PropertyInfo Updates { get; }
		internal MethodInfo GetAllDiagnostics { get; }
		internal PropertyInfo UpdateModule { get; }
		internal PropertyInfo UpdateILDelta { get; }
		internal PropertyInfo UpdateMetadataDelta { get; }
		internal PropertyInfo UpdatePdbDelta { get; }
		internal PropertyInfo UpdateUpdatedTypes { get; }
		internal PropertyInfo? SyntaxError { get; }
		internal MethodInfo? GetPersistentDiagnostics { get; }
		internal MethodInfo? GetTransientDiagnostics { get; }
		internal FieldInfo? TransientPairDiagnostics { get; }
		internal ProjectSet? ProjectsToRebuild { get; }
		internal ProjectSet? ProjectsToRestart { get; }
		internal ProjectSet? ProjectsToRedeploy { get; }

		internal static FieldInfo RequireField(Type type, string name)
			=> type.GetField(name, AnyInstance | AnyStatic)
				?? throw Missing(type, name, "was not found");

		private static PropertyInfo RequireProperty(Type type, string name, Type? expectedType = null)
		{
			var property = type.GetProperty(name, AnyInstance)
				?? throw Missing(type, name, "was not found");

			return expectedType is null || expectedType.IsAssignableFrom(property.PropertyType)
				? property
				: throw Missing(type, name, $"is of type {property.PropertyType} where {expectedType} was expected");
		}

		private static MethodInfo RequireMethod(Type type, string name, int parameterCount, Type? expectedReturnType = null)
		{
			var method = type.GetMethods(AnyInstance)
				.SingleOrDefault(m => m.Name == name && m.GetParameters().Length == parameterCount)
				?? throw Missing(type, name, $"taking {parameterCount} parameter(s) was not found");

			return expectedReturnType is null || expectedReturnType.IsAssignableFrom(method.ReturnType)
				? method
				: throw Missing(type, name, $"returns {method.ReturnType} where {expectedReturnType} was expected");
		}

		private static PropertyInfo? OptionalProperty(Type type, string name, Type expectedType, ImmutableArray<string>.Builder warnings)
		{
			if (type.GetProperty(name, AnyInstance) is not { } property)
			{
				return null;
			}

			if (expectedType.IsAssignableFrom(property.PropertyType))
			{
				return property;
			}

			warnings.Add(Unreadable(type, name, $"is of type {property.PropertyType} where {expectedType} was expected"));
			return null;
		}

		private static MethodInfo? OptionalMethod(Type type, string name, Type? expectedReturnType, ImmutableArray<string>.Builder warnings)
		{
			if (type.GetMethods(AnyInstance).SingleOrDefault(m => m.Name == name && m.GetParameters().Length == 0) is not { } method)
			{
				return null;
			}

			if (expectedReturnType is null || expectedReturnType.IsAssignableFrom(method.ReturnType))
			{
				return method;
			}

			warnings.Add(Unreadable(type, name, $"returns {method.ReturnType} where {expectedReturnType} was expected"));
			return null;
		}

		private static ProjectSet? OptionalProjectSet(Type type, string name, ImmutableArray<string>.Builder warnings)
		{
			if (type.GetProperty(name, AnyInstance) is not { } property)
			{
				return null;
			}

			if (!typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
			{
				warnings.Add(Unreadable(type, name, $"is of type {property.PropertyType}, which cannot be enumerated"));
				return null;
			}

			var entryType = property.PropertyType.IsGenericType
				? property.PropertyType.GetGenericArguments()[0]
				: null;

			if (entryType == typeof(ProjectId))
			{
				return new ProjectSet(property, static entry => entry as ProjectId);
			}

			// A dictionary keyed by project: 5.x reports ProjectsToRestart that way. The entries are
			// KeyValuePair<ProjectId, …>, so the key is the project.
			if (entryType?.GetProperty("Key", AnyInstance) is { } key && key.PropertyType == typeof(ProjectId))
			{
				return new ProjectSet(property, entry => entry is null ? null : key.GetValue(entry) as ProjectId);
			}

			warnings.Add(Unreadable(type, name, $"is of type {property.PropertyType}, whose entries are neither a ProjectId nor keyed by one"));
			return null;
		}

		private static InvalidOperationException Missing(Type type, string member, string problem)
			=> new(
				$"Roslyn shape mismatch: {type.FullName}.{member} {problem}. Hot reload cannot drive the " +
				"Edit-and-Continue engine against this version of Microsoft.CodeAnalysis.");

		private static string Unreadable(Type type, string member, string problem)
			=> $"{type.FullName}.{member} {problem}; the corresponding hot-reload information will be reported as empty.";
	}
}
