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
	private CancellationTokenSource? _wasmProjectReloadTask;
	private Stopwatch _lastOperation = new Stopwatch();
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

			if (!_pendingRequestedChanged && _lastOperation.IsRunning && _lastOperation.Elapsed < _profileOrFrameworkDelay)
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
			_lastOperation.Restart();

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
		}
	}

	private async Task OnDebugProfileChangedAsync(string? previousProfile, string newProfile)
	{
		var isFirstProfileTfmChange = _isFirstProfileTfmChange;
		_isFirstProfileTfmChange = false;

		// In this case, a new TargetFramework was selected. We need to file a matching target framework, if any.
		_debugAction?.Invoke($"OnDebugProfileChangedAsync({previousProfile},{newProfile}) isFirstProfileTfmChange:{isFirstProfileTfmChange}");

		if (!isFirstProfileTfmChange && !_pendingRequestedChanged && _lastOperation.IsRunning && _lastOperation.Elapsed < _profileOrFrameworkDelay)
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
		_lastOperation.Restart();

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

	private string? GetTargetFrameworkIdentifier(string newFramework)
	{
		var regex = new Regex(@"net(\d+\.\d+)-(?<tfi>\w+)(\d+\.\d+\.\d+)?");
		var match = regex.Match(newFramework);

		return match.Success
			? match.Groups["tfi"].Value
			: null;
	}

	private async Task OnStartupProjectChangedAsync()
	{
		if (_debuggerObserver is not null)
		{
			_debugAction?.Invoke($"The user setting is not yet initialized, aligning framework and profile");

			// The user settings file is not available, we have created the
			// file, but we also need to align the profile.
			string currentActiveDebugFramework = "";

			var hasTargetFramework = _debuggerObserver
				.UnconfiguredProject
				?.Services
				.ActiveConfiguredProjectProvider
				?.ActiveConfiguredProject
				?.ProjectConfiguration
				.Dimensions
				.TryGetValue("TargetFramework", out currentActiveDebugFramework) ?? false;

			if (hasTargetFramework)
			{
				await OnDebugFrameworkChangedAsync(null, currentActiveDebugFramework, true);
			}
		}
	}

}
