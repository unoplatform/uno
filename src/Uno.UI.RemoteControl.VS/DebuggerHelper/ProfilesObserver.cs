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
namespace Uno.UI.RemoteControl.VS.DebuggerHelper;

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread

internal class ProfilesObserver : IDisposable
{
	private readonly AsyncPackage _asyncPackage;
	private readonly DTE _dte;
	private readonly Func<string?, string, Task> _onDebugFrameworkChanged;
	private readonly Func<string?, string, Task> _onDebugProfileChanged;
	private string? _currentActiveDebugProfile;
	private string? _currentActiveDebugFramework;
	private IDisposable? _projectRuleSubscriptionLink;
	private UnconfiguredProject? _unconfiguredProject;
	private object? _activeDebugFrameworkServices;
	private MethodInfo? _setActiveFrameworkMethod;
	private MethodInfo? _getProjectFrameworksAsyncMethod;

	public string? CurrentActiveDebugProfile
		=> _currentActiveDebugProfile;

	public string? CurrentActiveDebugFramework
		=> _currentActiveDebugFramework;

	public ProfilesObserver(AsyncPackage asyncPackage, EnvDTE.DTE dte, Func<string?, string, Task> onDebugFrameworkChanged, Func<string?, string, Task> onDebugProfileChanged)
	{
		_asyncPackage = asyncPackage;
		_dte = dte;
		_onDebugFrameworkChanged = onDebugFrameworkChanged;
		_onDebugProfileChanged = onDebugProfileChanged;
	}

	public async Task ObserveProfilesAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		if (_dte.Solution.SolutionBuild.StartupProjects is object[] startupProjects
			&& startupProjects.Length > 0)
		{
			var startupProject = (string)startupProjects[0];

			if ((await _dte.GetProjectsAsync()).FirstOrDefault(p => p.UniqueName == startupProject) is Project dteProject
				&& (await GetUnconfiguredProjectAsync(dteProject)) is { } unconfiguredProject)
			{
				_unconfiguredProject = unconfiguredProject;

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
				var previousDebugFramework = _currentActiveDebugProfile;
				_currentActiveDebugFramework = activeDebugFramework;

				await _onDebugFrameworkChanged(previousDebugFramework, _currentActiveDebugFramework);
			}
		}
	}

	public async Task SetActiveTargetFrameworkAsync(string targetFramework)
	{
		EnsureActiveDebugFrameworkServices();

		if (_setActiveFrameworkMethod?.Invoke(_activeDebugFrameworkServices, [targetFramework]) is Task t)
		{
			await t;
		}
	}

	public async Task<List<string>?> GetActiveTargetFrameworksAsync()
	{
		EnsureActiveDebugFrameworkServices();

		if (_getProjectFrameworksAsyncMethod?.Invoke(_activeDebugFrameworkServices, []) is Task<List<string>?> listTask)
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
		if (_setActiveFrameworkMethod is null)
		{
			var provider = _unconfiguredProject?.Services.ActiveConfiguredProjectProvider?.ActiveConfiguredProject?.Services.ExportProvider;

			var type = Type.GetType("Microsoft.VisualStudio.ProjectSystem.Debug.IActiveDebugFrameworkServices, Microsoft.VisualStudio.ProjectSystem.Managed");
			if (typeof(MefExtensions).GetMethods().FirstOrDefault(m => m.Name == "GetService") is { } getServiceMethod)
			{
				var typedMethod = getServiceMethod.MakeGenericMethod(type);

				_activeDebugFrameworkServices = typedMethod.Invoke(null, [provider, /*allow default*/false]);

				// https://github.com/dotnet/project-system/blob/34eb57b35962367b71c2a1d79f6c486945586e24/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/IActiveDebugFrameworkServices.cs#L20-L21
				if (_activeDebugFrameworkServices.GetType().GetMethod("SetActiveDebuggingFrameworkPropertyAsync") is { } setActiveFrameworkMethod)
				{
					_setActiveFrameworkMethod = setActiveFrameworkMethod;
				}

				// https://github.com/dotnet/project-system/blob/34eb57b35962367b71c2a1d79f6c486945586e24/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/IActiveDebugFrameworkServices.cs#L20-L21
				if (_activeDebugFrameworkServices.GetType().GetMethod("GetProjectFrameworksAsync") is { } getProjectFrameworksAsyncMethod)
				{
					_getProjectFrameworksAsyncMethod = getProjectFrameworksAsyncMethod;
				}
			}
		}
	}

	public void Dispose()
		=> _projectRuleSubscriptionLink?.Dispose();
}
