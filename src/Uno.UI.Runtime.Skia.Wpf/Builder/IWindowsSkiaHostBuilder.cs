#nullable enable

using System;

namespace Uno.UI.Hosting;

public interface IWindowsSkiaHostBuilder
{
	internal Func<System.Windows.Application>? WpfApplication { get; set; }
}
