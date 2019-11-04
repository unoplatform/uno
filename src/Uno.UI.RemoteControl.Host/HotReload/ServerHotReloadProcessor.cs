using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Host.HotReload
{
	class ServerHotReloadProcessor : IServerProcessor, IDisposable
	{
		private FileSystemWatcher[] _watchers;
		private IRemoteControlServer _remoteControlServer;

		public ServerHotReloadProcessor(IRemoteControlServer remoteControlServer)
		{
			_remoteControlServer = remoteControlServer;
		}

		public string Scope => "hotreload";

		public async Task ProcessFrame(Frame frame)
		{
			switch (frame.Name)
			{
				case ConfigureServer.Name:
					await ProcessConfigureServer(JsonConvert.DeserializeObject<ConfigureServer>(frame.Content));
					break;
				case XamlLoadError.Name:
					await ProcessXamlLoadError(JsonConvert.DeserializeObject<XamlLoadError>(frame.Content));
					break;
			}
		}

		private async Task ProcessXamlLoadError(XamlLoadError xamlLoadError)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError(
					$"The XAML file failed to load [{xamlLoadError.FilePath}]\n" +
					$"{xamlLoadError.ExceptionType}: {xamlLoadError.Message}\n" +
					$"{xamlLoadError.StackTrace}");
			}
		}

		private async Task ProcessConfigureServer(ConfigureServer configureServer)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Base project path: {configureServer.ProjectPath}");
				this.Log().LogDebug($"Xaml Search Paths: {string.Join(", ", configureServer.XamlPaths)}");
			}

			_watchers = configureServer.XamlPaths
				.Select(p => new FileSystemWatcher
				{
					Path = p,
					Filter = "*.xaml",
					NotifyFilter = NotifyFilters.LastWrite |
						NotifyFilters.Attributes |
						NotifyFilters.Size |
						NotifyFilters.CreationTime |
						NotifyFilters.FileName,
					EnableRaisingEvents = true,
					IncludeSubdirectories = false
				})
				.ToArray();

			foreach (var watcher in _watchers)
			{
				watcher.Changed += OnXamlFileChanged;
			}
		}

		private void OnXamlFileChanged(object sender, FileSystemEventArgs e)
			=> Task.Run(async () =>
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"File {e.FullPath} changed");
				}

				await _remoteControlServer.SendFrame(
					new FileReload()
					{
						Content = File.ReadAllText(e.FullPath),
						FilePath = e.FullPath
					});
			});

		public void Dispose()
		{
			if (_watchers != null)
			{
				foreach (var watcher in _watchers)
				{
					watcher.Dispose();
				}
			}
		}
	}
}
