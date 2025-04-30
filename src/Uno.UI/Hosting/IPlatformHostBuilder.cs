#nullable enable

using System;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Hosting;

internal interface IPlatformHostBuilder
{
	bool IsSupported { get; }

	UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder);
}
