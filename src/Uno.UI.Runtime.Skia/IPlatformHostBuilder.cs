namespace Uno.UI.Runtime.Skia
{
    internal interface IPlatformHostBuilder
	{
		bool IsSupported { get; }

		SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder);
	}
}
