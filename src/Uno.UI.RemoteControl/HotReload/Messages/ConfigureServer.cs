using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	class ConfigureServer : IMessage
	{
		public const string Name = nameof(ConfigureServer);

		public ConfigureServer(string projectPath, string[] xamlPaths, string[] metadataUpdateCapabilities)
		{
			ProjectPath = projectPath;
			XamlPaths = xamlPaths;
			MetadataUpdateCapabilities = metadataUpdateCapabilities;
		}

		public string ProjectPath { get; set; }

		public string[] XamlPaths { get; set; }

		public string[] MetadataUpdateCapabilities { get; set; }

		public string Scope => HotReloadConstants.ScopeName;

		string IMessage.Name => Name;
	}
}
