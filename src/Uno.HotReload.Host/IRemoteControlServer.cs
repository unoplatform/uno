using System.Threading.Tasks;
using Uno.UI.HotReload;
using Uno.UI.HotReload.HotReload.Messages;

namespace Uno.HotReload.Host
{
	internal interface IRemoteControlServer
	{
		Task SendFrame(IMessage fileReload);
	}
}
