using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Uno.UI.RemoteControl.VS.Helpers;
using Task = System.Threading.Tasks.Task;

#pragma warning disable VSTHRD010
#pragma warning disable VSTHRD109

namespace Uno.UI.RemoteControl.VS;

public partial class EntryPoint : IDisposable
{
	private const string DesktopTargetFrameworkIdentifier = "desktop";
	private const string CompatibleTargetFrameworkProfileKey = "compatibleTargetFramework";
	private const string Windows10TargetFrameworkIdentifier = "windows10";
	private const string WasmTargetFrameworkIdentifier = "browserwasm";
	private const string UnoSelectedTargetFrameworkProperty = "_UnoSelectedTargetFramework";
	private CancellationTokenSource? _wasmProjectReloadTask;
	private Stopwatch _lastProfileOperation = new Stopwatch();
	private Stopwatch _lastTargetOperation = new Stopwatch();
	private TimeSpan _profileOrFrameworkDelay = TimeSpan.FromSeconds(1);
	private bool _pendingRequestedChanged;
	private bool _isFirstProfileTfmChange = true;

	private async Task OnDebugFrameworkChangedAsync(string? previousFramework, string newFramework, bool forceReload = false)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		// In this case, a new TargetFramework was selected. We need to file a matching launch profile, if any.
		if (GetTargetFrameworkIdentifier(newFramework) is { } targetFrameworkIdentifier && _debuggerObserver is not null)
		{
			var isFirstProfileTfmChange = _isFirstProfileTfmChange;
			_isFirstProfileTfmChange = false;

			_debugAction?.Invoke($"OnDebugFrameworkChangedAsync({previousFramework}, {newFramework}, {targetFrameworkIdentifier}, forceReload: {forceReload}, isFirstProfileTfmChange:{isFirstProfileTfmChange})");

			// Always synchronize the selected target, the reported value
			// is the right one.
			await WriteUnoTargetFrameworkToStartupProjectAsync(newFramework);

			if (!_pendingRequestedChanged && _lastTargetOperation.IsRunning && _lastTargetOperation.Elapsed < _profileOrFrameworkDelay)
			{
				// This debouncing needs to happen when VS intermittently changes the active
				// profile or target framework on project reloading. We skip the change if it
				// is arbitrarily too close to the previous one.
				// Note that this block must be done before the IsNullOrEmpty(previousFramework)
				// in order to catch automatic profile changes by VS when iOS is selected.
				_debugAction?.Invoke($"Skipping framework change because the active profile or framework was changed in the last {_profileOrFrameworkDelay}");
				return;
			}

			_pendingRequestedChanged = false;
			_lastTargetOperation.Restart();

			if (!isFirstProfileTfmChange && !forceReload && string.IsNullOrEmpty(previousFramework))
			{
				// The first change after a reload is always the active target. This happens
				// when going to/from desktop/wasm for VS issues.
				_debugAction?.Invoke($"Skipping for no previous framework");
				return;
			}

			var profiles = await _debuggerObserver.GetLaunchProfilesAsync();

			var profileFilter = targetFrameworkIdentifier switch
			{
				WasmTargetFrameworkIdentifier
					=> ((ILaunchProfile p) => p.LaunchBrowser),

				DesktopTargetFrameworkIdentifier
					=> (ILaunchProfile profile)
						=> profile.OtherSettings.TryGetValue(CompatibleTargetFrameworkProfileKey, out var compatibleTargetFramework)
							&& compatibleTargetFramework is string ctfs
							&& ctfs.Equals(DesktopTargetFrameworkIdentifier, StringComparison.OrdinalIgnoreCase),

				Windows10TargetFrameworkIdentifier
					=> (ILaunchProfile p) => p.CommandName.Equals("MsixPackage", StringComparison.OrdinalIgnoreCase),

				_ => (Func<ILaunchProfile, bool>?)null
			};

			if (profileFilter is not null)
			{
				// If the current profile already matches the TargetFramework we're going to
				// prefer using it. It can happen if the change is initiated through the profile
				// selector.
				var selectedProfile = profiles
					.FirstOrDefault(p => profileFilter(p) && p.Name == _debuggerObserver.CurrentActiveDebugProfile);

				// Otherwise, select the first compatible profile.
				selectedProfile ??= profiles.Find(p => profileFilter(p));

				if (selectedProfile is not null)
				{
					_debugAction?.Invoke($"Setting profile {selectedProfile}");

					_pendingRequestedChanged = true;
					await _debuggerObserver.SetActiveLaunchProfileAsync(selectedProfile.Name);
				}
			}

			await TryReloadTargetAsync(previousFramework, newFramework, targetFrameworkIdentifier, forceReload);
		}
	}

	private async Task OnDebugProfileChangedAsync(string? previousProfile, string newProfile)
	{
		var isFirstProfileTfmChange = _isFirstProfileTfmChange;
		_isFirstProfileTfmChange = false;

		// In this case, a new TargetFramework was selected. We need to file a matching target framework, if any.
		_debugAction?.Invoke($"OnDebugProfileChangedAsync({previousProfile},{newProfile}) isFirstProfileTfmChange:{isFirstProfileTfmChange}");

		if (!isFirstProfileTfmChange && !_pendingRequestedChanged && _lastProfileOperation.IsRunning && _lastProfileOperation.Elapsed < _profileOrFrameworkDelay)
		{
			// This debouncing needs to happen when VS intermittently changes the active
			// profile or target framework on project reloading. We skip the change if it
			// is arbitrarily too close to the previous one.
			// Note that this block must be done before the IsNullOrEmpty(previousProfile)
			// in order to catch automatic profile changes by VS when iOS is selected.
			_debugAction?.Invoke($"Skipping profile change because the active profile or framework was changed in the last {_profileOrFrameworkDelay}");
			return;
		}

		_pendingRequestedChanged = false;
		_lastProfileOperation.Restart();

		if (!isFirstProfileTfmChange && string.IsNullOrEmpty(previousProfile))
		{
			// The first change after a reload is always the active target. This happens
			// when going to/from desktop/wasm for VS issues.
			_debugAction?.Invoke($"Skipping for no previous profile");
			return;
		}

		if (_debuggerObserver is null)
		{
			return;
		}

		var targetFrameworks = await _debuggerObserver.GetActiveTargetFrameworksAsync();
		var profiles = await _debuggerObserver.GetLaunchProfilesAsync();

		if (profiles.FirstOrDefault(p => p.Name == newProfile) is { } profile)
		{
			if (profile.LaunchBrowser
				&& FindTargetFramework(WasmTargetFrameworkIdentifier) is { } targetFramework)
			{
				_debugAction?.Invoke($"Setting framework {targetFramework}");

				_pendingRequestedChanged = true;
				await _debuggerObserver.SetActiveTargetFrameworkAsync(targetFramework);
			}
			else if (profile.OtherSettings.TryGetValue(CompatibleTargetFrameworkProfileKey, out var compatibleTargetObject)
				&& compatibleTargetObject is string compatibleTarget
				&& FindTargetFramework(compatibleTarget) is { } compatibleTargetFramework)
			{
				_debugAction?.Invoke($"Setting framework {compatibleTarget}");

				_pendingRequestedChanged = true;
				await _debuggerObserver.SetActiveTargetFrameworkAsync(compatibleTargetFramework);
			}
		}

		string? FindTargetFramework(string identifier)
			=> targetFrameworks.FirstOrDefault(f => f.IndexOf("-" + identifier, StringComparison.OrdinalIgnoreCase) != -1);
	}

	private async Task TryReloadTargetAsync(string? previousFramework, string newFramework, string targetFrameworkIdentifier, bool forceReload)
	{
		if (_wasmProjectReloadTask is not null)
		{
			_wasmProjectReloadTask.Cancel();
		}

		var currentReloadTask = _wasmProjectReloadTask = new CancellationTokenSource();

		try
		{
			if (await _dte.GetStartupProjectsAsync() is { Length: > 0 } startupProjects
				&&
				(
					forceReload
					||
					(
						previousFramework is not null
						&& GetTargetFrameworkIdentifier(previousFramework) is { } previousTargetFrameworkIdentifier
						&& (
							(
								// 17.12 or later properly supports having their TFM anywhere in the
								// TFMs lists, except Wasm.
								GetVisualStudioReleaseVersion() >= new Version(17, 12)
								&& (
									previousTargetFrameworkIdentifier is WasmTargetFrameworkIdentifier
									|| targetFrameworkIdentifier is WasmTargetFrameworkIdentifier))

							|| (
								// 17.11 or earlier needs reloading most TFMs
								GetVisualStudioReleaseVersion() < new Version(17, 12)
								&& (previousTargetFrameworkIdentifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier or Windows10TargetFrameworkIdentifier
									|| targetFrameworkIdentifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier or Windows10TargetFrameworkIdentifier))
						)
					)
				)
			)
			{
				var startupProjectUniqueName = startupProjects[0].UniqueName;
				var userFilePath = startupProjects[0].FileName + ".user";

				var sw = Stopwatch.StartNew();

				// Wait for VS to write down the active target, so that on reload the selected target 
				// stays the same.
				while (!currentReloadTask.IsCancellationRequested && sw.Elapsed < TimeSpan.FromSeconds(2))
				{
					if (File.Exists(userFilePath))
					{
						var content = File.ReadAllText(userFilePath);

						if (content.Contains($"<{UnoSelectedTargetFrameworkProperty}>{newFramework}</{UnoSelectedTargetFrameworkProperty}>"))
						{
							_debugAction?.Invoke($"Detected new target framework in {userFilePath}, continuing.");
							break;
						}
					}

					await Task.Delay(100);
				}

				if (currentReloadTask.IsCancellationRequested)
				{
					return;
				}

				_warningAction?.Invoke($"Detected that the active framework was changed from/to WebAssembly/Desktop/Windows, reloading the project (See https://aka.platform.uno/singleproject-vs-reload)");

				// In this context, in order to work around the fact that VS does not handle Wasm
				// to be in the same project as other target framework, we're using the `_SelectedTargetFramework`
				// to reorder the list and make browser-wasm first or not.

				// Assuming serviceProvider is an IServiceProvider instance available in your context
				if (await _asyncPackage.GetServiceAsync(typeof(SVsSolution)) is IVsSolution solution
					&& solution is IVsSolution4 solution4)
				{
					if (currentReloadTask.IsCancellationRequested)
					{
						return;
					}

					if (solution.GetProjectOfUniqueName(startupProjects[0].UniqueName, out var startupProject) == 0)
					{
						if (startupProject.GetGuidProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out var guidObj) == 0
							&& guidObj is Guid projectGuid)
						{
							// Set the property early before unloading so the msbuild cache
							// properly keeps the value.
							await WriteUnoTargetFrameworkToStartupProjectAsync(newFramework);

							var reloadStopWatch = Stopwatch.StartNew();
							// Reload the project in-place. This allows to keep files related to
							// this project opened even when reloading.
							startupProject.ReloadProjectInSolution();
							reloadStopWatch.Stop();

							// Adjust the delay, but cannot be below 1s (fast machines) nor
							// above 3s (very slow machines) to attempt to guess for other
							// IDE delays after reloading a project.
							_profileOrFrameworkDelay = TimeSpan.FromSeconds(
								Math.Max(1.0, Math.Min(3.0, reloadStopWatch.Elapsed.TotalSeconds))
							);

							_debugAction?.Invoke($"Adjust in profile/framework change delay to {_profileOrFrameworkDelay}");

							await WriteUnoTargetFrameworkToStartupProjectAsync(newFramework);
						}
					}
				}
			}
			else
			{
				// No need to reload, but we still need to update the selected target framework
				await WriteUnoTargetFrameworkToStartupProjectAsync(newFramework);
			}
		}
		catch (Exception e)
		{
			_errorAction?.Invoke($"Failed to reload project {e}");
		}
		finally
		{
			if (currentReloadTask == _wasmProjectReloadTask)
			{
				_wasmProjectReloadTask = null;
			}
		}
	}

	private string? GetTargetFrameworkIdentifier(string newFramework)
	{
		var regex = new Regex(@"net(\d+\.\d+)-(?<tfi>\w+)(\d+\.\d+\.\d+)?");
		var match = regex.Match(newFramework);

		return match.Success
			? match.Groups["tfi"].Value
			: null;
	}

	private async Task<bool> HasUnoTargetFrameworkInStartupProjectAsync()
	{
		if (await _asyncPackage.GetServiceAsync(typeof(SVsSolution)) is not IVsSolution solution
			|| await _dte.GetStartupProjectsAsync() is not { Length: > 0 } startupProjects
			|| _debuggerObserver is null)
		{
			_debugAction?.Invoke("Could not find .user file (1)");

			return true; // Cannot find user file, assume it has TFM!
		}

		// Convert DTE project to IVsHierarchy
		solution.GetProjectOfUniqueName(startupProjects[0].UniqueName, out var hierarchy);
		if (hierarchy is not IVsBuildPropertyStorage propertyStorage)
		{
			_debugAction?.Invoke("Could not read .user file (2)");

			return true; // Cannot find user file, assume it has TFM!
		}

		var currentActiveDebugFramework = _debuggerObserver.CurrentActiveDebugFramework;
		if (currentActiveDebugFramework is null)
		{
			_debuggerObserver
				.UnconfiguredProject
				?.Services
				.ActiveConfiguredProjectProvider
				?.ActiveConfiguredProject
				?.ProjectConfiguration
				.Dimensions
				.TryGetValue("TargetFramework", out currentActiveDebugFramework);
		}

		var currentSettingValue = propertyStorage.GetUserProperty(UnoSelectedTargetFrameworkProperty);
		if (currentActiveDebugFramework is null || currentSettingValue is { Length: > 0 })
		{
			_debugAction?.Invoke($"User Setting is already set: {UnoSelectedTargetFrameworkProperty}={currentSettingValue}, currentActiveDebugFramework={currentActiveDebugFramework}");

			return true;
		}

		// The UnoSelectedTargetFrameworkProperty is not defined, we need to reload the
		// project so it can be set.
		return false;
	}

	private async Task WriteUnoTargetFrameworkToStartupProjectAsync(string targetFramework)
	{
		_debugAction?.Invoke($"WriteProjectUserSettingsAsync {targetFramework}");

		if (await _asyncPackage.GetServiceAsync(typeof(SVsSolution)) is not IVsSolution solution
			|| await _dte.GetStartupProjectsAsync() is not { Length: > 0 } startupProjects)
		{
			_debugAction?.Invoke("Could not write .user file (1)");

			return;
		}

		// Convert DTE project to IVsHierarchy
		solution.GetProjectOfUniqueName(startupProjects[0].UniqueName, out var hierarchy);
		if (hierarchy is not IVsBuildPropertyStorage propertyStorage)
		{
			_debugAction?.Invoke("Could not write .user file (2)");

			return;
		}

		propertyStorage.SetUserProperty(UnoSelectedTargetFrameworkProperty, targetFramework);
	}
}
