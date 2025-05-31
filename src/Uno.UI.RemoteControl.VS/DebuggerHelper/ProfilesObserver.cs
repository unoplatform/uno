﻿using System;
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
using System.Xml;
namespace Uno.UI.RemoteControl.VS.DebuggerHelper;

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread

internal class ProfilesObserver : IDisposable
{
	private readonly AsyncPackage _asyncPackage;
	private readonly Action<string> _debugLog;
	private readonly DTE _dte;
	private readonly Func<string?, string, Task> _onDebugFrameworkChanged;
	private readonly Func<string?, string, Task> _onDebugProfileChanged;
	private Func<Task> _onStartupProjectChanged;
	private object[] _existingStartupProjects = [];


	private string? _lastActiveDebugProfile;
	private string? _lastActiveDebugFramework;

	private record FrameworkServices(object? ActiveDebugFrameworkServices, MethodInfo? SetActiveFrameworkMethod, MethodInfo? GetProjectFrameworksAsyncMethod);
	private FrameworkServices? _projectFrameworkServices;

	private bool _isDisposed;
	private string? _currentActiveDebugProfile;
	private string? _currentActiveDebugFramework;
	private IDisposable? _projectRuleSubscriptionLink;
	private UnconfiguredProject? _unconfiguredProject;

	// Keep the handlers below in order to avoid collection
	// and allow DTE to call them.
	_dispSolutionEvents_ProjectAddedEventHandler? _projectAdded;
	_dispSolutionEvents_ProjectRemovedEventHandler? _projectRemoved;
	_dispSolutionEvents_ProjectRenamedEventHandler? _projectRenamed;
	_dispCommandEvents_AfterExecuteEventHandler? _afterExecute;

	public string? CurrentActiveDebugProfile
		=> _currentActiveDebugProfile;

	public string? CurrentActiveDebugFramework
		=> _currentActiveDebugFramework;

	public UnconfiguredProject? UnconfiguredProject
		=> _unconfiguredProject;

	public ProfilesObserver(
		AsyncPackage asyncPackage
		, EnvDTE.DTE dte
		, Func<string?, string, Task> onDebugFrameworkChanged
		, Func<string?, string, Task> onDebugProfileChanged
		, Func<Task> onStartupProjectChanged
		, Action<string> debugLog)
	{
		_asyncPackage = asyncPackage;
		_debugLog = debugLog;
		_dte = dte;
		_onDebugFrameworkChanged = onDebugFrameworkChanged;
		_onDebugProfileChanged = onDebugProfileChanged;
		_onStartupProjectChanged = onStartupProjectChanged;

		ObserveSolutionEvents();
		_ = ObserveStartupProjectAsync();
	}

	private async Task ObserveStartupProjectAsync()
	{
		while (!_isDisposed)
		{
			await Task.Delay(2000);

			TryUpdateSolution();
		}
	}

	private void TryUpdateSolution()
	{
		if (_dte.Solution.SolutionBuild.StartupProjects is object[] newStartupProjects)
		{
			if (_existingStartupProjects.Length == 0)
			{
				// We're starting up, no need to re-create the observer
				_existingStartupProjects = newStartupProjects;
			}
			else if (!newStartupProjects.SequenceEqual(_existingStartupProjects))
			{
				_debugLog("Startup projects have changed, reloading observer");

				// log all projects
				_existingStartupProjects = newStartupProjects;

				UnsubscribeCurrentProject();
			}

			if (_unconfiguredProject is null)
			{
				_ = ObserveProfilesAsync();
			}
		}
		else
		{
			if (_existingStartupProjects.Length > 0)
			{
				_ = _onStartupProjectChanged();
			}

			_existingStartupProjects = [];
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

						var projectBlock = projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(
							evaluationLinkOptions,
							// Request for the initial values of the source
							initialDataAsNewForProjectSubscriptionUpdate: true);

						var unconfiguredProjectBlock = ProjectDataSources.SyncLinkOptions(
							unconfiguredProject.Capabilities.SourceBlock,
							// Request for the initial values of the source
							initialDataAsNewForProjectSubscriptionUpdate: true);

						_projectRuleSubscriptionLink = ProjectDataSources.SyncLinkTo(
							projectBlock,
							unconfiguredProjectBlock,
							projectChangesBlock,
							new() { PropagateCompletion = true });

						await _onStartupProjectChanged();
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
			_debugLog($"_projectAdded: {s.FileName}");
			TryUpdateSolution();
		};
		_projectRemoved = (s) =>
		{
			_debugLog($"_projectRemoved: {s.FileName}");
			TryUpdateSolution();
		};
		_projectRenamed = (s, v) =>
		{
			_debugLog($"_projectRenamed: {s.FileName}");
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

	private void UnObserveSolutionEvents()
	{
		if (_projectAdded is not null)
		{
			_dte.Events.SolutionEvents.ProjectAdded -= _projectAdded;
		}
		if (_projectRemoved is not null)
		{
			_dte.Events.SolutionEvents.ProjectRemoved -= _projectRemoved;
		}
		if (_projectRenamed is not null)
		{
			_dte.Events.SolutionEvents.ProjectRenamed -= _projectRenamed;
		}
		if (_afterExecute is not null)
		{
			_dte.Events.CommandEvents.AfterExecute -= _afterExecute;
		}
	}

	private async Task OnUnconfiguredProject_ProjectUnloadingAsync(object? sender, EventArgs args)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		_debugLog($"ProfilesObserver: unconfiguredProject was unloaded");

		UnsubscribeCurrentProject();
	}

	private void UnsubscribeCurrentProject()
	{
		_currentActiveDebugFramework = null;
		_currentActiveDebugProfile = null;
		_lastActiveDebugProfile = null;
		_lastActiveDebugFramework = null;

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

				if (
					_lastActiveDebugProfile == activeDebugProfile
					&& _lastActiveDebugFramework == activeDebugFramework)
				{
					// Debounce ChangedAsync which may be invoked even if nothing changed.
					return;
				}

				_lastActiveDebugProfile = activeDebugProfile;
				_lastActiveDebugFramework = activeDebugFramework;

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
	{
		UnObserveSolutionEvents();
		_projectRuleSubscriptionLink?.Dispose();
		_isDisposed = true;
	}
}
