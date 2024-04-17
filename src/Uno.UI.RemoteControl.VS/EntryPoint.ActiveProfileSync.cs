using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using StreamJsonRpc;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.DebuggerHelper;
using Uno.UI.RemoteControl.VS.Helpers;
using Uno.UI.RemoteControl.VS.IdeChannel;
using ILogger = Uno.UI.RemoteControl.VS.Helpers.ILogger;
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

	private async Task OnDebugFrameworkChangedAsync(string? previousFramework, string newFramework, bool forceReload = false)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		// In this case, a new TargetFramework was selected. We need to file a matching launch profile, if any.
		if (GetTargetFrameworkIdentifier(newFramework) is { } targetFrameworkIdentifier)
		{
			_debugAction?.Invoke($"OnDebugFrameworkChangedAsync({previousFramework}, {newFramework}, {targetFrameworkIdentifier}, forceReload: {forceReload})");

			if (!forceReload && string.IsNullOrEmpty(previousFramework))
			{
				// The first change after a reload is always the active target. This happens
				// when going to/from desktop/wasm for VS issues.
				_debugAction?.Invoke($"Skipping for no previous framework");
				return;
			}

			var profiles = await _debuggerObserver.GetLaunchProfilesAsync();

			if (targetFrameworkIdentifier == WasmTargetFrameworkIdentifier)
			{
				if (profiles.Find(p => p.LaunchBrowser) is { } browserProfile)
				{
					_debugAction?.Invoke($"Setting profile {browserProfile.Name}");

					await _debuggerObserver.SetActiveLaunchProfileAsync(browserProfile.Name);
				}
			}
			else if (targetFrameworkIdentifier == DesktopTargetFrameworkIdentifier)
			{
				bool IsCompatible(ILaunchProfile profile)
					=> profile.OtherSettings.TryGetValue(CompatibleTargetFrameworkProfileKey, out var compatibleTargetFramework)
						&& compatibleTargetFramework is string ctfs
						&& ctfs.Equals(DesktopTargetFrameworkIdentifier, StringComparison.OrdinalIgnoreCase);

				if (profiles.Find(IsCompatible) is { } desktopProfile)
				{
					_debugAction?.Invoke($"Setting profile {desktopProfile.Name}");

					await _debuggerObserver.SetActiveLaunchProfileAsync(desktopProfile.Name);
				}
			}
			else if (targetFrameworkIdentifier == Windows10TargetFrameworkIdentifier)
			{
				if (profiles.Find(p => p.CommandName.Equals("MsixPackage", StringComparison.OrdinalIgnoreCase)) is { } msixProfile)
				{
					_debugAction?.Invoke($"Setting profile {msixProfile.Name}");

					await _debuggerObserver.SetActiveLaunchProfileAsync(msixProfile.Name);
				}
			}

			await TryReloadTargetAsync(previousFramework, newFramework, targetFrameworkIdentifier, forceReload);
		}
	}

	private async Task OnDebugProfileChangedAsync(string? previousProfile, string newProfile)
	{
		// In this case, a new TargetFramework was selected. We need to file a matching target framework, if any.
		_debugAction?.Invoke($"OnDebugProfileChangedAsync({previousProfile},{newProfile})");

		if (string.IsNullOrEmpty(previousProfile))
		{
			// The first change after a reload is always the active target. This happens
			// when going to/from desktop/wasm for VS issues.
			_debugAction?.Invoke($"Skipping for no previous profile");
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

				await _debuggerObserver.SetActiveTargetFrameworkAsync(targetFramework);
			}
			else if (profile.OtherSettings.TryGetValue(CompatibleTargetFrameworkProfileKey, out var compatibleTargetObject)
				&& compatibleTargetObject is string compatibleTarget
				&& FindTargetFramework(compatibleTarget) is { } compatibleTargetFramework)
			{
				_debugAction?.Invoke($"Setting framework {compatibleTarget}");

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
						&& (previousTargetFrameworkIdentifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier or Windows10TargetFrameworkIdentifier
							|| targetFrameworkIdentifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier or Windows10TargetFrameworkIdentifier)
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
							await WriteProjectUserSettingsAsync(newFramework);

							// Unload project
							solution4.UnloadProject(ref projectGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);

							// Reload project
							solution4.ReloadProject(ref projectGuid);

							var sw2 = Stopwatch.StartNew();

							while (sw2.Elapsed < TimeSpan.FromSeconds(5))
							{
								// Reset the startup project, as VS will move it to the next available
								// project in the solution on unload.
								if (_dte.Solution.SolutionBuild is SolutionBuild2 val)
								{
									_debugAction?.Invoke($"Setting startup project to {startupProjectUniqueName}");
									val.StartupProjects = startupProjectUniqueName;
								}

								await Task.Delay(50);

								if (await _dte.GetStartupProjectsAsync() is { Length: > 0 } newStartupProjects
									&& newStartupProjects[0].UniqueName == startupProjectUniqueName)
								{
									_debugAction?.Invoke($"Startup project changed successfully");
									break;
								}
								else
								{
									_debugAction?.Invoke($"Startup project was not changed, retrying...");
									await Task.Delay(1000);
								}

								await WriteProjectUserSettingsAsync(newFramework);
							}
						}
					}
				}
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

	private async Task OnStartupProjectChangedAsync()
	{
		if (!await EnsureProjectUserSettingsAsync())
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

	private async Task<bool> EnsureProjectUserSettingsAsync()
	{
		if (await _asyncPackage.GetServiceAsync(typeof(SVsSolution)) is IVsSolution solution
			&& await _dte.GetStartupProjectsAsync() is { Length: > 0 } startupProjects)
		{
			// Convert DTE project to IVsHierarchy
			solution.GetProjectOfUniqueName(startupProjects[0].UniqueName, out var hierarchy);

			if (hierarchy is IVsBuildPropertyStorage propertyStorage)
			{
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

				propertyStorage.GetPropertyValue(
					UnoSelectedTargetFrameworkProperty
					, null
					, (uint)_PersistStorageType.PST_USER_FILE
					, out var currentSettingValue);

				if (string.IsNullOrEmpty(currentSettingValue) && currentActiveDebugFramework is not null)
				{
					// The UnoSelectedTargetFrameworkProperty is not defined, we need to reload the
					// project so it can be set.
					return false;
				}
				else
				{
					_debugAction?.Invoke($"User Setting is already set: {UnoSelectedTargetFrameworkProperty}={currentSettingValue}, currentActiveDebugFramework={currentActiveDebugFramework}");
				}
			}
			else
			{
				_debugAction?.Invoke("Could not write .user file (2)");
			}
		}
		else
		{
			_debugAction?.Invoke("Could not write .user file (1)");
		}

		return true;
	}

	private async Task WriteProjectUserSettingsAsync(string targetFramework)
	{
		_debugAction?.Invoke($"WriteProjectUserSettingsAsync {targetFramework}");

		if (await _asyncPackage.GetServiceAsync(typeof(SVsSolution)) is IVsSolution solution
			&& await _dte.GetStartupProjectsAsync() is { Length: > 0 } startupProjects)
		{
			// Convert DTE project to IVsHierarchy
			solution.GetProjectOfUniqueName(startupProjects[0].UniqueName, out var hierarchy);

			if (hierarchy is IVsBuildPropertyStorage propertyStorage)
			{
				WriteUserProperty(propertyStorage, UnoSelectedTargetFrameworkProperty, targetFramework);
			}
			else
			{
				_debugAction?.Invoke("Could not write .user file (2)");
			}
		}
		else
		{
			_debugAction?.Invoke("Could not write .user file (1)");
		}
	}

	private static void WriteUserProperty(IVsBuildPropertyStorage propertyStorage, string propertyName, string propertyValue)
	{
		propertyStorage.SetPropertyValue(
			propertyName,        // Property name
			null,                 // Configuration name, null applies to all configurations
			(uint)_PersistStorageType.PST_USER_FILE,  // Specifies that this is a user-specific property
			propertyValue             // Property value
		);
	}
}
