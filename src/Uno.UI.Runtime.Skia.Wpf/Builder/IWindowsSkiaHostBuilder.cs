#nullable enable

using System;

namespace Uno.UI.Runtime.Skia;

public interface IWindowsSkiaHostBuilder
{
	internal Func<System.Windows.Application>? WpfApplication { get; set; }
}
