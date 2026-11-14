using System.Linq;
using Uno.UI.Hosting;

namespace AlcTestApp;

internal class Program
{
	internal const string HeadlessHostArgument = "--host=headless";

	// The host that runs the secondary app, so a test can shut it down.
	internal static IDisposable? Host { get; private set; }

	[STAThread]
	public static void Main(string[] args)
	{
		App.LaunchArguments = args ?? Array.Empty<string>();

		var builder = UnoPlatformHostBuilder.Create()
			.App(() => new App());

		if (App.LaunchArguments.Any(arg => string.Equals(arg, HeadlessHostArgument, StringComparison.OrdinalIgnoreCase)))
		{
			// Headless only: when the process already runs a primary host, this launches a secondary app on its dispatcher.
			builder.UseHeadless();
		}
		else
		{
			builder
				.UseX11()
				.UseMacOS()
				.UseWin32();
		}

		var host = builder.Build();
		Host = host as IDisposable;

		host.Run();
	}
}
