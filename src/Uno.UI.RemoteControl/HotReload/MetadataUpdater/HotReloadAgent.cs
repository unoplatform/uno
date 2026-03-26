// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on the implementation in https://github.com/dotnet/aspnetcore/blob/26e3dfc7f3f3a91ba445ec0f8b1598d12542fb9f/src/Components/WebAssembly/WebAssembly/src/HotReload/HotReloadAgent.cs

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Uno;
using Uno.UI.Helpers;

namespace Uno.UI.RemoteControl.HotReload.MetadataUpdater;

internal sealed class HotReloadAgent : IDisposable
{
	/// Flags for hot reload handler Types like MVC's HotReloadService.
	private const DynamicallyAccessedMemberTypes HotReloadHandlerLinkerFlags = DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods;

	private readonly Action<string> _log;
	private readonly AssemblyLoadEventHandler _assemblyLoad;
	private readonly AssemblyLoadContext _alc;
	private readonly ConcurrentDictionary<Guid, List<UpdateDelta>> _deltas = new();
	private readonly ConcurrentDictionary<Assembly, Assembly> _appliedAssemblies = new();
	private volatile UpdateHandlerActions? _handlerActions;

	internal const string MetadataUpdaterType = "System.Reflection.Metadata.MetadataUpdater";

	public HotReloadAgent(Action<string> log)
	{
		_log = log;
		_alc = AssemblyLoadContext.GetLoadContext(typeof(HotReloadAgent).Assembly)
			?? AssemblyLoadContext.Default;
		_assemblyLoad = OnAssemblyLoad;
		AppDomain.CurrentDomain.AssemblyLoad += _assemblyLoad;
	}

	private void OnAssemblyLoad(object? _, AssemblyLoadEventArgs eventArgs)
	{
		// Only process assemblies loaded in our own ALC
		if (AssemblyLoadContext.GetLoadContext(eventArgs.LoadedAssembly) != _alc)
		{
			return;
		}

		_handlerActions = null;
		var loadedAssembly = eventArgs.LoadedAssembly;
		var moduleId = TryGetModuleId(loadedAssembly);
		if (moduleId is null)
		{
			return;
		}

		if (_deltas.TryGetValue(moduleId.Value, out var updateDeltas) && _appliedAssemblies.TryAdd(loadedAssembly, loadedAssembly))
		{
			// A delta for this specific Module exists and we haven't called ApplyUpdate on this instance of Assembly as yet.
			ApplyDeltas(loadedAssembly, updateDeltas);
		}
	}

	internal sealed class UpdateHandlerActions
	{
		public List<Action<Type[]?>> ClearCache { get; } = new();
		public List<Action<Type[]?>> UpdateApplication { get; } = new();
	}

	[UnconditionalSuppressMessage("Trimmer", "IL2072",
		Justification = "The handlerType passed to GetHandlerActions is preserved by MetadataUpdateHandlerAttribute with DynamicallyAccessedMemberTypes.All.")]
	internal UpdateHandlerActions GetMetadataUpdateHandlerActions()
	{
		// We need to execute MetadataUpdateHandlers in a well-defined order. For v1, the strategy that is used is to topologically
		// sort assemblies so that handlers in a dependency are executed before the dependent (e.g. the reflection cache action
		// in System.Private.CoreLib is executed before System.Text.Json clears it's own cache.)
		// This would ensure that caches and updates more lower in the application stack are up to date
		// before ones higher in the stack are recomputed.
		//
		// When running in a non-default ALC (e.g. Studio Live inner app), MetadataUpdateHandler
		// attributes live on shared assemblies (Uno.UI) in the default ALC. We must scan both
		// ALCs to discover all handlers, deduplicating by assembly name.
		var sortedAssemblies = TopologicalSort(GetAllRelevantAssemblies());
		var handlerActions = new UpdateHandlerActions();
		foreach (var assembly in sortedAssemblies)
		{
			try
			{
				foreach (var attr in assembly.GetCustomAttributesData())
				{
					// Look up the attribute by name rather than by type. This would allow netstandard targeting libraries to
					// define their own copy without having to cross-compile.
					if (attr is not { AttributeType.FullName: "System.Reflection.Metadata.MetadataUpdateHandlerAttribute" })
					{
						continue;
					}

					if (attr is { ConstructorArguments: [{ Value: Type handlerType }] })
					{
						GetHandlerActions(handlerActions, handlerType);
					}
					else
					{
						_log($"'{attr}' found with invalid arguments.");
					}
				}
			}
			catch (Exception e)
			{
				// The handlers enumeration may fail for WPF assemblies that are part of the modified assemblies
				// when building under linux, but which are loaded in that context. We can ignore those assemblies
				// and continue the processing.
				_log($"Unable to process assembly {assembly}, ({e.Message})");
			}
		}

		return handlerActions;
	}

	internal void GetHandlerActions(
		UpdateHandlerActions handlerActions,
		[DynamicallyAccessedMembers(HotReloadHandlerLinkerFlags)]
		Type handlerType)
	{
		bool methodFound = false;

		if (GetUpdateMethod(handlerType, "ClearCache") is MethodInfo clearCache)
		{
			handlerActions.ClearCache.Add(CreateAction(clearCache));
			methodFound = true;
		}

		if (GetUpdateMethod(handlerType, "UpdateApplication") is MethodInfo updateApplication)
		{
			handlerActions.UpdateApplication.Add(CreateAction(updateApplication));
			methodFound = true;
		}

		if (!methodFound)
		{
			_log($"No invocable methods found on metadata handler type '{handlerType}'. " +
				$"Allowed methods are ClearCache, UpdateApplication");
		}
		else
		{
			_log($"Invocable methods found on metadata handler type '{handlerType}'. ");
		}

		Action<Type[]?> CreateAction(MethodInfo update)
		{
			Action<Type[]?> action = (Action<Type[]?>)update.CreateDelegate(typeof(Action<Type[]?>));
			return types =>
			{
				try
				{
					action(types);
				}
				catch (Exception ex)
				{
					_log($"Exception from '{action}': {ex}");
				}
			};
		}

		MethodInfo? GetUpdateMethod([DynamicallyAccessedMembers(HotReloadHandlerLinkerFlags)] Type handlerType, string name)
		{
			if (handlerType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Type[]) }, null) is MethodInfo updateMethod &&
				updateMethod.ReturnType == typeof(void))
			{
				return updateMethod;
			}

			foreach (MethodInfo method in handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
			{
				if (method.Name == name)
				{
					_log($"Type '{handlerType}' has method '{method}' that does not match the required signature.");
					break;
				}
			}

			return null;
		}
	}

	internal static List<Assembly> TopologicalSort(Assembly[] assemblies)
	{
		var sortedAssemblies = new List<Assembly>(assemblies.Length);

		var visited = new HashSet<string>(StringComparer.Ordinal);

		foreach (var assembly in assemblies)
		{
			Visit(assemblies, assembly, sortedAssemblies, visited);
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hot reload is only expected to work when trimming is disabled.")]
		static void Visit(Assembly[] assemblies, Assembly assembly, List<Assembly> sortedAssemblies, HashSet<string> visited)
		{
			var assemblyIdentifier = assembly.GetName().Name!;
			if (!visited.Add(assemblyIdentifier))
			{
				return;
			}

			foreach (var dependencyName in assembly.GetReferencedAssemblies())
			{
				var dependency = Array.Find(assemblies, a => a.GetName().Name == dependencyName.Name);
				if (dependency is not null)
				{
					Visit(assemblies, dependency, sortedAssemblies, visited);
				}
			}

			sortedAssemblies.Add(assembly);
		}

		return sortedAssemblies;
	}

	public void ApplyDeltas(IReadOnlyList<UpdateDelta> deltas)
	{

		for (var i = 0; i < deltas.Count; i++)
		{
			var item = deltas[i];
			foreach (var assembly in _alc.Assemblies)
			{
				if (TryGetModuleId(assembly) is Guid moduleId && moduleId == item.ModuleId)
				{
					_log($"Applying delta to {assembly} / {moduleId}");
					ApplyUpdate(assembly, item);
				}
			}

			// Additionally stash the deltas away so it may be applied to assemblies loaded later.
			var cachedDeltas = _deltas.GetOrAdd(item.ModuleId, static _ => new());
			cachedDeltas.Add(item);
		}

		try
		{
			// Defer discovering metadata update handlers until after hot reload deltas have been applied.
			// This should give enough opportunity for AppDomain.GetAssemblies() to be sufficiently populated.
			_handlerActions ??= GetMetadataUpdateHandlerActions();
			var handlerActions = _handlerActions;

			Type[]? updatedTypes = GetMetadataUpdateTypes(deltas);

			handlerActions.ClearCache.ForEach(a => a(updatedTypes));
			handlerActions.UpdateApplication.ForEach(a => a(updatedTypes));

			_log("Deltas applied.");

			MetadataUpdaterHelper.RaiseMetadataUpdated();
		}
		catch (Exception ex)
		{
			_log(ex.ToString());
		}
	}

	private static void ApplyUpdate(Assembly assembly, UpdateDelta item)
	{
		// Apply the deltas directly - this won't work when the debugger is attached
		// see https://github.com/dotnet/runtime/blob/aca5f6bdd995919411448379aea3651eb1f68133/src/mono/mono/metadata/icall.c#L5505
		System.Reflection.Metadata.MetadataUpdater.ApplyUpdate(assembly, item.MetadataDelta, item.ILDelta, item.PdbBytes ?? ReadOnlySpan<byte>.Empty);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hot reload is only expected to work when trimming is disabled.")]
	private Type[] GetMetadataUpdateTypes(IReadOnlyList<UpdateDelta> deltas)
	{
		List<Type>? types = null;

		foreach (var delta in deltas)
		{
			var assembly = _alc.Assemblies.FirstOrDefault(assembly => TryGetModuleId(assembly) is Guid moduleId && moduleId == delta.ModuleId);
			if (assembly is null)
			{
				continue;
			}

			var assemblyTypes = assembly.GetTypes();

			foreach (var updatedType in delta.UpdatedTypes ?? Array.Empty<int>())
			{
				var type = assemblyTypes.FirstOrDefault(t => t.MetadataToken == updatedType);
				if (type != null)
				{
					types ??= new();
					types.Add(type);
				}
			}
		}

		if (types?.Count != deltas.SelectMany(d => d.UpdatedTypes ?? Array.Empty<int>()).Count())
		{
			// List all the types that were updated but not found in the assembly.
			_log(
				"Some types were marked as updated, but were not found in the running app. " +
				"This may indicate a configuration mismatch between the compiled app and the hot reload engine.");
		}

		return types?.ToArray() ?? Type.EmptyTypes;
	}

	public void ApplyDeltas(Assembly assembly, IReadOnlyList<UpdateDelta> deltas)
	{
		try
		{
			foreach (var item in deltas)
			{
				ApplyUpdate(assembly, item);
			}

			_log("Deltas applied.");
		}
		catch (Exception ex)
		{
			_log(ex.ToString());
		}
	}

	/// <summary>
	/// Returns assemblies from the current ALC and, if different, the default ALC.
	/// This ensures handlers registered on shared assemblies (e.g. Uno.UI in the default ALC)
	/// are discovered even when the agent runs in a non-default ALC (e.g. Studio Live inner app).
	/// Assemblies are deduplicated by name — the current ALC's version takes precedence.
	/// </summary>
	private Assembly[] GetAllRelevantAssemblies()
	{
		var currentAssemblies = _alc.Assemblies.ToArray();
		if (_alc == AssemblyLoadContext.Default)
		{
			return currentAssemblies;
		}

		var seen = new HashSet<string>(StringComparer.Ordinal);
		var result = new List<Assembly>(currentAssemblies.Length * 2);

		// Current ALC first — its assemblies take precedence.
		foreach (var asm in currentAssemblies)
		{
			seen.Add(asm.GetName().Name!);
			result.Add(asm);
		}

		// Then default ALC — only add assemblies not already present.
		foreach (var asm in AssemblyLoadContext.Default.Assemblies)
		{
			if (seen.Add(asm.GetName().Name!))
			{
				result.Add(asm);
			}
		}

		return result.ToArray();
	}

	public void Dispose()
		=> AppDomain.CurrentDomain.AssemblyLoad -= _assemblyLoad;

	private static Guid? TryGetModuleId(Assembly loadedAssembly)
	{
		try
		{
			return loadedAssembly.Modules.FirstOrDefault()?.ModuleVersionId;
		}
		catch
		{
			// Assembly.Modules might throw. See https://github.com/dotnet/aspnetcore/issues/33152
		}

		return default;
	}
}
