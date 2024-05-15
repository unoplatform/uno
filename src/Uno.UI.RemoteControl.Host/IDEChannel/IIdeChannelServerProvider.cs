using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

internal interface IIdeChannelServerProvider
{
	Task<IdeChannelServer?> GetIdeChannelServerAsync();
}
