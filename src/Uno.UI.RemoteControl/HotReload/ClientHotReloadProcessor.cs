#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.HotReload;

public partial class ClientHotReloadProcessor : IClientProcessor
{
	private string? _projectPath;
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

			case UpdateFileResponse.Name:
				ProcessUpdateFileResponse(frame.GetContent<UpdateFileResponse>());
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

	partial void ProcessUpdateFileResponse(UpdateFileResponse response);

	#region Configure hot-reload
	private async Task ConfigureServer()
	{
		var assembly = _rcClient.AppType.Assembly;
		if (assembly.GetCustomAttributes(typeof(ProjectConfigurationAttribute), false) is ProjectConfigurationAttribute[] { Length: > 0 } configs)
		{
			_status.ReportServerState(HotReloadState.Initializing);

			try
			{
				var config = configs.First();

				_projectPath = config.ProjectPath;

				_msbuildProperties = Messages.ConfigureServer.BuildMSBuildProperties(config.MSBuildProperties);

				ConfigureHotReloadMode();
				InitializeMetadataUpdater();

				if (!_supportsMetadataUpdates)
				{
					_status.ReportInvalidRuntime();
				}

				var hrDebug = Environment.GetEnvironmentVariable("__UNO_SUPPORT_DEBUG_HOT_RELOAD__") == "true";
				var message = new ConfigureServer(_projectPath, GetMetadataUpdateCapabilities(), _serverMetadataUpdatesEnabled, config.MSBuildProperties, hrDebug);

				await _rcClient.SendMessage(message);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Successfully sent request to configure HR server for project '{_projectPath}'.");
				}
			}
			catch (Exception error)
			{
				_status.ReportServerState(HotReloadState.Disabled);

				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Unable to configure HR server", error);
				}
			}
		}
		else
		{
			_status.ReportServerState(HotReloadState.Disabled);

			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Unable to configure HR server as ProjectConfigurationAttribute is missing.");
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
