namespace Uno.UI.Runtime.Skia;

internal interface IPlatformHostBuilder
{
	bool IsSupported { get; }

	SkiaHost Create(Func<Windows.UI.Xaml.Application> appBuilder);
}
