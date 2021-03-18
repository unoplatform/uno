using System.Threading.Tasks;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Host
{
	internal interface IRemoteControlServer
	{
		Task SendFrame(IMessage fileReload);
	}
}
