// The structure of the ElementUpdaterAgent has been kept similar to the HotReloadAgent, 
// which is based on the implementation in https://github.com/dotnet/aspnetcore/blob/26e3dfc7f3f3a91ba445ec0f8b1598d12542fb9f/src/Components/WebAssembly/WebAssembly/src/HotReload/HotReloadAgent.cs

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
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.HotReload.MetadataUpdater;

internal sealed class ElementUpdateAgent : IDisposable
{
	/// Flags for hot reload handler Types like MVC's HotReloadService.
	private const DynamicallyAccessedMemberTypes HotReloadHandlerLinkerFlags = DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods;

	private readonly Action<string> _log;
	private readonly Action<MethodInfo, Exception> _onActionError;
	private readonly AssemblyLoadEventHandler _assemblyLoad;
	private readonly ConcurrentDictionary<Type, ElementUpdateHandlerActions> _elementHandlerActions = new();

	internal const string MetadataUpdaterType = "System.Reflection.Metadata.MetadataUpdater";

	public ElementUpdateAgent(Action<string> log, Action<MethodInfo, Exception> onActionError)
	{
		_log = log;
		_onActionError = onActionError;
		_assemblyLoad = OnAssemblyLoad;
		AppDomain.CurrentDomain.AssemblyLoad += _assemblyLoad;
		LoadElementUpdateHandlerActions();
	}

	public ImmutableDictionary<Type, ElementUpdateHandlerActions> ElementHandlerActions => _elementHandlerActions.ToImmutableDictionary();

	private void OnAssemblyLoad(object? _, AssemblyLoadEventArgs eventArgs) =>
		// This should only be invoked on the (rare) occasion that assemblies
		// haven't been loaded when the agent is initialized. Since the agent 
		// is initialized when the first UpdateApplication call is invoked on
		// the ClientHotReloadProcessor, most assemblies should already be loaded.
		// For this reason, we don't worry about incrementally loading handlers
		// we just reload from all assemblies
		LoadElementUpdateHandlerActions();

	internal sealed class ElementUpdateHandlerActions
	{
		/// <summary>
		/// This will get invoked whenever UpdateApplication is invoked 
		/// but before any updates are applied to the visual tree. 
		/// This is only invoked if 
		///   a) there's no other update in progress
		///   b) ui updating hasn't been paused
		/// This is only invoked once per UpdateApplication, 
		/// irrespective of the number of types the handler is registered for
		/// </summary>
		public Action<Type[]?> BeforeVisualTreeUpdate { get; set; } = _ => { };

		/// <summary>
		/// This will get invoked whenever UpdateApplication is invoked 
		/// after all updates have been applied to the visual tree. 
		/// This is only invoked if 
		///   a) there's no other update in progress
		///   b) ui updating hasn't been paused
		/// This is only invoked once per UpdateApplication, 
		/// irrespective of the number of types the handler is registered for
		/// </summary>
		public Action<Type[]?> AfterVisualTreeUpdate { get; set; } = _ => { };

		/// <summary>
		/// This will get invoked whenever UpdateApplication is invoked 
		/// after all updates have been applied to the visual tree. 
		/// This is always invoked, even if 
		///   a) there's another update in progress
		///   b) ui updating is paused
		/// This is only invoked once per UpdateApplication, 
		/// irrespective of the number of types the handler is registered for
		/// Second parameter (bool) indicates whether the ui was updated or not
		/// </summary>
		public Action<Type[]?, bool> ReloadCompleted { get; set; } = (_, _) => { };

		/// <summary>
		/// This is invoked when a specific element is found in the tree. 
		/// This would be useful if the element holds references to controls 
		/// that aren't in the visual tree and need to be updated 
		/// (eg pages in the backstack of a frame)
		/// </summary>
		public Action<FrameworkElement, Type[]?> ElementUpdate { get; set; } = (_, _) => { };

		/// <summary>
		/// This is invoked whenever UpdateApplication is invoked, 
		/// before an element is replaced in the visual three. 
		/// This is invoked for each element in the visual tree that 
		/// matches a type that has been updated. 
		/// The oldElement is attached to the visual tree and existing datacontext. 
		/// The newElement is not attached to the visual tree and won't yet have a data context
		/// </summary>
		public Action<FrameworkElement, FrameworkElement, Type[]?> BeforeElementReplaced { get; set; } = (_, _, _) => { };

		/// <summary>
		/// This is invoked whenever UpdateApplication is invoked,
		/// after an element is replaced in the visual three. 
		/// This is invoked for each element in the visual tree that 
		/// matches a type that has been updated. 
		/// The oldElement is no longer attached to the visual tree and datacontext will be null. 
		/// The newElement is attached to the visual tree and will have data context update, either inherited from parent or copies from the oldElement.
		/// </summary>
		public Action<FrameworkElement, FrameworkElement, Type[]?> AfterElementReplaced { get; set; } = (_, _, _) => { };

		/// <summary>
		/// This is invoked whenever UpdateApplication is invoked,
		/// as part of a preliminary pass through the visual tree prior
		/// to applying ui updates. 
		/// This is invoked for each element in the visual tree that 
		/// matches the type registered for by the handler. 
		/// Add key-value-pairs to the dictionary parameter
		/// </summary>
		public Action<FrameworkElement, IDictionary<string, object>, Type[]?> CaptureState { get; set; } = (_, _, _) => { };

		/// <summary>
		/// This is invoked whenever UpdateApplication is invoked,
		/// as part of a pass through the visual tree after applying 
		/// ui updates. 
		/// This is invoked for each element in the visual tree that 
		/// matches the type registered for by the handler. 
		/// Restore properties from the key-value-pairs to the dictionary parameter
		/// </summary>
		public Func<FrameworkElement, IDictionary<string, object>, Type[]?, Task> RestoreState { get; set; } = (_, _, _) => Task.CompletedTask;
	}

	[UnconditionalSuppressMessage("Trimmer", "IL2072",
		Justification = "The handlerType passed to GetHandlerActions is preserved by MetadataUpdateHandlerAttribute with DynamicallyAccessedMemberTypes.All.")]
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
			try
			{
				foreach (var attr in assembly.GetCustomAttributesData())
				{
					// Look up the attribute by name rather than by type. This would allow netstandard targeting libraries to
					// define their own copy without having to cross-compile.
					if (attr.AttributeType.FullName == "System.Reflection.Metadata.ElementMetadataUpdateHandlerAttribute")
					{

						var ctorArgs = attr.ConstructorArguments;
						var elementType = ctorArgs.Count == 2 ? ctorArgs[0].Value as Type : typeof(object);
						var handlerType = ctorArgs.Count == 2 ? ctorArgs[1].Value as Type :
												ctorArgs.Count == 1 ? ctorArgs[0].Value as Type : default;
						if (elementType is null || handlerType is null)
						{
							_log($"'{attr}' found with invalid arguments. elementType '{elementType?.Name}', handlerType '{handlerType?.Name}'");
							continue;
						}

						GetElementHandlerActions(elementType, handlerType);
					}
				}
			}
			catch (Exception e)
			{
				// The handlers enumeration may fail for WPF assemblies that are part of the modified assemblies
				// when building under linux, but which are loaded in that context. We can ignore those assemblies
				// and continue the processing.
				_log($"Failed to process assembly {assembly}, {e.Message}");
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

		_log($"Loading ElementMetatdataUpdateHandlerAttribute with constructor arguments {elementType.Name} and {handlerType.Name}");


		var updateActions = new ElementUpdateHandlerActions();
		_elementHandlerActions[elementType] = updateActions;

		if (GetUpdateMethod(handlerType, nameof(ElementUpdateHandlerActions.BeforeVisualTreeUpdate)) is MethodInfo beforeVisualTreeUpdate)
		{
			_log($"Adding handler for {nameof(updateActions.BeforeVisualTreeUpdate)} for {handlerType}");
			updateActions.BeforeVisualTreeUpdate = CreateAction(beforeVisualTreeUpdate, handlerType);
			methodFound = true;
		}

		if (GetUpdateMethod(handlerType, nameof(ElementUpdateHandlerActions.AfterVisualTreeUpdate)) is MethodInfo afterVisualTreeUpdate)
		{
			_log($"Adding handler for {nameof(updateActions.AfterVisualTreeUpdate)} for {handlerType}");
			updateActions.AfterVisualTreeUpdate = CreateAction(afterVisualTreeUpdate, handlerType);
			methodFound = true;
		}

		if (GetHandlerMethod(handlerType, nameof(ElementUpdateHandlerActions.ReloadCompleted), new[] { typeof(Type[]), typeof(bool) }) is MethodInfo reloadCompleted)
		{
			_log($"Adding handler for {nameof(updateActions.ReloadCompleted)} for {handlerType}");
			updateActions.ReloadCompleted = CreateHandlerAction<Action<Type[]?, bool>>(reloadCompleted);
			methodFound = true;
		}

		if (GetHandlerMethod(handlerType, nameof(ElementUpdateHandlerActions.ElementUpdate), new[] { typeof(FrameworkElement), typeof(Type[]) }) is MethodInfo elementUpdate)
		{
			_log($"Adding handler for {nameof(updateActions.ElementUpdate)} for {handlerType}");
			updateActions.ElementUpdate = CreateHandlerAction<Action<FrameworkElement, Type[]?>>(elementUpdate);
			methodFound = true;
		}

		if (GetHandlerMethod(
			handlerType,
			nameof(ElementUpdateHandlerActions.BeforeElementReplaced),
			new[] { typeof(FrameworkElement), typeof(FrameworkElement), typeof(Type[]) }) is MethodInfo beforeElementReplaced)
		{
			_log($"Adding handler for {nameof(updateActions.BeforeElementReplaced)} for {handlerType}");
			updateActions.BeforeElementReplaced = CreateHandlerAction<Action<FrameworkElement, FrameworkElement, Type[]?>>(beforeElementReplaced);
			methodFound = true;
		}

		if (GetHandlerMethod(
			handlerType,
			nameof(ElementUpdateHandlerActions.AfterElementReplaced),
			new[] { typeof(FrameworkElement), typeof(FrameworkElement), typeof(Type[]) }) is MethodInfo afterElementReplaced)
		{
			_log($"Adding handler for {nameof(updateActions.AfterElementReplaced)} for {handlerType}");
			updateActions.AfterElementReplaced = CreateHandlerAction<Action<FrameworkElement, FrameworkElement, Type[]?>>(afterElementReplaced);
			methodFound = true;
		}

		if (GetHandlerMethod(
			handlerType,
			nameof(ElementUpdateHandlerActions.CaptureState),
			new[] { typeof(FrameworkElement), typeof(IDictionary<string, object>), typeof(Type[]) }) is MethodInfo captureState)
		{
			_log($"Adding handler for {nameof(updateActions.CaptureState)} for {handlerType}");
			updateActions.CaptureState = CreateHandlerAction<Action<FrameworkElement, IDictionary<string, object>, Type[]?>>(captureState);
			methodFound = true;
		}

		if (GetHandlerMethod(
			handlerType,
			nameof(ElementUpdateHandlerActions.RestoreState),
			new[] { typeof(FrameworkElement), typeof(IDictionary<string, object>), typeof(Type[]) },
			typeof(Task)) is MethodInfo restoreState)
		{
			_log($"Adding handler for {nameof(updateActions.RestoreState)} for {handlerType}");
			updateActions.RestoreState = CreateHandlerAction<Func<FrameworkElement, IDictionary<string, object>, Type[]?, Task>>(restoreState);
			methodFound = true;
		}

		if (!methodFound)
		{
			_log($"No invokable methods found on metadata handler type '{handlerType}'. " +
				$"Allowed methods are BeforeVisualTreeUpdate, AfterVisualTreeUpdate, ElementUpdate, BeforeElementReplaced, AfterElementReplaced, CaptureState, RestoreState, ReloadCompleted");
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
		Type handlerType, string name, Type[] parameterTypes, Type? returnType = default)
	{
		if (handlerType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, parameterTypes, null) is MethodInfo updateMethod &&
			(
				(returnType is null && updateMethod.ReturnType == typeof(void)) ||
				(updateMethod.ReturnType == returnType)
			)
		)
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

	private Action<Type[]?> CreateAction(MethodInfo update, Type handlerType)
	{
		var action = CreateHandlerAction<Action<Type[]?>>(update);
		return types =>
		{
			try
			{
				action?.Invoke(types);
			}
			catch (Exception ex)
			{
				_onActionError(update, ex);
				_log($"Exception from '{update.Name}' on {handlerType.Name}: {ex}");
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
