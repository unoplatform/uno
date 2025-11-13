using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Host
{
	public interface IRemoteControlServer
	{
		string GetServerConfiguration(string key);

		Task SendFrame(IMessage message);

		Task SendMessageToIDEAsync(IdeMessage message);
	}
}
