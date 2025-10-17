using System;
using System.IO;
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

	private VsAppLaunchStateConsumer(
		AsyncPackage package,
		VsAppLaunchStateService<AppLaunchDetails> stateService,
		Func<IdeChannelClient?> ideChannelAccessor,
		string packageVersion)
	{
		_package = package;
		_stateService = stateService;
		_ideChannelAccessor = ideChannelAccessor;
		_packageVersion = packageVersion;
	}

	public static async Task<VsAppLaunchStateConsumer> CreateAsync(
		AsyncPackage package,
		VsAppLaunchStateService<AppLaunchDetails> stateService,
		Func<IdeChannelClient?> ideChannelAccessor,
		string packageVersion)
	{
		var c = new VsAppLaunchStateConsumer(package, stateService, ideChannelAccessor, packageVersion);
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
		return CreateAsync(package, stateService, ideChannelAccessor, packageVersion);
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
				// Provide IDE and plugin metadata. For Visual Studio host, report product name and unknown plugin version when not available.
				var ideName = "vswin-<version>";
				var pluginVersion = _packageVersion;
				var message = new AppLaunchRegisterIdeMessage(mvid, platform, isDebug, ideName, pluginVersion);
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
					var configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync();
					if (configuredProject?.Services?.ProjectPropertiesProvider != null)
					{
						var projectProperties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
						var targetPath = await projectProperties.GetEvaluatedPropertyValueAsync("TargetPath");
						if (!string.IsNullOrEmpty(targetPath))
						{
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
