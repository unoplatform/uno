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
using Microsoft.VisualStudio.PlatformUI;
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

			if (await IsCorrectFirstProjectAsync(newFramework))
			{
				// The first change after a reload is always the active target. This happens
				// when going to/from desktop/wasm for VS issues.
				_debugAction?.Invoke($"Skipping for valid first target framework");
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

			await TryReloadTargetAsync(previousFramework, newFramework, targetFrameworkIdentifier);
		}
	}

	private async Task OnDebugProfileChangedAsync(string? previousProfile, string newProfile)
	{
		// In this case, a new TargetFramework was selected. We need to file a matching target framework, if any.
		_debugAction?.Invoke($"OnDebugProfileChangedAsync({previousProfile},{newProfile})");

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

	private async Task TryReloadTargetAsync(string? previousFramework, string newFramework, string targetFrameworkIdentifier)
	{
		if (_wasmProjectReloadTask is not null)
		{
			_wasmProjectReloadTask.Cancel();
		}

		_wasmProjectReloadTask = new CancellationTokenSource();

		var isValidFirstProject = await IsCorrectFirstProjectAsync(newFramework);
		var startupProjects = await _dte.GetStartupProjectsAsync();

		var isValidTargetFrameworkChange =
			previousFramework is not null
						&& GetTargetFrameworkIdentifier(previousFramework) is { } previousTargetFrameworkIdentifier
						&& (
							previousTargetFrameworkIdentifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier or Windows10TargetFrameworkIdentifier
							|| targetFrameworkIdentifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier or Windows10TargetFrameworkIdentifier);

		try
		{
			if (
				startupProjects is { Length: > 0 }
				&& (
					!isValidFirstProject
					|| isValidTargetFrameworkChange))
			{
				if (previousFramework is null && !isValidFirstProject)
				{
					// If the previous target framework is null, it means that we got here
					// because the first target framework is not valid and needs to be adjusted.
					// Let's arbitrarily wait a bit for VS to load its internal state properly before we
					// reload the project.
					await Task.Delay(3000);
				}

				var startupProjectUniqueName = startupProjects[0].UniqueName;
				var startupProjectFileName = startupProjects[0].FileName;

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
							_debugAction?.Invoke($"Detected new target framework in {userFilePath}, continuing.");
							break;
						}
					}

					await Task.Delay(100);
				}

				if (_wasmProjectReloadTask.IsCancellationRequested)
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

							// Write an updated csproj with reordered targets while the project is unloaded
							ReorderTargetFrameworks(startupProjectFileName, newFramework);

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
			_wasmProjectReloadTask = null;
		}
	}

	private async Task<bool> IsCorrectFirstProjectAsync(string newFramework)
	{
		if (await _dte.GetStartupProjectsAsync() is { Length: > 0 } startupProjects)
		{
			XmlDocument doc = new();
			doc.PreserveWhitespace = true;
			doc.Load(startupProjects[0].FileName);

			if (doc.SelectSingleNode("//UnoDisableVSTargetFrameworksRewrite") is { } disableRewriteNode
				&& disableRewriteNode.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
			{
				_warningAction?.Invoke($"TargetFrameworks rewriting is disabled by UnoDisableVSTargetFrameworksRewrite");

				// Escape hatch to disable the rewriting.
				return true;
			}

			if (IsValidUnoProject(doc))
			{
				if (doc.SelectSingleNode("//TargetFrameworks") is { } targetFrameworksNode)
				{
					var originalTargetFrameworksText = targetFrameworksNode.InnerText;
					var parts = GetTargetFrameworkParts(originalTargetFrameworksText);

					if (parts.Length > 1 && !originalTargetFrameworksText.Trim().StartsWith(newFramework, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
				}
			}
		}

		return true;
	}

	private void ReorderTargetFrameworks(string projectFileName, string targetFramework)
	{
		try
		{
			XmlDocument doc = new();
			doc.PreserveWhitespace = true;
			doc.Load(projectFileName);

			var isValidUnoProject = IsValidUnoProject(doc);

			if (isValidUnoProject)
			{
				if (doc.SelectSingleNode("//TargetFrameworks") is { } tfmsNode)
				{
					var originalTargetFrameworksText = tfmsNode.InnerText;

					var parts = GetTargetFrameworkParts(originalTargetFrameworksText);

					if (parts.Length > 1 && !originalTargetFrameworksText.Trim().StartsWith(targetFramework, StringComparison.OrdinalIgnoreCase))
					{
						var targetTfmIndex = parts
							.Select((tfm, index) => (index, tfm))
							.Where(v => v.tfm.Equals(targetFramework))
							.FirstOrDefault();

						if (targetTfmIndex.tfm is not null)
						{
							StringBuilder tempText = new(tfmsNode.InnerXml);
							tempText.Replace(parts[0], "**changeme**");
							tempText.Replace(parts[targetTfmIndex.index], parts[0]);
							tempText.Replace("**changeme**", targetFramework);

							const string warningComment = " The TargetFrameworks property was modified by Uno Platform. See https://aka.platform.uno/singleproject-vs-reload ";

							tfmsNode.InnerXml = tempText.ToString();

							if (!doc.InnerXml.Contains(warningComment))
							{
								var commentNode = doc.CreateComment(warningComment);
								var crNode = doc.CreateWhitespace("\r");
								var wsNode = tfmsNode.PreviousSibling is XmlWhitespace ws ? ws.Clone() : null;

								tfmsNode.ParentNode?.InsertBefore(commentNode, tfmsNode);
								tfmsNode.ParentNode?.InsertAfter(crNode, commentNode);

								if (wsNode is not null)
								{
									wsNode.InnerText = wsNode.InnerText.Replace("\r\n", "");
									tfmsNode.ParentNode?.InsertBefore(wsNode, tfmsNode);
								}
							}

							doc.Save(projectFileName);

							_debugAction?.Invoke($"The TargetFrameworks in [{projectFileName}] have been reordered to place {targetFramework} first.");
						}
						else
						{
							_warningAction?.Invoke($"Unable to find the TargetFramework {targetFramework} in [{projectFileName}]");
						}
					}
				}
				else
				{
					_warningAction?.Invoke($"Unable to find the TargetFrameworks property in [{projectFileName}]");
				}
			}
			else
			{
				_debugAction?.Invoke($"Skipping non-uno.sdk project [{projectFileName}]");
			}
		}
		catch (Exception e)
		{
			_errorAction?.Invoke($"Failed to update the project file [{projectFileName}]: {e}");
		}
	}

	private static string[] GetTargetFrameworkParts(string targetFrameworks)
	{
		StringBuilder sb = new(targetFrameworks.Trim());
		sb.Replace("\n", "").Replace("\r", "");

		return sb
			.ToString()
			.Split([';'], StringSplitOptions.RemoveEmptyEntries)
			.Select(v => v.Trim())
			.ToArray();
	}

	/// <summary>
	/// Determines if the csproj in the XmlDocument targets the Uno.Sdk
	/// </summary>
	private static bool IsValidUnoProject(XmlDocument doc) => doc.SelectSingleNode("/Project") is { } projectNode
					&& projectNode.Attributes is not null
					&& projectNode.Attributes["Sdk"] is { } sdkAttribute
					&& (
					sdkAttribute.Value.Equals("Uno.Sdk", StringComparison.OrdinalIgnoreCase)
					|| sdkAttribute.Value.StartsWith("Uno.Sdk/", StringComparison.OrdinalIgnoreCase));

	private string? GetTargetFrameworkIdentifier(string newFramework)
	{
		var regex = new Regex(@"net(\d+\.\d+)-(?<tfi>\w+)(\d+\.\d+\.\d+)?");
		var match = regex.Match(newFramework);

		return match.Success
			? match.Groups["tfi"].Value
			: null;
	}
}
