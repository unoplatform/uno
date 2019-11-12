using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	internal class FileReload : IMessage
	{
		public const string Name = nameof(FileReload);

		[JsonProperty]
		public string FilePath { get; set; }

		[JsonProperty]
		public string Content { get; set; }

		[JsonIgnore]
		public string Scope => HotReloadConstants.ScopeName;

		[JsonIgnore]
		string IMessage.Name => Name;
	}
}
