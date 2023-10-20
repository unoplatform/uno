using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.RemoteControl.HotReload;

public partial class ClientHotReloadProcessor : IRemoteControlProcessor
{
	private string? _projectPath;
	private string[]? _xamlPaths;
	private readonly IRemoteControlClient _rcClient;

	private static readonly Logger _log = typeof(ClientHotReloadProcessor).Log();
	private Dictionary<string, string>? _msbuildProperties;

	public ClientHotReloadProcessor(IRemoteControlClient rcClient)
	{
		_rcClient = rcClient;
		InitializeMetadataUpdater();
	}

	partial void InitializeMetadataUpdater();

	string IRemoteControlProcessor.Scope => HotReloadConstants.HotReload;

	public async Task Initialize()
		=> await ConfigureServer();

	public async Task ProcessFrame(Messages.Frame frame)
	{
		switch (frame.Name)
		{
			case AssemblyDeltaReload.Name:
				AssemblyReload(JsonConvert.DeserializeObject<HotReload.Messages.AssemblyDeltaReload>(frame.Content)!);
				break;

			case FileReload.Name:
				await PartialReload(JsonConvert.DeserializeObject<HotReload.Messages.FileReload>(frame.Content)!);
				break;

			case HotReloadWorkspaceLoadResult.Name:
				WorkspaceLoadResult(JsonConvert.DeserializeObject<HotReload.Messages.HotReloadWorkspaceLoadResult>(frame.Content)!);
				break;

			default:
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Unknown frame [{frame.Scope}/{frame.Name}]");
				}
				break;
		}

		return;
	}

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

			ConfigureServer message = new(_projectPath, _xamlPaths, GetMetadataUpdateCapabilities(), MetadataUpdatesEnabled, config.MSBuildProperties);

			await _rcClient.SendMessage(message);

			InitializePartialReload();
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Unable to find ProjectConfigurationAttribute");
			}
		}
	}
}
