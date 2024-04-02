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

	private CancellationTokenSource? _wasmProjectReloadTask;

	private async Task OnDebugFrameworkChangedAsync(string? previousFramework, string newFramework)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		// In this case, a new TargetFramework was selected. We need to file a matching launch profile, if any.
		if (GetTargetFrameworkIdentifier(newFramework) is { } targetFrameworkIdentifier)
		{
			_debugAction?.Invoke($"OnDebugFrameworkChangedAsync({previousFramework}, {newFramework}, {targetFrameworkIdentifier})");

			if (string.IsNullOrEmpty(previousFramework))
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

			await TryReloadWebAssemblyOrDesktopTargetAsync(previousFramework, newFramework, targetFrameworkIdentifier);
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
				_errorAction?.Invoke($"Setting framework {targetFramework}");

				await _debuggerObserver.SetActiveTargetFrameworkAsync(targetFramework);
			}
			else if (profile.OtherSettings.TryGetValue(CompatibleTargetFrameworkProfileKey, out var compatibleTargetObject)
				&& compatibleTargetObject is string compatibleTarget
				&& FindTargetFramework(compatibleTarget) is { } compatibleTargetFramework)
			{
				_errorAction?.Invoke($"Setting framework {compatibleTarget}");

				await _debuggerObserver.SetActiveTargetFrameworkAsync(compatibleTargetFramework);
			}
		}

		string? FindTargetFramework(string identifier)
			=> targetFrameworks.FirstOrDefault(f => f.IndexOf("-" + identifier, StringComparison.OrdinalIgnoreCase) != -1);
	}

	private async Task TryReloadWebAssemblyOrDesktopTargetAsync(string? previousFramework, string newFramework, string targetFrameworkIdentifier)
	{
		if (_wasmProjectReloadTask is not null)
		{
			_wasmProjectReloadTask.Cancel();
		}

		_wasmProjectReloadTask = new CancellationTokenSource();

		try
		{
			if (previousFramework is not null
				&& GetTargetFrameworkIdentifier(previousFramework) is { } previousTargetFrameworkIdentifier
				&& (
					previousTargetFrameworkIdentifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier
					|| targetFrameworkIdentifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier)
				&& await _dte.GetStartupProjectsAsync() is { Length: > 0 } startupProjects
			)
			{
				var sw = Stopwatch.StartNew();

				// Wait for VS to write down the active target, so that on reload the selected target 
				// stays the same.
				while (!_wasmProjectReloadTask.IsCancellationRequested && sw.Elapsed < TimeSpan.FromSeconds(5))
				{
					var userFilePath = startupProjects[0].FileName + ".user";

					if (File.Exists(userFilePath))
					{
						var content = File.ReadAllText(userFilePath);

						if (content.Contains($"<ActiveDebugFramework>{newFramework}</ActiveDebugFramework>"))
						{
							_warningAction?.Invoke($"Detected new target framework in {userFilePath}");
							break;
						}
					}

					await Task.Delay(100);
				}

				if (_wasmProjectReloadTask.IsCancellationRequested)
				{
					return;
				}

				_warningAction?.Invoke($"Detected that the active framework was changed from/to WebAssembly/Desktop, reloading the project (See https://aka.platform.uno/singleproject-vs-reload)");

				// In this context, in order to work around the fact that VS does not handle Wasm
				// to be in the same project as other target framework, we're using the `_SelectedTargetFramework`
				// to reorder the list and make browser-wasm first or not.

				// Assuming serviceProvider is an IServiceProvider instance available in your context
				if (await _asyncPackage.GetServiceAsync(typeof(SVsSolution)) is IVsSolution solution
					&& solution is IVsSolution4 solution4)
				{
					if (_wasmProjectReloadTask.IsCancellationRequested)
					{
						return;
					}

					if (solution.GetProjectOfUniqueName(startupProjects[0].UniqueName, out var startupProject) == 0)
					{
						if (startupProject.GetGuidProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out var guidObj) == 0
							&& guidObj is Guid projectGuid)
						{
							// Unload project
							solution4.UnloadProject(ref projectGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);

							// Reload project
							solution4.ReloadProject(ref projectGuid);
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
			_wasmProjectReloadTask = null;
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
}
