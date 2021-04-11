//-:cnd:noEmit
using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace UnoWinUIQuickStart.Skia.Tizen
{
	class Program
{
	static void Main(string[] args)
	{
		var host = new TizenHost(() => new UnoWinUIQuickStart.App(), args);
		host.Run();
	}
}
}
