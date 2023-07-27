using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	class ConfigureServer : IMessage
	{
		public const string Name = nameof(ConfigureServer);
		private Dictionary<string, string>? _msbuildPropertiesCache;

		public ConfigureServer(string projectPath, string[] xamlPaths, string[] metadataUpdateCapabilities, string[] msbuildPropertiesRaw)
		{
			ProjectPath = projectPath;
			XamlPaths = xamlPaths;
			MetadataUpdateCapabilities = metadataUpdateCapabilities;
			MSBuildPropertiesRaw = msbuildPropertiesRaw;
		}

		public string ProjectPath { get; set; }

		public string[] MSBuildPropertiesRaw { get; set; }

		public string[] XamlPaths { get; set; }

		public string[] MetadataUpdateCapabilities { get; set; }

		public string Scope => HotReloadConstants.HotReload;

		string IMessage.Name => Name;

		public Dictionary<string, string> MSBuildProperties
		{
			get
			{
				if (_msbuildPropertiesCache is null)
				{
					_msbuildPropertiesCache = new Dictionary<string, string>();

					foreach (var property in MSBuildPropertiesRaw)
					{
						var firstEqual = property.IndexOf('=');
						var split = new[] { property.Substring(0, firstEqual), property.Substring(firstEqual + 1) };
						_msbuildPropertiesCache.Add(split[0], Encoding.UTF8.GetString(Convert.FromBase64String(split[1])));
					}
				}

				return _msbuildPropertiesCache;
			}
		}
	}
}
