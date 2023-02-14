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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RemoteControl.HotReload
{
	public partial class ClientHotReloadProcessor : IRemoteControlProcessor
	{
		private string? _projectPath;
		private string[]? _xamlPaths;
		private bool _useXamlReaderHotReload;
		private readonly IRemoteControlClient _rcClient;

		public ClientHotReloadProcessor(IRemoteControlClient rcClient)
		{
			_rcClient = rcClient;
			InitializeMetadataUpdater();
		}

		partial void InitializeMetadataUpdater();

		string IRemoteControlProcessor.Scope => HotReloadConstants.ScopeName;

		public async Task Initialize()
		{
			await ConfigureServer();
		}

		public Task ProcessFrame(Messages.Frame frame)
		{
			switch (frame.Name)
			{
				case FileReload.Name:
					ReloadFile(JsonConvert.DeserializeObject<HotReload.Messages.FileReload>(frame.Content)!);
					break;

#if NET6_0_OR_GREATER || __WASM__ || __SKIA__
				case AssemblyDeltaReload.Name:
					AssemblyReload(JsonConvert.DeserializeObject<HotReload.Messages.AssemblyDeltaReload>(frame.Content)!);
					break;
#endif

				default:
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError($"Unknown frame [{frame.Scope}/{frame.Name}]");
					}
					break;
			}

			return Task.CompletedTask;
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

				await _rcClient.SendMessage(new HotReload.Messages.ConfigureServer(_projectPath, _xamlPaths, GetMetadataUpdateCapabilities()));
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Unable to find ProjectConfigurationAttribute");
				}
			}

			if (assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false) is AssemblyMetadataAttribute[] asmMetadataAttributes)
			{
				if (asmMetadataAttributes.FirstOrDefault(a => a.Key.Equals("UnoUseXamlReaderHotReload", StringComparison.OrdinalIgnoreCase)) is { } keyAttribute
					&& bool.TryParse(keyAttribute.Value, out var useXamlReaderHotReload))
				{
					_useXamlReaderHotReload = useXamlReaderHotReload;
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"UseXamlReaderHotReload={_useXamlReaderHotReload}");
				}
			}
		}

#if !(NET6_0_OR_GREATER || __WASM__ || __SKIA__)
		private string[] GetMetadataUpdateCapabilities() => Array.Empty<string>();
#endif
	}
}
