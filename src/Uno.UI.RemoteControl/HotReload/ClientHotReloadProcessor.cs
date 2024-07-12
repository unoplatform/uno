#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.Diagnostics.UI;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Uno.UI.Helpers;

namespace Uno.UI.RemoteControl.HotReload;

public partial class ClientHotReloadProcessor : IClientProcessor
{
	private string? _projectPath;
	private string[]? _xamlPaths;
	private readonly IRemoteControlClient _rcClient;
	private HotReloadMode? _forcedHotReloadMode;

	private Dictionary<string, string>? _msbuildProperties;

	public ClientHotReloadProcessor(IRemoteControlClient rcClient)
	{
		_rcClient = rcClient;
		_status = new(this);
	}

	partial void InitializeMetadataUpdater();

	string IClientProcessor.Scope => WellKnownScopes.HotReload;

	public async Task Initialize()
		=> await ConfigureServer();

	public async Task ProcessFrame(Messages.Frame frame)
	{
		switch (frame.Name)
		{
			case AssemblyDeltaReload.Name:
				ProcessAssemblyReload(frame.GetContent<AssemblyDeltaReload>());
				break;

			case FileReload.Name:
				await ProcessFileReload(frame.GetContent<FileReload>());
				break;

			case HotReloadWorkspaceLoadResult.Name:
				WorkspaceLoadResult(frame.GetContent<HotReloadWorkspaceLoadResult>());
				break;

			case HotReloadStatusMessage.Name:
				await ProcessServerStatus(frame.GetContent<HotReloadStatusMessage>());
				break;

			default:
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Unknown frame [{frame.Scope}/{frame.Name}]");
				}
				break;
		}
	}

	private async Task ProcessFileReload(HotReload.Messages.FileReload fileReload)
	{
		if ((
				_forcedHotReloadMode is null
				&& !_supportsLightweightHotReload
				&& !_serverMetadataUpdatesEnabled
				&& _supportsXamlReader)
			|| _forcedHotReloadMode == HotReloadMode.XamlReader)
		{
			ReloadFileWithXamlReader(fileReload);
		}
		else
		{
			await PartialReload(fileReload);
		}
	}

	#region Configure hot-reload
	private async Task ConfigureServer()
	{
		var assembly = _rcClient.AppType.Assembly;

		if (assembly.GetCustomAttributes(typeof(ProjectConfigurationAttribute), false) is ProjectConfigurationAttribute[] configs)
		{
			var config = configs.First();

			_projectPath = config.ProjectPath;
			_xamlPaths = config.XamlPaths;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"ProjectConfigurationAttribute={config.ProjectPath}, Paths={_xamlPaths.Length}");
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				foreach (var path in _xamlPaths)
				{
					this.Log().Trace($"\t- {path}");
				}
			}

			_msbuildProperties = Messages.ConfigureServer.BuildMSBuildProperties(config.MSBuildProperties);

			ConfigureHotReloadMode();
			InitializeMetadataUpdater();
			InitializePartialReload();
			InitializeXamlReader();

			ConfigureServer message = new(_projectPath, _xamlPaths, GetMetadataUpdateCapabilities(), _serverMetadataUpdatesEnabled, config.MSBuildProperties);

			await _rcClient.SendMessage(message);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Unable to find ProjectConfigurationAttribute");
			}
		}
	}

	private void ConfigureHotReloadMode()
	{
		var unoHotReloadMode = GetMSBuildProperty("UnoHotReloadMode");

		if (!string.IsNullOrEmpty(unoHotReloadMode))
		{
			if (!Enum.TryParse<HotReloadMode>(unoHotReloadMode, true, out var hotReloadMode))
			{
				throw new NotSupportedException($"The hot reload mode {unoHotReloadMode} is not supported.");
			}

			_forcedHotReloadMode = hotReloadMode;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Forced Hot Reload Mode:{_forcedHotReloadMode}");
			}
		}
	}

	private string GetMSBuildProperty(string property, string defaultValue = "")
	{
		var output = defaultValue;

		if (_msbuildProperties is not null && !_msbuildProperties.TryGetValue(property, out output))
		{
			return defaultValue;
		}

		return output;
	}
	#endregion

	private async Task ProcessServerStatus(HotReloadStatusMessage status)
	{
#if HAS_UNO_WINUI
		_status.ReportServerStatus(status);
#endif
	}
}
