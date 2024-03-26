using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using System.Collections.Immutable;
using System.Threading.Tasks.Dataflow;
using EnvDTE;
using Microsoft.VisualStudio.LanguageServer.Client;
using Uno.UI.RemoteControl.VS.Helpers;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.RpcContracts.Logging;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using System.Reflection;
using System.Management.Instrumentation;
using Microsoft.VisualStudio.RpcContracts.Build;
using EnvDTE80;
namespace Uno.UI.RemoteControl.VS.DebuggerHelper;

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread

internal class ProfilesObserver : IDisposable
{
	private readonly AsyncPackage _asyncPackage;
	private readonly Action<string> _debugLog;
	private readonly DTE _dte;
	private readonly Func<string?, string, Task> _onDebugFrameworkChanged;
	private readonly Func<string?, string, Task> _onDebugProfileChanged;

	private record FrameworkServices(object? ActiveDebugFrameworkServices, MethodInfo? SetActiveFrameworkMethod, MethodInfo? GetProjectFrameworksAsyncMethod);
	private FrameworkServices? _projectFrameworkServices;

	private string? _currentActiveDebugProfile;
	private string? _currentActiveDebugFramework;
	private IDisposable? _projectRuleSubscriptionLink;
	private UnconfiguredProject? _unconfiguredProject;

	_dispSolutionEvents_ProjectAddedEventHandler? _projectAdded;
	_dispSolutionEvents_ProjectRemovedEventHandler? _projectRemoved;
	_dispSolutionEvents_ProjectRenamedEventHandler? _projectRenamed;
	_dispCommandEvents_AfterExecuteEventHandler? _afterExecute;

	public string? CurrentActiveDebugProfile
		=> _currentActiveDebugProfile;

	public string? CurrentActiveDebugFramework
		=> _currentActiveDebugFramework;

	public ProfilesObserver(
		AsyncPackage asyncPackage
		, EnvDTE.DTE dte
		, Func<string?, string, Task> onDebugFrameworkChanged
		, Func<string?, string, Task> onDebugProfileChanged
		, Action<string> debugLog)
	{
		_asyncPackage = asyncPackage;
		_debugLog = debugLog;
		_dte = dte;
		_onDebugFrameworkChanged = onDebugFrameworkChanged;
		_onDebugProfileChanged = onDebugProfileChanged;

		ObserveSolutionEvents();
	}

	object[]? _existingStartupProjects = [];

	private void TryUpdateSolution()
	{
		if (_dte.Solution.SolutionBuild.StartupProjects is object[] newStartupProjects)
		{
			if (!newStartupProjects.SequenceEqual(_existingStartupProjects))
			{
				// log all projects
				_existingStartupProjects = newStartupProjects;
			}

			if (_unconfiguredProject is null)
			{
				_ = ObserveProfilesAsync();
			}
		}
	}

	public async Task ObserveProfilesAsync()
	{
		try
		{
			_debugLog("Starting observing profile");

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			if ((await _dte.GetStartupProjectsAsync()) is { } startupProjects)
			{
				if (startupProjects.FirstOrDefault() is Project dteProject
					&& (await GetUnconfiguredProjectAsync(dteProject)) is { } unconfiguredProject)
				{
					_debugLog($"Observing {unconfiguredProject.FullPath}");

					_unconfiguredProject = unconfiguredProject;
					_unconfiguredProject.ProjectUnloading += OnUnconfiguredProject_ProjectUnloadingAsync;

					var configuredProject = unconfiguredProject.Services.ActiveConfiguredProjectProvider?.ActiveConfiguredProject;
					var projectSubscriptionService = configuredProject?.Services.ActiveConfiguredProjectSubscription;

					if (projectSubscriptionService is not null)
					{
						var projectChangesBlock = DataflowBlockSlim.CreateActionBlock(
							CaptureAndApplyExecutionContext<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCapabilitiesSnapshot>>>(ProjectRuleBlock_ChangedAsync));

						var evaluationLinkOptions = new StandardRuleDataflowLinkOptions
						{
							RuleNames = ImmutableHashSet.Create("ProjectDebugger"),
							PropagateCompletion = true
						};

						var projectBlock = projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(evaluationLinkOptions, true);
						var unconfiguredProjectBlock = ProjectDataSources.SyncLinkOptions(unconfiguredProject.Capabilities.SourceBlock);

						_projectRuleSubscriptionLink = ProjectDataSources.SyncLinkTo(
							projectBlock,
							unconfiguredProjectBlock,
							projectChangesBlock,
							new() { PropagateCompletion = true });
					}
				}
			}
		}
		catch (Exception ex)
		{
			_debugLog($"Failed to observe {ex}");
		}
	}

	private void ObserveSolutionEvents()
	{
		_projectAdded = (s) =>
		{
			_debugLog($"_projectAdded: {s}");
			TryUpdateSolution();
		};
		_projectRemoved = (s) =>
		{
			_debugLog($"_projectRemoved: {s}");
			TryUpdateSolution();
		};
		_projectRenamed = (s, v) =>
		{
			_debugLog($"_projectRenamed: {s}");
			TryUpdateSolution();
		};
		_afterExecute = (s, c, o, m) =>
		{
			_debugLog($"_afterExecute: {s} {c} {o} {m}");
			TryUpdateSolution();
		};

		_debugLog("Observing solution");
		_dte.Events.SolutionEvents.ProjectAdded += _projectAdded;
		_dte.Events.SolutionEvents.ProjectRemoved += _projectRemoved;
		_dte.Events.SolutionEvents.ProjectRenamed += _projectRenamed;
		_dte.Events.CommandEvents.AfterExecute += _afterExecute;
	}

	private async Task OnUnconfiguredProject_ProjectUnloadingAsync(object? sender, EventArgs args)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		_debugLog($"unconfiguredProject was unloaded");

		_currentActiveDebugFramework = null;
		_currentActiveDebugProfile = null;

		// Force a refresh of reflection calls
		_projectFrameworkServices = null;

		_projectRuleSubscriptionLink?.Dispose();
		_projectRuleSubscriptionLink = null;

		if (_unconfiguredProject is not null)
		{
			_unconfiguredProject.ProjectUnloading -= OnUnconfiguredProject_ProjectUnloadingAsync;
			_unconfiguredProject = null;
		}
	}

	private static Func<TInput, Task> CaptureAndApplyExecutionContext<TInput>(Func<TInput, Task> function)
	{
		var context = ExecutionContext.Capture();

		return (TInput input) =>
		{
			var currentSynchronizationContext = SynchronizationContext.Current;
			using var executionContext = context.CreateCopy();

			Task? result = null;
			ExecutionContext.Run(executionContext, delegate
			{
				SynchronizationContext.SetSynchronizationContext(currentSynchronizationContext);
				result = function(input);
			}, null);

			return result!;
		};
	}

	private async Task ProjectRuleBlock_ChangedAsync(IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCapabilitiesSnapshot>> projectSnapshot)
	{
		try
		{
			if (projectSnapshot.Value.Item1.CurrentState.TryGetValue("ProjectDebugger", out var ruleSnapshot))
			{
				ruleSnapshot.Properties.TryGetValue("ActiveDebugProfile", out var activeDebugProfile);
				ruleSnapshot.Properties.TryGetValue("ActiveDebugFramework", out var activeDebugFramework);

				if (!string.IsNullOrEmpty(activeDebugProfile) && activeDebugProfile != _currentActiveDebugProfile)
				{
					var previousProfile = _currentActiveDebugProfile;
					_currentActiveDebugProfile = activeDebugProfile;

					await _onDebugProfileChanged(previousProfile, _currentActiveDebugProfile);
				}

				if (!string.IsNullOrEmpty(activeDebugFramework) && activeDebugFramework != _currentActiveDebugFramework)
				{
					var previousDebugFramework = _currentActiveDebugFramework;
					_currentActiveDebugFramework = activeDebugFramework;

					await _onDebugFrameworkChanged(previousDebugFramework, _currentActiveDebugFramework);
				}
			}
		}
		catch (Exception e)
		{
			_debugLog($"Failed to process changedAsync: {e}");
		}
	}

	public async Task SetActiveTargetFrameworkAsync(string targetFramework)
	{
		_debugLog($"SetActiveTargetFrameworkAsync({targetFramework})");

		EnsureActiveDebugFrameworkServices();

		if (_projectFrameworkServices?.SetActiveFrameworkMethod?.Invoke(_projectFrameworkServices.ActiveDebugFrameworkServices, [targetFramework]) is Task t)
		{
			await t;
		}
	}

	public async Task<List<string>?> GetActiveTargetFrameworksAsync()
	{
		_debugLog($"GetActiveTargetFrameworksAsync()");

		EnsureActiveDebugFrameworkServices();

		if (_projectFrameworkServices?.GetProjectFrameworksAsyncMethod?.Invoke(_projectFrameworkServices.ActiveDebugFrameworkServices, []) is Task<List<string>?> listTask)
		{
			return await listTask;
		}

		return new();
	}

	public async Task SetActiveLaunchProfileAsync(string launchProfile)
	{
		var provider = _unconfiguredProject?.Services.ActiveConfiguredProjectProvider?.ActiveConfiguredProject?.Services.ExportProvider;

		if (provider?.GetService<ILaunchSettingsProvider>() is { } launchSettingsProvider)
		{
			await launchSettingsProvider.SetActiveProfileAsync(launchProfile);
		}
	}

	public async Task<ImmutableList<ILaunchProfile>> GetLaunchProfilesAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var provider = _unconfiguredProject?.Services.ActiveConfiguredProjectProvider?.ActiveConfiguredProject?.Services.ExportProvider;

		if (provider?.GetService<ILaunchSettingsProvider>() is { } launchSettingsProvider)
		{
			return launchSettingsProvider.CurrentSnapshot.Profiles;
		}

		return ImmutableList.Create<ILaunchProfile>();
	}

	public async Task<UnconfiguredProject?> GetUnconfiguredProjectAsync(Project dteProject)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		// Get the IVsSolution service
		if (await _asyncPackage.GetServiceAsync(typeof(SVsSolution)) is IVsSolution solution)
		{
			// Convert DTE project to IVsHierarchy
			solution.GetProjectOfUniqueName(dteProject.UniqueName, out var hierarchy);

			// Get UnconfiguredProject from IVsHierarchy
			if (hierarchy is IVsBrowseObjectContext browseContext)
			{
				return browseContext.UnconfiguredProject;
			}
			else if (hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_BrowseObject, out object browseObject) >= 0 && browseObject is IVsBrowseObjectContext context)
			{
				return context.UnconfiguredProject;
			}
		}

		return null;
	}

	private void EnsureActiveDebugFrameworkServices()
	{
		var provider = _unconfiguredProject?.Services.ActiveConfiguredProjectProvider?.ActiveConfiguredProject?.Services.ExportProvider;

		if (_projectFrameworkServices is null && provider is not null)
		{
			var type = Type.GetType("Microsoft.VisualStudio.ProjectSystem.Debug.IActiveDebugFrameworkServices, Microsoft.VisualStudio.ProjectSystem.Managed");

			if (typeof(MefExtensions).GetMethods().FirstOrDefault(m => m.Name == "GetService") is { } getServiceMethod)
			{
				var typedMethod = getServiceMethod.MakeGenericMethod(type);

				var activeDebugFrameworkServices = typedMethod.Invoke(null, [provider, /*allow default*/false]);

				// https://github.com/dotnet/project-system/blob/34eb57b35962367b71c2a1d79f6c486945586e24/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/IActiveDebugFrameworkServices.cs#L20-L21
				if (activeDebugFrameworkServices.GetType().GetMethod("SetActiveDebuggingFrameworkPropertyAsync") is { } setActiveFrameworkMethod

					// https://github.com/dotnet/project-system/blob/34eb57b35962367b71c2a1d79f6c486945586e24/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/IActiveDebugFrameworkServices.cs#L20-L21
					&& activeDebugFrameworkServices.GetType().GetMethod("GetProjectFrameworksAsync") is { } getProjectFrameworksAsyncMethod)
				{
					_projectFrameworkServices = new(activeDebugFrameworkServices, setActiveFrameworkMethod, getProjectFrameworksAsyncMethod);
				}
			}
		}
	}

	public void Dispose()
		=> _projectRuleSubscriptionLink?.Dispose();
}
