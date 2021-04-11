//-:cnd:noEmit
using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace UnoQuickStart.Skia.Tizen
{
	class Program
{
	static void Main(string[] args)
	{
		var host = new TizenHost(() => new UnoQuickStart.App(), args);
		host.Run();
	}
}
}
