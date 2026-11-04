using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.IdeChannel;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.VS.AppLaunch;

internal sealed class VsAppLaunchStateConsumer : IDisposable
{
	private readonly AsyncPackage _package;
	private readonly VsAppLaunchStateService<AppLaunchDetails> _stateService;
	private readonly Func<IdeChannelClient?> _ideChannelAccessor;
	private readonly string _packageVersion;
	private readonly string _ideVersion;
	private readonly Action<string>? _debugLog;

	private VsAppLaunchStateConsumer(
		AsyncPackage package,
		VsAppLaunchStateService<AppLaunchDetails> stateService,
		Func<IdeChannelClient?> ideChannelAccessor,
		string packageVersion,
		string ideVersion,
		Action<string>? debugLog)
	{
		_package = package;
		_stateService = stateService;
		_ideChannelAccessor = ideChannelAccessor;
		_packageVersion = packageVersion;
		_ideVersion = ideVersion;
		_debugLog = debugLog;
	}

	public static async Task<VsAppLaunchStateConsumer> CreateAsync(
		AsyncPackage package,
		VsAppLaunchStateService<AppLaunchDetails> stateService,
		Func<IdeChannelClient?> ideChannelAccessor,
		string packageVersion,
		string ideVersion,
		Action<string>? debugLog = null)
	{
		var c = new VsAppLaunchStateConsumer(package, stateService, ideChannelAccessor, packageVersion, ideVersion, debugLog);
		await c.InitializeAsync();
		return c;
	}

	// Backward-compatible overload: compute package version when caller doesn't provide it.
	public static Task<VsAppLaunchStateConsumer> CreateAsync(
		AsyncPackage package,
		VsAppLaunchStateService<AppLaunchDetails> stateService,
		Func<IdeChannelClient?> ideChannelAccessor)
	{
		var packageVersion = typeof(VsAppLaunchStateConsumer).Assembly.GetName().Version?.ToString() ?? string.Empty;
		var ideVersion = "vswin";
		return CreateAsync(package, stateService, ideChannelAccessor, packageVersion, ideVersion);
	}

	private Task InitializeAsync()
	{
		// Only subscribe; handling will run on the package's JoinableTaskFactory when needed
		_stateService.StateChanged += OnStateChanged;
		return Task.CompletedTask;
	}

	private void OnStateChanged(object? sender, StateChangedEventArgs<AppLaunchDetails> e)
	{
		if (e.BuildSucceeded)
		{
			_package.JoinableTaskFactory.RunAsync(async () =>
			{
				await ExtractAndSendAppLaunchInfoAsync(e.StateDetails);
			}).FileAndForget("uno/appLaunch/stateConsumer/onStateChanged");
		}
	}

	private async Task ExtractAndSendAppLaunchInfoAsync(AppLaunchDetails details)
	{
		try
		{
			var targetPath = await GetTargetPathAsync(details.StartupProjectPath);
			if (targetPath == null || !File.Exists(targetPath))
			{
				return;
			}

			var (mvid, platform) = AssemblyInfoReader.Read(targetPath);
			if (mvid == Guid.Empty || string.IsNullOrEmpty(platform))
			{
				return;
			}

			var ideChannel = _ideChannelAccessor();
			if (ideChannel != null && details.IsDebug is { } isDebug)
			{
				var message = new AppLaunchRegisterIdeMessage(mvid, platform, isDebug, _ideVersion, _packageVersion);
				await ideChannel.SendToDevServerAsync(message, CancellationToken.None);
			}
		}
		catch
		{
			// swallow
		}
	}

	private async Task<string?> GetTargetPathAsync(string? projectPath)
	{
		if (string.IsNullOrEmpty(projectPath))
		{
			return null;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		try
		{
			var sbm = await _package.GetServiceAsync(
				typeof(SVsSolutionBuildManager));
			if (sbm is IVsSolutionBuildManager2 sbm2
				&& sbm2.get_StartupProject(out var hierarchy) == Microsoft.VisualStudio.VSConstants.S_OK
				&& hierarchy != null)
			{
				var unconfiguredProject = await GetUnconfiguredProjectAsync(hierarchy);
				if (unconfiguredProject != null
					&& unconfiguredProject.FullPath.Equals(projectPath, StringComparison.InvariantCultureIgnoreCase))
				{
					var launchedProject = await GetActiveDebugFrameworkConfiguredProjectAsync(unconfiguredProject);
					var configuredProject = launchedProject ?? await unconfiguredProject.GetSuggestedConfiguredProjectAsync();
					if (configuredProject?.Services?.ProjectPropertiesProvider != null)
					{
						var projectProperties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
						var targetPath = await projectProperties.GetEvaluatedPropertyValueAsync("TargetPath");
						if (!string.IsNullOrEmpty(targetPath))
						{
							_debugLog?.Invoke($"[AppLaunch] Registering {targetPath} from the {(launchedProject is null ? "suggested" : "active debug framework's")} configured project");
							return targetPath;
						}
					}
				}
			}
		}
		catch
		{
			// ignore
		}

		return null;
	}

	/// <summary>
	/// Gets the configured project the debugger launches, the one of the active debug framework. For a multi-targeted
	/// project the suggested configured project is the first target framework instead, which need not be the launched one.
	/// </summary>
	private async Task<ConfiguredProject?> GetActiveDebugFrameworkConfiguredProjectAsync(UnconfiguredProject unconfiguredProject)
	{
		try
		{
			// The referenced project system package predates this service, so it is resolved by reflection (see ProfilesObserver)
			// https://github.com/dotnet/project-system/blob/fb1a4eed736e01f2a17e4627836addafe5f92dda/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/IActiveDebugFrameworkServices.cs#L27-L30
			var servicesType = Type.GetType("Microsoft.VisualStudio.ProjectSystem.Debug.IActiveDebugFrameworkServices, Microsoft.VisualStudio.ProjectSystem.Managed");
			var exportProvider = unconfiguredProject.Services.ActiveConfiguredProjectProvider?.ActiveConfiguredProject?.Services.ExportProvider;

			if (servicesType is not null
				&& exportProvider is not null
				&& typeof(MefExtensions).GetMethods().FirstOrDefault(m => m.Name == "GetService") is { } getServiceMethod
				&& getServiceMethod.MakeGenericMethod(servicesType).Invoke(null, [exportProvider, /*allow default*/true]) is { } services
				&& servicesType.GetMethod("GetConfiguredProjectForActiveFrameworkAsync")?.Invoke(services, []) is Task<ConfiguredProject> getConfiguredProject)
			{
				return await getConfiguredProject;
			}

			_debugLog?.Invoke($"[AppLaunch] The active debug framework's configured project is unavailable (service type from {servicesType?.Assembly.Location ?? "nowhere"})");
		}
		catch (Exception e)
		{
			_debugLog?.Invoke($"[AppLaunch] Failed to get the active debug framework's configured project: {e.GetBaseException().Message}");
		}

		return null;
	}

	private static async Task<UnconfiguredProject?> GetUnconfiguredProjectAsync(
		IVsHierarchy hierarchy)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		return hierarchy is IVsBrowseObjectContext context
			? context.UnconfiguredProject
			: null;
	}

	public void Dispose()
	{
		try
		{
			_stateService.StateChanged -= OnStateChanged;
		}
		catch { }
	}
}
