using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia;

public interface ISkiaHostBuilder
{
	internal Func<Application>? AppBuilder { get; set; }

	internal Action? AfterInit { get; set; }

	internal void AddHostBuilder(Func<IPlatformHostBuilder> hostBuilder);

	public SkiaHost Build();
}
