#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.Hosting;

internal interface IWpfApplicationHost : ISkiaApplicationHost
{
	bool IgnorePixelScaling { get; }
}
