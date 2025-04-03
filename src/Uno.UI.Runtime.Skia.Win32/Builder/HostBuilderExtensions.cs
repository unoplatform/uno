namespace Uno.UI.Runtime.Skia;

public static class HostBuilderExtensions
{
	public static ISkiaHostBuilder UseWin32(this ISkiaHostBuilder builder)
	{
		builder.AddHostBuilder(() => new Win32HostBuilder());
		return builder;
	}
}
