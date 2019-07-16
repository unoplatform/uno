using System.Threading.Tasks;
using Uno.UI.HotReload.HotReload.Messages;

namespace Uno.HotReload.Host
{
	internal interface IServerProcessor
	{
		string Scope { get; }

		Task ProcessFrame(Frame frame);
	}
}
