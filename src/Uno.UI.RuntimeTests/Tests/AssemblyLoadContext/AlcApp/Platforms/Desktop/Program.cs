using Uno.UI.Hosting;

namespace AlcTestApp;

internal class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		var host = UnoPlatformHostBuilder.Create()
			.App(() => new App())
			.UseX11()
			.UseMacOS()
			.UseWin32()
			.Build();

		host.Run();
	}
}
