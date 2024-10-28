using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Uno.UI.RemoteControl.VS.Helpers;
using Microsoft.VisualStudio.PlatformUI;
using Uno.UI.RemoteControl.VS.Notifications;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.VS;

internal class GlobalJsonObserver
{
	private AsyncPackage _asyncPackage;
	private DTE _dte;
	private Action<string> _debugAction;
	private readonly Action<string> _infoAction;
	private readonly Action<string> _warningAction;
	private readonly Action<string> _errorAction;
	private readonly InfoBarFactory _infoBarFactory;
	private FileSystemWatcher? _fileWatcher;
	private readonly JsonSerializerOptions _readerOptions = new() { ReadCommentHandling = JsonCommentHandling.Skip };

	public GlobalJsonObserver(
		AsyncPackage asyncPackage
		, DTE dte
		, InfoBarFactory infoBarFactory
		, Action<string> debugAction
		, Action<string> infoAction
		, Action<string> warningAction
		, Action<string> errorAction)
	{
		_asyncPackage = asyncPackage;
		_dte = dte;
		_debugAction = debugAction;
		_infoAction = infoAction;
		_warningAction = warningAction;
		_errorAction = errorAction;
		_infoBarFactory = infoBarFactory;

		_debugAction("GlobalJsonObserver: Starting");

		try
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			var globalJsonPath = GetGlobalJsonPath();

			ObserveChanges(globalJsonPath);
		}
		catch (Exception e)
		{
			_debugAction($"GlobalJsonObserver: Failed to start " + e);
		}
	}

	private void ObserveChanges(string? globalJsonPath)
	{
		// Observe file changes to globalJsonPath, and only reload when "msbuild-sdks.Uno.Sdk" changes value
		if (globalJsonPath != null)
		{
			var unoSdkVersion = ReadUnoSdkVersion(globalJsonPath);

			_debugAction($"GlobalJsonObserver: Observing {globalJsonPath}");

			_fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(globalJsonPath), Path.GetFileName(globalJsonPath));

			_fileWatcher.Changed += (sender, e) =>
			{
				_debugAction($"GlobalJsonObserver: File changed: {e.FullPath}");

				try
				{
					var newVersion = ReadUnoSdkVersion(globalJsonPath);

					if (newVersion != unoSdkVersion)
					{
						unoSdkVersion = newVersion;

						// Reload all projects that use the Uno.SDK
						_debugAction($"GlobalJsonObserver: Uno.Sdk version changed to {unoSdkVersion}");

						_asyncPackage.JoinableTaskFactory.Run(async () =>
						{
							await NotifyRestartAsync(unoSdkVersion);
						});

					}
				}
				catch (Exception ex)
				{
					_debugAction($"GlobalJsonObserver: Error reading global.json: {ex.Message}");
				}
			};

			_fileWatcher.NotifyFilter = NotifyFilters.LastWrite |
						NotifyFilters.Attributes |
						NotifyFilters.Size |
						NotifyFilters.CreationTime |
						NotifyFilters.FileName;
			_fileWatcher.EnableRaisingEvents = true;
		}
		else
		{
			_debugAction("GlobalJsonObserver: No global.json file found");
		}
	}

	private async Task NotifyRestartAsync(string? unoSdkVersion)
	{
		await _asyncPackage.JoinableTaskFactory.SwitchToMainThreadAsync();

		if (
			await _asyncPackage.GetServiceAsync(typeof(SVsShell)) is IVsShell shell
			&& await _asyncPackage.GetServiceAsync(typeof(SVsInfoBarUIFactory)) is IVsInfoBarUIFactory infoBarFactory)
		{
			var factory = new InfoBarFactory(infoBarFactory, shell);
			var restartVSItem = new ActionBarItem { Text = "Restart Visual Studio" };
			var moreInformationVSItem = new ActionBarItem { Text = "More information" };

			var infoBar = await factory.CreateAsync(
				new InfoBarModel(
					$"The Uno.Sdk version has changed to {unoSdkVersion}, and a restart of Visual Studio is required.",
					new[]
					{
						restartVSItem,
						moreInformationVSItem
					},
					KnownMonikers.StatusError,
					true));

			if (infoBar is not null)
			{
				infoBar.ActionItemClicked += (s, e) =>
				{
					_asyncPackage.JoinableTaskFactory.Run(async () =>
					{
						if (e.ActionItem == restartVSItem)
						{
							await _asyncPackage.JoinableTaskFactory.SwitchToMainThreadAsync();

							if (shell is IVsShell4 shell4)
							{
								((IVsShell3)shell).IsRunningElevated(out var elevated);
								var type = elevated ? __VSRESTARTTYPE.RESTART_Elevated : __VSRESTARTTYPE.RESTART_Normal;
								var hr = shell4.Restart((uint)type);
							}
						}
						else if (e.ActionItem == moreInformationVSItem)
						{
							System.Diagnostics.Process.Start("https://aka.platform.uno/upgrade-uno-packages");
						}
					});
				};

				await infoBar.TryShowInfoBarUIAsync();
			}
		}
	}

	private string? ReadUnoSdkVersion(string globalJsonPath)
	{
		var document = JsonSerializer.Deserialize<JsonElement>(
			File.ReadAllText(globalJsonPath)
			, _readerOptions);

		if (document.TryGetProperty("msbuild-sdks", out var msbuildSdksElement))
		{
			if (msbuildSdksElement.TryGetProperty("Uno.Sdk", out var unoSdkElement))
			{
				return unoSdkElement.ToString();
			}
		}

		return null;
	}

	private string? GetGlobalJsonPath()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		// Get the solution directory
		string solutionDir = Path.GetDirectoryName(_dte.Solution.FullName);

		// Traverse the directory tree upwards to find global.json
		string? currentDir = solutionDir;
		while (!string.IsNullOrEmpty(currentDir))
		{
			string globalJsonPath = Path.Combine(currentDir, "global.json");

			if (File.Exists(globalJsonPath))
			{
				return globalJsonPath;
			}

			currentDir = Directory.GetParent(currentDir)?.FullName;
		}

		// If no global.json file is found
		return null;
	}

	internal void Dispose()
	{
		_fileWatcher?.Dispose();
	}
}
