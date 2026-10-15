#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
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
	/// Drives Roslyn's Edit-and-Continue engine directly, so this host decides when an emit becomes
	/// the next baseline: <c>UnitTestingHotReloadService</c> commits on <c>Ready</c>, which Roslyn 5.x
	/// also answers for a rejected rude edit.
	/// </summary>
	/// <remarks>
	/// Reflection because the public replacement, <c>ExternalAccess.HotReload.HotReloadService</c>,
	/// ships in no released package yet; move to it when it does (#25506).
	/// </remarks>
	private sealed class EnCEngine
	{
		private const BindingFlags Unwrapped = BindingFlags.DoNotWrapExceptions;

		private readonly object _service;
		private readonly object _encService;
		private readonly EngineShape _shape;

		private EnCEngine(object service, object encService, EngineShape shape)
		{
			_service = service;
			_encService = encService;
			_shape = shape;
		}

		internal static EnCEngine Create(object service)
		{
			var serviceType = service.GetType();
			var encService = EngineShape.RequireField(serviceType, "_encService").GetValue(service)
				?? throw new InvalidOperationException($"{serviceType.Name}._encService is null.");

			return new EnCEngine(service, encService, new EngineShape(serviceType, encService.GetType()));
		}

		internal async Task<HotReloadEmitResult> EmitAsync(Solution solution, CancellationToken ct)
		{
			// DebuggingSessionId is a struct: boxed, never null.
			var sessionId = _shape.SessionId.GetValue(_service)!;

			// ValueTask<EmitSolutionUpdateResults>, not Task: unwrap before awaiting.
			var valueTask = Invoke(_shape.Emit, _encService, [sessionId, solution, _shape.NoRunningProjects, _shape.SpanProvider, ct])!;
			var task = (Task)Invoke(_shape.AsTask, valueTask, [])!;
			await task.ConfigureAwait(false);
			var results = _shape.TaskResult.GetValue(task)!;

			var moduleUpdates = _shape.ModuleUpdates.GetValue(results)!;
			var rawStatus = _shape.Status.GetValue(moduleUpdates)!.ToString()!;

			// Roslyn holds a pending update exactly when the status is Ready, and EndSession throws while
			// one is pending, so a Ready emit must always end in a commit or a discard — even if reading
			// its results fails.
			var pending = rawStatus == "Ready";

			HotReloadEmitResult result;
			try
			{
				result = Read(solution, results, moduleUpdates, rawStatus);
			}
			catch when (pending)
			{
				Invoke(_shape.Discard, _encService, [sessionId]);
				throw;
			}

			// The baseline advances only to an emit whose deltas are handed out.
			if (pending)
			{
				Invoke(result.Deltas.IsEmpty ? _shape.Discard : _shape.Commit, _encService, [sessionId]);
			}

			return result;
		}

		private HotReloadEmitResult Read(Solution solution, object results, object moduleUpdates, string rawStatus)
		{
			var diagnostics = (ImmutableArray<Diagnostic>)Invoke(_shape.GetAllDiagnostics, results, [])!;
			var projectsToRebuild = ReadProjectNames(solution, _shape.ProjectsToRebuild, results);
			var projectsToRestart = ReadProjectNames(solution, _shape.ProjectsToRestart, results);

			var applicable = !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
				&& projectsToRebuild.IsEmpty
				&& projectsToRestart.IsEmpty;

			return new HotReloadEmitResult
			{
				Status = rawStatus switch
				{
					"None" => HotReloadEmitStatus.NoChanges,
					"Ready" => HotReloadEmitStatus.Ready,
					"Blocked" => HotReloadEmitStatus.Blocked,
					"RestartRequired" => HotReloadEmitStatus.RestartRequired,
					_ => throw new InvalidOperationException($"Unknown Edit-and-Continue status '{rawStatus}'."),
				},
				Deltas = applicable
					? ReadUpdates((IEnumerable)_shape.Updates.GetValue(moduleUpdates)!)
					: ImmutableArray<Update>.Empty,
				Diagnostics = diagnostics,
				ProjectsToRebuild = projectsToRebuild,
				ProjectsToRestart = projectsToRestart,
			};
		}

		private static object? Invoke(MethodInfo method, object target, object?[] arguments)
			=> method.Invoke(target, Unwrapped, binder: null, arguments, culture: null);

		private static ImmutableArray<string> ReadProjectNames(Solution solution, EngineShape.ProjectSet set, object results)
		{
			var names = ImmutableArray.CreateBuilder<string>();

			foreach (var entry in (IEnumerable)set.Property.GetValue(results)!)
			{
				if (set.ReadId(entry) is { } id
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
	/// Every reflective lookup <see cref="EnCEngine"/> needs, resolved once per session from the emit
	/// method's own signature, so a Roslyn bump that moves a member fails at session creation, naming it.
	/// </summary>
	internal sealed class EngineShape
	{
		private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>A project set of the engine, plus how to read a project out of one of its entries.</summary>
		internal readonly struct ProjectSet(PropertyInfo property, Func<object?, ProjectId?> readId)
		{
			internal PropertyInfo Property { get; } = property;

			internal Func<object?, ProjectId?> ReadId { get; } = readId;
		}

		internal EngineShape(Type serviceType, Type encServiceType)
		{
			SessionId = RequireField(serviceType, "_sessionId");
			SpanProvider = RequireField(serviceType, "s_solutionActiveStatementSpanProvider").GetValue(null)
				?? throw Missing(serviceType, "s_solutionActiveStatementSpanProvider", "is null");

			Emit = RequireMethod(encServiceType, "EmitSolutionUpdateAsync", parameterCount: 5);
			Commit = RequireMethod(encServiceType, "CommitSolutionUpdate", parameterCount: 1);
			Discard = RequireMethod(encServiceType, "DiscardSolutionUpdate", parameterCount: 1);

			// 4.x takes an IImmutableSet<ProjectId>, 5.x an ImmutableDictionary<ProjectId, …>; this host
			// runs no project the engine could restart, so an empty one of either will do.
			var runningProjectsType = Emit.GetParameters()[2].ParameterType;
			var concreteRunningProjects = runningProjectsType.IsInterface
				? typeof(ImmutableHashSet<>).MakeGenericType(runningProjectsType.GetGenericArguments())
				: runningProjectsType;
			NoRunningProjects =
				concreteRunningProjects.GetField("Empty", AnyStatic)?.GetValue(null)
				?? concreteRunningProjects.GetProperty("Empty", AnyStatic)?.GetValue(null)
				?? throw Missing(concreteRunningProjects, "Empty", "was not found");

			var valueTaskType = Emit.ReturnType;
			AsTask = RequireMethod(valueTaskType, "AsTask", parameterCount: 0);
			var resultsType = valueTaskType.IsGenericType
				? valueTaskType.GetGenericArguments()[0]
				: throw Missing(valueTaskType, "ValueTask<T>", "is not generic, so the result type cannot be read from it");
			TaskResult = RequireProperty(typeof(Task<>).MakeGenericType(resultsType), "Result", resultsType);

			ModuleUpdates = RequireProperty(resultsType, "ModuleUpdates");
			Status = RequireProperty(ModuleUpdates.PropertyType, "Status");
			if (!Status.PropertyType.IsEnum || !Enum.IsDefined(Status.PropertyType, "Ready"))
			{
				// The commit policy hinges on this value.
				throw Missing(ModuleUpdates.PropertyType, "Status", $"is of type {Status.PropertyType}, which has no 'Ready' value");
			}

			Updates = RequireProperty(ModuleUpdates.PropertyType, "Updates");
			GetAllDiagnostics = RequireMethod(resultsType, "GetAllDiagnostics", parameterCount: 0, typeof(ImmutableArray<Diagnostic>));
			ProjectsToRebuild = RequireProjectSet(resultsType, "ProjectsToRebuild");
			ProjectsToRestart = RequireProjectSet(resultsType, "ProjectsToRestart");

			var updateType = Updates.PropertyType.IsGenericType
				? Updates.PropertyType.GetGenericArguments()[0]
				: throw Missing(Updates.PropertyType, "ImmutableArray<T>", "is not generic, so the update type cannot be read from it");
			UpdateModule = RequireProperty(updateType, "Module", typeof(Guid));
			UpdateILDelta = RequireProperty(updateType, nameof(Update.ILDelta), typeof(ImmutableArray<byte>));
			UpdateMetadataDelta = RequireProperty(updateType, nameof(Update.MetadataDelta), typeof(ImmutableArray<byte>));
			UpdatePdbDelta = RequireProperty(updateType, nameof(Update.PdbDelta), typeof(ImmutableArray<byte>));
			UpdateUpdatedTypes = RequireProperty(updateType, nameof(Update.UpdatedTypes), typeof(ImmutableArray<int>));
		}

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
		internal ProjectSet ProjectsToRebuild { get; }
		internal ProjectSet ProjectsToRestart { get; }
		internal PropertyInfo UpdateModule { get; }
		internal PropertyInfo UpdateILDelta { get; }
		internal PropertyInfo UpdateMetadataDelta { get; }
		internal PropertyInfo UpdatePdbDelta { get; }
		internal PropertyInfo UpdateUpdatedTypes { get; }

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

		/// <remarks>
		/// <c>ProjectsToRestart</c> holds <see cref="ProjectId"/>s on 4.x and is keyed by one on 5.x.
		/// </remarks>
		internal static ProjectSet RequireProjectSet(Type type, string name)
		{
			var property = RequireProperty(type, name, typeof(IEnumerable));
			var entryType = property.PropertyType.GetInterfaces()
				.Append(property.PropertyType)
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				?.GetGenericArguments()[0];

			if (entryType == typeof(ProjectId))
			{
				return new ProjectSet(property, static entry => entry as ProjectId);
			}

			if (entryType?.GetProperty("Key", AnyInstance) is { } key && key.PropertyType == typeof(ProjectId))
			{
				return new ProjectSet(property, entry => entry is null ? null : key.GetValue(entry) as ProjectId);
			}

			throw Missing(type, name, $"is of type {property.PropertyType}, whose entries are neither a ProjectId nor keyed by one");
		}

		private static InvalidOperationException Missing(Type type, string member, string problem)
			=> new(
				$"Roslyn shape mismatch: {type.FullName}.{member} {problem}. Hot reload cannot drive the " +
				"Edit-and-Continue engine against this version of Microsoft.CodeAnalysis.");
	}
}
