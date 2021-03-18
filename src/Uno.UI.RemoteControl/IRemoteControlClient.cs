using System;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl
{
	public interface IRemoteControlClient
	{
		Type AppType { get; }

		Task SendMessage(IMessage configureServer);
	}
}
