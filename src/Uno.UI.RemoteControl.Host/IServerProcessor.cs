using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Host
{
	internal interface IServerProcessor
	{
		string Scope { get; }

		Task ProcessFrame(Frame frame);
	}
}
