#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Win32.Hosting;

internal interface IWpfApplicationHost : ISkiaApplicationHost
{
	bool IgnorePixelScaling { get; }
}
