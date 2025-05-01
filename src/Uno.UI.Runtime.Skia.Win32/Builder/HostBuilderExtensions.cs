namespace Uno.UI.Hosting;

public static class HostBuilderExtensions
{
	public static IUnoPlatformHostBuilder UseWin32(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new Win32HostBuilder());
		return builder;
	}
}
