using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace $ext_safeprojectname$.Skia.Tizen
{
	class Program
{
	static void Main(string[] args)
	{
		var host = new TizenHost(() => new $ext_safeprojectname$.App(), args);
		host.Run();
	}
}
}
