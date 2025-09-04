using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	class ConfigureServer : IMessage
	{
		public const string Name = nameof(ConfigureServer);
		private Dictionary<string, string>? _msbuildPropertiesCache;

		public ConfigureServer(string projectPath, string[] metadataUpdateCapabilities, bool enableMetadataUpdates, string[] msbuildPropertiesRaw, bool enableHotReloadThruDebugger)
		{
			ProjectPath = projectPath;
			MetadataUpdateCapabilities = metadataUpdateCapabilities;
			MSBuildPropertiesRaw = msbuildPropertiesRaw;
			EnableMetadataUpdates = enableMetadataUpdates;
			EnableHotReloadThruDebugger = enableHotReloadThruDebugger;
		}

		public string ProjectPath { get; set; }

		public string[] MSBuildPropertiesRaw { get; set; }

		public string[] MetadataUpdateCapabilities { get; set; }

		public bool EnableMetadataUpdates { get; set; }

		public bool EnableHotReloadThruDebugger { get; set; }

		public string Scope => WellKnownScopes.HotReload;

		string IMessage.Name => Name;

		public Dictionary<string, string> MSBuildProperties
			=> _msbuildPropertiesCache ??= BuildMSBuildProperties(MSBuildPropertiesRaw);

		public static Dictionary<string, string> BuildMSBuildProperties(string[] rawMSBuildProperties)
		{
			var msbuildPropertiesCache = new Dictionary<string, string>();

			foreach (var property in rawMSBuildProperties)
			{
				var firstEqual = property.IndexOf('=');
				var split = new[] { property.Substring(0, firstEqual), property.Substring(firstEqual + 1) };
				msbuildPropertiesCache.Add(split[0], Encoding.UTF8.GetString(Convert.FromBase64String(split[1])));
			}

			return msbuildPropertiesCache;
		}
	}
}
