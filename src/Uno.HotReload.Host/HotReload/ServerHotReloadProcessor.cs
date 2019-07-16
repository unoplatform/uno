using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.UI.HotReload.HotReload.Messages;

namespace Uno.HotReload.Host.HotReload
{
	class ServerHotReloadProcessor : IServerProcessor
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
			}
		}

		private async Task ProcessConfigureServer(ConfigureServer configureServer)
		{
			Console.WriteLine($"Base project path: {configureServer.ProjectPath}");
			Console.WriteLine($"Xaml Search Paths: {string.Join(", ", configureServer.XamlPaths)}");

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
				Console.WriteLine($"File {e.FullPath} changed");

				await _remoteControlServer.SendFrame(
					new FileReload()
					{
						Content = File.ReadAllText(e.FullPath),
						FilePath = e.FullPath
					});
			});
	}
}
