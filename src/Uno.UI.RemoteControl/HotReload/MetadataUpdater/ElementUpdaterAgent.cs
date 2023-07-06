#if NET6_0_OR_GREATER || __WASM__ || __SKIA__
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Windows.UI.Xaml;
using Uno;


namespace Uno.UI.RemoteControl.HotReload.MetadataUpdater;

internal sealed class ElementUpdateAgent : IDisposable
{
	/// Flags for hot reload handler Types like MVC's HotReloadService.
	private const DynamicallyAccessedMemberTypes HotReloadHandlerLinkerFlags = DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods;

	private readonly Action<string> _log;
	private readonly AssemblyLoadEventHandler _assemblyLoad;
	private readonly ConcurrentDictionary<Type, ElementUpdateHandlerActions> _elementHandlerActions = new();

	internal const string MetadataUpdaterType = "System.Reflection.Metadata.MetadataUpdater";

	public ElementUpdateAgent(Action<string> log)
	{
		_log = log;
		_assemblyLoad = OnAssemblyLoad;
		AppDomain.CurrentDomain.AssemblyLoad += _assemblyLoad;
		LoadElementUpdateHandlerActions();
	}

	public ImmutableDictionary<Type, ElementUpdateHandlerActions> ElementHandlerActions => _elementHandlerActions.ToImmutableDictionary();

	private void OnAssemblyLoad(object? _, AssemblyLoadEventArgs __)
	{
		// This should only be invoked on the (rare) occasion that assemblies
		// haven't been loaded when the agent is initialized. Since the agent 
		// is initialized when the first hot reload message is received, this
		// should mean that all assemblies are already loaded.
		// For this reason, we don't worry about incrementally loading handlers
		// we just reload from all assemblies
		LoadElementUpdateHandlerActions();
	}

	internal sealed class ElementUpdateHandlerActions
	{
		public Action<Type[]?> BeforeVisualTreeUpdate { get; set; } = _ => { };
		public Action<Type[]?> AfterVisualTreeUpdate { get; set; } = _ => { };
		public Action<FrameworkElement, Type[]?> ElementUpdate { get; set; } = (_, _) => { };
		public Action<FrameworkElement, FrameworkElement, Type[]?> BeforeElementReplaced { get; set; } = (_, _, _) => { };
		public Action<FrameworkElement, FrameworkElement, Type[]?> AfterElementReplaced { get; set; } = (_, _, _) => { };
	}

#if NET6_0_OR_GREATER
	[UnconditionalSuppressMessage("Trimmer", "IL2072",
		Justification = "The handlerType passed to GetHandlerActions is preserved by MetadataUpdateHandlerAttribute with DynamicallyAccessedMemberTypes.All.")]
#endif
	private void LoadElementUpdateHandlerActions()
	{
		// We need to execute MetadataUpdateHandlers in a well-defined order. For v1, the strategy that is used is to topologically
		// sort assemblies so that handlers in a dependency are executed before the dependent (e.g. the reflection cache action
		// in System.Private.CoreLib is executed before System.Text.Json clears it's own cache.)
		// This would ensure that caches and updates more lower in the application stack are up to date
		// before ones higher in the stack are recomputed.
		var sortedAssemblies = TopologicalSort(AppDomain.CurrentDomain.GetAssemblies());
		_elementHandlerActions.Clear();
		foreach (var assembly in sortedAssemblies)
		{
			foreach (var attr in assembly.GetCustomAttributesData())
			{
				// Look up the attribute by name rather than by type. This would allow netstandard targeting libraries to
				// define their own copy without having to cross-compile.
				if (attr.AttributeType.FullName == "System.Reflection.Metadata.ElementMetadataUpdateHandlerAttribute")
				{

					var ctorArgs = attr.ConstructorArguments;
					if (ctorArgs.Count != 2 ||
						ctorArgs[0].Value is not Type elementType ||
						ctorArgs[1].Value is not Type handlerType)
					{
						_log($"'{attr}' found with invalid arguments.");
						continue;
					}

					GetElementHandlerActions(elementType, handlerType);
				}
			}
		}
	}

	internal void GetElementHandlerActions(
		[DynamicallyAccessedMembers(HotReloadHandlerLinkerFlags)]
		Type elementType,
		[DynamicallyAccessedMembers(HotReloadHandlerLinkerFlags)]
		Type handlerType)
	{
		bool methodFound = false;

		var updateActions = new ElementUpdateHandlerActions();
		_elementHandlerActions[elementType] = updateActions;

		if (GetUpdateMethod(handlerType, nameof(ElementUpdateHandlerActions.BeforeVisualTreeUpdate)) is MethodInfo beforeVisualTreeUpdate)
		{
			updateActions.BeforeVisualTreeUpdate = CreateAction(beforeVisualTreeUpdate);
			methodFound = true;
		}

		if (GetUpdateMethod(handlerType, nameof(ElementUpdateHandlerActions.AfterVisualTreeUpdate)) is MethodInfo afterVisualTreeUpdate)
		{
			updateActions.AfterVisualTreeUpdate = CreateAction(afterVisualTreeUpdate);
			methodFound = true;
		}

		if (GetHandlerMethod(handlerType, nameof(ElementUpdateHandlerActions.ElementUpdate), new[] { typeof(FrameworkElement), typeof(Type[]) }) is MethodInfo elementUpdate)
		{
			updateActions.ElementUpdate = CreateHandlerAction<Action<FrameworkElement, Type[]?>>(elementUpdate);
			methodFound = true;
		}

		if (GetHandlerMethod(
			handlerType,
			nameof(ElementUpdateHandlerActions.BeforeElementReplaced),
			new[] { typeof(FrameworkElement), typeof(FrameworkElement), typeof(Type[]) }) is MethodInfo beforeElementReplaced)
		{
			updateActions.BeforeElementReplaced = CreateHandlerAction<Action<FrameworkElement, FrameworkElement, Type[]?>>(beforeElementReplaced);
			methodFound = true;
		}

		if (GetHandlerMethod(
			handlerType,
			nameof(ElementUpdateHandlerActions.AfterElementReplaced),
			new[] { typeof(FrameworkElement), typeof(FrameworkElement), typeof(Type[]) }) is MethodInfo afterElementReplaced)
		{
			updateActions.AfterElementReplaced = CreateHandlerAction<Action<FrameworkElement, FrameworkElement, Type[]?>>(afterElementReplaced);
			methodFound = true;
		}

		if (!methodFound)
		{
			_log($"No invokable methods found on metadata handler type '{handlerType}'. " +
				$"Allowed methods are BeforeVisualTreeUpdate, AfterVisualTreeUpdate, ElementUpdate, BeforeElementReplaced, AfterElementReplaced");
		}
		else
		{
			_log($"Invokable methods found on metadata handler type '{handlerType}'. ");
		}
	}

	private MethodInfo? GetUpdateMethod(
		[DynamicallyAccessedMembers(HotReloadHandlerLinkerFlags)]
		Type handlerType, string name)
			=> GetHandlerMethod(handlerType, name, new[] { typeof(Type[]) });

	private MethodInfo? GetHandlerMethod(
[DynamicallyAccessedMembers(HotReloadHandlerLinkerFlags)]
		Type handlerType, string name, Type[] parameterTypes)
	{
		if (handlerType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, parameterTypes, null) is MethodInfo updateMethod &&
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

	private Action<Type[]?> CreateAction(MethodInfo update)
	{
		var action = CreateHandlerAction<Action<Type[]?>>(update);
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

	private TAction CreateHandlerAction<TAction>(MethodInfo update) where TAction : Delegate
	{
		TAction action = (TAction)update.CreateDelegate(typeof(TAction));
		return action;
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

	public void Dispose()
		=> AppDomain.CurrentDomain.AssemblyLoad -= _assemblyLoad;

}

#endif
