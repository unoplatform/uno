using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl.Messages
{
	public class ProcessorsDiscovery : IMessage
	{
		public const string Name = nameof(ProcessorsDiscovery);

		public ProcessorsDiscovery(string basePath)
		{
			BasePath = basePath;
		}

		public string Scope => "RemoteControlServer";

		string IMessage.Name => Name;

		public string BasePath { get; }
	}
}
