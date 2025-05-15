namespace Uno.UI.Hosting;

public static class HostBuilderExtensions
{
	public static IUnoPlatformHostBuilder UseWin32(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new Win32HostBuilder());
		return builder;
	}

	public static IUnoPlatformHostBuilder UseWin32(this IUnoPlatformHostBuilder builder, bool preloadMediaPlayer)
	{
		builder.AddHostBuilder(() => new Win32HostBuilder().PreloadMediaPlayer(preloadMediaPlayer));
		return builder;
	}
}
