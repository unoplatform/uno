using System;
using System.Threading.Tasks;
using Uno.UI.HotReload.HotReload.Messages;

namespace Uno.UI.HotReload
{
	public interface IRemoteControlClient
	{
		Type AppType { get; }

		Task SendMessage(IMessage configureServer);
	}
}
